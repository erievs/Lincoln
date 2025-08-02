using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

#pragma warning disable CS8602
#pragma warning disable CS8603 
#pragma warning disable CS8604

namespace Lincon
{
    // modals
    public class YouTubeSession
    {
        public required string DeviceId { get; set; } // this will be the same DeviceId as created in device registration

        public required string Username { get; set; } // for client login
        public required string Password { get; set; } // yep for client login

        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }

        public bool IsLinked { get; set; }
    }

    public class IsUsernameTakeResult {
        public required bool Status { get; set; }
    }

    public class OAuth2TokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public required string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public required int ExpiresIn { get; set; } 

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; set; } 
    }
    
    public class OAuth2UserInfoResponse
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }

        [JsonPropertyName("verified_email")]
        public required bool VerifiedEmail { get; set; }
    }
    
    // end

    public static class HandleLogin
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

            // what we do here is we just check if the token is still valid
            // by making a request to guide and checking it's loggedInState

            // if it fails we refresh and save

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

            var res = await HttpClient.SendAsync(request);

            var res_body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                return false;
            }

            var doc = JsonDocument.Parse(res_body);

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

                    Console.WriteLine("\nDoc: " + doc.RootElement);

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

                Console.WriteLine($"\nDevice Session: " + JsonSerializer.Serialize(session).ToString()); // may need to disable this if you're running a public server (PLEASE DONT RUN A PUBLIC SERVER WITH LOGIN)

                if (!string.IsNullOrEmpty(session.AccessToken))
                {
                    var isValid = await ValidateAccessTokenAsync(session.AccessToken);
                    if (isValid)
                        return session.AccessToken;
                    else
                        Console.WriteLine("Opps!");
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
        public static async Task<(string, string)> GetValidLoginDataAsync(string? username, string? password)
        {
            try
            {
                if (!File.Exists(FilePath))
                    return ("lincoln", "lincoln");

                var json = await File.ReadAllTextAsync(FilePath);
                var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new();

                var session = sessions.Find(s => s.Username == username && s.Password == password && s.IsLinked);

                if (session == null)
                    return ("lincoln", "lincoln");

                Console.WriteLine($"\nDevice Session: " + JsonSerializer.Serialize(session).ToString()); // may need to disable this if you're running a public server (PLEASE DONT RUN A PUBLIC SERVER WITH LOGIN)

                if (!string.IsNullOrEmpty(session.AccessToken))
                {
                    var isValid = await ValidateAccessTokenAsync(session.AccessToken);
                    if (isValid)
                        return (session.DeviceId, session.AccessToken);
                    else
                        Console.WriteLine("Opps!");
                }

                if (!string.IsNullOrEmpty(session.RefreshToken))
                {
                    var newToken = await RefreshAccessTokenAsync(session.RefreshToken);
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        session.AccessToken = newToken;
                        Save(sessions);
                        return (session.DeviceId, session.AccessToken);
                    }
                }

                return ("lincoln", "lincoln");
            }
            catch
            {
                return ("lincoln", "lincoln");
            }
        }
        public static string ExtractDeviceIDFromRequest(HttpRequest request)
        {
            if (request.Query.TryGetValue("device_id", out var deviceIdQueryValues))
            {
                return deviceIdQueryValues.ToString();
            }
            if (request.Headers.TryGetValue("User-Agent", out var uaValues) &&
                uaValues.ToString().StartsWith("com.google.ios.youtube/", StringComparison.OrdinalIgnoreCase))
            {
                if (request.Headers.TryGetValue("Authorization", out var authHeaderValues))
                {
                    var authHeader = authHeaderValues.ToString();
                    const string bearerPrefix = "Bearer ";
                    if (authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                    {

                        return authHeader.Substring(bearerPrefix.Length).Trim();
                    }
                }
            }
            else if (request.Headers.TryGetValue("X-GData-Device", out var deviceHeaderValues))
            {
                var deviceHeader = deviceHeaderValues.ToString();
                const string prefix = "device-id=\"";
                var startIndex = deviceHeader.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (startIndex >= 0)
                {
                    startIndex += prefix.Length;
                    var endIndex = deviceHeader.IndexOf("\"", startIndex);
                    if (endIndex > startIndex)
                    {
                        return deviceHeader.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            else if (request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authHeaderValue = authHeader.ToString();
                if (authHeaderValue.StartsWith("GoogleLogin", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = authHeaderValue.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        var kv = parts[1].Split('=', 2);
                        if (kv.Length == 2 && kv[0].Trim() == "auth")
                        {
                            var authToken = kv[1].Trim();
                            return authToken;
                        }
                    }
                }
            }
            else if (request.Headers.TryGetValue("X-YouTube-DeviceAuthToken", out var classicDeviceHeaderValues))
            {
                return classicDeviceHeaderValues;
            }

            return null;
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
        public static bool IsUsernamePasswordCorrect(string username, string password) // so you can hide the sign in video
        {
            try
            {
                if (!File.Exists(FilePath))
                    return false;

                var json = File.ReadAllText(FilePath);
                var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new List<YouTubeSession>();

                var session = sessions.Find(s => s.Username == username);

                if (session == null)
                    return false;

                if (session.Password != password)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsUserNameTaken(string username) // so you can hide the sign in video
        {
            try
            {
                if (!File.Exists(FilePath))
                    return false;

                var json = File.ReadAllText(FilePath);
                var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new List<YouTubeSession>();

                var session = sessions.Find(s => s.Username == username);

                if (session == null)
                    return false;

                if (session.Username == username)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
        public static void HandleStuff(WebApplication app)
        {

            app.MapGet("/check_if_username_is_taken", (HttpRequest request) =>
            {
                try
                {
                    string? username = System.Security.SecurityElement.Escape(request.Query["username"]);

                    if (String.IsNullOrEmpty(username))
                    {
                        return Results.BadRequest("Must have a username parm.");
                    }

                    var response = new IsUsernameTakeResult
                    {
                        Status = false
                    };

                    if (IsUserNameTaken(username))
                    {
                        response.Status = true;
                    }

                    return Results.Json(response);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Yikes! Exception in /check_if_username_is_taken: {ex}");
                    return Results.Problem(ex.Message);
                }

            });

            app.MapPost("/link_device_token", async (HttpRequest request) =>
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(request.Body);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("device_id", out var deviceIdProp) || string.IsNullOrEmpty(deviceIdProp.GetString()))
                    {
                        Console.WriteLine("Missing device_id in request body.");
                        return Results.BadRequest("Missing device_id");
                    }

                    string deviceId = deviceIdProp.GetString()!;

                    string username = root.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() ?? "" : "";
                    string password = root.TryGetProperty("password", out var passwordProp) ? passwordProp.GetString() ?? "" : "";

                    string accessToken = root.TryGetProperty("access_token", out var accessTokenProp) ? accessTokenProp.GetString() ?? "" : "";
                    string refreshToken = root.TryGetProperty("refresh_token", out var refreshTokenProp) ? refreshTokenProp.GetString() ?? "" : "";

                    Console.WriteLine($"Received device_id: {deviceId}");

                    Console.WriteLine($"Received username: {username}");
                    Console.WriteLine($"Received password: {password}");

                    Console.WriteLine($"Received access_token: {accessToken}");
                    Console.WriteLine($"Received refresh_token: {refreshToken}");

                    List<YouTubeSession> sessions; // idk why i called em sessions, probbaly accounts would be better (since they are fairly perm)

                    if (File.Exists(HandleLogin.FilePath))
                    {
                        try
                        {
                            var existingJson = await File.ReadAllTextAsync(HandleLogin.FilePath);
                            sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(existingJson) ?? new List<YouTubeSession>();
                        }
                        catch
                        {
                            sessions = new List<YouTubeSession>();
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(HandleLogin.FilePath)!);
                        File.WriteAllText(HandleLogin.FilePath, "[]");
                        sessions = new List<YouTubeSession>();
                    }

                    var existingUsernames = sessions.Find(s => s.Username == username);

                    if (existingUsernames != null && existingUsernames.Username == username)
                    {
                        return Results.BadRequest("Username taken");
                    }

                    var existingSession = sessions.Find(s => s.DeviceId == deviceId);

                    if (existingSession != null && !existingSession.IsLinked)
                    {
                        existingSession.Username = username;
                        existingSession.Password = password;

                        existingSession.AccessToken = accessToken;
                        existingSession.RefreshToken = refreshToken;

                        existingSession.IsLinked = await HandleLogin.ValidateAccessTokenAsync(accessToken);
                    }
                    else
                    {
                        var newSession = new YouTubeSession
                        {
                            DeviceId = deviceId,
                            Username = password,
                            Password = password,
                            AccessToken = accessToken,
                            RefreshToken = refreshToken,
                            IsLinked = await HandleLogin.ValidateAccessTokenAsync(accessToken)
                        };
                        sessions.Add(newSession);
                    }

                    var newJson = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });

                    await File.WriteAllTextAsync(HandleLogin.FilePath, newJson);

                    return Results.Ok("Device linked");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Yikes! Exception in /link_device_token: {ex}");
                    return Results.Problem(ex.Message);
                }
            });

            app.MapPost("/get_session", async (HttpRequest request) =>
            {
                try
                {

                    if (!request.HasFormContentType)
                    {
                        return Results.BadRequest("Invalid content type");
                    }

                    var form = await request.ReadFormAsync();

                    string? username = form["username"];
                    string? password = form["password"];

                    if (String.IsNullOrEmpty(username))
                    {
                        return Results.BadRequest("Must have a username parm.");
                    }

                    if (String.IsNullOrEmpty(password))
                    {
                        return Results.BadRequest("Must have a password parm.");
                    }

                    var userRight = IsUsernamePasswordCorrect(username, password);

                    if (!userRight)
                        return Results.Unauthorized();

                    if (!File.Exists(FilePath))
                        return Results.StatusCode(500);


                    var json = File.ReadAllText(FilePath);
                    var sessions = JsonSerializer.Deserialize<List<YouTubeSession>>(json) ?? new List<YouTubeSession>();

                    var session = sessions.Find(s => s.Username == username);

                    if (session == null)
                        return Results.Unauthorized();


                    var response = session;

                    return Results.Json(response);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Yikes! Exception in /get_session: {ex}");
                    return Results.Problem(ex.Message);
                }

            });

            // the original YouTube login api (very very simple as simple as it gets)
            // used for IOS and I think early apple tv versions?
            // this predates oauth v1 and AuthToken by a bit (at least at YouTube idr release dates for the tech i am dumb)
            // it is pretty much just a form
            app.MapPost("/accounts/ClientLogin", async (HttpRequest request) =>
            {
                try
                {

                    var username = "";
                    var password = "";

                    if (request.Form.TryGetValue("Email", out var email))
                    {
                        username = email;
                    }

                    if (request.Form.TryGetValue("Passwd", out var passwd))
                    {
                        password = passwd;
                    }

                    if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password))
                    {
                        return Results.Ok("You must have a username and password!"); // note this MUST be statuscode 200 or the client won't give a password error
                    }

                    if (!IsUsernamePasswordCorrect(username, password))
                    {
                        return Results.Ok("Your username and password are wrong, or your account isn't linked!!!");
                    }

                    var data = await GetValidLoginDataAsync(username, password);

                    var template =
                        $"SID={data.Item1}\n" +
                        $"LSID={data.Item1}\n" +
                        $"Auth={data.Item1}\n"; // just giving the device id just as well as doing the authTOken (GetValidClientLoginDataAsync .item2 gives authtoken if you really need it)


                    return Results.Content(template);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Yikes! Exception in Client Login: {ex}");
                    return Results.Problem(ex.Message);
                }
            });

            // other endpoint for it 
            app.MapPost("/youtube/accounts/ClientLogin", async (HttpRequest request) =>
            {
                try
                {

                    var username = "";
                    var password = "";

                    if (request.Form.TryGetValue("Email", out var email))
                    {
                        username = email;
                    }

                    if (request.Form.TryGetValue("Passwd", out var passwd))
                    {
                        password = passwd;
                    }

                    if (String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password))
                    {
                        return Results.Ok("You must have a username and password!"); // note this MUST be statuscode 200 or the client won't give a password error
                    }

                    if (!IsUsernamePasswordCorrect(username, password))
                    {
                        return Results.Ok("Your username and password are wrong, or your account isn't linked!!!");
                    }

                    var data = await GetValidLoginDataAsync(username, password);

                    var template =
                        $"SID={data.Item1}\n" +
                        $"LSID={data.Item1}\n" +
                        $"Auth={data.Item1}\n"; // just giving the device id just as well as doing the authTOken (GetValidClientLoginDataAsync .item2 gives authtoken if you really need it)


                    return Results.Content(template);
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"Yikes! Exception in Client Login: {ex}");
                    return Results.Problem(ex.Message);
                }
            });

            // the orignal endpoint that YouTube used for devices that used a webview login
            // it still works however youtube stopped supporting the client
            // this works enough to trigger an v1.xx login
            // i think the scheme also changed
            // this is used for linking other devices as will it is just easier to have one endpoint for this shit
            app.MapGet("/o/oauth2/programmatic_auth", (HttpRequest req) =>
            {
                var deviceId = req.Query["device_id"].ToString();
                if (string.IsNullOrEmpty(deviceId))
                    deviceId = "unknown";

                // maybe we should pretty this up
                // at some point 
                // all this does is pulls a YouTube tv token!

                // shitty ui as always 
                // if you wanna make a good looking one feel free

                var html = $@"  
                    <!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                    <meta charset=""UTF-8"" />
                    
                    <title>Register Device</title>
                        <style>
                            #urlSection, #codeSection, #tokenSection, #statusSection {{ display:none; margin-top:10px; }}
                        </style>
                    </head>
                    
                    <body>
                    
                    <h1>Register Device</h1>
                   
                    <p>Device ID: <span id=""deviceId"">{deviceId}</span></p>
                                        
                    <div>
                        <p>
                            Input the username and password that will be used for Classic iOS (and maybe IOS v1.xx login and some point)!
                        </p>
                        <p>
                            Please note: for android clients, you will need to relink your account for each new device with a different username/password.
                            This is down to how android clients get their Auth Header token, it is done at a system level and not a app level. 
                        </p>
                        <p>
                            You also cannot reuse usernames (we have to use your username/password for clientLogin), however you can just click the generateBtn and a unqiqe username/password will be made for you!
                            This is useful for Android.
                        </p>
                        <h2>IOS</h2>
                        <p>
                            The username and password you enter here will be used to login into the app. Unlike Android, you shouldn't have to relink the app, even on different device. 
                            As already mentioned, this has easy access to setting the auth header.
                        </p>
                        <h2>IOS V1.xx</h2>
                        <p>
                            You must link it outside of the app in a web browser as we cannot get the deviceID from the webview.
                            Once you do that instead of pressing 'Start Device Authorization' press 'Login In With Linked Account'.
                            You should be able to use it across devices.
                        </p>
                        <h2>Android</h2>
                        <p>
                            If you clear your cache or uninstall the app you will be given a new device id, and thus has to relink accounts.
                        </p>
                    </div>

                    <br> 

                    <b>Username: </b> 
                    <input id=""usernameInput""/>

                    <br> <br>

                    <b>Password: </b>
                    <input id=""passwordInput""/>

                    <br> <br>  

                    <button id=""loginV1"">Login In With Linked Account</button>

                    <br> <br>
                    
                    <button id=""generateBtn"">Auto‑Generate Username & Password</button>

                    <br> <br>  

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
                        window.onload = function() {{
                            var href = window.location.href;
                            var q = href.indexOf('?');
                            var baseUrl = q > -1 ? href.substring(0, q) : href;
                            var query = q > -1 ? href.substring(q + 1) : '';
                            var parts = query.split('&');
                            var deviceId = '';
                            var newParams = '';
                            for (var i = 0; i < parts.length; i++) {{
                                var kv = parts[i].split('=');
                                if (kv[0] === 'device_id') {{
                                    deviceId = kv[1];
                                }}
                            }}
                            if (deviceId !== '') {{
                                newParams = '?device_id=' + deviceId;
                            }}
                            var cleanUrl = baseUrl + newParams;
                            if (href !== cleanUrl) {{
                                window.location.href = cleanUrl;
                            }}
                        }};
                    </script>
                    
                    <script>
                    function checkUsername() {{
                        var username = document.getElementById('usernameInput').value;
                        if (!username) {{
                            document.getElementById('usernameStatus').innerText = """";
                            return;
                        }}

                        var xhr = new XMLHttpRequest();
                        xhr.open('GET', '/check_if_username_is_taken?username=' + encodeURIComponent(username), true);
                        xhr.onreadystatechange = function() {{
                            if (xhr.readyState === 4) {{
                                if (xhr.status === 200) {{
                                    try {{
                                        var data = JSON.parse(xhr.responseText);
                                        if (data.Status === true) {{
                                            document.getElementById('usernameStatus').innerText = 'Username already taken';
                                            document.getElementById('usernameStatus').style.color = 'red';
                                        }} else {{
                                            document.getElementById('usernameStatus').innerText = 'Available';
                                            document.getElementById('usernameStatus').style.color = 'green';
                                        }}
                                    }} catch (e) {{
                                        console.error('JSON parse error:', e);
                                        document.getElementById('usernameStatus').innerText = 'Bad server response';
                                        document.getElementById('usernameStatus').style.color = 'orange';
                                    }}
                                }} else {{
                                    console.error('Request failed. Status:', xhr.status);
                                    document.getElementById('usernameStatus').innerText = 'Error checking username';
                                    document.getElementById('usernameStatus').style.color = 'orange';
                                }}
                            }}
                        }};
                        xhr.send();
                    }}

                    document.getElementById('loginV1').onclick = function() {{
                    
                        var xhr = new XMLHttpRequest();
                        xhr.open(""POST"", ""/get_session"", true);
                        xhr.setRequestHeader(""Content-Type"", ""application/x-www-form-urlencoded"");

                        var username = document.getElementById('usernameInput').value;
                        var password = document.getElementById('passwordInput').value;

                        xhr.onreadystatechange = function() {{
                            if (xhr.readyState === 4) {{
                                if (xhr.status === 200) {{
                                    try {{
                                        var response = JSON.parse(xhr.responseText);
                                        var device_id = response.deviceId;
                                        window.location.replace(""/o/oauth2/programmatic_auth_stage_2?oauth="" + encodeURIComponent(device_id));
                                    }} catch(e) {{
                                        alert(""Invalid server response"");
                                    }}
                                }} else {{
                                    document.getElementById(""responseOutput"").innerText = ""Error logging in: "" + xhr.status;
                                }}
                            }}
                        }};

                        xhr.send(""username="" + encodeURIComponent(username) + ""&password="" + encodeURIComponent(password));
                    }};

                    document.getElementById('generateBtn').onclick = function() {{
                        var rand = Math.floor(Math.random() * 100000);
                        var username = 'user' + rand;
                        var password = Math.random().toString(36).slice(-10);

                        document.getElementById('usernameInput').value = username;
                        document.getElementById('passwordInput').value = password;

                        checkUsername();
                    }};

                    var deviceId = '{deviceId}';
                    var clientId = ""861556708454-d6dlm3lh05idd8npek18k6be8ba3oc68.apps.googleusercontent.com"";
                    var clientSecret = ""SboVhoG9s0rNafixCSGGKXAT"";
                    var scope = ""http://gdata.youtube.com"";

                    var deviceCode = """";
                    var username = """";
                    var password = """";
                    var accessToken = """";
                    var refreshToken = """";

                    document.getElementById('startDeviceFlow').onclick = function() {{
                        checkUsername();

                        if (document.getElementById('usernameInput').value !== """" && document.getElementById('passwordInput').value !== """") {{

                            var xhr = new XMLHttpRequest();
                            xhr.open(""POST"", ""https://oauth2.googleapis.com/device/code"", true);
                            xhr.setRequestHeader(""Content-Type"", ""application/x-www-form-urlencoded"");

                            username = document.getElementById('usernameInput').value;
                            password = document.getElementById('passwordInput').value;

                            xhr.onreadystatechange = function() {{
                                if (xhr.readyState === 4) {{
                                    if (xhr.status === 200) {{
                                        try {{
                                            var response = JSON.parse(xhr.responseText);
                                            deviceCode = response.device_code;
                                            document.getElementById(""verificationUrl"").innerText = response.verification_url;
                                            document.getElementById(""activationLink"").href = response.verification_url;
                                            document.getElementById(""userCode"").innerText = response.user_code;
                                            document.getElementById(""urlSection"").style.display = ""block"";
                                            document.getElementById(""codeSection"").style.display = ""block"";
                                            document.getElementById(""tokenSection"").style.display = ""block"";
                                            pollForToken();
                                        }} catch(e) {{
                                            alert(""Invalid server response"");
                                        }}
                                    }} else {{
                                        document.getElementById(""responseOutput"").innerText = ""Error getting device code: "" + xhr.status;
                                    }}
                                }}
                            }};

                            var data = ""client_id="" + encodeURIComponent(clientId) + ""&scope="" + encodeURIComponent(scope);
                            xhr.send(data);

                        }} else {{
                            alert('Woops! You have to fill in the username and password you on, on your computer!');
                        }}
                    }};

                    function pollForToken() {{
                        var interval = setInterval(function() {{
                            if (!deviceCode) {{
                                clearInterval(interval);
                                return;
                            }}

                            var xhr = new XMLHttpRequest();
                            xhr.open(""POST"", ""https://oauth2.googleapis.com/token"", true);
                            xhr.setRequestHeader(""Content-Type"", ""application/x-www-form-urlencoded"");

                            xhr.onreadystatechange = function() {{
                                if (xhr.readyState === 4) {{
                                    if (xhr.status === 200) {{
                                        try {{
                                            var response = JSON.parse(xhr.responseText);
                                            accessToken = response.access_token;
                                            refreshToken = response.refresh_token || """";
                                            if (accessToken) {{
                                                document.getElementById(""tokenOutput"").innerText = ""Tokens acquired! Linking device ... Close and reopen the YouTube App! (You should be good?)"";
                                                linkDevice();
                                                clearInterval(interval);
                                            }}
                                        }} catch(e) {{
                                            alert(""Invalid server response"");
                                        }}
                                    }} else if (xhr.status === 400) {{
                                        // handle 400 if needed
                                    }} else if (xhr.status === 428) {{
                                        // handle 428 if needed
                                    }} else {{
                                        document.getElementById(""tokenOutput"").innerText = ""Error polling token: "" + xhr.status;
                                        clearInterval(interval);
                                    }}
                                }}
                            }};

                            var data = ""client_id="" + encodeURIComponent(clientId) + 
                                    ""&client_secret="" + encodeURIComponent(clientSecret) + 
                                    ""&device_code="" + encodeURIComponent(deviceCode) + 
                                    ""&grant_type=urn:ietf:params:oauth:grant-type:device_code"";
                            xhr.send(data);
                        }}, 5000);
                    }}

                    function linkDevice() {{
                        var xhr = new XMLHttpRequest();
                        xhr.open(""POST"", ""/link_device_token"", true);
                        xhr.setRequestHeader(""Content-Type"", ""application/json"");

                        var body = JSON.stringify({{
                            device_id: deviceId,
                            username: username,
                            password: password,
                            access_token: accessToken,
                            refresh_token: refreshToken
                        }});

                        xhr.onreadystatechange = function() {{
                            if (xhr.readyState === 4) {{
                                if (xhr.status >= 200 && xhr.status < 300) {{
                                    try {{
                                        var text = xhr.responseText;
                                        document.getElementById(""tokenOutput"").innerText = text;
                                    }} catch(e) {{
                                        alert(""Error processing server response"");
                                    }}
                                }} else {{
                                    document.getElementById(""tokenOutput"").innerText = 'Error linking device: ' + xhr.status;
                                }}
                            }}
                        }};

                        xhr.send(body);
                    }}
                    </script>
                    </body>
                    </html>
                    ";

                return Results.Content(html, "text/html");
            });

            // you to redrict to another page and then set the cookie in that page to make the client happy 
            app.MapGet("/o/oauth2/programmatic_auth_stage_2", async (HttpRequest request, HttpResponse response) =>
            {
                string? oauth_value = request.Query["oauth"];
                if (string.IsNullOrEmpty(oauth_value))
                {
                    oauth_value = "ya29.fake_token";
                }

                var cookieOptions = new CookieOptions
                {
                    Path = "/",
                    Domain = ".google.com",
                    HttpOnly = true,
                    Secure = true
                };

                response.Cookies.Append("oauth_code", oauth_value, cookieOptions);
                response.Cookies.Append("oauth_code", oauth_value, cookieOptions);

                string htmlContent = $"<html><body><h1>OAuth Stage 2</h1><p>OAuth token set: {oauth_value}</p></body></html>";
                response.ContentType = "text/html";
                await response.WriteAsync(htmlContent);
            });

            // we don't need a real token here so we can fake it tell we me make it
            // we set the oauth token in the above thing and it would have used it
            // here to get and return a access code, we dont have to here since
            // we have session id and shit and already have a access toke n shit
            app.MapPost("/o/oauth2/token", async (HttpRequest request) =>
            {

                using var reader = new StreamReader(request.Body);
                var body = await reader.ReadToEndAsync();

                var parsed = HttpUtility.ParseQueryString(body);

                var code = parsed["code"];
                var refreshToken = parsed["refresh_token"];

                var accessToken = code ?? refreshToken ?? "lifeisstrange";

                var response = new OAuth2TokenResponse
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    RefreshToken = accessToken
                };

                return Results.Json(response);
            });

            // this really isn't used in anywhere expect in the login process
            // the app itself uses default feeds for user info
            // maybe like to see if an account isn't linked to youtube yet idk
            // probbaly just a quirk of the lib they use in the app 
            // doesn't matter to use 
            app.MapGet("/oauth2/v1/userinfo", (HttpRequest request) =>
            {

                var response = new OAuth2UserInfoResponse
                {
                    Id = "2013",
                    Name = "David Price Is My Bea",
                    Email = "ilovemenandwomenandenbies@gmail.com",
                    VerifiedEmail = true
                };

                return Results.Json(response);

            });

            
        }
    }
}
