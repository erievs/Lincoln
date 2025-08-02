using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

#pragma warning disable CS8600 
#pragma warning disable CS8604
#pragma warning disable CS0168 
#pragma warning disable CS8602 

namespace Lincon
{
    public static class PlaylistInfoFeed
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string id, string? accessToken)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/browse?key=AIzaSyDCU8hByM-4DrUqRUYnGn-3llEO78bcxq8&params=cAc%253D";

                var payload = new Dictionary<string, object>
                {
                    ["context"] = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            deviceMake = "Samsung",
                            deviceModel = "SmartTV",
                            userAgent = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.0) AppleWebKit/538.1 (KHTML, like Gecko) Version/5.0 NativeTVAds Safari/538.1,gzip(gfe)",
                            clientName = "TVHTML5",
                            clientVersion = "7.20250209.19.00",
                            osName = "Tizen",
                            osVersion = "5.0",
                            platform = "TV",
                            clientFormFactor = "UNKNOWN_FORM_FACTOR",
                            screenPixelDensity = 1
                        }
                    },
                    ["browseId"] = id,
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(payload);

                var res = await client.SendAsync(request);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                    throw new Exception($"Browse request failed with status {res.StatusCode}");

                return json;
            }
            static string ConvertRelativeDate(string relativeDate)
            {
                DateTime now = DateTime.UtcNow;

                var match = Regex.Match(relativeDate, @"(\d+)\s*(day|days|week|weeks|month|months|year|years)\s*ago", RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    return "2013-05-10T00:00:01.000Z";
                }

                int value = int.Parse(match.Groups[1].Value);
                string unit = match.Groups[2].Value.ToLower();

                switch (unit)
                {
                    case "day":
                    case "days":
                        now = now.AddDays(-value);
                        break;
                    case "week":
                    case "weeks":
                        now = now.AddDays(-value * 7);
                        break;
                    case "month":
                    case "months":
                        now = now.AddMonths(-value);
                        break;
                    case "year":
                    case "years":
                        now = now.AddYears(-value);
                        break;
                    default:
                        return "Invalid unit";
                }

                return now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }

            // youtube hates us and doesn't do normal dates anymore /;
            int ConvertViewsToInt(string viewsText)
            {
                if (string.IsNullOrEmpty(viewsText)) return 0;

                viewsText = viewsText.ToLower().Trim();

                viewsText = viewsText.Replace("views", "").Trim();

                double result;

                if (viewsText.Contains("thousand"))
                {
                    var views = viewsText.Replace("thousand", "").Trim();
                    if (double.TryParse(views, out result))
                    {
                        return (int)(result * 1000);
                    }
                }

                else if (viewsText.Contains("k"))
                {
                    var views = viewsText.Replace("k", "").Trim();
                    if (double.TryParse(views, out result))
                    {
                        return (int)(result * 1000);
                    }
                }

                else if (viewsText.Contains("million") || viewsText.Contains("m"))
                {
                    var views = viewsText.Replace("million", "").Replace("m", "").Trim();
                    if (double.TryParse(views, out result))
                    {
                        return (int)(result * 1000000);
                    }
                }

                else if (viewsText.Contains("billion") || viewsText.Contains("b"))
                {
                    var views = viewsText.Replace("billion", "").Replace("b", "").Trim();
                    if (double.TryParse(views, out result))
                    {
                        return (int)(result * 1000000000);
                    }
                }

                return 301; // easter egg for old youtube
            }
          
            Tuple<string, int, string, string, int> ExtractData(string json, string access_token, HttpRequest request)
            {

                using var doc = JsonDocument.Parse(json);
                var combined = new List<string>();
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                int count = 0;

                // the FEwhat_to_watch feed is layed out like this
                // multiple shelfs with different topics
                // first being recommendations
                // all other shelfs tend to change over time and based on what you watch 
                // (though recently uploaded tends to be second more often than not)

                // the /browse responses tend to be layed out the same way 
                // however sometimes the entry point to videos change

                // innertube is formated in a way to help render the app
                // so the names are pretty straight foward, and by looking at the site
                // you get a clear picture of what is what

                string playlist_title = "";
                string author = "";
                int videoCount = 0;
                JsonElement rightColumnContents;

                try
                {

                    var leftColumn = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("tvBrowseRenderer")
                        .GetProperty("content")
                        .GetProperty("tvSurfaceContentRenderer")
                        .GetProperty("content")
                        .GetProperty("twoColumnRenderer")
                        .GetProperty("leftColumn")
                        .GetProperty("entityMetadataRenderer");

                    if (leftColumn.TryGetProperty("title", out var titleObj) &&
                        titleObj.TryGetProperty("simpleText", out var titleText))
                    {
                        playlist_title = titleText.GetString() ?? "";
                    }

                    if (leftColumn.TryGetProperty("bylines", out var bylines) &&
                        bylines.ValueKind == JsonValueKind.Array &&
                        bylines.GetArrayLength() > 1)
                    {
                        var lineRenderer = bylines[0].GetProperty("lineRenderer")
                            .GetProperty("items");

                        foreach (var item in lineRenderer.EnumerateArray())
                        {
                            if (item.TryGetProperty("lineItemRenderer", out var lineItem) &&
                                lineItem.TryGetProperty("text", out var textObj))
                            {
                                if (textObj.TryGetProperty("simpleText", out var simpleTextProp))
                                {
                                    var text = simpleTextProp.GetString();
                                    if (!string.IsNullOrEmpty(text) && text != "•")
                                    {
                                        author = text;
                                    }
                                }
                            }
                        }
                    }

                    if (leftColumn.TryGetProperty("bylines", out var bylines2) &&
                        bylines2.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var byline in bylines2.EnumerateArray())
                        {
                            if (byline.TryGetProperty("lineRenderer", out var lr) &&
                                lr.TryGetProperty("items", out var items) &&
                                items.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in items.EnumerateArray())
                                {
                                    if (item.TryGetProperty("lineItemRenderer", out var lir) &&
                                        lir.TryGetProperty("text", out var text))
                                    {
                                        if (text.TryGetProperty("runs", out var runs) &&
                                            runs.ValueKind == JsonValueKind.Array &&
                                            runs.GetArrayLength() > 0)
                                        {
                                            string countText = runs[0].GetProperty("text").GetString() ?? "0";
                                            countText = countText.Replace(",", "");
                                            int.TryParse(countText, out videoCount);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    return Tuple.Create("", 0, "", "", 0);
                }

                JsonElement sectionsRoot;
                try
                {
                    sectionsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("tvBrowseRenderer")
                        .GetProperty("content")
                        .GetProperty("tvSurfaceContentRenderer")
                        .GetProperty("content")
                        .GetProperty("twoColumnRenderer")
                        .GetProperty("rightColumn")
                        .GetProperty("playlistVideoListRenderer")
                        .GetProperty("contents");
                }
                catch
                {
                    return Tuple.Create("", 0, "", "", 0);
                }


                // weclome to hell again 
                foreach (var item in sectionsRoot.EnumerateArray())
                {
                    if (item.TryGetProperty("tileRenderer", out var tileRenderer))
                    {

                        var id = "";
                        if (tileRenderer.TryGetProperty("onSelectCommand", out var onSelectCommand))
                        {
                            if (onSelectCommand.TryGetProperty("watchEndpoint", out var watchEndpoint))
                            {
                                id = watchEndpoint.TryGetProperty("videoId", out var videoIdProp)
                                    ? videoIdProp.GetString()
                                    : "Unknown";
                            }
                        }

                        string title = tileRenderer.GetProperty("metadata")
                            .GetProperty("tileMetadataRenderer")
                            .GetProperty("title")
                            .GetProperty("simpleText")
                            .GetString() ?? "No Title";


                        // we have this in a try and catch block
                        // to avoid annoying TryGetProperty expection errors
                        // this isn't too important and it seems hit or miss if a video has the text
                        // and it isn't useful seeing the error log whatsoever lol
                        // just default to 301 (easter egg) in the catch block
                        string views_text = "";
                        int view_count = 301;
                        try
                        {
                            if (tileRenderer.TryGetProperty("metadata", out var metadata) && metadata.TryGetProperty("tileMetadataRenderer", out var tileMetadataRenderer) &&
                                tileMetadataRenderer.TryGetProperty("lines", out var lines) &&
                                lines.GetArrayLength() > 1)
                            {
                                var lineRenderer = lines[1].GetProperty("lineRenderer");
                                var items_view = lineRenderer.GetProperty("items");

                                if (items_view.GetArrayLength() > 0)
                                {
                                    var lineItemRenderer = items_view[0].GetProperty("lineItemRenderer");
                                    var text = lineItemRenderer.GetProperty("text");

                                    if (text.TryGetProperty("accessibility", out var accessibility))
                                    {
                                        var accessibilityData = accessibility.GetProperty("accessibilityData");

                                        if (accessibilityData.TryGetProperty("label", out var label))
                                        {
                                            views_text = label.GetString() ?? "301";
                                            view_count = ConvertViewsToInt(views_text);
                                        }
                                    }
                                }
                            }

                        }
                        catch (Exception ex) {/* this stops it from logging, nothing to do here besides that!*/}


                        string duration_text = "0:00";
                        if (tileRenderer.TryGetProperty("header", out var header))
                        {
                            if (header.TryGetProperty("tileHeaderRenderer", out var tileHeaderRenderer))
                            {
                                if (tileHeaderRenderer.TryGetProperty("thumbnailOverlays", out var thumbnailOverlays) && thumbnailOverlays.GetArrayLength() > 0)
                                {
                                    var thumbnailOverlay = thumbnailOverlays[0];
                                    if (thumbnailOverlay.TryGetProperty("thumbnailOverlayTimeStatusRenderer", out var timeStatusRenderer))
                                    {
                                        if (timeStatusRenderer.TryGetProperty("text", out var text))
                                        {
                                            if (text.TryGetProperty("simpleText", out var simpleText))
                                            {
                                                duration_text = simpleText.GetString() ?? "0:00";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        int total_seconds = 0;
                        if (!string.IsNullOrEmpty(duration_text))
                        {
                            var durationParts = duration_text.Split(':');
                            if (durationParts.Length == 2)
                            {
                                if (int.TryParse(durationParts[0], out int minutes) && int.TryParse(durationParts[1], out int seconds))
                                {
                                    total_seconds = (minutes * 60) + seconds;
                                }
                                else
                                {
                                    total_seconds = 0;
                                }
                            }
                            else
                            {
                                total_seconds = 0;
                            }
                        }

                        string time_ago = "13 Years Ago";
                        string published = "";
                        if (tileRenderer.TryGetProperty("metadata", out var metadataDate))
                        {

                            if (metadataDate.TryGetProperty("tileMetadataRenderer", out var tileMetadataRenderer))
                            {

                                if (tileMetadataRenderer.TryGetProperty("lines", out var lines) && lines.GetArrayLength() > 1)
                                {

                                    var lineRenderer = lines[1].GetProperty("lineRenderer");

                                    if (lineRenderer.TryGetProperty("items", out var itemsDate) && itemsDate.GetArrayLength() > 2)
                                    {

                                        var lineItemRenderer = itemsDate[2].GetProperty("lineItemRenderer");

                                        if (lineItemRenderer.TryGetProperty("text", out var text))
                                        {

                                            if (text.TryGetProperty("simpleText", out var simpleText))
                                            {
                                                time_ago = simpleText.GetString() ?? "1 Day Ago";
                                                published = ConvertRelativeDate(time_ago);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            published = ConvertRelativeDate(time_ago);
                        }


                        string channel_id = "default";
                        string uploader = "default";
                        try
                        {   // browseids are very nested away
                            if (tileRenderer.TryGetProperty("onLongPressCommand", out var onLongPressCommand))
                            {
                                if (onLongPressCommand.TryGetProperty("showMenuCommand", out var showMenuCommand))
                                {
                                    if (showMenuCommand.TryGetProperty("menu", out var menu))
                                    {
                                        if (menu.TryGetProperty("menuRenderer", out var menuRenderer))
                                        {
                                            foreach (var menuItem in menuRenderer.GetProperty("items").EnumerateArray())
                                            {
                                                if (menuItem.TryGetProperty("menuNavigationItemRenderer", out var menuNavigationItemRenderer))
                                                {
                                                    // this can varry so we have to check
                                                    var text = menuNavigationItemRenderer
                                                        .GetProperty("text")
                                                        .GetProperty("runs")[0]
                                                        .GetProperty("text")
                                                        .GetString();

                                                    if (text == "Go to channel")
                                                    {
                                                        channel_id = menuNavigationItemRenderer
                                                        .GetProperty("navigationEndpoint")
                                                        .GetProperty("browseEndpoint")
                                                        .GetProperty("browseId")
                                                        .GetString();

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (onLongPressCommand.TryGetProperty("showMenuCommand", out var showMenuCommandUploader)) // sadly I had issue reusing them from above ohwell
                            {
                                if (showMenuCommandUploader.TryGetProperty("subtitle", out var subtitle))
                                {
                                    uploader = subtitle.GetProperty("simpleText").GetString();

                                    int atIndex = uploader.IndexOf('@');

                                    if (atIndex >= 0)
                                    {
                                        uploader = uploader.Substring(atIndex + 1).Trim();
                                    }

                                }
                            }
                        }
                        catch (Exception ex) { continue; /* this should skip videos that dont have a browseid (I think) */ }


                        var description = ""; // we are ignoring this for now

                        var rating = 5;
                        var likes = 0;
                        var dislikes = 0;

                        
                        var item_xml = $"""
                        <entry>
                            <id>tag:youtube.com,2008:video:{id}</id>
                            <published>{published}</published>
                            <updated>{published}</updated>
                            <category scheme='https://schemas.google.com/g/2005#kind' term='https://gdata.youtube.com/schemas/1970#video'/>
                                <category scheme='https://gdata.youtube.com/schemas/1970/categories.cat' term='Howto' label='Howto &amp; Style'/>
                            <title>{SecurityElement.Escape(title)}</title>
                            <content type='application/x-shockwave-flash' src='https://www.youtube.com/v/{id}?version=3&amp;f=playlists&amp;app=youtube_gdata'/>
                            <link rel='alternate' type='text/html' href='https://www.youtube.com/watch?v={id}&amp;feature=youtube_gdata'/>
                                             <link rel="http://gdata.youtube.com/schemas/2007#video.related" href="https://gdata.youtube.com/feeds/api/videos/{id}/related"/>
                            <link rel='http://gdata.youtube.com/schemas/2007#mobile' type='text/html' href='https://m.youtube.com/details?v={id}'/>
                            <link rel='http://gdata.youtube.com/schemas/2007#uploader' type='application/atom+xml' href='{base_url}/feeds/api/users/{channel_id}?v=2'/>
                            <link rel='related' type='application/atom+xml' href='{base_url}/feeds/api/videos/{id}?v=2'/>
                            <link rel='self' type='application/atom+xml' href='{base_url}/feeds/api/playlists/8E2186857EE27746/PLyl9mKRbpNIpJC5B8qpcgKX8v8NI62Jho?v=2'/>
                            <author>
                                <name>{SecurityElement.Escape(uploader)}</name>
                                <uri>{base_url}/feeds/api/users/{channel_id}</uri>
                                <yt:userId>{channel_id}</yt:userId>
                            </author>
                            <yt:accessControl action='comment' permission='allowed'/>
                            <yt:accessControl action='commentVote' permission='allowed'/>
                            <yt:accessControl action='videoRespond' permission='moderated'/>
                            <yt:accessControl action='rate' permission='allowed'/>
                            <yt:accessControl action='embed' permission='allowed'/>
                            <yt:accessControl action='list' permission='allowed'/>
                            <yt:accessControl action='autoPlay' permission='allowed'/>
                            <yt:accessControl action='syndicate' permission='allowed'/>
                            <gd:comments>
                                <gd:feedLink rel='https://gdata.youtube.com/schemas/2007#comments' href='{base_url}/api/videos/{id}/comments' countHint='5'/>
                            </gd:comments>
                            <yt:location>Cleveland ,US</yt:location>
                            <media:group>
                                <media:category label='Howto &amp; Style' scheme='https://gdata.youtube.com/schemas/1970/categories.cat'>Howto</media:category>
                                <media:content url='https://www.youtube.com/v/{id}?version=3&amp;f=playlists&amp;app=youtube_gdata' type='application/x-shockwave-flash' medium='video' isDefault='true' expression='full' duration='{total_seconds}' yt:format='5'/>
                                <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{total_seconds}' yt:format='1'/>
                                <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{total_seconds}' yt:format='6'/>
                                <media:credit role='uploader' scheme='urn:youtube' yt:display='{SecurityElement.Escape(uploader)}' yt:type='partner'>{channel_id}</media:credit>
                                <media:description type='plain'>{SecurityElement.Escape(description)}</media:description>
                                <media:keywords/>
                                <media:license type='text/html' href='https://www.youtube.com/t/terms'>youtube</media:license>
                                <media:player url='https://www.youtube.com/watch?v={id}&amp;feature=youtube_gdata_player'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='default'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/mqdefault.jpg' height='180' width='320' yt:name='mqdefault'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/hqdefault.jpg' height='360' width='480' yt:name='hqdefault'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='start'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='middle'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='end'/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/mp4" medium="video" isDefault="true" expression="full" duration="{total_seconds}" yt:format="3"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/3gpp" medium="video" expression="full" duration="{total_seconds}" yt:format="2"/>
                                <media:content url="{base_url}/getvideo/{id}?muxed=true" type="video/mp4" medium="video" expression="full" duration="{total_seconds}" yt:format="5"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/mp4" medium="video" expression="full" duration="{total_seconds}" yt:format="8"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/3gpp" medium="video" expression="full" duration="{total_seconds}" yt:format="9"/>
                                <media:title type='plain'>{SecurityElement.Escape(title)}</media:title>
                                <yt:duration seconds='{total_seconds}'/>
                                <yt:uploaded>{published}</yt:uploaded>
                                <yt:uploaderId>{channel_id}</yt:uploaderId>
                                <yt:userId>{channel_id}</yt:userId>
                                <yt:videoid>{id}</yt:videoid>
                            </media:group>
                                <gd:rating average='{rating}' max='0' min='0' numRaters='0' rel='https://schemas.google.com/g/2005#overall'/>
                                <yt:recorded>1970-08-22</yt:recorded>
                                <yt:statistics favoriteCount='0' viewCount="{view_count}"/>
                                <yt:rating numDislikes='{dislikes}' numLikes='{likes}'/>
                                <yt:position>1</yt:position>
                        </entry>
                        """;


                        combined.Add(item_xml);
                        count++;
                    }
                }

                return Tuple.Create(string.Join("\n", combined), count, playlist_title, author, videoCount);
            }

            app.MapGet("/feeds/api/playlists/{playlist_id}", async (string playlist_id, HttpRequest request) =>
            {
                try
                {

                    string? device_id = null;

                    device_id = HandleLogin.ExtractDeviceIDFromRequest(request);

                    if (string.IsNullOrEmpty(device_id))
                        return Results.Problem("Invalid device id header", statusCode: 403);

                    if (string.IsNullOrEmpty(playlist_id))
                        return Results.Problem("Woops you need video id lol", statusCode: 400);

                    var access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);

                    var json = await UseInnerTube(playlist_id, access_token);

                    var data = ExtractData(json, access_token, request);

                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                                        
                    var template = @$"<?xml version='1.0' encoding='UTF-8'?>
                    <feed
                        xmlns='http://www.w3.org/2005/Atom'
                        xmlns:media='http://search.yahoo.com/mrss/'
                        xmlns:openSearch='http://a9.com/-/spec/opensearchrss/1.0/'
                        xmlns:gd='http://schemas.google.com/g/2005'
                        xmlns:yt='http://gdata.youtube.com/schemas/2007'>
                    <id>{base_url}/feeds/api/playlists/{playlist_id}</id>
                    <yt:playlistId>{playlist_id}</yt:playlistId>
                    <updated>2012-08-23T12:33:58.000Z</updated>
                    <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#playlist'/>
                    <title>{data.Item4}</title>
                    <logo>https://www.gstatic.com/youtube/img/logo.png</logo>
                    <link rel='alternate' type='text/html' href='{base_url}/view_play_list?p={playlist_id}'/>
                    <gd:feedLink rel='http://gdata.youtube.com/schemas/2007#playlist' href='{base_url}/feeds/api/playlists/{playlist_id}' countHint='20'/>
                    <link rel='http://schemas.google.com/g/2005#feed' type='application/atom+xml' href='{base_url}/feeds/api/playlists/{playlist_id}'/>
                    <link rel='http://schemas.google.com/g/2005#batch' type='application/atom+xml' href='{base_url}/feeds/api/playlists/{playlist_id}/batch'/>
                    <link rel='self' type='application/atom+xml' href='{base_url}/feeds/api/playlists/{playlist_id}?start-index=1&amp;max-results=25'/>
                    <author>
                        <name>{data.Item3}</name>
                        <uri>{base_url}/feeds/api/users/</uri>
                        <yt:userId></yt:userId>
                    </author>
                    <generator version='2.1' uri='{base_url}'>YouTube data API</generator>
                    <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                    <openSearch:startIndex>1</openSearch:startIndex>
                    <openSearch:itemsPerPage>1</openSearch:itemsPerPage>
                    <media:group>
                        <media:content url='http://www.youtube.com/ep.swf?id={playlist_id}' type='application/x-shockwave-flash' yt:format='5'/>
                        <media:description type='plain'>yap</media:description>
                        <media:title type='plain'>yap</media:title>
                    </media:group>
                    {data.Item1 ?? ""}
                    </feed>";

                    
                    return Results.Content(template, "application/atom+xml");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n" + ex);

                    return Results.StatusCode(500);
                }
            });

      
        }

    }
}