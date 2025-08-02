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
    public static class DefaultPlaylists
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string? accessToken)
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
                    ["browseId"] = "FEplaylist_aggregation",
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

          
            Tuple<string, int> ExtractData(string json, string access_token, HttpRequest request)
            {

                using var doc = JsonDocument.Parse(json);
                var combined = new List<string>();
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                int count = 0;

                // this is the FEplaylist_aggregation
                // i'd explain it but I have already done it mmany times
                // it's pretty like the other feeds
                // they even still use tilerenders for playlists

                JsonElement sectionsRoot;
                try
                {
                    sectionsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("tvBrowseRenderer")
                        .GetProperty("content")
                        .GetProperty("tvSurfaceContentRenderer")
                        .GetProperty("content")
                        .GetProperty("gridRenderer")
                        .GetProperty("items");
                }
                catch
                {
                    return Tuple.Create("", 0);
                }


                // weclome to hell again 
                foreach (var item in sectionsRoot.EnumerateArray())
                {
                    if (item.TryGetProperty("tileRenderer", out var tileRenderer))
                    {

                        var id = "";
                        if (tileRenderer.TryGetProperty("onSelectCommand", out var onSelectCommand))
                        {
                            if (onSelectCommand.TryGetProperty("browseEndpoint", out var browseEndpoint))
                            {
                                id = browseEndpoint.TryGetProperty("browseId", out var videoIdProp)
                                    ? videoIdProp.GetString()
                                    : "Unknown";
                            }
                        }

                        string title = tileRenderer.GetProperty("metadata")
                            .GetProperty("tileMetadataRenderer")
                            .GetProperty("title")
                            .GetProperty("simpleText")
                            .GetString() ?? "No Title";


                        string thumbnail_url = "";

                        if (tileRenderer.TryGetProperty("onLongPressCommand", out var onLongPressCommand))
                        {
                            if (onLongPressCommand.TryGetProperty("showMenuCommand", out var showMenuCommand))
                            {
                                if (showMenuCommand.TryGetProperty("thumbnail", out var thumbnail))
                                {
                                    if (thumbnail.TryGetProperty("thumbnails", out var thumbnailsArray) && thumbnailsArray.ValueKind == JsonValueKind.Array)
                                    {
                                        if (thumbnailsArray.GetArrayLength() > 0)
                                        {
                                            var firstThumb = thumbnailsArray[0];
                                            if (firstThumb.TryGetProperty("url", out var urlProp))
                                            {
                                                thumbnail_url = urlProp.GetString() ?? "";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        string author = "";
                        if (tileRenderer.TryGetProperty("metadata", out var metadata))
                        {
                            if (metadata.TryGetProperty("tileMetadataRenderer", out var tileMetadataRenderer))
                            {
                                // Extract lines[0].lineRenderer.items[0].lineItemRenderer.text.simpleText
                                if (tileMetadataRenderer.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array && lines.GetArrayLength() > 0)
                                {
                                    var firstLine = lines[0];

                                    if (firstLine.TryGetProperty("lineRenderer", out var lineRenderer))
                                    {
                                        if (lineRenderer.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
                                        {
                                            var firstItem = items[0];

                                            if (firstItem.TryGetProperty("lineItemRenderer", out var lineItemRenderer))
                                            {
                                                if (lineItemRenderer.TryGetProperty("text", out var text))
                                                {
                                                    if (text.TryGetProperty("simpleText", out var simpleText))
                                                    {
                                                        author = simpleText.GetString() ?? "";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        int video_count = 0;

                        if (tileRenderer.TryGetProperty("header", out var header) &&
                            header.TryGetProperty("tileHeaderRenderer", out var tileHeaderRenderer) &&
                            tileHeaderRenderer.TryGetProperty("thumbnailOverlays", out var thumbnailOverlays) &&
                            thumbnailOverlays.ValueKind == JsonValueKind.Array &&
                            thumbnailOverlays.GetArrayLength() > 0)
                        {

                            foreach (var overlay in thumbnailOverlays.EnumerateArray())
                            {
                                if (overlay.TryGetProperty("thumbnailOverlayTimeStatusRenderer", out var timeStatusRenderer))
                                {
                                    if (timeStatusRenderer.TryGetProperty("text", out var text) &&
                                        text.TryGetProperty("runs", out var runs) &&
                                        runs.ValueKind == JsonValueKind.Array &&
                                        runs.GetArrayLength() > 0)
                                    {

                                        var firstRun = runs[0];
                                        if (firstRun.TryGetProperty("text", out var countTextProp))
                                        {
                                            string countText = countTextProp.GetString() ?? "0";

                                            countText = countText.Replace(",", "");

                                            if (!int.TryParse(countText, out video_count))
                                            {
                                                video_count = 0;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        var item_xml = $"""
                        <entry>
                            <id>tag:youtube.com,2008:playlist:snippet: {id}</id>
                            <playlistId>{id}</playlistId>
                            <yt:playlistId>{id}</yt:playlistId>
                            <published>2011-12-19T22:02:40.000Z</published>
                            <updated>2011-12-27T18:33:18.000Z</updated>
                            <category scheme="http://schemas.google.com/g/2005#kind" term="http://gdata.youtube.com/schemas/2007#playlistLink"/>
                            <title type="text">{SecurityElement.Escape(title)}</title>
                            <content type="text"/>
                            <link rel="related" type="application/atom+xml" href="http://gdata.youtube.com/feeds/api/users/youtube"/>
                            <link rel="alternate" type="text/html" href="http://www.youtube.com/view_play_list?p={id}"/>
                            <link rel="self" type="application/atom+xml" href="http://gdata.youtube.com/feeds/api/users/youtube/playlists/{id}"/>
                            <author>
                                <name>{SecurityElement.Escape(author)}</name>
                                <uri>http://gdata.youtube.com/feeds/api/users/youtube</uri>
                            </author>
                            <yt:description/>
                            <gd:feedLink rel="http://gdata.youtube.com/schemas/2007#playlist" href="http://gdata.youtube.com/feeds/api/playlists/{id}" countHint="{video_count}"/>
                            <yt:countHint>{video_count}</yt:countHint>
                            <media:group>
                                <media:thumbnail url="{thumbnail_url}" height="90" width="120" yt:name="default"/>
                                <media:thumbnail url="{thumbnail_url}" height="360" width="480" yt:name="hqdefault"/>
                                <yt:duration seconds="60"/>
                            </media:group>
                            <summary></summary>
                        </entry>
                        """;


                        combined.Add(item_xml);
                        count++;
                    }
                }

                return Tuple.Create(string.Join("\n", combined), count);
            }

            app.MapGet("/feeds/api/users/default/playlists", async (HttpRequest request) =>
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
                    
                    var template = @$"<?xml version='1.0' encoding='UTF-8'?>
                    <feed
                        xmlns='http://www.w3.org/2005/Atom'
                        xmlns:media='http://search.yahoo.com/mrss/'
                        xmlns:openSearch='http://a9.com/-/spec/opensearchrss/1.0/'
                        xmlns:gd='http://schemas.google.com/g/2005'
                        xmlns:yt='http://gdata.youtube.com/schemas/2007'>
                        <id>http://gdata.youtube.com/feeds/api/users/youtube/playlists</id>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#playlistLink'/>
                        <title type='text'>Playlists of youtube</title>
                        <logo>http://www.youtube.com/img/pic_youtubelogo_123x63.gif</logo>
                        <link rel='related' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/users/youtube'/>
                        <link rel='alternate' type='text/html' href='http://www.youtube.com/profile?user=youtube#p/p'/>
                        <link rel='http://schemas.google.com/g/2005#feed' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/users/youtube/playlists'/>
                        <link rel='http://schemas.google.com/g/2005#batch' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/users/youtube/playlists/batch'/>
                        <link rel='self' type='application/atom+xml' href='{base_url}/feeds/api/users/youtube/playlists?start-index=1&amp;max-results=25'/>
                        <link rel='next' type='application/atom+xml' href='{base_url}/feeds/api/users/youtube/playlists?start-index=26&amp;max-results=25'/>
                        <author>
                            <name>youtube</name>
                            <uri>http://gdata.youtube.com/feeds/api/users/youtube</uri>
                        </author>
                        <generator version='2.1' uri='http://gdata.youtube.com'>YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>25</openSearch:itemsPerPage>
                        {data.Item1 ?? ""}
                    </feed>
                    ";
                    
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