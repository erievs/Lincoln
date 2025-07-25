using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeDLSharp.Helpers;

namespace Lincon
{

    public static class CommentFeed
    {

        public static void HandleFeed(WebApplication app)
        {

            YoutubeDL ytdlp = new();

            HttpClient client = new();
            Dictionary<string, string> videoDict = [];

            
            async Task<string> UseYTDlP(string url)
            {     
                // WE WILL NEED MORE OPTIONS
                // youtube:search: [youtube] YouTube search; "ytsearch:" prefix (e.g. "ytsearch:running tortoise")
                            
                var options = new OptionSet
                {
                    DumpSingleJson = true,
                    SkipDownload = true,
                    FlatPlaylist = true,
                    Verbose = true,
                    ExtractorArgs = new[]
                    {
                        "youtube:max_comments=20,comment_sort=newest"
                    },
                    WriteComments = true
                };
                
                var res = await ytdlp.RunVideoDataFetch(url, overrideOptions: options);

                Console.WriteLine("\nRes:\n" + res.Data);
                    
                // still very cool we do not need to deal with the innertube directly
                // wish I would have known about this earliler lol

                // https://github.com/Bluegrams/YoutubeDLSharp
                // we need to use .Data lol

                return res.Data.ToString();
            }

            async Task<(string, int)> ExtractData(string data, string video_id, HttpRequest request)
            {
                await Task.Delay(0); // why uh we got that stupid warning (GOD I HATE ASYNC)

                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("comments", out var comments))
                    return ("", 0);

                var combined = new List<string>();

                // yeah this pretty much a copy and past from the search feed

                foreach(var result in comments.EnumerateArray()) {
                    
                    var id = System.Security.SecurityElement.Escape(result.GetProperty("id").ToString());
                    var text = System.Security.SecurityElement.Escape(result.GetProperty("text").ToString());
                    var uploader = System.Security.SecurityElement.Escape(result.GetProperty("author").ToString());
                    var channel_id = System.Security.SecurityElement.Escape(result.GetProperty("author_id").ToString());
                    var uploader_pfp = System.Security.SecurityElement.Escape(result.GetProperty("author_thumbnail").ToString());
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(result.GetProperty("timestamp").GetString() ?? "0")).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);

                    var item = $"""
                        <entry>
                            <id>tag:youtube.com,2008:video:{id}:comment:</id>
                            <published>{timestamp}</published>
                            <updated>{timestamp}</updated>
                            <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#comment'/>
                            <title>Comment from {uploader}</title>
                            <content>{text}</content>
                            <link rel='related' type='application/atom+xml' href="http://gdata.youtube.com/feeds/api/videos/{id}?v=2"/>
                            <link rel='alternate' type='text/html' href="http://www.youtube.com/watch?v={id}"/>
                            <link rel='self' type='application/atom+xml' href="http://gdata.youtube.com/feeds/api/videos/{id}/comments/useless?v=2"/>
                            <author>
                                <name>{uploader}</name>
                                <uri>{channel_id}</uri>
                                <yt:userId>{channel_id}</yt:userId>
                            </author>
                            <yt:channelId>{channel_id}</yt:channelId>
                            <yt:replyCount>1</yt:replyCount>
                            <yt:videoid>{id}</yt:videoid>
                        </entry>
                    """;

                    var comment = item.Split("\n");

                    combined.AddRange(comment);

                }


                return (string.Join("\n", combined), comments.EnumerateArray().Count());
            }

            app.MapGet(@"/api/videos/{video_id}/comments", async (string video_id, HttpRequest request) => 
            {
                try
                {
                    video_id = System.Security.SecurityElement.Escape(video_id);
                    // I forgot how to use ? : thingy so I am just doing a if, feel free to change
                    if(String.IsNullOrEmpty(video_id)) {
                    return Results.StatusCode(500);
                }

                Console.WriteLine("\nVideo Id: " + video_id);

                var json = await UseYTDlP("http://youtube.com/watch?v=" + video_id);

                var data = await ExtractData(json, video_id, request);

                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var continuation = "todo";

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
                        {(continuation != null 
                            ? "<link rel='next' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/videos?continuation={continuation}'/>" 
                            : "")}
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version=""2.1"" uri=""{base_url}"">YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>1</openSearch:itemsPerPage>
                        {data.Item1}
                    </feed>";

                    return Results.Content(template, "application/xml"); // broken on firefox 
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });


            // yeah login's weird for some reason they changed it up
            app.MapGet(@"/feeds/api/videos/{video_id}/comments", async (string video_id, HttpRequest request) => 
            {
                try
                {
                    video_id = System.Security.SecurityElement.Escape(video_id);
                    // I forgot how to use ? : thingy so I am just doing a if, feel free to change
                    if(String.IsNullOrEmpty(video_id)) {
                    return Results.StatusCode(500);
                }

                Console.WriteLine("\nVideo Id: " + video_id);

                var json = await UseYTDlP("http://youtube.com/watch?v=" + video_id);

                var data = await ExtractData(json, video_id, request);

                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var continuation = "todo";

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
                        {(continuation != null 
                            ? "<link rel='next' type='application/atom+xml' href='{System.Security.SecurityElement.Escape(base_url)}/feeds/api/videos?continuation={continuation}'/>" 
                            : "")}
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version=""2.1"" uri=""{base_url}"">YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>1</openSearch:itemsPerPage>
                        {data.Item1}
                    </feed>";

                    return Results.Content(template, "application/xml"); // broken on firefox 
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });

        }

    }

}