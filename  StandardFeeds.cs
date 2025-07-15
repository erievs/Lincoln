using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;


namespace Lincon
{

    public static class StandardFeeds
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
                    
                    // you must escape the feilds (I learbed that the hard way once)
                    var id = System.Security.SecurityElement.Escape(result.GetProperty("id").ToString());
                    var title = System.Security.SecurityElement.Escape(result.GetProperty("title").ToString());
                    var uploader = System.Security.SecurityElement.Escape(result.GetProperty("uploader").ToString());
                    var thumbnail = $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
                    var duration = System.Security.SecurityElement.Escape(result.GetProperty("duration").ToString());
                    var channel_id = System.Security.SecurityElement.Escape(result.GetProperty("channel_id").ToString());
                    var view_count = System.Security.SecurityElement.Escape(result.GetProperty("view_count").ToString());
                    var published = $"{DateTime.UnixEpoch:yyyy-MM-ddTHH:mm:ss.fffZ}"; // placeholder pretty much
                    var description = "placeholder";
                    var rating = 0;
                    var dislikes = 0;
                    var likes = 0;

                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                    // if you'd like to replace all ' with ' feel free
                    // I forgot about ' untiul I added the media links

                    var item = $"""
                        <entry>
                            <id>tag:youtube.com,2008:video:{id}</id>
                            <published>{published}</published>
                            <updated>{published}</updated>
                            <category scheme='https://schemas.google.com/g/2005#kind' term='https://gdata.youtube.com/schemas/1970#video'/>
                                <category scheme='https://gdata.youtube.com/schemas/1970/categories.cat' term='Howto' label='Howto &amp; Style'/>
                            <title>{title}</title>
                            <content type='application/x-shockwave-flash' src='https://www.youtube.com/v/{id}?version=3&amp;f=playlists&amp;app=youtube_gdata'/>
                            <link rel='alternate' type='text/html' href='https://www.youtube.com/watch?v={id}&amp;feature=youtube_gdata'/>
                            <link rel='http://gdata.youtube.com/schemas/2007#video.related' type='application/atom+xml' href="https://gdata.youtube.com/feeds/api/videos/{id}/related"/>
                            <link rel='https://gdata.youtube.com/schemas/1970#mobile' type='text/html' href='https://m.youtube.com/details?v={id}'/>
                            <link rel='https://gdata.youtube.com/schemas/1970#uploader' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/users/{channel_id}?v=2'/>
                            <link rel='related' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/videos/{id}?v=2'/>
                            <link rel='self' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/playlists/8E2186857EE27746/PLyl9mKRbpNIpJC5B8qpcgKX8v8NI62Jho?v=2'/>
                            <author>
                                <name>{uploader}</name>
                                <uri>https://gdata.youtube.com/feeds/api/users/{channel_id}</uri>
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
                                <media:content url='https://www.youtube.com/v/{id}?version=3&amp;f=playlists&amp;app=youtube_gdata' type='application/x-shockwave-flash' medium='video' isDefault='true' expression='full' duration='{duration}' yt:format='5'/>
                                <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{duration}' yt:format='1'/>
                                <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{duration}' yt:format='6'/>
                                <media:credit role='uploader' scheme='urn:youtube' yt:display='{uploader}' yt:type='partner'>{channel_id}</media:credit>
                                <media:description type='plain'>{description}</media:description>
                                <media:keywords/>
                                <media:license type='text/html' href='https://www.youtube.com/t/terms'>youtube</media:license>
                                <media:player url='https://www.youtube.com/watch?v={id}&amp;feature=youtube_gdata_player'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='default'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/mqdefault.jpg' height='180' width='320' yt:name='mqdefault'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/hqdefault.jpg' height='360' width='480' yt:name='hqdefault'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='start'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='middle'/>
                                <media:thumbnail url='http://i.ytimg.com/vi/{id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='end'/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/mp4" medium="video" isDefault="true" expression="full" duration="{duration}" yt:format="3"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/3gpp" medium="video" expression="full" duration="{duration}" yt:format="2"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/mp4" medium="video" expression="full" duration="{duration}" yt:format="8"/>
                                <media:content url="{base_url}/getvideo/{id}" type="video/3gpp" medium="video" expression="full" duration="{duration}" yt:format="9"/>
                                <media:title type='plain'>{title}</media:title>
                                <yt:duration seconds='{duration}'/>
                                <yt:uploaded>{published}</yt:uploaded>
                                <yt:uploaderId>{channel_id}</yt:uploaderId>
                                <yt:videoid>{id}</yt:videoid>
                            </media:group>
                                <gd:rating average='{rating}' max='0' min='0' numRaters='0' rel='https://schemas.google.com/g/2005#overall'/>
                                <yt:recorded>1970-08-22</yt:recorded>
                                <yt:statistics favoriteCount='0' viewCount="{view_count}"/>
                                <yt:rating numDislikes='{dislikes}' numLikes='{likes}'/>
                                <yt:position>1</yt:position>
                        </entry>
                    """;

                    var video = item.Split("\n");

                    combined.AddRange(video);

                }


                return (string.Join("\n", combined), videos.EnumerateArray().Count());
            }

            app.MapGet(@"/feeds/api/standardfeeds/US/{feed}", async (string feed, HttpRequest request) => // needs to be ?q at some point for real hardware
            {
                try
                {

                if(String.IsNullOrEmpty(feed)) {
                    return Results.StatusCode(500);
                }

                Console.WriteLine("\nFeed: " + feed);

                var feed_url = "";

                switch(feed) {
                    

                    // may have to replace with search soon
                    // i heard rumors they may get rid of this feed

                    case "most_popular_News":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=news+today";                    
                    break;

                    case "most_popular_Games":
                        feed_url = "https://www.youtube.com/gaming/trending";                    
                    break;

                    case "most_popular_Music":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=music";                    
                    break;

                    case "most_popular_Tech":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Tech";                    
                    break;

                    case "most_popular_Sports":
                        feed_url = "https://www.youtube.com/channel/UCEgdi0XIXXZ-qJOFPf4JSKw/sportstab?ss=CMcB";                    
                    break;

                    case "most_popular_Film":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=film+and+animation+";                    
                    break;

                    case "most_popular_Entertainment":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=entertainment";                    
                    break;

                    case "most_popular_Howto":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=How+To";                    
                    break;

                    case "most_popular_Education":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Education";                    
                    break;

                    case "most_popular_Animals":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Animals";                    
                    break;

                    case "most_popular_Comedy":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Comedy";                    
                    break;

                    case "most_popular_Travel":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Travel";                    
                    break;

                    case "most_popular_Auto":
                        feed_url = "ytsearch20:https://www.youtube.com/results?search_query=Auto+and+Vehicles";                    
                    break;

                    default:
                      feed_url = "https://www.youtube.com/feed/trending?bp=6gQJRkVleHBsb3Jl";
                    break;

                }

                var json = await UseYTDlP($"{feed_url}");

                var data = await ExtractData(json, request);

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