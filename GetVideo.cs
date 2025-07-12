using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeDLSharp;

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
                
                Console.WriteLine("Response For Formats " + (formats));

                var combined = new List<string>();

                // video track
                var video = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("protocol", out var protocol) &&
                        protocol.GetString()?.Contains("m3u8") == true &&
                        f.TryGetProperty("ext", out var ext) &&
                        ext.GetString()?.Equals("mp4", StringComparison.OrdinalIgnoreCase) == true &&
                        f.TryGetProperty("format_id", out var formatId) &&
                        formatId.GetString()?.StartsWith("6") == true
                    )
                    .Select(f => f.GetProperty("url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();


                if (video.Count == 0)
                    return "";

                var video_content = await client.GetStringAsync(video.First());
                var video_url = video_content.Split('\n');
                
                Console.WriteLine("\nVideo track " + video.First());

                combined.AddRange(video_url);

                // audio track (doesnt work rn)
                var audio = formats.EnumerateArray()
                    .Where(f =>
                        f.TryGetProperty("protocol", out var protocol) &&
                        protocol.GetString()?.Contains("m3u8") == true &&
                        f.TryGetProperty("ext", out var ext) &&
                        ext.GetString()?.Equals("m4a", StringComparison.OrdinalIgnoreCase) == true &&
                        f.TryGetProperty("format_id", out var formatId) &&
                        formatId.GetString()?.StartsWith("") == true
                    )
                    .Select(f => f.GetProperty("url").GetString())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Distinct()
                    .ToList();


                if (audio.Count > 0) {
                    var audio_content = await client.GetStringAsync(audio.First());
                    var audio_url = video_content.Split('\n');

                    Console.WriteLine("\nAudio track " + video.First());

                    combined.AddRange(audio_url);
                } else {
                    Console.WriteLine("\nWe're working on getting the audio track back! Sorry in the meantime!");
                }

                return string.Join("\n", combined);
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

            app.MapGet("/getvideo/{videoId}", async (string videoId) =>
            {
                try
                {
                    var json = await UseYTDlP($"https://youtube.com/watch?v={videoId}", "--dump-json");
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

}