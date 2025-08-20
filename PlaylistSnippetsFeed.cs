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
    public static class PlaylistSnippetsFeed
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string query)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/search";

                var payload = new Dictionary<string, object>
                {
                    ["context"] = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            clientName = "MWEB",
                            clientVersion = "2.20250209.01.00",
                            userAgent = "Mozilla/5.0 (Linux; Android 12; Pixel 6 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.6167.164 Mobile Safari/537.36",
                            screenPixelDensity = 3,
                            platform = "MOBILE"
                        }
                    },
                    ["query"] = query,
                    ["params"] = "EgIQAw%3D%3D" 
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

                request.Content = JsonContent.Create(payload);

                var res = await client.SendAsync(request);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                    throw new Exception($"Browse request failed with status {res.StatusCode}");

                return json;
            }

          
            Tuple<string, int> ExtractData(string json, HttpRequest request)
            {

                using var doc = JsonDocument.Parse(json);
                var combined = new List<string>();
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                int count = 0;

                // this is a mweb feed not tv
                // so it is layed out a bit weirdly

                JsonElement sectionsRoot;
                try
                {
                    sectionsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("sectionListRenderer")
                        .GetProperty("contents")[0]
                        .GetProperty("itemSectionRenderer")
                        .GetProperty("contents");

                }
                catch
                {
                    return Tuple.Create("", 0);
                }


                // weclome to hell again 
                foreach (var item in sectionsRoot.EnumerateArray())
                {
                    if (item.TryGetProperty("compactPlaylistRenderer", out var compactPlaylistRenderer))
                    {


                Console.WriteLine("\nFuck: " + item);

                        var id = "";
                        if (compactPlaylistRenderer.TryGetProperty("playlistId", out var playlistId))
                        {
                            id = "VL" + playlistId.GetString(); // you must add VL!
                        }

                        string title = compactPlaylistRenderer.GetProperty("title")
                            .GetProperty("runs")[0]
                            .GetProperty("text")
                            .GetString() ?? "No Title";


                        string thumbnail_url = "";

                        if (compactPlaylistRenderer.TryGetProperty("thumbnailRenderer", out var thumbnailRenderer))
                        {
                            if (thumbnailRenderer.TryGetProperty("playlistVideoThumbnailRenderer", out var playlistVideoThumbnailRenderer))
                            {
                                if (playlistVideoThumbnailRenderer.TryGetProperty("thumbnail", out var thumbnail))
                                {
                                    if (thumbnail.TryGetProperty("thumbnails", out var thumbnailsArray) && thumbnailsArray.ValueKind == JsonValueKind.Array)
                                    {
                                        if (thumbnailsArray.GetArrayLength() > 0)
                                        {
                                            var firstThumb = thumbnailsArray[1];
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
                        string channel_id = "";

                        if (compactPlaylistRenderer.TryGetProperty("longBylineText", out var longBylineText) &&
                            longBylineText.TryGetProperty("runs", out var runs) &&
                            runs.ValueKind == JsonValueKind.Array &&
                            runs.GetArrayLength() > 0)
                        {
                            var firstRun = runs[0];
                            if (firstRun.TryGetProperty("text", out var authorText))
                            {
                                author = authorText.GetString() ?? "";
                            }

                            if (firstRun.TryGetProperty("navigationEndpoint", out var navigationEndpoint) &&
                                navigationEndpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
                                browseEndpoint.TryGetProperty("browseId", out var browseIdProp))
                            {
                                channel_id = browseIdProp.GetString() ?? "";
                            }
                        }


                        int video_count = 0;

                        if (compactPlaylistRenderer.TryGetProperty("thumbnailText", out var thumbnailText) &&
                            thumbnailText.TryGetProperty("runs", out var runs_again) &&
                            runs_again.ValueKind == JsonValueKind.Array &&
                            runs_again.GetArrayLength() > 0)
                        {
                            var firstRun = runs_again[0];

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

                 
                        var item_xml = @$"
                        <entry gd:etag='W/&quot;D0ANRn47eCp7ImA9WxVUF04.&quot;'>
                            <id>tag:youtube.com,2008:playlist:snippet:{id}</id>
                            <published>2008-05-26T23:39:53.000Z</published>
                            <updated>2009-03-21T12:45:57.000Z</updated>
                            <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#playlistLink'/>
                            <title>{System.Security.SecurityElement.Escape(title)}</title>
                            <summary/>
                            <content type='application/atom+xml;type=feed' src='https://gdata.youtube.com/feeds/api/playlists/{id}?v=2'/>
                            <link rel='related' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/users/GoogleDevelopers?v=2'/>
                            <link rel='alternate' type='text/html' href='https://www.youtube.com/view_play_list?p={id}'/>
                            <link rel='self' type='application/atom+xml' href='{base_url}/feeds/api/playlists/snippets/{id}?v=2'/>
                            <link rel='edit' type='application/atom+xml' href='{base_url}/feeds/api/playlists/snippets/{id}?v=2'/>
                            <author>
                                <name>{System.Security.SecurityElement.Escape(author)}</name>
                                <uri>{base_url}/feeds/api/users/{channel_id}</uri>
                                <yt:userId>{channel_id}</yt:userId>
                            </author>
                            <yt:countHint>{video_count}</yt:countHint>
                            <media:group>
                                <media:thumbnail url='{System.Security.SecurityElement.Escape(thumbnail_url)}' height='90' width='120' yt:name='default'/>
                                <media:thumbnail url='{System.Security.SecurityElement.Escape(thumbnail_url)}' height='180' width='320' yt:name='mqdefault'/>
                                <media:thumbnail url='{System.Security.SecurityElement.Escape(thumbnail_url)}' height='360' width='480' yt:name='hqdefault'/>
                            </media:group>
                            <yt:playlistId>{id}</yt:playlistId>
                            <gd:feedLink rel='http://gdata.youtube.com/schemas/2007#playlist' href='{base_url}/feeds/api/playlists/{id}' countHint='{video_count}'/>
                        </entry>
                        ";

                        if (video_count == 0)
                        {
                            item_xml = "";
                        }

                        combined.Add(item_xml);
                        count++;
                    }
                }

                return Tuple.Create(string.Join("\n", combined), count);
            }

            app.MapGet("/feeds/api/playlists/snippets", async (HttpRequest request) =>
            {
                try
                {

                    string? query = System.Security.SecurityElement.Escape(request.Query["q"]);

                    if (String.IsNullOrEmpty(query))
                    {
                        return Results.StatusCode(500);
                    }

                    var json = await UseInnerTube(query);

                    var data = ExtractData(json, request);

                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                                        
                    var template = $@"<?xml version='1.0' encoding='UTF-8'?>
                    <feed xmlns='http://www.w3.org/2005/Atom'
                        xmlns:openSearch='http://a9.com/-/spec/opensearch/1.1/'
                        xmlns:media='http://search.yahoo.com/mrss/'
                        xmlns:batch='http://schemas.google.com/gdata/batch'
                        xmlns:yt='http://gdata.youtube.com/schemas/2007'
                        xmlns:gd='http://schemas.google.com/g/2005'
                        gd:etag='W/&quot;DEAAQH44eip7ImA9WxVUGUQ.&quot;'>
                        <id>tag:youtube.com,2008:playlists:snippets</id>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#playlistLink'/>
                        <title>YouTube Playlists matching query: {System.Security.SecurityElement.Escape(query)}</title>
                        <logo>http://www.youtube.com/img/pic_youtubelogo_123x63.gif</logo>
                        <link rel='http://schemas.google.com/g/2005#feed' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/playlists/snippets?v=2'/>
                        <link rel='http://schemas.google.com/g/2005#batch' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/playlists/snippets/batch?v=2'/>
                        <link rel='self' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/playlists/snippets?q={System.Security.SecurityElement.Escape(query)}&amp;start-index=1&amp;max-results=25&amp;v=2'/>
                        <link rel='service' type='application/atomsvc+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/playlists/snippets?alt=atom-service&amp;v=2'/>
                        <link rel='next' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/playlists/snippets?q={System.Security.SecurityElement.Escape(query)}&amp;start-index=26&amp;max-results=25&amp;v=2'/>
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version='2.0' uri='http://gdata.youtube.com/'>YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>25</openSearch:itemsPerPage>
                        {data.Item1}
                    </feed>";


                    return Results.Content(template, "application/xml");
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