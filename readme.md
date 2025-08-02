## Lincoln ##

Free the tubes!

## Progress ##

- [F] Device Registration: Finished  

- [F] Categories: Finished  
- [*] Search: Working  
- [F] Standard Feeds: Finished  
- [*] Comments: Started  
- [*] Related Videos: Started  
- [^] User Info: Started  

- [F] Get Video: Finished  
- [*] Protobuff Player /youtube/v3/: Not Started (used only on v5021)  
- [*] Protobuff Player /v1/: Mostly Finished  


- [F] Client Login: Working  
- [*] iOS v1.xx Login: Working..  
- [0] Android Login: Currently, only tested on 5.1xx!

- [^] Default Feeds: Subscriptions, Recommendations, New Subscription Videos, Watch History, Playlists, Watch Later, Favourites (aka liked videos!)
- [*] Ratings: Finished (Mostly)

## Progress Atlas ##

- [F] Means it is finished and tested!  
- [*] Means the feed works but is lacking pagination and/or proper dates (or other minor issues).  
- [^] Means working to some degree but lacking in some very important way, or has multiple other feeds that haven't been done.  
- [?] Means it has been implemented to some degree but hasn't been tested.  
- [O] Means the endpoint does not work or is not started.  
- [C] Means endpoint is cancelled.  

## Supported Platforms / Future Supported Platforms ##

- Android (1.xx to 4.xx should work; but untested; 5.1.x and above work; and tested; while 5.0.x doesn't work for videos)  
- iOS (Classic through 2.2.0)  
- YouTube Wii  
- YouTube Leanback (2012–2014)  
- Leanback Lite v3  
- YouTube Windows 7 Gadget  
- And more!  

## What Is This? ##

This is a reimplementation of YouTube's old GDATA v2 API, which was YouTube's primary API from roughly 2007/2008 through 2013/2014.

## Tidbits ##

- Lincoln is built around a yt-dlp wrapper and makes as few requests as possible to InnerTube directly, which means that as long as yt-dlp exists (and download URLs remain valid), this should continue to work—even if this project stops receiving updates. However, some login features may break.  
- This is a continuation/reimagining of my older "JATB3000" project.  
- For developers using Android/iOS or other non-PC devices, I recommend using mitmproxy (https://mitmproxy.org/). It lets you see missing requests and more (hint: mitmweb -p 8082 --listen-host 192.168.1.150).  
- /getvideo/ Supports muxed and HLS streams. Currently, there is no easy user option to switch between muxed (itag 18) and HLS streams. To force the server to use muxed itag-18 format, append ?muxed=true to each feed with a /getvideo/ request, e.g., http://192.168.1.150/get_video/{video_id}?muxed=true. This forces 360p.  
- We offer two endpoints for getting videos: /getvideo/ and /get_video. They do the exact same thing, but /get_video/video_id= is the proper endpoint YouTube used back in the day, unlike /getvideo/ (which TubeRepair Python and JATB used/uses).  
- The HLS system is simple: it downloads the manifest URL and removes all lines containing vp9 (may need to add av1 later).  
- Your DeviceID is linked to your refresh token!  
- Recommendations work, but on 5.1xx Android builds, it’s a little feisty (just keep retrying; it'll work!). I’ve looked through logcat but can’t pinpoint why.  
- The reason why on IOS Classic you can use the same login details across devices is the acess_token can be easily set. And in turn we can just send your deviceID that is linked to the login details. We could send your access token. but just sending the device_id here works just as well.

## Warnings ##

- Many feeds will have issues with dates, sorry about that! I will slowly fix all of that, but it'll take a bit and I'm sure ya'll are fine
with weird dates for a little bit, rather than me delaying updates to fix them.
- Comments do not work sometimes (idk why they're fine on android lol)
- Some feeds have missing video IDs seldom, super sorry if you find one!
- Some feeds have wonky durations sorry!
- HOST THIS FOR YOURSLED (IF YOU GET PEOPLE'S ACCSESS TOKENS LEAKED IT"S YOUR OWN FAULT)
- LOGIN WILL BE BROKEN FOR TUBEREPAIR (IT"S A TWEAK ISSUE)

## Server Setup ##

# VERY EARLY DEVELOPMENT — YOU HAVE BEEN WARNED

If you have IP issues, check `launchSettings.json`!  
By default, it uses `192.168.1.150`.

1. `git clone https://github.com/erievs/Lincoln`  
2. `cd Lincoln`  
3. `dotnet run`  
4. To use the server on mobile devices or other devices on your network, you need your computer’s **local IP address**. This is **not** your public IP — it’s the address your devices use to communicate within your local network, often starting with `192.168.1.xxx`.

   - On **Linux/BSD**: Run  
     ```bash
     hostname -I | awk '{print $1}'
     ```
   - On **Windows**: Run  
     ```cmd
     ipconfig
     ```  
     and look for the **IPv4 address** (not the DNS server address) under your active network adapter.
   - On **macOS**: Run  
     ```bash
     ifconfig
     ```
     and look for the **IPv4 address** in the active network interface.

5. Open `Properties/launchSettings.json` and replace every instance of  
   `http://localhost:5156` or `192.168.1.150:8090`  
   with your local IP address and port (e.g., `http://192.168.1.100:5156`).  
   If you use port 80, omit the port number or set it to 80.

6. Restart the server with `dotnet run`.

> **Note:**  
> - For Android patching, **DO NOT** add a port like `:8090` — you must run on port 80 or omit the port entirely.  
> - On Linux, you may need to run:  
> ```bash
> echo 'net.ipv4.ip_unprivileged_port_start=0' | sudo tee -a /etc/sysctl.conf && sudo sysctl -p
> ```  
> Running `dotnet` as root may cause IDE issues.  
> - You may need to mark `yt-dlp` and `ffmpeg` as executable:  
> ```bash
> chmod +x yt-dlp ffmpeg
> ```  
> Most file managers let you right-click and enable **“Allow executing file as program.”**

## iOS Setup ##

1. Complete the optional server setup steps above.  
2. Install TubeRepair on your device (https://tuberepair.bag-xml.com/). It requires a jailbroken device.  
3. Ensure your device is on the same network as your computer.  
4. In TubeRepair settings, set the server URL to http://{your-ip}:{your-port} (no trailing slash).  
5. Close any open YouTube apps and enjoy!

> **Note:**  
> - IOS v1.xx login using TubeRepair is broken, the tweak causes sign in issues. 
> You'll need to use mitmproxy (or equivalent)
>
> See the section bellow to redirect manually without the use of TubeRepar.

## Using Mitmproxy to Fix YouTube on iOS (DO NOT HAVE TUBEREPAIR INSTALLED!!!!)

This setup will redirect your iOS device’s YouTube app login requests through your Lincoln server.

Install mitmproxy on your machine:

- **macOS:**  
  ```bash
  brew install mitmproxy
  ```
- **Linux:**  
  ```bash
  sudo apt install mitmproxy
  ```
  (or use your distro’s package manager)
- **Windows:**  
  Download the installer from [https://mitmproxy.org](https://mitmproxy.org) and follow the installation steps.

Create a new file called `lincoln_redirect.py` and paste in the following script:

```python
from mitmproxy import http
import socket

def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
    except Exception:
        ip = "127.0.0.1"
    finally:
        s.close()
    return ip

LOCAL_IP = get_local_ip()
LOCAL_PORT = 80  # default should be 80, but use whatever you did in the server_setup

def request(flow: http.HTTPFlow):
    mappings = {
        "https://accounts.google.com/o/oauth2/programmatic_auth": "",
        "https://accounts.google.com/o/oauth2/token": "",
        "https://www.googleapis.com/oauth2/v1/userinfo": "",
        "http://gdata.youtube.com": "",
        "https://gdata.youtube.com": "",
        "https://www.google.com/youtube/": "",
        "https://accounts.google.com/": ""
    }

    for prefix, new_path in mappings.items():
        if flow.request.pretty_url.startswith(prefix):
            flow.request.scheme = "http"
            flow.request.host = LOCAL_IP
            flow.request.port = LOCAL_PORT
            if new_path:
                flow.request.path = new_path
            break
```

This script detects your computer’s local IP automatically and rewrites YouTube/Google login requests so they route to your Lincoln server.

Run mitmproxy with:

```bash
mitmweb -s lincoln_redirect.py --listen-port 8082 --listen-host 0.0.0.0
```

This should start mitmproxy and show its web UI at `http://127.0.0.1:8081`. Your iOS device will send its traffic through your computer on port 8082.

Configure on iOS device:

1. Open **Settings** on the device.  
2. Go to **Wi‑Fi**.  
3. Tap the **(i)** next to the connected Wi‑Fi network.  
4. Scroll to **HTTP Proxy** and set it to **Manual**.  
5. Under **Server**, enter your computer’s **local IP address** (not public).  
   - Example: `192.168.1.150`  
   - **Tip:**  
     - **Windows:** run `ipconfig` and copy the **IPv4 Address** (not the DNS address).  
     - **macOS/Linux:** run `hostname -I` and use the first IP listed.
6. Under **Port**, enter `8082`.  
7. Leave **Authentication** OFF unless you explicitly set up a username/password.

Install and trust the mitmproxy certificate:

1. On the iOS device, open Safari and go to:  
   ```
   http://mitm.it
   ```
2. Choose **iOS** and download the certificate.  
3. Go to **Settings → General → VPN & Device Management** (older versions: **Profiles & Device Management**).  
4. Tap the mitmproxy certificate and hit **Install**.  
5. Then go to **Settings → General → About → Certificate Trust Settings**.  
6. Find “mitmproxy” and toggle **Full Trust** ON.

Verify the setup:

- Open the YouTube app (or any Google login screen) on the iOS device.  
- On your computer, visit `http://127.0.0.1:8081` to see mitmproxy logs.  
- Requests should appear in real-time, and the script will rewrite login URLs to point to your Lincoln server.

## Android Setup ##

- Very early WIP: https://github.com/ftde0/yt2009/blob/main/apk_setup.md  
- Replace any "googleapis.com" requests with your IP address.  
- If you see "https" near "gdata.youtube.com", change it to "http".

## Android Login ##

If sign-in doesn’t work, you probably didn’t link your account:

1. Click a video prompting "Link Your Google Account" or similar.  
2. Click the link in the description.  
3. Input your desired username and password (not too important for Android, but required).  
4. Visit google.com/device and enter the displayed code.  
5. Once linked, the website should say “device linked” (or similar). Close the site.  
6. Return to your device and sign in (click the first result).

## iOS Classic Login ##

If sign-in doesn’t work, you probably didn’t link your account:

1. Click a video prompting "Link Your Google Account" or similar.  
2. Click the link in the description.  
3. Input your desired username and password.  
4. Visit google.com/device and link your device with the code.  
5. Once linked, the website should say “device linked.” Close the site.  
6. Return to your device and sign in. It is VERY important you use the same device here since your username/password must be associated with your DeviceID—otherwise, login may succeed but requests will fail.

## iOS 1.xx Login ##

If sign-in doesn’t work, you probably didn’t link your account:

1. Click on the "Link Your Google Account" or similar.
2. Take the link you find in the description of it and use it on your computer or phone or some sort.
3. Follow the steps.
4. Click sign in, and enter your user and password, and press "Login with linked account".

## Disclaimer ##

This project is NOT affiliated with or endorsed by Google, YouTube, or any other Alphabet Inc. product, company, or service.

Do NOT use login on any public server—the server owner will have access to your YouTube account!

## Libraries Used / Credits ##

- YoutubeDLSharp (https://github.com/Bluegrams/YoutubeDLSharp) (Bluegrams)  
- System.Text.Json (built-in .NET library)  
- YT2009 Protobuff (https://github.com/ftde0/yt2009/blob/main/back/proto/)  
