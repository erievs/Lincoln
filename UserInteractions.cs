using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Namotion.Reflection;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

#pragma warning disable CS8602
#pragma warning disable CS8603 
#pragma warning disable CS8604 
#pragma warning disable CS0618 
namespace Lincon
{
    public static class UserInteractions
    {
        public static void HandleFeed(WebApplication app)
        {
            app.MapPost("/feeds/api/videos/{videoId}/ratings", async (HttpRequest request, string videoId) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(videoId))
                    {

                        var errorXml = new XDocument(
                            new XElement("error",
                                new XElement("message", "Video ID is required and must be a non-empty string.")
                            )
                        ).ToString();

                        return Results.Content(errorXml, "application/xml");
                    }


                    string? device_id = null;

                    device_id = HandleLogin.ExtractDeviceIDFromRequest(request);

                    if (string.IsNullOrEmpty(device_id))
                        return Results.Problem("Invalid device id header", statusCode: 403);


                    var access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);

                    string xmlData;
                    using (var reader = new StreamReader(request.Body))
                        xmlData = await reader.ReadToEndAsync();

                    var xmlDoc = XDocument.Parse(xmlData);
                    XNamespace yt = "http://gdata.youtube.com/schemas/2007";

                    var ratingElement = xmlDoc.Root?.Element(yt + "rating");
                    string? ratingValue = ratingElement?.Attribute("value")?.Value;

                    if (string.IsNullOrWhiteSpace(ratingValue) || !(ratingValue == "like" || ratingValue == "dislike"))
                    {
                        var invalidRatingXml = new XDocument(
                            new XElement("error",
                                new XElement("message", "Invalid rating value. Only \"like\" or \"dislike\" are allowed.")
                            )
                        ).ToString();

                        return Results.Content(invalidRatingXml, "application/xml");
                    }

                    string apiUrl = ratingValue == "like"
                        ? "https://www.youtube.com/youtubei/v1/like/like"
                        : "https://www.youtube.com/youtubei/v1/like/dislike";

                    var requestBody = new
                    {
                        context = new
                        {
                            client = new
                            {
                                clientName = "TVHTML5",
                                clientVersion = "5.20150715",
                                screenWidthPoints = 1632,
                                screenHeightPoints = 904,
                                screenPixelDensity = 1,
                                theme = "CLASSIC",
                                webpSupport = false,
                                acceptRegion = "US",
                                acceptLanguage = "en-US"
                            },
                            user = new { enableSafetyMode = false }
                        },
                        target = new { videoId = videoId }
                    };

                    using (var http = new HttpClient())
                    {
                        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access_token);

                        var ytResponse = await http.PostAsync(apiUrl,
                            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                        string responseBody = await ytResponse.Content.ReadAsStringAsync();

                        if (!ytResponse.IsSuccessStatusCode)
                        {

                            var ytErrorXml = new XDocument(
                                new XElement("error",
                                    new XElement("message", $"YouTube API returned {ytResponse.StatusCode}")
                                )
                            ).ToString();

                            return Results.Content(ytErrorXml, "application/xml");
                        }

                        var successXml = new XDocument(
                            new XElement("response",
                                new XElement("message", $"Video {ratingValue}d successfully!"),
                                new XElement("rawData", responseBody)
                            )
                        ).ToString();

                        return Results.Content(successXml, "application/xml");
                    }
                }
                catch (Exception ex)
                {

                    var catchXml = new XDocument(
                        new XElement("error",
                            new XElement("message", ex.Message ?? "Something went wrong while processing the request.")
                        )
                    ).ToString();

                    return Results.Content(catchXml, "application/xml");
                }
            });

            app.MapPost("/feeds/api/users/default/watch_later", async (HttpRequest request) =>
            {
                try
                {

                    string? device_id = HandleLogin.ExtractDeviceIDFromRequest(request);
                    if (string.IsNullOrEmpty(device_id))
                        return Results.Problem("Invalid device id header", statusCode: 403);

                    var access_token = await HandleLogin.GetValidAccessTokenAsync(device_id);

                    string xmlData;
                    using (var reader = new StreamReader(request.Body))
                        xmlData = await reader.ReadToEndAsync();

                    var xmlDoc = XDocument.Parse(xmlData);
                    var videoId = xmlDoc.Root?.Element(XName.Get("id", "http://www.w3.org/2005/Atom"))?.Value;

                    if (string.IsNullOrWhiteSpace(videoId))
                    {
                        var errorXml = new XDocument(
                            new XElement("error",
                                new XElement("message", "No video ID provided in request.")
                            )
                        ).ToString();
                        return Results.Content(errorXml, "application/xml");
                    }

                    string apiUrl = "https://www.youtube.com/youtubei/v1/browse/edit_playlist?t=XdGgNVt3ejN1trHT";

                    var requestBody = new
                    {
                        context = new
                        {
                            client = new
                            {
                                screenWidthPoints = 1629,
                                screenHeightPoints = 840,
                                utcOffsetMinutes = -240,
                                hl = "en",
                                gl = "US",
                                remoteHost = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                                deviceMake = "Samsung",
                                deviceModel = "SmartTV",
                                visitorData = "CgtmT09nWEZ5cUtEQSj1r6TEBjIKCgJVUxIEGgAgaQ==",
                                userAgent = "Mozilla/5.0 (SMART-TV; Linux; Tizen 5.0) AppleWebKit/538.1 (KHTML, like Gecko) Version/5.0 NativeTVAds Safari/538.1,gzip(gfe)",
                                clientName = "TVHTML5",
                                clientVersion = "7.20250723.13.00",
                                osName = "Tizen",
                                osVersion = "5.0",
                                originalUrl = "https://www.youtube.com/tv",
                                theme = "CLASSIC",
                                platform = "TV",
                                clientFormFactor = "UNKNOWN_FORM_FACTOR",
                                webpSupport = false,
                                userInterfaceTheme = "USER_INTERFACE_THEME_DARK",
                                timeZone = "America/New_York",
                                browserName = "Safari",
                                browserVersion = "5.0",
                                acceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                            },
                            user = new { enableSafetyMode = false },
                            request = new
                            {
                                internalExperimentFlags = new object[] { },
                                consistencyTokenJars = new object[] { }
                            },
                            clickTracking = new
                            {
                                clickTrackingParams = "CAAQisQGIhMI6O3Cxt7ijgMV-xsvCB3DNSJV"
                            }
                        },
                        playlistId = "WL",
                        actions = new object[]
                        {
                                    new { addedVideoId = videoId, action = "ACTION_ADD_VIDEO" }
                        }
                    };

                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

                    var ytResponse = await http.PostAsync(apiUrl,
                        new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                    var ytResponseBody = await ytResponse.Content.ReadAsStringAsync();

                    if (!ytResponse.IsSuccessStatusCode)
                    {
                        var ytErrorXml = new XDocument(
                            new XElement("error",
                                new XElement("message", $"YouTube API returned {ytResponse.StatusCode}")
                            )
                        ).ToString();

                        return Results.Content(ytErrorXml, "application/xml");
                    }

                    XNamespace yt = "http://gdata.youtube.com/schemas/2007";

                    var successXml = new XDocument(
                        new XElement("entry",
                            new XAttribute(XNamespace.Xmlns + "yt", yt),
                            new XElement(yt + "status", "success"),
                            new XElement(yt + "videoId", videoId)
                        )
                    ).ToString();

                    return Results.Content(successXml, "application/atom+xml");
                }
                catch (Exception ex)
                {
                    var catchXml = new XDocument(
                        new XElement("error",
                            new XElement("message", ex.Message ?? "Something went wrong.")
                        )
                    ).ToString();

                    return Results.Content(catchXml, "application/xml");
                }
            });


            // to shut it up (idk how to add history atm but this was clogglibng logs up)
            app.MapPost("/feeds/api/users/default/watch_history", (HttpRequest request) =>
            {

                XNamespace yt = "http://gdata.youtube.com/schemas/2007";

                var successXml = new XDocument(
                    new XElement("entry",
                        new XAttribute(XNamespace.Xmlns + "yt", yt),
                        new XElement(yt + "status", "success"),
                        new XElement(yt + "videoId", "videoId")
                    )
                ).ToString();

                return Results.Content(successXml, "application/atom+xml");

            });


        }

    }
}