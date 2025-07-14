using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeDLSharp;
using System.Text.RegularExpressions;

namespace Lincon
{

    public static class GetVideo
    {

        public static void HandleVideos(WebApplication app)
        {

            YoutubeDL ytdlp = new();

            HttpClient client = new();
            Dictionary<string, string> videoDict = [];


            string ComputeHash(string input)
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

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
                                if (lines[i].Contains("vp09"))
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

            async Task<string> ExtractMuxedUrl(string data)
            {

                // to shut up warning
                await Task.Delay(0); // (GOD I HATE ASYNC)


                // god I have no idea how syncs work
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

                return video.First();
            }

            app.MapPost("/getURL", async (HttpContext ctx, HttpRequest req) =>
            {
                var json = await new StreamReader(req.Body).ReadToEndAsync();

                var res = JsonDocument.Parse(json);

                if (!res.RootElement.TryGetProperty("url", out var urlProp))
                    return Results.BadRequest("Missing 'url'");

                var url = urlProp.GetString();

                Console.WriteLine(url);

                if (ctx.Request.Headers.ContainsKey("HLS-Video"))
                {
                    var output = await UseYTDlP(url!, "--dump-json");
                    var hash = ComputeHash(output);
                    videoDict[hash] = output;

                    _ = Task.Delay(15000).ContinueWith(t =>
                    {
                        videoDict.Remove(hash, out string? ignored);
                    }); // idk we just have to wait 15 secs

                    return Results.Ok(new { url = $"{ctx.Request.Scheme}://{ctx.Request.Host}/getURLFinal/{hash}" });
                }
                else
                {
                    var audioUrl = await UseYTDlP(url!, "-f", "bestaudio", "-g");
                    return Results.Ok(new { url = audioUrl.Trim() });
                }
            });

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
                        var video_url = await ExtractMuxedUrl(json);
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

        }


    }

    public static class Extensions
    {
        public static TResult Let<T, TResult>(this T input, Func<T, TResult> func) => func(input);
    }

}