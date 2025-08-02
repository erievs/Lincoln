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
    public static class RelatedFeed
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string id, string? accessToken)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/next?key=AIzaSyDCU8hByM-4DrUqRUYnGn-3llEO78bcxq8";
                var rparams = "6gILWVd6bTFQdlE1S3fqAgtBdUFfTmlQNXItUeoCC0NuTlpKcmtEQ19B6gILR2dEMUo3eWc3TDDqAgtqSGhzRWJUTlJJNOoCC3FVX2RGc2hkcGs06gILa0Z5UG1vZGRRTVnqAgt1QUhHS2FucU82c-oCC2pGckdob2RxQzA46gILQUVNZG1uTmJDWkHqAgt6X1V0UmU5RGd2ReoCCzFyekZ5QmRLTHZV6gILb25lZTdMZkRwQ3fqAgtXWGpUc19OTERtY-oCC05hLUNzaVpFcUQ46gILX09GNnZQLVNrR0HqAgszSWZKU0dXSXJDb-oCC3VkdExLeFZaYzVZ6gILR1ZTaFF6cTJfZ2PqAgtEWVdUdzE5XzhyNOoCC1NOMEY1YWdwV2NF6gILVXZrMml3NUtsN3PqAgtJNmRlOGhxYW84c-oCC3ZEc1dVMXFWXzBr6gILbU5FNjIzOVlMZ2fqAgs1bEtPaHcwSW1xY-oCC1BFdy1KLV84V0d36gILbktaZngyS2NfRjTqAgt2T1ZEeVJJUzA5a_oCC1JlY29tbWVuZGVkugMKCKzJw97P2rm2YboDCgjk3-af4uaP8AK6AwoI8JeMyOuk1rkKugMKCL3Zg-X7pL2AGroDCwiOibWmm4KbvIwBugMLCM7M9sLsovenqQG6AwsIxoH1uqjzo66QAboDCwir96jPmsXxgLgBugMLCM-WqLvo0LGtjAG6AwkIkJPsmqezxyG6AwsI8YWO-t6oy_rPAboDCwj13ai6gbmx3tYBugMLCKzIjr7L3ee7ogG6AwoI55ysmr_2tLxZugMKCL_QkrKi1uDXNboDCwjgoMr8z9fe8PwBugMLCKrYoqyGqfLD3AG6AwsIluflqrHl0u25AboDCgiH_NvVs6ioqhm6AwoIvuX_-7X45MINugMKCMGzpcHavMHuSLoDCgi7r6rysNHN_FK6AwoIy8fq1KHe19MjugMLCMn-19S1ysWdvAG6AwsIiNzg-rfbzuiYAboDCwintaLo8NCjqeYBugMKCOyw8f_-xI-mPLoDCwje-POU9viX05wBugMLCNmny5CR-dDyvAE%3D";

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
                    ["videoId"] = id,
                    ["params"] = rparams // you NEED these parms to get vids
                };
                
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

                if (String.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

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

            Tuple<string, int> ExtractData(string json, string access_token, HttpRequest request)
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


                JsonElement sectionsRoot;
                try
                {
                    sectionsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("singleColumnWatchNextResults")
                        .GetProperty("pivot")
                        .GetProperty("sectionListRenderer")
                        .GetProperty("contents");
                }
                catch
                {
                    return Tuple.Create("", 0);
                }

                foreach (var section in sectionsRoot.EnumerateArray())
                {
                    if (section.TryGetProperty("shelfRenderer", out var shelfRenderer) &&
                        shelfRenderer.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("horizontalListRenderer", out var horizontalListRenderer) &&
                        horizontalListRenderer.TryGetProperty("items", out var items))
                    {
                        int countShelf = 0; // there's like 21 of shelfRenderer we don't need that many ()
                        foreach (var item in items.EnumerateArray())
                        {
                            if (countShelf >= 6) break; // less = less vids in suggested (more is the oppsite)

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
                                string channel_id = "default";
                                string uploader = "default";

                                if (tileRenderer.TryGetProperty("metadata", out var metadata_again) &&
                                    metadata_again.TryGetProperty("tileMetadataRenderer", out var tileMetadataRendererAgain))
                                {

                                    if (tileMetadataRendererAgain.TryGetProperty("lines", out var lines) && lines.GetArrayLength() > 0)
                                    {

                                        var firstLine = lines[0];
                                        if (firstLine.TryGetProperty("lineRenderer", out var firstLineRenderer) &&
                                            firstLineRenderer.TryGetProperty("items", out var channelItems) &&
                                            channelItems.GetArrayLength() > 0)
                                        {

                                            var firstItem = channelItems[0];
                                            if (firstItem.TryGetProperty("lineItemRenderer", out var lineItemRenderer) &&
                                                lineItemRenderer.TryGetProperty("text", out var text) &&
                                                text.TryGetProperty("runs", out var runs) &&
                                                runs.GetArrayLength() > 0)
                                            {
                                                var run = runs[0];

                                                if (run.TryGetProperty("text", out var uploaderText))
                                                    uploader = uploaderText.GetString() ?? "default";

                                                if (run.TryGetProperty("navigationEndpoint", out var navigationEndpoint) &&
                                                    navigationEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
                                                    browseEndpoint.TryGetProperty("browseId", out var browseId))
                                                {
                                                    channel_id = browseId.GetString() ?? "default";
                                                }
                                            }
                                        }

                                        if (lines.GetArrayLength() > 1)
                                        {
                                            var secondLine = lines[1];
                                            if (secondLine.TryGetProperty("lineRenderer", out var lineRenderer) &&
                                                lineRenderer.TryGetProperty("items", out var itemsDate) &&
                                                itemsDate.GetArrayLength() > 3) 
                                            {

                                                foreach (var itemDate in itemsDate.EnumerateArray())
                                                {
                                                    if (itemDate.TryGetProperty("lineItemRenderer", out var dateRenderer) &&
                                                        dateRenderer.TryGetProperty("text", out var dateText) &&
                                                        dateText.TryGetProperty("simpleText", out var simpleText))
                                                    {
                                                        string value = simpleText.GetString();

                                                        if (!string.IsNullOrEmpty(value) && !value.Equals("•") && !value.Equals("New"))
                                                        {
                                                            time_ago = value;
                                                            published = ConvertRelativeDate(time_ago);
                                                            break;
                                                        }
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


           
                                var description = ""; // we are ignoring this for now

                                var rating = 5;
                                var likes = 0;
                                var dislikes = 0;

                                
                                string item_xml = @$"
                                <entry>
                                    <id>tag:youtube.com,2008:video:{id}</id>
                                    <published>1970-01-01T00:00:00.000Z</published>
                                    <updated>1970-01-01T00:00:00.000Z</updated>
                                    <category scheme='https://schemas.google.com/g/2005#kind' term='https://gdata.youtube.com/schemas/1970#video'/>
                                    <category scheme='https://gdata.youtube.com/schemas/1970/categories.cat' term='Howto' label='Howto &amp; Style'/>
                                    <title>{SecurityElement.Escape(title)}</title>
                                    <content type='application/x-shockwave-flash' src='https://www.youtube.com/v/9DjIh0sVBR0?version=3&amp;f=playlists&amp;app=youtube_gdata'/>
                                    <link rel='alternate' type='text/html' href='https://www.youtube.com/watch?v=9DjIh0sVBR0&amp;feature=youtube_gdata'/>
                                    <link rel=""http://gdata.youtube.com/schemas/2007#video.related"" href=""https://gdata.youtube.com/feeds/api/videos/{id}/related""/>
                                    <link rel='http://gdata.youtube.com/schemas/2007#mobile' type='text/html' href='https://m.youtube.com/details?v={id}'/>
                                    <link rel='http://gdata.youtube.com/schemas/2007#uploader' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/users/{channel_id}?v=2'/>
                                    <link rel='related' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/videos/9DjIh0sVBR0?v=2'/>
                                    <link rel='self' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/playlists/8E2186857EE27746/PLyl9mKRbpNIpJC5B8qpcgKX8v8NI62Jho?v=2'/>
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
                                        <gd:feedLink rel='https://gdata.youtube.com/schemas/1970#comments' href='{base_url}/api/videos/{id}/comments' countHint='5'/>
                                    </gd:comments>
                                    <yt:location>Cleveland ,US</yt:location>
                                    <media:group>
                                        <media:category label='Howto &amp; Style' scheme='https://gdata.youtube.com/schemas/1970/categories.cat'>Howto</media:category>
                                        <media:content url='https://www.youtube.com/v/9DjIh0sVBR0?version=3&amp;f=playlists&amp;app=youtube_gdata' type='application/x-shockwave-flash' medium='video' isDefault='true' expression='full' duration='36' yt:format='5'/>
                                        <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{total_seconds}' yt:format='1'/>
                                        <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{total_seconds}' yt:format='6'/>
                                        <media:credit role='uploader' scheme='urn:youtube' yt:display='{SecurityElement.Escape(uploader)}' yt:type='partner'>{channel_id}</media:credit>
                                        <media:description type='plain'>{description}</media:description>
                                        <media:keywords/>
                                        <media:license type='text/html' href='https://www.youtube.com/t/terms'>youtube</media:license>
                                        <media:player url='https://www.youtube.com/watch?v=9DjIh0sVBR0&amp;feature=youtube_gdata_player'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='default'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/mqdefault.jpg' height='180' width='320' yt:name='mqdefault'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/hqdefault.jpg' height='360' width='480' yt:name='hqdefault'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='start'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='middle'/>
                                        <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='end'/>
                                        <media:content url=""{base_url}/getvideo/{id}"" type=""video/mp4"" medium=""video"" isDefault=""true"" expression=""full"" duration=""{total_seconds}"" yt:format=""3""/>
                                        <media:content url=""{base_url}/getvideo/{id}"" type=""video/3gpp"" medium=""video"" expression=""full"" duration=""{total_seconds}"" yt:format=""2""/>
                                        <media:content url=""{base_url}/getvideo/{id}?muxed=true"" type=""video/mp4"" medium=""video"" expression=""full"" duration=""{total_seconds}"" yt:format=""5""/>
                                        <media:content url=""{base_url}/getvideo/{id}"" type=""video/mp4"" medium=""video"" expression=""full"" duration=""{total_seconds}"" yt:format=""8""/>
                                        <media:content url=""{base_url}/getvideo/{id}"" type=""video/3gpp"" medium=""video"" expression=""full"" duration=""{total_seconds}"" yt:format=""9""/>
                                        <media:title type='plain'>{SecurityElement.Escape(title)}</media:title>
                                        <yt:duration seconds='{total_seconds}'/>
                                        <yt:uploaded>1970-01-01T00:00:00.000Z</yt:uploaded>
                                        <yt:uploaderId>{channel_id}</yt:uploaderId>
                                        <yt:userId>{channel_id}</yt:userId>
                                        <yt:videoid>{id}</yt:videoid>
                                    </media:group>
                                    <gd:rating average='{rating}' max='0' min='0' numRaters='0' rel='https://schemas.google.com/g/2005#overall'/>
                                    <yt:recorded>1970-08-22</yt:recorded>
                                    <yt:statistics favoriteCount='0' viewCount=""7856961""/>
                                    <yt:rating numDislikes='{dislikes}' numLikes='{likes}'/>
                                    <yt:position>1</yt:position>
                                </entry>";


                                
                                combined.Add(item_xml);
                                count++;
                            }
                        }
                    }
                }
                

                return Tuple.Create(string.Join("\n", combined), count);
            }

            app.MapGet("/feeds/api/videos/{id}/related", async (string id, HttpRequest request) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                        return Results.StatusCode(400);

                    string? device_id = null;

                    device_id = HandleLogin.ExtractDeviceIDFromRequest(request);

                    if (string.IsNullOrEmpty(device_id))
                        Console.WriteLine("No deviceid, will just be using unloggined realted videos");

                    var access_token = "";
 
                    access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);
   
                    var json = await UseInnerTube(id, access_token);

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
                        <openSearch:totalResults>1000</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>1</openSearch:itemsPerPage>
                        {data.Item1}
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