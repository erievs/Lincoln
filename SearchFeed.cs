using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;


namespace Lincon
{

    public static class SearchFeed
    {

        public static void HandleFeed(WebApplication app)
        {

            YoutubeDL ytdlp = new();

            HttpClient client = new();
            Dictionary<string, string> videoDict = [];

            
            async Task<string> UseYTDlP(string query)
            {     
                // WE WILL NEED MORE OPTIONS
                // youtube:search: [youtube] YouTube search; "ytsearch:" prefix (e.g. "ytsearch:running tortoise")

                var options = new OptionSet
                {
                    DumpSingleJson = true,
                    SkipDownload = true,
                    FlatPlaylist = true,
                    WriteComments = true
                };
                
                var res = await ytdlp.RunVideoDataFetch(query, overrideOptions: options);

                Console.WriteLine("\nRes:\n" + res.Data);
                    
                // still very cool we do not need to deal with the innertube directly
                // wish I would have known about this earliler lol

                // https://github.com/Bluegrams/YoutubeDLSharp
                // we need to use .Data lol

                return res.Data.ToString();
            }

            async Task<(string, int)> ExtractData(string data, HttpRequest request)
            {
                await Task.Delay(0); // why uh we got that stupid warning (GOD I HATE ASYNC)

                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("entries", out var videos))
                    return ("", 0);


                var combined = new List<string>();

                // results (no linqs, because I don't understand linqs that much)
                // I know we used em in get_video but there's only one value you need the url 
                // here is more trcky

                foreach(var result in videos.EnumerateArray()) {
                    
                    var id = System.Security.SecurityElement.Escape(result.GetProperty("id").ToString());
                    var title = System.Security.SecurityElement.Escape(result.GetProperty("title").ToString());
                    var uploader = System.Security.SecurityElement.Escape(result.GetProperty("uploader").ToString());
                    var thumbnail = $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
                    var duration = System.Security.SecurityElement.Escape(result.GetProperty("duration").ToString());
                    var channel_id = System.Security.SecurityElement.Escape(result.GetProperty("channel_id").ToString());
                    var view_count = System.Security.SecurityElement.Escape(result.GetProperty("view_count").ToString());

                    // if you'd like to replace all ' with ' feel free
                    // I forgot about ' untiul I added the media links

                    var item = $@"<entry>
                            <id>tag:youtube.com,2008:playlist:{id}</id>
                            <published>2025-07-04T14:04:29.000Z</published>
                            <updated>2025-07-04T14:04:29.000Z</updated>
                            <category scheme='http://gdata.youtube.com/schemas/2007/categories.cat' label='-' term='-' >-</category>
                            <title type='text'>{title}</title>
                            <content type='text'>{uploader}</content>
                            <link rel='http://gdata.youtube.com/schemas/2007#video.related' href='http://PLACEHOLDER/feeds/api/videos/{channel_id}/related'/>
                            <author>
                                <name>{uploader}</name>
                                <uri>{request.Scheme}://{request.Host}{request.PathBase}/feeds/api/users/{channel_id}</uri>
                            </author>
                            <gd:comments>
                                <gd:feedLink href='{request.Scheme}://{request.Host}{request.PathBase}/feeds/api/videos/{id}/comments' countHint='530'/>
                            </gd:comments>
                            <media:group>
                                <media:category label='-' scheme='http://gdata.youtube.com/schemas/2007/categories.cat'>-</media:category>
                                <media:description type='plain'>{uploader}</media:description>
                                <media:keywords>-</media:keywords>
                                <media:player url='http://www.youtube.com/watch?v=XVBL9-GwzU0'/>
                                <media:thumbnail yt:name='hqdefault' url='{thumbnail}' height='240' width='320' time='00:00:00'/>
                                <media:thumbnail yt:name='poster' url='{thumbnail}' height='240' width='320' time='00:00:00'/>
                                <media:thumbnail yt:name='default' url='{thumbnail}g' height='240' width='320' time='00:00:00'/>
                                <media:content url='{request.Scheme}://{request.Host}{request.PathBase}/getvideo/{id}' type='video/mp4' medium='video' isDefault='true' expression='full' duration='0' yt:format='3' />
                                <media:content url='{request.Scheme}://{request.Host}{request.PathBase}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='0' yt:format='2' />
                                <media:content url='{request.Scheme}://{request.Host}{request.PathBase}/getvideo/{id}' type='video/mp4' medium='video' expression='full' duration='0' yt:format='8' />
                                <media:content url='{request.Scheme}://{request.Host}{request.PathBase}/getvideo/{id}' type='video/mp4' medium='video' expression='full' duration='0' yt:format='8' />             
                                <media:content url='{request.Scheme}://{request.Host}{request.PathBase}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='0' yt:format='9' />             
                                <yt:duration seconds='{duration}'/>
                                <yt:videoid id='{id}'>{id}</yt:videoid>
                                <media:credit role='uploader' name='{channel_id}'>{uploader}</media:credit>
                            </media:group>
                            <gd:rating average='5' max='5' min='1' numRaters='1641' rel='http://schemas.google.com/g/2005#overall'/>
                            <yt:statistics favoriteCount='6564' viewCount='{view_count}'/>
                            <yt:rating numLikes='59080' numDislikes='6564'/>
                        </entry>
                    ";

                    var video = item.Split(); // uh this converts it to what range wants jank I know.

                    combined.AddRange(video);

                }


                return (string.Join("\n", combined), videos.EnumerateArray().Count());
            }

            app.MapGet(@"/feeds/api/videos/", async (HttpRequest request) => // needs to be ?q at some point for real hardware
            {
                try
                {
                    string? query = System.Security.SecurityElement.Escape(request.Query["q"]);
                    // I forgot how to use ? : thingy so I am just doing a if, feel free to change
                    if(String.IsNullOrEmpty(query)) {
                    return Results.StatusCode(500);
                }

                Console.WriteLine("\nQuery: " + query);

                var json = await UseYTDlP($"ytsearch20:{query}");

                var data = await ExtractData(json, request);

                string template = @$"<?xml version='1.0' encoding='UTF-8'?>
                    <feed xmlns='http://www.w3.org/2005/Atom'
                        xmlns:openSearch='http://a9.com/-/spec/opensearch/1.1/'
                        xmlns:gd='http://schemas.google.com/g/2005'
                        xmlns:media='http://search.yahoo.com/mrss/'
                        xmlns:yt='http://gdata.youtube.com/schemas/2007'>
                        <id>tag:youtube.com,2008:channels</id>
                        <updated>2015-02-16T19:14:12.656Z</updated>
                        <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#video'/>
                        <title type='text'>YouTube Videos</title>
                        <logo>http://www.gstatic.com/youtube/img/logo.png</logo>
                        <link rel='alternate' type='text/html' href='http://www.youtube.com'/>
                        <link rel='http://schemas.google.com/g/2005#feed' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/videos'/>
                        <link rel='http://schemas.google.com/g/2005#batch' type='application/atom+xml' href='http://gdata.youtube.com/feeds/api/videos/batch'/>
                        <author>
                            <name>YouTube</name>
                            <uri>http://www.youtube.com/</uri>
                        </author>
                        <generator version='2.1' uri='http://gdata.youtube.com/'>YouTube data API</generator>
                        <openSearch:totalResults>1</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>{data.Item2}</openSearch:itemsPerPage>
                        <link rel='next' type='application/atom+xml' href='{request.Scheme}://{request.Host}{request.PathBase}/feeds/api/videos?q=bob&amp;start-index=20'/>
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