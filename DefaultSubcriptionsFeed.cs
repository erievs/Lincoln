using System.Net.Http.Headers;
using System.Text.Json;
using System.Security;
using Lincon;
using System.Threading.Tasks;

#pragma warning disable CS8600 
#pragma warning disable CS8604
#pragma warning disable CS0168 
namespace Lincon
{
    public static class DefaultSubscriptionsFeed
    {
        public static void HandleFeed(WebApplication app)
        {
            HttpClient client = new();

            async Task<string> UseInnerTube(string? accessToken)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/browse?key=AIzaSyDCU8hByM-4DrUqRUYnGn-3llEO78bcxq8";

                var payload = new
                {
                    context = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            deviceMake = "Samsung",
                            deviceModel = "SmartTV",
                            userAgent = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.0) AppleWebKit/538.1 (KHTML, like Gecko) Version/5.0 NativeTVAds Safari/538.1,gzip(gfe)",
                            clientName = "TVHTML5",
                            clientVersion = "7.20250209.19.00",
                            osName = "Tizen",
                            osVersion = "5.0",
                            platform = "TV",
                            clientFormFactor = "UNKNOWN_FORM_FACTOR",
                            screenPixelDensity = 1
                        }
                    },
                    browseId = "FEsubscriptions"
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(payload);

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API request failed with status {response.StatusCode}");

                return json;
            }

            async Task<string> SendContinuationRequest(string continuation, string accessToken)
            {
                var endpoint = "https://www.googleapis.com/youtubei/v1/browse";

                var payload = new
                {
                    context = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            deviceMake = "Samsung",
                            deviceModel = "SmartTV",
                            userAgent = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.0) AppleWebKit/538.1 (KHTML, like Gecko) Version/5.0 NativeTVAds Safari/538.1,gzip(gfe)",
                            clientName = "TVHTML5",
                            clientVersion = "7.20250209.19.00",
                            osName = "Tizen",
                            osVersion = "5.0",
                            platform = "TV",
                            clientFormFactor = "UNKNOWN_FORM_FACTOR",
                            screenPixelDensity = 1
                        }
                    },
                    continuation = continuation
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                request.Content = JsonContent.Create(payload);

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API continuation request failed with status {response.StatusCode}");

                return json;
            }

            async Task<Tuple<string, int>> ExtractData(string json, string access_token, HttpRequest request)
            {

                using var doc = JsonDocument.Parse(json);
                var combined = new List<string>();
                int count = 0;
                var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                // the FEsubscriptions feed is layed out like this
                // one sidebar with your channels
                // next to it videos (normal tileRenders)

                // you can get "all", which is helpful for another feed

                // each channel is a tab (and all is kinda a channel)

                // for the newsubfeed or whatever that is called 
                // we just select [0] and cycle through tab renders!

                // again innertube is formated in a way to help render the app
                // so the names are pretty straight foward, and by looking at the site
                // you get a clear picture of what is what

                JsonElement tabsRoot;
                try
                {
                    tabsRoot = doc.RootElement
                        .GetProperty("contents")
                        .GetProperty("tvBrowseRenderer")
                        .GetProperty("content")
                        .GetProperty("tvSecondaryNavRenderer")
                        .GetProperty("sections")[0]
                        .GetProperty("tvSecondaryNavSectionRenderer")
                        .GetProperty("tabs");
                }
                catch
                {
                    return Tuple.Create("", 0);
                }

                foreach (var tab in tabsRoot.EnumerateArray())
                {
                    if (!tab.TryGetProperty("tabRenderer", out var tabRenderer))
                        continue;

                    if (count >= 20) 
                        break;

                
                    var username = SecurityElement.Escape(tabRenderer.GetProperty("title").GetString() ?? "Unknown");

                    string thumbnail = "https://s.ytimg.com/yt/img/no_videos_140-vfl5AhOQY.png";
                    if (tabRenderer.TryGetProperty("thumbnail", out var thumb)
                        && thumb.TryGetProperty("thumbnails", out var thumbs)
                        && thumbs.GetArrayLength() > 0)
                    {
                        var lastThumb = thumbs[thumbs.GetArrayLength() - 1];
                        if (lastThumb.TryGetProperty("url", out var url))
                        {
                            thumbnail = "https:" + url.GetString();
                        }
                    }

                    string browseId = "Unknown";

                    // sadly we must make a continuation request to grab a browseId for each channel (sucks) but is needed
                    var continuationToken = tabRenderer
                        .GetProperty("content")
                        .GetProperty("tvSurfaceContentRenderer")
                        .GetProperty("continuation")
                        .GetProperty("reloadContinuationData")
                        .GetProperty("continuation")
                        .GetString();
                 
                    try // since the first tab is "All" and has no browseid or continuation, it'll error 500 if we don't wrap in try and catch (we check if the browse id after here)
                    {
                        if (!string.IsNullOrEmpty(continuationToken))
                        {
                            var res = await SendContinuationRequest(continuationToken, access_token);

                            using var con = JsonDocument.Parse(res);

                            // we just need the first video 
                            // if the user has no uploads it doesn't really matter since uploads won't need to be soon anyways
                            var firstVideoItem = con.RootElement
                                .GetProperty("continuationContents")
                                .GetProperty("tvSurfaceContentContinuation")
                                .GetProperty("content")
                                .GetProperty("gridRenderer")
                                .GetProperty("items")[0];

                            if (firstVideoItem.TryGetProperty("tileRenderer", out var tileRenderer))
                            {

                                if (tileRenderer.TryGetProperty("onLongPressCommand", out var onLongPressCommand))
                                {

                                    if (onLongPressCommand.TryGetProperty("showMenuCommand", out var showMenuCommand))
                                    {
                                        if (showMenuCommand.TryGetProperty("menu", out var menu))
                                        {
                                            if (menu.TryGetProperty("menuRenderer", out var menuRenderer))
                                            {
                                                foreach (var menuItem in menuRenderer.GetProperty("items").EnumerateArray())
                                                {
                                                    if (menuItem.TryGetProperty("menuNavigationItemRenderer", out var menuNavigationItemRenderer))
                                                    {

                                                        var text = menuNavigationItemRenderer
                                                            .GetProperty("text")
                                                            .GetProperty("runs")[0]
                                                            .GetProperty("text")
                                                            .GetString();

                                                        // since it is a list of items (that I think changes sometimes)
                                                        // it is better to just loop and check if it says channel rather than direct index like [0]
                                                        if (text == "Go to channel")
                                                        {
                                                            browseId = menuNavigationItemRenderer
                                                            .GetProperty("navigationEndpoint")
                                                            .GetProperty("browseEndpoint")
                                                            .GetProperty("browseId")
                                                            .GetString();

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } catch (Exception ex) {}

                    if (string.IsNullOrEmpty(browseId) || browseId == "Unknown"){} // we must ignore channels with a bad id
                    else
                    {   
                        // this is a channel entry, I think it is used in one or two other feeds, they're shorter than videos.
                        string item = $"""
                        <entry gd:etag='W/"DU4DRX47eCp7ImA9WB9RFEU."'>
                            <id>tag:youtube.com,2008:user:{browseId}:subscription:{Guid.NewGuid()}</id>
                            <published>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</published>
                            <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                            <app:edited xmlns:app='http://www.w3.org/2007/app'>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</app:edited>
                            <category scheme='http://gdata.youtube.com/schemas/2007/subscriptiontypes.cat' term='channel'/>
                            <category scheme='http://schemas.google.com/g/2005#kind' term='http://gdata.youtube.com/schemas/2007#subscription'/>
                            <title>Videos published by : {username}</title>
                            <content type='application/atom+xml;type=feed' src='{base_url}/feeds/api/users/{browseId}/uploads?v=2'/>
                            <link rel='related' type='application/atom+xml' href='{base_url}/feeds/api/users/{browseId}?v=2'/>
                            <link rel='alternate' type='text/html' href='{base_url}/profile?user={username}#p/u'/>
                            <link rel='self' type='application/atom+xml' href='{base_url}/feeds/api/users/{browseId}/subscriptions?v=2'/>
                            <link rel='edit' type='application/atom+xml' href='{base_url}/feeds/api/users/{browseId}/subscriptions?v=2'/>
                            <author>
                                <name>{username}</name>
                                <uri>{base_url}/feeds/api/users/{browseId}</uri>
                                <yt:userId>{browseId}</yt:userId>
                            </author>
                            <yt:channelId>{browseId}</yt:channelId>
                            <media:thumbnail url='{thumbnail}'/>
                            <yt:username display='{username}'>{username}</yt:username>
                        </entry>
                        """;

                        combined.Add(item);
                        count++;
                    }

                }

                return Tuple.Create(string.Join("\n", combined), count);
            }

            app.MapGet("/feeds/api/users/default/subscriptions", async (HttpRequest request) =>
            {
                try
                {

                    string? device_id = null;

                    device_id = HandleLogin.ExtractDeviceIDFromRequest(request);

                    if (string.IsNullOrEmpty(device_id))
                        return Results.Problem("Invalid device id header", statusCode: 403);

                    var access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);

                    var json = await UseInnerTube(access_token);

                    var data = await ExtractData(json, access_token, request);


                    var base_url = $"{request.Scheme}://{request.Host}{request.PathBase}";

                    var template = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <feed
                        xmlns=""http://www.w3.org/2005/Atom""
                        xmlns:openSearch=""http://a9.com/-/spec/opensearchrss/1.0/""
                        xmlns:gd=""http://schemas.google.com/g/2005""
                        xmlns:yt=""http://gdata.youtube.com/schemas/2007"">
                        <id>{base_url}/feeds/api/users/default/subscriptions</id>
                        <updated>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</updated>
                        <category scheme=""http://schemas.google.com/g/2005#kind"" term=""http://gdata.youtube.com/schemas/2007#subscription""/>
                        <title type=""text"">Subscriptions feed</title>
                        <logo>http://www.youtube.com/img/pic_youtubelogo_123x63.gif</logo>
                        <link rel=""related"" type=""application/atom+xml"" href=""{base_url}feeds/api/users/default""/>
                        <link rel=""alternate"" type=""text/html"" href=""http://www.youtube.com/profile?user=default&amp;view=subscriptions""/>
                        <link rel=""http://schemas.google.com/g/2005#feed"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/default/subscriptions""/>
                        <link rel=""http://schemas.google.com/g/2005#batch"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/default/subscriptions/batch""/>
                        <link rel=""self"" type=""application/atom+xml"" href=""{base_url}/feeds/api/users/default/subscriptions?start-index=1&amp;max-results={data.Item2}""/>
                        <generator version=""2.1"" uri=""http://gdata.youtube.com"">YouTube data API</generator>
                        <openSearch:totalResults>{data.Item2}</openSearch:totalResults>
                        <openSearch:startIndex>1</openSearch:startIndex>
                        <openSearch:itemsPerPage>{data.Item2}</openSearch:itemsPerPage>
                        {data.Item1 ?? ""}
                    </feed>";

                    return Results.Content(template, "application/atom+xml");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n" + ex);

                    return Results.StatusCode(500);
                }
            });
        }

    }
}