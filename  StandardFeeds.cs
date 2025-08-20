using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using Google.Protobuf;

namespace Lincon
{

    public static class StandardFeeds
    {

        public static void HandleFeed(WebApplication app)
        {

            YoutubeDL ytdlp = new();

            HttpClient client = new();
            Dictionary<string, string> videoDict = [];


            async Task<string> UseYTDlP(string query, bool isNestedEntry = false)
            {
                // WE WILL NEED MORE OPTIONS
                // youtube:search: [youtube] YouTube search; "ytsearch:" prefix (e.g. "ytsearch:running tortoise")

                var options = new OptionSet
                {
                    DumpSingleJson = true,     
                    SkipDownload = true,        
                    FlatPlaylist = true,        
                };

                if (isNestedEntry)
                {
                    options.ExtractorArgs = new[]
                    {
                        "youtube:tab=home;",
                    };

                    options.SkipPlaylistAfterErrors = 0;
                    options.PlaylistItems = "1:50";
                    
                }

                var res = await ytdlp.RunVideoDataFetch(query, overrideOptions: options);

                return res.Data.ToString();
            }

            Tuple<string, int> ExtractData(string data, bool isNestedEntry, HttpRequest request)
            {

                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("entries", out var videos))
                {
                    return Tuple.Create("", 0);
                }

                if (isNestedEntry) // for the home of trending and such the playlist is nested
                {
                    if (videos.ValueKind == JsonValueKind.Array && videos.GetArrayLength() > 0)
                    {

                        var item = videos[0];

                        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("entries", out var nested_videos))
                        {
                            videos = nested_videos;
                        }
                        else
                        {
                            return Tuple.Create("", 0);
                        }
                    }
                    else if (videos.ValueKind == JsonValueKind.Object && videos.TryGetProperty("entries", out var nested_videos))
                    {
                        videos = nested_videos;
                    }
                    else
                    {
                        return Tuple.Create("", 0);
                    }
                }


                var combined = new List<string>();

                var video_count = videos.EnumerateArray().Count();
                
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                string? device_id = null;

                device_id = HandleLogin.ExtractDeviceIDFromRequest(request);

                // just the video to link accounts (eaisest way for a user to grab their device id)
                if (!String.IsNullOrEmpty(device_id) && !HandleLogin.IsDeviceLinked(device_id))
                {

                    var id = "_iPtv6ZpC6c";
                    var thumbnail_id = "94OFIVpwua0";
                    var title = "Link Your Google Account!";
                    var uploader = "Lincoln";
                    double duration = 56;
                    var channel_id = "UC8tD_jEkVm-7O83BeONUG8A";
                    var view_count = 1863;
                    var published = "1809-02-12T12:00:00.000Z";
                    var description = $"Visit: {base_url}/o/oauth2/programmatic_auth?device_id={device_id}";
                    var rating = 5;
                    var dislikes = 1861;
                    var likes = 1865;

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
                                         <link rel="http://gdata.youtube.com/schemas/2007#video.related" href="https://gdata.youtube.com/feeds/api/videos/{id}/related"/>
                        <link rel='http://gdata.youtube.com/schemas/2007#mobile' type='text/html' href='https://m.youtube.com/details?v={id}'/>
                        <link rel='http://gdata.youtube.com/schemas/2007#uploader' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/users/{channel_id}?v=2'/>
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
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='default'/>
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/mqdefault.jpg' height='180' width='320' yt:name='mqdefault'/>
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/hqdefault.jpg' height='360' width='480' yt:name='hqdefault'/>
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='start'/>
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='middle'/>
                            <media:thumbnail url='http://i.ytimg.com/vi/{thumbnail_id}/default.jpg' height='90' width='120' time='00:00:00.000' yt:name='end'/>
                            <media:content url="{base_url}/getvideo/{id}" type="video/mp4" medium="video" isDefault="true" expression="full" duration="{duration}" yt:format="3"/>
                            <media:content url="{base_url}/getvideo/{id}" type="video/3gpp" medium="video" expression="full" duration="{duration}" yt:format="2"/>
                            <media:content url="{base_url}/getvideo/{id}?muxed=true" type="video/mp4" medium="video" expression="full" duration="{duration}" yt:format="5"/>
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

                foreach (var result in videos.EnumerateArray())
                {

                    // you must escape the feilds (I learbed that the hard way once)
                    var id = System.Security.SecurityElement.Escape(result.GetProperty("id").ToString());
                    var title = System.Security.SecurityElement.Escape(result.GetProperty("title").ToString());
                    var uploader = System.Security.SecurityElement.Escape(result.GetProperty("uploader").ToString()) ?? "";
                        if (String.IsNullOrEmpty(uploader))
                                uploader = "Spotlight";
                    var thumbnail = $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";
                    double duration = 42;
                        if (result.TryGetProperty("duration", out var duration_value) && duration_value.ValueKind == JsonValueKind.Number)
                            duration = duration_value.GetDouble();
                    var channel_id = System.Security.SecurityElement.Escape(result.GetProperty("channel_id").ToString()) ?? "UCBR8-60-B28hp2BmDPdntcQ";
                        if (String.IsNullOrEmpty(channel_id))
                            channel_id = "UCBR8-60-B28hp2BmDPdntcQ";
                    var view_count = System.Security.SecurityElement.Escape(result.GetProperty("view_count").ToString());
                    var published = result.TryGetProperty("view_count", out var d) && d.ValueKind == JsonValueKind.Number ? DateTimeOffset.UtcNow.AddSeconds(-d.GetInt64()).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture) : "1970-01-01T00:00:00.000Z";
                    var description = System.Security.SecurityElement.Escape(result.GetProperty("description").ToString()) ?? "placeholder";
                    var rating = 0;
                    var dislikes = 0;
                    var likes = 0;

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
                                         <link rel="http://gdata.youtube.com/schemas/2007#video.related" href="https://gdata.youtube.com/feeds/api/videos/{id}/related"/>
                        <link rel='http://gdata.youtube.com/schemas/2007#mobile' type='text/html' href='https://m.youtube.com/details?v={id}'/>
                        <link rel='http://gdata.youtube.com/schemas/2007#uploader' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/users/{channel_id}?v=2'/>
                        <link rel='related' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/videos/{id}?v=2'/>
                        <link rel='self' type='application/atom+xml' href='https://gdata.youtube.com/feeds/api/playlists/8E2186857EE27746/PLyl9mKRbpNIpJC5B8qpcgKX8v8NI62Jho?v=2'/>
                        <author>
                            <name>{System.Security.SecurityElement.Escape(uploader)}</name>
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
                            <gd:feedLink rel='http://gdata.youtube.com/schemas/2007comments' href='{base_url}/api/videos/{id}/comments' countHint='5'/>
                        </gd:comments>
                        <yt:location>Cleveland ,US</yt:location>
                        <media:group>
                            <media:category label='Howto &amp; Style' scheme='http://gdata.youtube.com/schemas/2007/categories.cat'>Howto</media:category>
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
                            <media:content url="{base_url}/getvideo/{id}?muxed=true" type="video/mp4" medium="video" expression="full" duration="{duration}" yt:format="5"/>
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

                return Tuple.Create(string.Join("\n", combined), videos.EnumerateArray().Count());
            }

            app.MapGet(@"/feeds/api/standardfeeds/US/{feed}", async (string feed, HttpRequest request) => // needs to be ?q at some point for real hardware
            {
                try
                {

                    if (String.IsNullOrEmpty(feed))
                    {
                        return Results.StatusCode(500);
                    }

                    var feed_url = "";
                    var isNestedEntry = false;

                    switch (feed)
                    {
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
                            feed_url = "https://www.youtube.com/channel/UCBR8-60-B28hp2BmDPdntcQ";
                            isNestedEntry = true;
                            break;

                    }

                    var json = await UseYTDlP($"{feed_url}", isNestedEntry);


                    var data = ExtractData(json, isNestedEntry, request);

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