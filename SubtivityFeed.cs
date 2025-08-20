using System.Net.Http.Headers;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;

#pragma warning disable CS8600 
#pragma warning disable CS8604
#pragma warning disable CS0168 
#pragma warning disable CS8602 

namespace Lincon
{
    public static class SubtivityFeed
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string? accessToken)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/browse?key=AIzaSyDCU8hByM-4DrUqRUYnGn-3llEO78bcxq8";

                var payload = new
                {
                    context = new
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
                    browseId = "FEsubscriptions"
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(payload);

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API request failed with status {response.StatusCode}");

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

            Tuple<string, int> ExtractData(string json, string access_token, HttpRequest request)
            {

                using var doc = JsonDocument.Parse(json);
                var combined = new List<string>();
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                int count = 0;

                // the FEsubscriptions feed is layed out like this
                // one sidebar with your channels
                // next to it videos (normal tileRenders)

                // you can get "all", which is helpful for another feed

                // each channel is a tab (and all is kinda a channel)

                // for this feed 
                // we just select [0] and cycle through tab renders!

                // again innertube is formated in a way to help render the app
                // so the names are pretty straight foward, and by looking at the site
                // you get a clear picture of what is what

                JsonElement tabsRoot;
                try
                {
                    tabsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("tvBrowseRenderer")
                        .GetProperty("content")
                        .GetProperty("tvSecondaryNavRenderer")
                        .GetProperty("sections")[0]
                        .GetProperty("tvSecondaryNavSectionRenderer")
                        .GetProperty("tabs")[0];
                }
                catch
                {
                    return Tuple.Create("", 0);
                }

                JsonElement allTab;
                try
                {
                    allTab = tabsRoot
                        .GetProperty("tabRenderer");
                }
                catch
                {
                    return Tuple.Create("", 0);
                }
                
                // almost the same proccess as recommendations but more nested
                if (allTab.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("tvSurfaceContentRenderer", out var tvSurfaceContentRenderer))
                    {
                        if (tvSurfaceContentRenderer.TryGetProperty("content", out var nestedContent))
                        {
                            if (nestedContent.TryGetProperty("gridRenderer", out var gridRenderer))

                            {
                                if (gridRenderer.TryGetProperty("items", out var items))
                                {
                                    foreach (var item in items.EnumerateArray())
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
                                            catch {/* this stops it from logging, nothing to do here besides that!*/}

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

                                            // this is broken for this atm
                                            // lazy and people were begging
                                            // will be fixed later!
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
                                            catch { continue; /* this should skip videos that dont have a browseid (I think) */ }

                                            string item_xml = @$"
                                            <entry gd:etag='W/&quot;D0EGSH47eCp7ImA9WxRQQEg.&quot;'
                                                xmlns='http://www.w3.org/2005/Atom'
                                                xmlns:gd='http://schemas.google.com/g/2005'
                                                xmlns:yt='http://gdata.youtube.com/schemas/2007'
                                                xmlns:media='http://search.yahoo.com/mrss/'>
                                            <id>tag:youtube.com,2008:user:{uploader}:event:{id}:video:xuJkc9ENdt4</id>
                                            <updated>2009-01-16T09:13:49.000Z</updated>
                                            <category scheme='http://schemas.google.com/g/2005#kind'
                                                        term='http://gdata.youtube.com/schemas/2007#userEvent'/>
                                            <category scheme='http://gdata.youtube.com/schemas/2007/userevents.cat'
                                                        term='video_uploaded'/>
                                            <title>GoogleTechTalks has uploaded a video</title>
                                            <link rel='alternate' type='text/html' href='https://www.youtube.com'/>
                                            <link rel='http://gdata.youtube.com/schemas/2007#video'
                                                    type='application/atom+xml'
                                                    href='https://gdata.youtube.com/feeds/api/videos/_gZK0tW8EhQ?v=2'/>
                                            <link rel='self' type='application/atom+xml'
                                                    href='https://gdata.youtube.com/feeds/api/users/GoogleDevelopers/subtivity/VGF5Wm9uZGF5MzEy%3D%3D?v=2'/>
                                            <author>
                                                <name>GoogleTechTalks</name>
                                                <uri>https://gdata.youtube.com/feeds/api/users/tXKDgv1AVoG88PLl8nGXmw</uri>
                                                <yt:userId>tXKDgv1AVoG88PLl8nGXmw</yt:userId>
                                            </author>
                                            <media:group>
                                                <media:category label='Howto &amp; Style' scheme='https://gdata.youtube.com/schemas/1970/categories.cat'>Howto</media:category>
                                                <media:content url='https://www.youtube.com/v/xuJkc9ENdt4?version=3&amp;f=playlists&amp;app=youtube_gdata' type='application/x-shockwave-flash' medium='video' isDefault='true' expression='full' duration='3951' yt:format='5'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/3gpp' medium='video' expression='full' duration='3951' yt:format='1'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/3gpp' medium='video' expression='full' duration='3951' yt:format='6'/>
                                                <media:credit role='uploader' scheme='urn:youtube' yt:display='Syd &amp; Olivia' yt:type='partner'>UCYLG0W5s5aA6q39oTqf-H4w</media:credit>
                                                <media:description type='plain'></media:description>
                                                <media:keywords/>
                                                <media:license type='text/html' href='https://www.youtube.com/t/terms'>youtube</media:license>
                                                <media:player url='https://www.youtube.com/watch?v=xuJkc9ENdt4&amp;feature=youtube_gdata_player'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='default'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/mqdefault.jpg' height='180' width='320' yt:name='mqdefault'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/hqdefault.jpg' height='360' width='480' yt:name='hqdefault'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='start'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='middle'/>
                                                <media:thumbnail url='http://i.ytimg.com/vi/xuJkc9ENdt4/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='end'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/mp4' medium='video' isDefault='true' expression='full' duration='3951' yt:format='3'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/3gpp' medium='video' expression='full' duration='3951' yt:format='2'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4?muxed=true' type='video/mp4' medium='video' expression='full' duration='3951' yt:format='5'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/mp4' medium='video' expression='full' duration='3951' yt:format='8'/>
                                                <media:content url='http://192.168.1.150/getvideo/xuJkc9ENdt4' type='video/3gpp' medium='video' expression='full' duration='3951' yt:format='9'/>
                                                <media:title type='plain'>Existential Crisis â¤ï¸ | Syd &amp; Olivia Talk Sh*t - S3 Ep44</media:title>
                                                <yt:duration seconds='3951'/>
                                                <yt:uploaded>1970-01-01T00:00:00.000Z</yt:uploaded>
                                                <yt:uploaderId>UCYLG0W5s5aA6q39oTqf-H4w</yt:uploaderId>
                                                <yt:userId>UCYLG0W5s5aA6q39oTqf-H4w</yt:userId>
                                                <yt:videoid>xuJkc9ENdt4</yt:videoid>
                                            </media:group>

                                            <yt:videoid>_gZK0tW8EhQ</yt:videoid>
                                            </entry>";

       

                                            combined.Add(item_xml);
                                            count++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return Tuple.Create(string.Join("\n", combined), count);
            }

            // for now it'll pretty just another section for subbed videos
            // but cool to have
            app.MapGet("/feeds/api/users/default/subtivity", async (HttpRequest request) =>
            {
                try
                {

                    string? device_id = null;

                    device_id = HandleLogin.ExtractDeviceIDFromRequest(request);
 
                    if (string.IsNullOrEmpty(device_id))
                        return Results.Problem("Invalid device id header", statusCode: 403);
    

                    var access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);
                    

                    var json = await UseInnerTube(access_token);

                    var data = ExtractData(json, access_token, request);


                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                    

                    var template = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <feed xmlns=""http://www.w3.org/2005/Atom""
                        xmlns:gd=""http://schemas.google.com/g/2005""
                        xmlns:openSearch=""http://a9.com/-/spec/opensearch/1.1/""
                        xmlns:yt=""http://gdata.youtube.com/schemas/2007""
                        xmlns:media=""http://search.yahoo.com/mrss/"">
                        <id>tag:youtube.com,2008:channels</id>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#channel""/>
                        <title>Channels matching: webauditors</title>
                        <logo>http://www.gstatic.com/youtube/img/logo.png</logo>
                        <link rel=""http://schemas.google.com/g/2006#spellcorrection"" type=""application/atom+xml"" href=""{base_url}/feeds/api/channels?q=web+auditors&amp;start-index=1&amp;max-results=1&amp;oi=spell&amp;spell=1&amp;v=2"" title=""web auditors""/>
                        <link rel=""http://schemas.google.com/g/2005#feed"" type=""application/atom+xml"" href=""{base_url}/feeds/api/channels?v=2""/>
                        <link rel=""http://schemas.google.com/g/2005#batch"" type=""application/atom+xml"" href=""{base_url}/feeds/api/channels/batch?v=2""/>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/channels?q=webauditors&amp;start-index=1&amp;max-results=1&amp;v=2""/>
                        <link rel=""service"" type=""application/atomsvc+xml"" href=""{base_url}/feeds/api/channels?alt=atom-service&amp;v=2""/>
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version=""2.1"" uri=""{base_url}"">YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>1</openSearch:itemsPerPage>
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