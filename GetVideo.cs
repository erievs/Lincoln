using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeDLSharp;
using System.Text.RegularExpressions;
using Player;
using Google.Protobuf;
using System.IO;
using System.Web;

#pragma warning disable CS8619 

namespace Lincon
{

    public static class GetVideo
    {

        public static void HandleVideos(WebApplication app)
        {

            YoutubeDL ytdlp = new();
            
            HttpClient client = new();
            Dictionary<string, string> videoDict = [];

            async Task<string> UseYTDlP(string url, params string[] args)
            {

                // todo -> potokens
                var res = await ytdlp.RunVideoDataFetch(url);

                // https://github.com/Bluegrams/YoutubeDLSharp
                // we need to use .Data lol

                return res.Data.ToString();
            }

            async Task<string> ExtractManifest(string data)
            {

                // god I have no idea how syncs work

                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("formats", out var formats))
                    return "";

                Console.WriteLine("(HLS) Response For Formats " + (formats));

                // video (turns out you DO not need to combine em)
                // also it doesn't really matter which one you grab this is just left over code
                // all manifest_url seem to have the same stuff in em, but if it aint broke dont fix
                var video = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("protocol", out var protocol) &&
                        protocol.GetString()?.Contains("m3u8") == true &&
                        f.TryGetProperty("ext", out var ext) &&
                        ext.GetString()?.Equals("mp4", StringComparison.OrdinalIgnoreCase) == true &&
                        f.TryGetProperty("vcodec", out var vCodec) &&
                        vCodec.GetString()?.StartsWith("avc1") == true
                    )
                    .Select(f => f.GetProperty("manifest_url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();


                if (video.Count == 0)
                    return "";

                var video_content = await client.GetStringAsync(video.First());

                video_content = string.Join("\n",
                    video_content
                        .Split('\n')
                        .ToList()
                        .Let(lines =>
                        {
                            var result = new List<string>();
                            for (int i = 0; i < lines.Count; i++)
                            {
                                if (lines[i].Contains("vp09") || lines[i].Contains("vp09"))
                                {
                                    i++;
                                    continue;
                                }

                                result.Add(lines[i]);
                            }

                            return result;
                        })
                );


                Console.WriteLine("\nUrl " + video.First());

                return video_content;
            }

            String ExtractMuxedUrl(string data)
            {
                using var res = JsonDocument.Parse(data);

                if (!res.RootElement.TryGetProperty("formats", out var formats))
                    return "";

                Console.WriteLine("(Muxed) Response For Formats " + (formats));

                var video = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("format_id", out var protocol) &&
                        protocol.GetString()?.Contains("18") == true
                    )
                    .Select(f => f.GetProperty("url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();


                Console.WriteLine("\nVideo Url: " + video.First());

                return video.First() ?? "";
            }

            Tuple<string, string, string, string, double, string, string> FetchVideoDetails(string data)
            {
                using var res = JsonDocument.Parse(data);

                var title = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("title").GetString());
                var uploader = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("uploader").GetString());

                var id = res.RootElement.GetProperty("id").GetString();
                var thumbnail = $"https://i.ytimg.com/vi/{id}/hqdefault.jpg";

                double duration = 0;
                if (res.RootElement.TryGetProperty("duration", out var durationProp) && durationProp.ValueKind == JsonValueKind.Number)
                {
                    duration = durationProp.GetDouble();
                }

                var channel_id = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("channel_id").GetString());

                var view_count = "0";

                var published = res.RootElement.TryGetProperty("published", out var publishedProp) && publishedProp.ValueKind == JsonValueKind.String
                    ? publishedProp.GetString()
                    : "1970-01-01T00:00:00.000Z";

                var description = System.Security.SecurityElement.Escape(res.RootElement.GetProperty("description").GetString()) ?? "placeholder";

                return Tuple.Create(title, uploader, description, thumbnail, duration, channel_id, view_count);
            }

            app.MapGet("/getURLFinal/{hash}", (string hash) =>
            {
                if (videoDict.TryGetValue(hash, out var value))
                    return Results.Ok(new { video_url = value });

                return Results.NotFound("Video not found");
            });

            app.MapGet("/getvideo/{videoId}", async (string videoId, HttpRequest request) =>
            {
                try
                {

                    string? query = System.Security.SecurityElement.Escape(request.Query["muxed"]);

                    var json = await UseYTDlP($"https://youtube.com/watch?v={videoId}", "--dump-json");

                    if (query == "true")
                    {
                        var video_url = ExtractMuxedUrl(json);
                        return Results.Redirect(video_url);
                    }

                    var hls = await ExtractManifest(json);

                    if (string.IsNullOrWhiteSpace(hls))
                        return Results.NotFound("Couldn't find a HLS stream found");

                    return Results.Text(hls, "application/vnd.apple.mpegurl");
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

                    var json = await UseYTDlP($"https://youtube.com/watch?v={video_id}", "--dump-json");

                    Console.WriteLine("\nUgggh " + query);

                    if (query == "true")
                    {
                        var video_url = ExtractMuxedUrl(json);
                        return Results.Redirect(video_url);
                    }

                    var hls = await ExtractManifest(json);

                    if (string.IsNullOrWhiteSpace(hls))
                        return Results.NotFound("Couldn't find a HLS stream found");

                    return Results.Text(hls, "application/vnd.apple.mpegurl");
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

                string? video_id = "";

                byte[] bodyBytes;

                using (var ms = new MemoryStream())
                {
                    await request.Body.CopyToAsync(ms);
                    bodyBytes = ms.ToArray();
                }

                var cis = new CodedInputStream(bodyBytes);

                while (!cis.IsAtEnd)
                {
                    uint tag = cis.ReadTag();
                    if (tag == 0) break;

                    int fieldNumber = (int)(tag >> 3);

                    if (fieldNumber == 2)
                    {

                        video_id = cis.ReadString();
                        break; 
                    }
                    else
                    {

                        cis.SkipLastField();
                    }
                }

                Console.WriteLine($"\nVideo ID: {video_id}");

                if (String.IsNullOrEmpty(video_id))
                {
                    return Results.StatusCode(418);
                }

                var root = new root();

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

                    var json = await UseYTDlP($"https://youtube.com/watch?v={id}", "--dump-json");

                    var data = FetchVideoDetails(json);


                    var template = $@"<?xml version='1.0' encoding='UTF-8'?>
                    <entry xmlns:yt='http://www.youtube.com/xml/schemas/2007' 
                        xmlns:media='http://search.yahoo.com/mrss/' 
                        xmlns:gd='http://schemas.google.com/g/2005'>
                      	<id>{base_url}/feeds/api/videos/{id}</id>
                        <published>2005-04-24T03:31:52.000Z</published>
                        <updated>2005-04-24T03:31:52.000Z</updated>
                        <category scheme='http://gdata.youtube.com/schemas/2007/categories.cat' label='Film &amp; Animation' term='Film &amp; Animation'>Film &amp; Animation</category>
                        <title type='text'>{data.Item1}</title>
                        <content type='text'>{data.Item3}</content>
                        <link rel='http://gdata.youtube.com/schemas/2007#video.related' href='{base_url}/feeds/api/videos/{id}/related'/>
                        <author>
                            <name>{data.Item2}</name>
                            <uri>{base_url}/feeds/api/users/{data.Item6}</uri>
                            <yt:userId>{data.Item6}</yt:userId>
                        </author>
                        <gd:comments>
                            <gd:feedLink href='{base_url}/feeds/api/videos/{id}/comments' countHint='530'/>
                        </gd:comments>
                        <media:group>
                            <media:category label='Film &amp; Animation' scheme='http://gdata.youtube.com/schemas/2007/categories.cat'>Film &amp; Animation</media:category>
                            <media:thumbnail yt:name='hqdefault' url='http://i.ytimg.com/vi/{id}/hqdefault.jpg' height='240' width='320' time='00:00:00'/>
                            <media:thumbnail yt:name='poster' url='http://i.ytimg.com/vi/{id}/0.jpg' height='240' width='320' time='00:00:00'/>
                            <media:thumbnail yt:name='default' url='http://i.ytimg.com/vi/{id}/0.jpg' height='240' width='320' time='00:00:00'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/mp4' medium='video' isDefault='true' expression='full' duration='{data.Item5}' yt:format='3'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{data.Item5}' yt:format='2'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/mp4' medium='video' expression='full' duration='{data.Item5}' yt:format='8'/>
                            <media:content url='{base_url}/getvideo/{id}' type='video/3gpp' medium='video' expression='full' duration='{data.Item5}' yt:format='9'/>
                            <media:description type='plain'>{data.Item3}</media:description>
                            <media:keywords>ben</media:keywords>
                            <media:player url='http://www.youtube.com/watch?v={id}'/>
                            <media:credit role='uploader' name='{data.Item6}'>{data.Item6}</media:credit>
                            <yt:duration seconds='104'/>
                            <yt:videoid>${id}</yt:videoid>
                            <yt:userId>{data.Item6}</yt:userId>
                            <yt:uploaderId>{data.Item6}</yt:uploaderId>
                        </media:group>
                        <gd:rating average='5' max='5' min='1' numRaters='611860' rel='http://schemas.google.com/g/2005#overall'/>
                        <yt:statistics favoriteCount='2447440' viewCount='367116085'/>
                        <yt:rating numLikes='2447440' numDislikes='17890'/>
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