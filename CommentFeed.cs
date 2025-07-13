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
                    var uploader_id = System.Security.SecurityElement.Escape(result.GetProperty("author_id").ToString());
                    var uploader_pfp = System.Security.SecurityElement.Escape(result.GetProperty("author_thumbnail").ToString());
                    var timestamp = System.Security.SecurityElement.Escape(result.GetProperty("timestamp").ToString());

                    var item = $@"<entry gd:etag='placeholder'>
                            <id>tag:youtube.com,2008:video:{video_id}:comment:{id}</id>
                            <published>{timestamp}</published>
                            <updated>{timestamp}</updated>
                            <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#comment'/>
                            <title>Comment from {uploader}</title>
                            <content>{text}</content>
                            <link rel='related' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/videos/{video_id}?v=2'/>
                            <link rel='alternate' type='text/html' href='http://www.youtube.com/watch?v={video_id}'/>
                            <link rel='self' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/videos/{video_id}/comments/useless?v=2'/>
                            <author>
                                <name>{uploader}</name>
                                <uri>{uploader_id}</uri>
                                <yt:userId>{uploader_id}</yt:userId>
                            </author>
                            <yt:channelId>{uploader_id}</yt:channelId>
                            <yt:replyCount>0</yt:replyCount>
                            <yt:videoid>{video_id}</yt:videoid>
                        </entry>
                    ";

                    var comment = item.Split();

                    combined.AddRange(comment);

                }


                return (string.Join("\n", combined), comments.EnumerateArray().Count());
            }

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

                string template = @$"<?xml version='1.0' encoding='UTF-8'?>
                    <feed xmlns='http://www.w3.org/2005/Atom'
                            xmlns:media='http://search.yahoo.com/mrss/'
                            xmlns:openSearch='http://a9.com/-/spec/opensearchrss/1.0/'
                            xmlns:gd='http://schemas.google.com/g/2005'
                            xmlns:yt='http://gdata.youtube.com/schemas/2007'>
                        <id>http://gdata.youtube.com/feeds/api/standardfeeds/us/recently_featured</id>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme='http://schemas.google.com/g/2005#kind' 
                                    term='http://gdata.youtube.com/schemas/2007#video'/>
                        <title type='text'>YouTube Feed</title>
                        <logo>http://www.youtube.com/img/pic_youtubelogo_123x63.gif</logo>
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version='2.0' uri='http://gdata.youtube.com/'>YouTube data API</generator>
                        <openSearch:totalResults>25</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>25</openSearch:itemsPerPage>
                        <entry gd:etag=' '>
                            {data}
                        </entry>
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