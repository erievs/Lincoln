using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

#pragma warning disable CS8603 

namespace Lincon
{

    public class YouTubeSession
    {
        public required string DeviceId { get; set; } // this will be the same DeviceId as created in device registration
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
        public bool IsLinked { get; set; }
    }

    // pretty much what we do here is
    // link an access/refresh token to a device id

    public static class AndroidLogin
    {
        // very insecure!
        private static readonly string FilePath = Path.Combine("assets", "tokens.json");

        private const string CLIENT_ID = "861556708454-d6dlm3lh05idd8npek18k6be8ba3oc68.apps.googleusercontent.com";
        private const string CLIENT_SECRET = "SboVhoG9s0rNafixCSGGKXAT";
        private const string TOKEN_REFRESH_URL = "https://oauth2.googleapis.com/token";

        private static readonly HttpClient HttpClient = new HttpClient();

        private static void Save(List<YouTubeSession> sessions)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public static async Task<bool> ValidateAccessTokenAsync(string token)
        {
            var payload = new
            {
                context = new
                {
                    client = new
                    {
                        clientName = "TVHTML5",
                        clientVersion = "7.20250205.16.00",
                        hl = "en",
                        gl = "US"
                    }
                }
            };


            // this point seems to work alr
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/guide");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(payload);

            var response = await HttpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("responseContext", out var respContext) &&
                respContext.TryGetProperty("serviceTrackingParams", out var serviceParams) &&
                serviceParams.ValueKind == JsonValueKind.Array &&
                serviceParams.GetArrayLength() > 0)
            {
                var firstParams = serviceParams[0];
                if (firstParams.TryGetProperty("params", out var paramArray))
                {
                    foreach (var param in paramArray.EnumerateArray())
                    {
                        if (param.TryGetProperty("key", out var key) && key.GetString() == "logged_in" &&
                            param.TryGetProperty("value", out var value))
                        {
                            var loggedInValue = value.GetString();
                            return loggedInValue == "1"; // learned this trick back in 2016Tube
                        }
                    }
                    Console.WriteLine("Did not find 'logged_in' key in params.");
                }
                else
                {
                    Console.WriteLine("'params' property not found in first serviceTrackingParams element.");
                }
            }
            else
            {
                Console.WriteLine("'responseContext' or 'serviceTrackingParams' missing or malformed in JSON.");
            }

            Console.WriteLine("Access token validation returning false.");
            return false;
        }

        public static async Task<Tuple<string, string, string>?> GetLoggedInAccountInfoAsync(string accessToken)
        {
            var payload = new
            {
                context = new
                {
                    client = new
                    {
                        clientName = "TVHTML5",
                        clientVersion = "7.20250205.16.00",
                        hl = "en",
                        gl = "US"
                    }
                }
            };

            // this has the benfit of coming with a userhandle so you don't need two seprete request!
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/account/accounts_list?key=AIzaSyA-4WJ3XXXXXXXfA3VGk4U0QXXXXXXXqfgw");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(payload);

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }

                    var doc = JsonDocument.Parse(responseBody);

                    // weclome to the hell known as innertube

                    if (!doc.RootElement.TryGetProperty("contents", out var contents) || contents.GetArrayLength() == 0)
                    {
                        return null;
                    }

                    var firstContent = contents[0];

                    if (!firstContent.TryGetProperty("accountSectionListRenderer", out var accountSection))
                    {
                        return null;
                    }

                    if (!accountSection.TryGetProperty("contents", out var sectionContents) || sectionContents.GetArrayLength() == 0)
                    {
                        return null;
                    }

                    var firstSectionContent = sectionContents[0];

                    if (!firstSectionContent.TryGetProperty("accountItemSectionRenderer", out var accountItemSection))
                    {
                        return null;
                    }

                    if (!accountItemSection.TryGetProperty("contents", out var itemSectionContents) || itemSectionContents.GetArrayLength() == 0)
                    {
                        return null;
                    }

                    var firstItemContent = itemSectionContents[0];

                    if (!firstItemContent.TryGetProperty("accountItem", out var account))
                    {
                        return null;
                    }

                    string username = "";
                    string handle = "";
                    string pfpUrl = "";

                    if (account.TryGetProperty("accountName", out var accountNameProp) &&
                        accountNameProp.TryGetProperty("simpleText", out var usernameProp))
                    {
                        username = usernameProp.GetString() ?? "";
                    }
                    else
                    {
                        Console.WriteLine("Missing 'accountName.simpleText'.");
                    }

                    if (account.TryGetProperty("channelHandle", out var handleProp) &&
                        handleProp.TryGetProperty("simpleText", out var handleTextProp))
                    {
                        handle = handleTextProp.GetString() ?? ""; // I prefer browseids, but this is easier (and should work the same)
                    }
                    else
                    {
                        Console.WriteLine("Missing 'channelHandle.simpleText'.");
                    }

                    if (account.TryGetProperty("accountPhoto", out var photoProp) &&
                        photoProp.TryGetProperty("thumbnails", out var thumbnails) &&
                        thumbnails.GetArrayLength() > 0)
                    {
                        pfpUrl = thumbnails[0].GetProperty("url").GetString() ?? "";
                    }
                    else
                    {
                        Console.WriteLine("Missing 'accountPhoto.thumbnails[0].url'.");
                    }

                    // exit the hell known as innertube

                    return Tuple.Create(username, handle, pfpUrl);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching account info: " + ex);
                return null;
            }
        }

        public static async Task<string?> RefreshAccessTokenAsync(string refreshToken)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", CLIENT_ID),
                new KeyValuePair<string, string>("client_secret", CLIENT_SECRET),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            });

            var response = await HttpClient.PostAsync(TOKEN_REFRESH_URL, data);
            if (!response.IsSuccessStatusCode)
                return null;

            var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var newToken))
                return newToken.GetString();

            return null;
        }

        public static async Task<string?> GetValidAccessTokenAsync(string? deviceId)
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                var json = await File.ReadAllTextAsync(FilePath);
                var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new();

                var session = sessions.Find(s => s.DeviceId == deviceId && s.IsLinked);
                if (session == null)
                    return null;

                if (!string.IsNullOrEmpty(session.AccessToken))
                {
                    var isValid = await ValidateAccessTokenAsync(session.AccessToken);
                    if (isValid)
                        return session.AccessToken;
                }

                if (!string.IsNullOrEmpty(session.RefreshToken))
                {
                    var newToken = await RefreshAccessTokenAsync(session.RefreshToken);
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        session.AccessToken = newToken;
                        Save(sessions);
                        return newToken;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
        public static bool IsDeviceLinked(string deviceId) // so you can hide the sign in video
        {
            try
            {
                if (!File.Exists(FilePath))
                    return false;

                var json = File.ReadAllText(FilePath);
                var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new List<YouTubeSession>();

                var session = sessions.Find(s => s.DeviceId == deviceId);
                if (session == null)
                    return false;

                return session.IsLinked;
            }
            catch
            {
                return false;
            }
        }
        public static void HandleStuff(WebApplication app)
        {

            app.MapPost("/link_device_token", async (HttpRequest req) =>
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(req.Body);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("device_id", out var deviceIdProp) || string.IsNullOrEmpty(deviceIdProp.GetString()))
                    {
                        Console.WriteLine("Missing device_id in request body.");
                        return Results.BadRequest("Missing device_id");
                    }

                    string deviceId = deviceIdProp.GetString()!;

                    string accessToken = root.TryGetProperty("access_token", out var accessTokenProp) ? accessTokenProp.GetString() ?? "" : "";
                    string refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenProp) ? refreshTokenProp.GetString() ?? "" : "";

                    Console.WriteLine($"Received device_id: {deviceId}");
                    Console.WriteLine($"Received access_token: {accessToken}");
                    Console.WriteLine($"Received refresh_token: {refreshToken}");

                    List<YouTubeSession> sessions; // idk why i called em sessions, probbaly accounts would be better (since they are fairly perm)

                    if (File.Exists(AndroidLogin.FilePath))
                    {
                        try
                        {
                            var existingJson = await File.ReadAllTextAsync(AndroidLogin.FilePath);
                            sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(existingJson) ?? new List<YouTubeSession>();
                        }
                        catch
                        {
                            sessions = new List<YouTubeSession>();
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(AndroidLogin.FilePath)!);
                        File.WriteAllText(AndroidLogin.FilePath, "[]");
                        sessions = new List<YouTubeSession>();
                    }

                    var existingSession = sessions.Find(s => s.DeviceId == deviceId);

                    if (existingSession != null)
                    {
                        existingSession.AccessToken = accessToken;
                        existingSession.RefreshToken = refreshToken;
                        existingSession.IsLinked = await AndroidLogin.ValidateAccessTokenAsync(accessToken);
                    }

                    else
                    {
                        var newSession = new YouTubeSession
                        {
                            DeviceId = deviceId,
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            IsLinked = await AndroidLogin.ValidateAccessTokenAsync(accessToken)
                        };
                        sessions.Add(newSession);
                    }

                    var newJson = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(AndroidLogin.FilePath, newJson);

                    return Results.Ok("Device linked");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in /link_device_token: {ex}");
                    return Results.Problem(ex.Message);
                }
            });

            app.MapGet("/register_android", (HttpRequest req) =>
            {
                var deviceId = req.Query["device_id"].ToString();
                if (string.IsNullOrEmpty(deviceId))
                    deviceId = "unknown";

                // maybe we should pretty this up
                // at some point 
                // all this does is pulls a YouTube tv token!

                // I idk if it really will update once it makes the    function linkDevice() {{ request
                // but whatever

                var html = $@"  
                    <!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                    <meta charset=""UTF-8"" />
                    
                    <title>Register Android Device</title>
                        <style>
                            #urlSection, #codeSection, #tokenSection, #statusSection {{ display:none; margin-top:10px; }}
                        </style>
                    </head>
                    
                    <body>
                    
                    <h1>Register Android Device</h1>
                   
                    <p>Device ID: <span id=""deviceId"">{deviceId}</span></p>
                   
                    <button id=""startDeviceFlow"">Start Device Authorization</button>
                    
                    <div id=""urlSection"">
                        <p>Go to URL: <a id=""activationLink"" href=""#"" target=""_blank""><span id=""verificationUrl""></span></a></p>
                    </div>

                    <div id=""codeSection"">
                        <p>Your user code: <strong><span id=""userCode""></span></strong></p>
                    </div>

                    <div id=""tokenSection"">
                        <p id=""tokenOutput""></p>
                    </div>

                    <div id=""statusSection"">
                        <p id=""linkStatus""></p>
                    </div>

                    <div id=""responseOutput"" style=""color:red;""></div>

                    <script>

                    const deviceId = '{deviceId}';

                    const clientId = ""861556708454-d6dlm3lh05idd8npek18k6be8ba3oc68.apps.googleusercontent.com"";
                    const clientSecret = ""SboVhoG9s0rNafixCSGGKXAT"";

                    const scope = ""http://gdata.youtube.com"";

                    let deviceCode = """";
                    let accessToken = """";
                    let refreshToken = """";

                    document.getElementById('startDeviceFlow').addEventListener('click', () => {{
                        const xhr = new XMLHttpRequest();
                        xhr.open(""POST"", ""https://oauth2.googleapis.com/device/code"", true);
                        xhr.setRequestHeader(""Content-Type"", ""application/x-www-form-urlencoded"");

                        xhr.onreadystatechange = () => {{
                        if (xhr.readyState === 4) {{
                            if (xhr.status === 200) {{
                                const response = JSON.parse(xhr.responseText);
                                deviceCode = response.device_code;
                                document.getElementById(""verificationUrl"").innerText = response.verification_url;
                                document.getElementById(""activationLink"").href = response.verification_url;
                                document.getElementById(""userCode"").innerText = response.user_code;
                                document.getElementById(""urlSection"").style.display = ""block"";
                                document.getElementById(""codeSection"").style.display = ""block"";
                                document.getElementById(""tokenSection"").style.display = ""block"";
                            pollForToken();
                            }} else {{
                            document.getElementById(""responseOutput"").innerText = ""Error getting device code: "" + xhr.status;
                            }}
                        }}
                        }};

                        const data = `client_id=${{encodeURIComponent(clientId)}}&scope=${{encodeURIComponent(scope)}}`;
                        xhr.send(data);
                    }});

                    function pollForToken() {{
                        const interval = setInterval(() => {{

                            if (!deviceCode) {{
                                clearInterval(interval);
                                return;
                            }}

                            const xhr = new XMLHttpRequest();

                            xhr.open(""POST"", ""https://oauth2.googleapis.com/token"", true);
                            xhr.setRequestHeader(""Content-Type"", ""application/x-www-form-urlencoded"");

                            xhr.onreadystatechange = () => {{
                                if (xhr.readyState === 4) {{
                                    if (xhr.status === 200) {{
                                        const response = JSON.parse(xhr.responseText);
                                        accessToken = response.access_token;
                                        refreshToken = response.refresh_token || """";
                                        if (accessToken) {{
                                            document.getElementById(""tokenOutput"").innerText = ""Tokens acquired! Linking device ... Close and reopen the YouTube App! (You should be good?)"";
                                            linkDevice();
                                            clearInterval(interval);
                                        }}
                                    }} else if (xhr.status === 400) {{
                                    }} else if (xhr.status === 428) {{
                                    }} else {{
                                        document.getElementById(""tokenOutput"").innerText = ""Error polling token: "" + xhr.status;
                                        clearInterval(interval);
                                    }}
                                }}
                            }};

                            const data = `client_id=${{encodeURIComponent(clientId)}}&client_secret=${{encodeURIComponent(clientSecret)}}&device_code=${{encodeURIComponent(deviceCode)}}&grant_type=urn:ietf:params:oauth:grant-type:device_code`;
                            xhr.send(data);

                        }}, 5000);
                    }}

                    function linkDevice() {{
                        const pollIntervalMs = 3000;
                        let maxAttempts = 20;

                        function attemptLink() {{
                            fetch('/link_device_token', {{
                                method: 'POST',
                                headers: {{ 'Content-Type': 'application/json' }},
                                body: JSON.stringify({{
                                    device_id: deviceId,
                                    access_token: accessToken,
                                    refresh_token: refreshToken
                                }})
                            }})
                            .then(res => {{
                                if(res.ok) return res.text();
                                throw new Error('Failed to link device');
                            }})
                            .then(text => {{
                                if (text === 'Device linked') {{
                                    document.getElementById('linkStatus').innerText = 'Device linked successfully :3';
                                }} else {{
                                    if (maxAttempts-- > 0) {{
                                        setTimeout(attemptLink, pollIntervalMs);
                                    }} else {{
                                        document.getElementById('linkStatus').innerText = 'Failed to confirm device linking.';
                                    }}
                                }}
                            }})
                            .catch(err => {{
                                if (maxAttempts-- > 0) {{
                                    setTimeout(attemptLink, pollIntervalMs);
                                }} else {{
                                    document.getElementById('linkStatus').innerText = 'Error linking device: ' + err.message;
                                }}
                            }});
                        }}

                        attemptLink();
                    }}

                    </script>

                    </body>
                    </html>
                    ";

                return Results.Content(html, "text/html");
            });

        }

    }
}
