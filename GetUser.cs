using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lincon.Enums;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;


#pragma warning disable CS8602
#pragma warning disable CS8603 
#pragma warning disable CS8604 

namespace Lincon
{

    public static class UserFeeds
    {
        [Obsolete]
        public static void HandleFeed(WebApplication app)
        {

            YoutubeDL ytdlp = new();

            HttpClient client = new();
            Dictionary<string, string> videoDict = [];

            async Task<string> UseYTDlP(string query)
            {
                var options = new OptionSet
                {
                    DumpSingleJson = true,
                    SkipDownload = true,
                    FlatPlaylist = true,
                    PlaylistStart = 1,
                    PlaylistEnd = 1
                };

                var res = await ytdlp.RunVideoDataFetch(query, overrideOptions: options);

                if (res == null || res.Data == null)
                {
                    Console.Error.WriteLine("YoutubeDL returned null or empty data.");
                    return "";
                }

                Console.WriteLine("\nRes:\n" + res.Data);

                return res.Data.ToString();
            }

            Tuple<string, string, string, string, string, string, string> ExtractData(string data, FeedType feedType, HttpRequest request)
            {
                string id = "", channel_name = "", title = "", description = "", channel_follower_count = "", avatar_url = "", banner_url = "";

            
                var res = JsonDocument.Parse(data);
                var root = res.RootElement;

                id = root.GetProperty("id").GetString() ?? "";
                channel_name = root.GetProperty("channel").GetString() ?? "";
                title = root.GetProperty("title").GetString() ?? "";
                description = root.GetProperty("description").GetString() ?? "";
                channel_follower_count = root.GetProperty("channel_follower_count").GetInt32().ToString() ?? "";

                if (root.TryGetProperty("thumbnails", out JsonElement thumbnails) && thumbnails.ValueKind == JsonValueKind.Array && thumbnails.GetArrayLength() > 0)
                {
                    var lastThumbnail = thumbnails[thumbnails.GetArrayLength() - 1];
                    if (lastThumbnail.TryGetProperty("url", out JsonElement urlProp))
                    {
                        avatar_url = urlProp.GetString() ?? "";
                    }

                    var firstThumbnail = thumbnails[0];
                    if (firstThumbnail.TryGetProperty("url", out JsonElement bannerProp))
                    {
                        banner_url = bannerProp.GetString() ?? "";
                    }
                }
                

                return Tuple.Create(id, channel_name, title, avatar_url, description, channel_follower_count, banner_url);
            }

            app.MapGet(@"/feeds/api/users/{channel_id}", async (string channel_id, HttpRequest request) => // needs to be ?q at some point for real hardware
            {
                try
                {

                    if (String.IsNullOrEmpty(channel_id))
                    {
                        return Results.StatusCode(500);
                    }

                    Console.WriteLine("\nChannel ID: " + channel_id);

                    var json = await UseYTDlP($"https://www.youtube.com/channel/{channel_id}");

                    var data = ExtractData(json, FeedType.XML, request);

                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                    var template = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <entry
                        xmlns=""http://www.w3.org/2005/Atom""
                        xmlns:media=""http://search.yahoo.com/mrss/""
                        xmlns:gd=""http://schemas.google.com/g/2005""
                        xmlns:yt=""http://gdata.youtube.com/schemas/2007"">
                        <id>{base_url}/feeds/api/users/{data.Item1}</id>
                        <published>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</published>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#userProfile""/>
                        <title type=""text"">{data.Item2} Channel</title>
                        <content type=""text""></content>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/{data.Item1}""/>
                        <link rel=""alternate"" type=""text/html"" href=""https://www.youtube.com/user/{data.Item1}""/>
                        <author>
                            <name>{data.Item2}</name>
                            <uri>{base_url}/feeds/api/users/{data.Item1}</uri>
                        </author>
                        <yt:age>1</yt:age> 
                        <yt:description></yt:description> 
                        <yt:channelId>{channel_id}</yt:channelId>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.uploads"" href=""{base_url}/feeds/api/users/{data.Item1}/uploads"" countHint=""0""/>
                        <yt:statistics lastWebAccess=""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}"" subscriberCount=""{data.Item6}"" videoWatchCount=""0"" viewCount=""0"" totalUploadViews=""0""/>
                        <gd:feedLink rel='/schemas/2007#channel.content' href='/feeds/api/users/webauditors/uploads?v=2' countHint='0'/>
                        <media:thumbnail url=""{data.Item4}""/>
                        <yt:username>{data.Item2}</yt:username>
                    </entry>";

                    return Results.Content(template, "application/xml"); // broken on firefox 
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });

            app.MapGet(@"/feeds/api/channels/{channel_id}", async (string channel_id, HttpRequest request) => // needs to be ?q at some point for real hardware
             {
                 try
                 {

                     if (String.IsNullOrEmpty(channel_id))
                     {
                         return Results.StatusCode(500);
                     }

                     Console.WriteLine("\nChannel ID: " + channel_id);

                     var json = await UseYTDlP($"https://www.youtube.com/channel/{channel_id}");

                     var data = ExtractData(json, FeedType.XML, request);

                     var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                     var template = $@"<entry gd:etag='W/&quot;Ck8GRH47eCp7I2A9XRdTGEQ.&quot;'>
                    <id>tag:youtube.com,2008:channel:{data.Item1}</id>
                    <updated>{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}</updated>
                    <category scheme='http://schemas.google.com/g/2005#kind' term='/schemas/2007#channel'/>
                    <title>{data.Item2}</title>
                    <summary>{data.Item5}</summary>
                    <link rel='/schemas/2007#featured-video' type='application/atom+xml' href='/feeds/api/videos/YM582qGZHLI?v=2'/>
                    <link rel='alternate' type='text/html' href='https://www.youtube.com/channel/{data.Item1}'/>
                    <link rel='self' type='application/atom+xml' href='/feeds/api/channels/{data.Item1}?v=2'/>
                    <author>
                        <name>{data.Item2}</name>
                        <uri>/feeds/api/users/webauditors</uri>
                        <yt:userId>{data.Item1}</yt:userId>
                    </author>
                    <yt:channelId>{data.Item1}</yt:channelId>
                    <yt:channelStatistics subscriberCount='{data.Item6}' viewCount='0'/>
                    <gd:feedLink rel='/schemas/2007#channel.content' href='/feeds/api/users/webauditors/uploads?v=2' countHint='0'/>
                    <media:thumbnail url='{data.Item4}'/>
                    </entry>";

                     return Results.Content(template, "application/xml"); // broken on firefox 
                 }
                 catch (Exception ex)
                 {
                     Console.Error.WriteLine(ex);
                     return Results.StatusCode(500);
                 }
             });

            app.MapGet(@"/feeds/api/users", async (HttpRequest request) =>
            {
                try
                {
                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                    var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                    string? device_id = null;

                    if (request.Headers.TryGetValue("X-GData-Device", out var deviceHeaderValues))
                    {
                        var deviceHeader = deviceHeaderValues.ToString();
                        var prefix = "device-id=\"";
                        var startIndex = deviceHeader.IndexOf(prefix);
                        if (startIndex >= 0)
                        {
                            startIndex += prefix.Length;
                            var endIndex = deviceHeader.IndexOf("\"", startIndex);
                            if (endIndex > startIndex)
                            {
                                device_id = deviceHeader.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                    else
                    {
                        return Results.Problem("You must link your android device 3:", statusCode: 403);
                    }

                    var access_token = await AndroidLogin.GetValidAccessTokenAsync(device_id);

                    var login_data = await AndroidLogin.GetLoggedInAccountInfoAsync(access_token);

                    // YOU NEED TWO ENTRIES (otherwise the app will crash)

                    var template = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <feed xmlns=""http://www.w3.org/2005/Atom""
                        xmlns:media=""http://search.yahoo.com/mrss/""
                        xmlns:gd=""http://schemas.google.com/g/2005""
                        xmlns:yt=""http://gdata.youtube.com/schemas/2007"">

                    <entry>
                        <id>{base_url}/feeds/api/users/default</id>
                        <published>{now}</published>
                        <updated>{now}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#userProfile""/>
                        <title type=""text"">{login_data.Item1}</title>
                        <content type=""text"">{login_data.Item1} YouTube user profile.</content>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/{login_data.Item2}""/>
                        <link rel=""alternate"" type=""text/html"" href=""https://www.youtube.com/user/{login_data.Item2}""/>
                        <author>
                        <name>{login_data.Item1}</name>
                        <uri>{base_url}/feeds/api/users/default</uri>
                        <email>default@example.com</email>
                        </author>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.uploads"" href=""{base_url}/feeds/api/users/{login_data.Item2}/uploads"" countHint=""0""/>
                        <yt:username>{login_data.Item1}</yt:username>
                        <yt:channelId>UC0000000000000000000000</yt:channelId>
                        <yt:googlePlusUserId>123456789012345678901</yt:googlePlusUserId>
                        <yt:age>1</yt:age>
                        <yt:location>Earth</yt:location>
                        <yt:gender>m</yt:gender>
                        <yt:incomplete>false</yt:incomplete>
                        <yt:eligibleForChannel>true</yt:eligibleForChannel>
                        <yt:statistics lastWebAccess=""{now}"" subscriberCount=""0"" videoWatchCount=""0"" viewCount=""0"" totalUploadViews=""0""/>
                        <media:thumbnail url=""{login_data.Item3}""/>
                        <yt:description>This is a YouTube user.</yt:description>
                    </entry>

                    <entry>
                        <id>{base_url}/feeds/api/users/fallback</id>
                        <published>{now}</published>
                        <updated>{now}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#userProfile""/>
                        <title type=""text"">Fallback</title>
                        <content type=""text"">Second profile to satisfy app.</content>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/fallback""/>
                        <link rel=""alternate"" type=""text/html"" href=""https://www.youtube.com/user/fallback""/>
                        <author>
                        <name>Fallback (Do Not Use)</name>
                        <uri>{base_url}/feeds/api/users/fallback</uri>
                        <email>fallback@example.com</email>
                        </author>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.uploads"" href=""{base_url}/feeds/api/users/fallback/uploads"" countHint=""0""/>
                        <yt:username>fallback</yt:username>
                        <yt:channelId>UC1111111111111111111111</yt:channelId>
                        <yt:googlePlusUserId>987654321098765432109</yt:googlePlusUserId>
                        <yt:age>2</yt:age>
                        <yt:location>Mars</yt:location>
                        <yt:gender>f</yt:gender>
                        <yt:incomplete>false</yt:incomplete>
                        <yt:eligibleForChannel>true</yt:eligibleForChannel>
                        <yt:statistics lastWebAccess=""{now}"" subscriberCount=""1"" videoWatchCount=""1"" viewCount=""1"" totalUploadViews=""1""/>
                        <media:thumbnail url=""https://yt3.ggpht.com/yti/ANjgQV8y24P02td9Sd_Xf1-bVBRdqqm_U00zYGqY6x43YrQ=s108-c-k-c0x00ffffff-no-rj""/>
                        <yt:description>Backup profile to stop crash.</yt:description>
                    </entry>

                    </feed>";

                    return Results.Content(template, "application/xml");

                }

                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return Results.StatusCode(500);
                }
            });

            app.MapGet(@"/feeds/api/users/default", async (HttpRequest request) => // needs to be ?q at some point for real hardware
            {
                try
                {

                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";
                    string? device_id = null;

                    if (request.Headers.TryGetValue("X-GData-Device", out var deviceHeaderValues))
                    {
                        var deviceHeader = deviceHeaderValues.ToString();
                        var prefix = "device-id=\"";
                        var startIndex = deviceHeader.IndexOf(prefix);
                        if (startIndex >= 0)
                        {
                            startIndex += prefix.Length;
                            var endIndex = deviceHeader.IndexOf("\"", startIndex);
                            if (endIndex > startIndex)
                            {
                                device_id = deviceHeader.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                    else
                    {
                        return Results.Problem("You must link your android device", statusCode: 403);
                    }

                    var access_token = await AndroidLogin.GetValidAccessTokenAsync(device_id);

                    Console.WriteLine("\nAccess Token: " + access_token);

                    var login_data = await AndroidLogin.GetLoggedInAccountInfoAsync(access_token);

                    var template = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <entry
                        xmlns=""http://www.w3.org/2005/Atom""
                        xmlns:media=""http://search.yahoo.com/mrss/""
                        xmlns:gd=""http://schemas.google.com/g/2005""
                        xmlns:yt=""http://gdata.youtube.com/schemas/2007"">
                        <id>{base_url}/feeds/api/users/default</id>
                        <published>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</published>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#userProfile""/>
                        <title type=""text"">Default Channel</title>
                        <content type=""text""></content>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/{login_data.Item2}""/>
                        <link rel=""alternate"" type=""text/html"" href=""https://www.youtube.com/user/default""/>
                        <author>
                            <name>{login_data.Item1}</name>
                            <uri>{base_url}/feeds/api/users/default</uri>
                        </author>
                        <yt:age>1</yt:age> 
                        <yt:description></yt:description> 
                        <yt:channelId>{login_data.Item3}</yt:channelId>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.subscriptions"" href=""{base_url}/feeds/api/users/default/subscriptions"" countHint=""0""/>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.contacts"" href=""{base_url}/feeds/api/users/default/contacts"" countHint=""0""/>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.inbox"" href=""{base_url}/feeds/api/users/default/inbox"" countHint=""0""/>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.playlists"" href=""{base_url}/feeds/api/users/default/playlists"" countHint=""0""/>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#user.uploads"" href=""{base_url}/feeds/api/users/default/uploads"" countHint=""0""/>
                        <gd:feedLink rel=""http://gdata.youtube.com/schemas/2007#channel.content"" href=""{base_url}/feeds/api/users/default/uploads?v=2"" countHint=""0""/>
                        <yt:statistics lastWebAccess=""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}"" subscriberCount=""0"" videoWatchCount=""0"" viewCount=""0"" totalUploadViews=""0""/>
                        <media:thumbnail url=""{login_data.Item3}""/>
                        <yt:username>{login_data.Item1}</yt:username>
                    </entry>";


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