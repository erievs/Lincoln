using YoutubeDLSharp;
using Player;
using Google.Protobuf;
using System.Text.Json;
using YoutubeDLSharp.Options;
using System.Text.RegularExpressions;
using System.Security;

#pragma warning disable CS0618 
#pragma warning disable CS8619 
#pragma warning disable CS8604

namespace Lincon
{

    public static class GetVideo
    {

        public static void HandleVideos(WebApplication app)
        {

            YoutubeDL ytdlp = new();
            
            HttpClient client = new();
            Dictionary<string, string> videoDict = [];

            // Options

            bool skipHLSCheck = false;

            // End

            async Task<string> UseYTDlP(string url, bool skipDownload, params string[] args)
            {

                var res = "";

                var options = new OptionSet { };

                if (skipDownload == true)
                {
                    options = new OptionSet
                    {
                        DumpSingleJson = true,
                        SkipDownload = true,
                        FlatPlaylist = true,
                        NoHlsUseMpegts = true,
                        HlsSplitDiscontinuity = true,
                        Referer = "https://youtube.com",
                        HlsPreferNative = true
                    };

                    var skiped_res = await ytdlp.RunVideoDataFetch(url, overrideOptions: options);
                    res = skiped_res.Data.ToString();
                }
                else
                {
                    options = new OptionSet
                    {
                        NoHlsUseMpegts = true
                    };

                    var unskiped_res = await ytdlp.RunVideoDataFetch(url, overrideOptions: options);
                    res = unskiped_res.Data.ToString();
                }

                return res;
            }

            async Task<(string manifest, bool isMuxed)> ExtractManifest(string data)
            {
                using var res = JsonDocument.Parse(data);
                if (!res.RootElement.TryGetProperty("formats", out var formats))
                    return ("", false);

                var manifestUrls = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("protocol", out var p) && p.GetString()?.Contains("m3u8") == true &&
                        f.TryGetProperty("ext", out var e) && e.GetString()?.Equals("mp4", StringComparison.OrdinalIgnoreCase) == true &&
                        f.TryGetProperty("vcodec", out var v) && v.GetString()?.StartsWith("avc1") == true)
                    .Select(f => f.GetProperty("manifest_url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();

                const string ua = "com.google.ios.youtube/20.14.2 (iPhone12,1; U; CPU iOS 18_3_2 like Mac OS X; en_US)";
                const string refer = "https://www.youtube.com/";

                if (!skipHLSCheck)
                {
                    foreach (var manifestUrl in manifestUrls)
                    {
                        try
                        {
                            if (await IsForbidden(manifestUrl, ua, refer))
                                continue;

                            var manifestContent = await GetStringAsync(manifestUrl, ua);
                            var manifestLines = manifestContent.Split('\n').ToList();
                            var lines = new List<string>();

                            for (int i = 0; i < manifestLines.Count; i++)
                            {
                                var line = manifestLines[i];
                                if (line.Contains("vp09") || line.Contains("dubbed-auto"))
                                {

                                    i++;
                                    continue;
                                }
                                lines.Add(line);
                            }

                            if (await CheckAudioSegment(lines, manifestUrl, ua, refer))
                                return (string.Join("\n", lines), false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Manifest check failed: {ex.Message}");
                            continue;
                        }
                    }
                }

                var muxed = ExtractMuxedUrl(data);
                return muxed != null ? (muxed, true) : ("", false);
            }

            async Task<bool> IsForbidden(string url, string ua, string refer)
            {
                var headReq = new HttpRequestMessage(HttpMethod.Head, url);
                headReq.Headers.UserAgent.ParseAdd(ua);
                headReq.Headers.Referrer = new Uri(refer);
                var res = await client.SendAsync(headReq);
                return res.StatusCode == System.Net.HttpStatusCode.Forbidden;
            }

            async Task<string> GetStringAsync(string url, string ua)
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd(ua);
                var res = await client.SendAsync(req);
                return await res.Content.ReadAsStringAsync();
            }

            async Task<bool> CheckAudioSegment(List<string> lines, string manifestUrl, string ua, string refer)
            {
                foreach (var line in lines.Where(l => l.StartsWith("#EXT-X-MEDIA") && l.Contains("TYPE=AUDIO")))
                {
                    var match = Regex.Match(line, "URI=\"(.*?)\"");
                    if (!match.Success) continue;

                    var audioUrl = new Uri(new Uri(manifestUrl), match.Groups[1].Value).ToString();
                    var audioContent = await GetStringAsync(audioUrl, ua);
                    var seg = audioContent.Split('\n')
                        .FirstOrDefault(l => !l.StartsWith("#") && 
                                            (l.EndsWith(".ts") || l.EndsWith(".m4s") || l.EndsWith(".seg")));

                    if (seg == null) continue;

                    var segUrl = new Uri(new Uri(audioUrl), seg).ToString();
                    if (!await IsForbidden(segUrl, ua, refer))
                        return true;
                }
                return false;
            }

            string ExtractMuxedUrl(string data)
            {
                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("formats", out var formats))
                    return "";

                var video = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("format_id", out var protocol) &&
                        protocol.GetString()?.Contains("18") == true
                    )
                    .Select(f => f.GetProperty("url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();

                return video.First() ?? "";
            }

            Tuple<string, string, string, string, double, string, Tuple<string, string, string, string, string>> FetchVideoDetails(string data)
            {
                using var res = JsonDocument.Parse(data);

                var title = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("title").GetString());
                var uploader = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("uploader").GetString());

                var id = res.RootElement.GetProperty("id").GetString();
                var thumbnail = $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";

                double duration = 0; // strings are nicer to use ?? with, but this is the only entry that HAS to be a number type
                if (res.RootElement.TryGetProperty("duration", out var durationProp) && durationProp.ValueKind == JsonValueKind.Number)
                {
                    duration = durationProp.GetDouble();
                }

                var channel_id = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("channel_id").GetString());

                var description = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("description").GetString()) ?? "placeholder";

                var view_count = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("view_count").GetInt32().ToString()) ?? "301"; 

                var like_count = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("like_count").GetInt32().ToString()) ?? "1809"; // (Lincon's birthyear)
                
                var category = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("categories")[0].GetString()) ?? System.Security.SecurityElement.Escape("Film & Animation");
            
                var comment_count = "0";

                if (res.RootElement.TryGetProperty("comment_count", out var commentCountProp) &&
                commentCountProp.ValueKind == JsonValueKind.Number)
                {
                comment_count = commentCountProp.GetInt32().ToString();
                }

                comment_count = System.Security.SecurityElement.Escape(comment_count);

                var published = "2013-01-01T00:00:00.000Z";
                if (res.RootElement.TryGetProperty("timestamp", out var timestamp))
                {
                    double timestamp_double = int.Parse(timestamp.ToString());
                    published = DateTimeOffset.UnixEpoch.AddSeconds(timestamp_double).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
                }

                return Tuple.Create(title, uploader, description, thumbnail, duration, channel_id, Tuple.Create(view_count, like_count, category, comment_count, published)); // tuple secptions (7 items max ): )
            }

            app.MapGet("/getURLFinal/{hash}", (string hash) =>
            {
                if (videoDict.TryGetValue(hash, out var value))
                    return Results.Ok(new { video_url = value });

                return Results.NotFound("Video not found");
            }).ExcludeFromDescription();

            app.MapGet("/getvideo/{videoId}", async (string videoId, HttpRequest request) =>
            {
                try
                {
                    string? query = System.Security.SecurityElement.Escape(request.Query["muxed"]);

                    var json = await UseYTDlP($"https://youtube.com/watch?v={videoId}", false, "--dump-json");

                    if (query == "true")
                    {
                        var video_url = ExtractMuxedUrl(json);
                        return Results.Redirect(video_url); // easy as pie
                    }

                    var hls = await ExtractManifest(json);

                    if (string.IsNullOrWhiteSpace(hls.Item1))
                        return Results.NotFound("No stream found");

                    Console.WriteLine();

                    if (hls.Item2 == true)
                        return Results.Redirect(hls.Item1);

                    return Results.Text(hls.Item1, "application/vnd.apple.mpegurl");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });

            app.MapGet("/get_video", async (HttpRequest request) =>
            {
                try
                {

                    string? video_id = System.Security.SecurityElement.Escape(request.Query["video_id"]);

                    string? query = System.Security.SecurityElement.Escape(request.Query["muxed"]);

                    var json = await UseYTDlP($"https://youtube.com/watch?v={video_id}", false, "--dump-json");

                    if (query == "true")
                    {
                        var video_url = ExtractMuxedUrl(json);

                        if (string.IsNullOrWhiteSpace(video_url))
                            return Results.NotFound("No muxed stream found");

                        return Results.Redirect(video_url); // easy as pie
                    }

                    var hls = await ExtractManifest(json);

                    if (string.IsNullOrWhiteSpace(hls.Item1))
                        return Results.NotFound("No stream found");

                    if (hls.Item2 == true)
                        return Results.Redirect(hls.Item1);
                        
                    return Results.Text(hls.Item1, "application/vnd.apple.mpegurl");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });

            app.MapPost("/youtubei/v1/player", async (HttpRequest request) =>
            {

                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                var root = new root(); 

                string? video_id = "";

                // getting the video id from protobuff request

                byte[] bodyBytes; // we need the body as an array of bytes

                using (var ms = new MemoryStream())
                {
                    await request.Body.CopyToAsync(ms);
                    bodyBytes = ms.ToArray();
                }

                var cis = new CodedInputStream(bodyBytes); // still cis tho
                while (!cis.IsAtEnd) // just loop through untuil the seocond item
                {
                    uint tag = cis.ReadTag();
                    if (tag == 0) break;

                    int fieldNumber = (int)(tag >> 3);
                    if (fieldNumber == 2) // second field is the video is in the request (mitmproxy viewed)
                    {
                        video_id = cis.ReadString();
                        break;
                    }
                    else
                    {
                        cis.SkipLastField();
                    }
                    
                }

                // this isn't the 'proper way' but I couldn't find much on the proper way to do it 
                // so this works since we just need the video id really

                if (String.IsNullOrEmpty(video_id))
                {
                    // this is just to make it easy to make POST requests for debugging
                    // I do not think any YouTube clients ever used ?video_id= in this endpoint 
                    if (!String.IsNullOrEmpty(System.Security.SecurityElement.Escape(request.Query["video_id"])))
                    {
                        video_id = System.Security.SecurityElement.Escape(request.Query["video_id"]);
                    }
                    else
                    {
                        return Results.StatusCode(418);
                    }
                }

                // end

                // ints 

                var playbackInts = new root.Types.playbackInts
                {
                    Int1 = 0,
                    Int9 = 0,
                    Str31 = "CAESAggB"
                };

                root.PInts.Add(playbackInts);

                // end 

                // formats

                var formats = new root.Types.playerFormats
                {
                    SomeInt = 21540 // idk what this means, doesn't matter much idk (perhaps bitrate /: )
                };

                var format360p = new root.Types.playerFormats.Types.format
                {
                    FormatId = 18,
                    Url = $"{base_url}/get_video?video_id={video_id}&muxed=true",
                    MimeType = "video/mp4",
                    VideoQuality = "360p"
                };

                formats.NondashFormat.Add(format360p); // non dash = muxed (audio/video one file)

                root.Formats.Add(formats);


                // metadata

                var metadata = new root.Types.metadata
                {
                    Id = video_id,
                    Title = "Placeholder",
                    ChannelId = "8675304",
                    Description = "Placeholder",
                    ViewCount = "301",
                    AuthorName = "Jane Doe"
                };

                var thumbnailList = new root.Types.metadata.Types.thumbList { };

                var thumbnail = new root.Types.metadata.Types.thumbList.Types.thumb
                {
                    Url = $"https://i.ytimg.com/vi/{video_id}/hqdefault.jpg",
                    Width = 480,
                    Height = 360
                };

                thumbnailList.Thumbnail.Add(thumbnail);

                metadata.Thumbnails.Add(thumbnailList);

                root.VideoMetadata.Add(metadata);

                // end

                // misc

                root.PbVarious69 = "CAA%3D";

                // end
            
                return Results.File(root.ToByteArray(), "application/octet-stream");
            });

            app.MapGet("/feeds/api/videos/{id}", async (string id, HttpRequest request) =>
            {
                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                    // janky fix for android rel vids
                    if (id.StartsWith("%24", StringComparison.OrdinalIgnoreCase))
                        id = id.Substring(3);
                        
                    if (id.StartsWith("$"))
                        id = id.Substring(1);
        
                    var json = await UseYTDlP($"https://youtube.com/watch?v={id}", true, "--dump-json");

                    var data = Tuple.Create("", "", "", "", (double) 0, "", Tuple.Create("", "", "", "", ""));

                    try{
                        data = FetchVideoDetails(json);
                    } catch(Exception ex) {
                        Console.WriteLine("\nException For /feed/api/videos/{id}" + ex);
                        return Results.StatusCode(500);
                    }
                   
                    var template = $@"<?xml version='1.0' encoding='UTF-8'?>
                    <entry xmlns:yt='http://www.youtube.com/xml/schemas/2007' 
                        xmlns:media='http://search.yahoo.com/mrss/' 
                        xmlns:gd='http://schemas.google.com/g/2005'>
                      	<id>{base_url}/feeds/api/videos/{id}</id>
                        <published>{data.Item7.Item5}</published>
                        <updated>{data.Item7.Item5}</updated>
                        <category scheme='http://gdata.youtube.com/schemas/2007/categories.cat' label='{data.Item7.Item3}' term='{data.Item7.Item3}'>{data.Item7.Item3}</category>
                        <title type='text'>{data.Item1}</title>
                        <content type='text'>{data.Item3}</content>
                        <link rel='http://gdata.youtube.com/schemas/2007#video.related' href='{base_url}/feeds/api/videos/{id}/related'/>
                        <author>
                            <name>{SecurityElement.Escape(data.Item2)}</name>
                            <uri>{base_url}/feeds/api/users/{data.Item6}</uri>
                            <yt:userId>{data.Item6}</yt:userId>
                        </author>
                        <gd:comments>
                            <gd:feedLink href='{base_url}/feeds/api/videos/{id}/comments' countHint='{data.Item7.Item4}'/>
                        </gd:comments>
                        <media:group>
                            <media:category label='{data.Item7.Item3}' scheme='http://gdata.youtube.com/schemas/2007/categories.cat'>{data.Item7.Item3}</media:category>
                            <media:thumbnail yt:name='hqdefault' url='http://i.ytimg.com/vi/{id}/hqdefault.jpg' height='240' width='320' time='00:00:00'/>
                            <media:thumbnail yt:name='poster' url='http://i.ytimg.com/vi/{id}/0.jpg' height='240' width='320' time='00:00:00'/>
                            <media:thumbnail yt:name='default' url='http://i.ytimg.com/vi/{id}/0.jpg' height='240' width='320' time='00:00:00'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/mp4' medium='video' isDefault='true' expression='full' duration='{data.Item5}' yt:format='3'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{data.Item5}' yt:format='2'/>
                            <media:content url='{base_url}/getvideo/{id}?muxed=true' type='video/mp4' medium='video' expression='full' duration='{data.Item5}' yt:format='5'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/mp4' medium='video' expression='full' duration='{data.Item5}' yt:format='8'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{data.Item5}' yt:format='9'/>
                            <media:description type='plain'>{data.Item3}</media:description>
                            <media:keywords>ben</media:keywords>
                            <media:player url='http://www.youtube.com/watch?v={id}'/>
                            <media:credit role='uploader' name='{data.Item6}'>{data.Item6}</media:credit>
                            <yt:duration seconds='{data.Item5}'/>
                            <yt:videoid>${id}</yt:videoid>
                            <yt:userId>{data.Item6}</yt:userId>
                            <yt:uploaderId>{data.Item6}</yt:uploaderId>
                        </media:group>
                        <gd:rating average='5' max='5' min='1' numRaters='611860' rel='http://schemas.google.com/g/2005#overall'/>
                        <yt:statistics favoriteCount='{data.Item7.Item2}' viewCount='{data.Item7.Item1}'/>
                        <yt:rating numLikes='{data.Item7.Item2}' numDislikes='12'/>
                    </entry>
                    ";

                    return Results.Content(template, "application/xml"); // broken on firefox 
            });

        }
    }
    public static class Extensions
    {
        public static TResult Let<T, TResult>(this T input, Func<T, TResult> func) => func(input);
    }
}