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

- [*] Protobuff Player: Mostly Finished

- [O] Client Login: Not Started

- [O] IOS v1.xx Login: Not Started

- [0] Android (V2.xx through V5.xx) Login: You can login, many login features work, and many dont.
 
- [^] Default Feeds: Subcriptions, Recommendations (You will have to click retry like 5-10 times on startup! IDK WHY IT IS FIESTY).

- [O] Ratings: Not Started
 
## Progress Atlas

- [F] Means it is finished, and tested! 

- [*] Means that the feed works, however is lacking pagination/and or proper dates (or other minor way). 

- [^] Means working to some degree, however is lacking in some very important way, or has multiple other feeds that haven't been done.

- [?] Means it has been implemented to some degree, however hasen't been tested.

- [O] Means the end point does not work, or is not started.

- [C] Means end point is cancelled.

## Suppored Platfroms/Future Supported Platforms

- IOS (Classic through 2.2.0)

- YouTube Wii

- YouTube Leanback (2012-2014)

- Leanback Lite v3

- YouTube Windows 7 Gadget

- Android (1.xx to 4.xx work, 5.1.x and above work, 5.0.x doesn't work for videos).

- Windows Phone Offical YouTube App (Some Progress, JSON feeds are done)

- And More!

# What Is This?

This is a reimplementation of YouTube's old GDATA v2 API, which was YouTube primary API from
roughly 2008(2007) through 2013/2014. 

# Tidbits

- Lincoln is built around yt-dlp wrapper, and makes as few as possible requsts to InnerTube directly,
which means that as long as yt-dlp is around (and download urls stick around) this should as well. Even
if this stops receiving updates. Due take in mind some login stuff doesn't!

- This is a continuation/reimagining of my older "JATB3000" project.

- I'd suggest (for devs) using if you're using an android/ios or other not on computer device https://mitmproxy.org/
as it lets you see missing requsts and such (hint: `mitmweb -p 8082 --listen-host 192.168.1.150`).

- This supports muxed and HLS streams, right now there is not an option (easily) for users to switch between the muxed (itag 18),
and HLS streams. However if you want to make your server use the muxed itag-18 format, use ?muxed=true in each feed that has a 
`/getvideo/` request. Like `http://192.168.1.150/get_video/{video_id}?muxed=true". The muxed option will force 360p.

- We offer two endpoints for getting videos, /getvideo/ and /get_video. They do the exact same thing however, /get_video/video_id=
is the proper endpoint YouTube used back in the day, and not /getvideo/ (what TubeRepaier Python and JATB uses/used).

- The HLS system is pretty simple, all it does it download the manifiest_url, and remove all lines that contain vp9 (may have to add av1 as well idk)!

- Your DeviceId is linked to your access token/refresh token!

- The recommendations works, but on 5.1xx it is a little fiesty (just keep retrying it'll work!). It is weird I have looked through
logcat and such!

# Server Setup

[**THIS IS VERY EARLY DEVELOPMENT, YOU HAVE BEEN WARNED**]

[**IF YOU HAVE IP ISSUES MAKE SURE TO CHECK IN launchSettings.json !**]
[**IT USES 192.168.1.150 BY DEFAULT**]

- Step 1 `git clone https://github.com/erievs/Lincoln`

- Step 2 `cd Lincoln`

- Step 3 `dotnet run`

- Step 4 (Optional) For mobile devices and the like, you will need to set Lincoln to use your computer's local ip-address.
Please remember you're local ip-address IS NOT the same as your public ip-address, this is the address local devices use in your
network to talk to one another. These addresses tend to be pretty uniform, and (sometimes) start with `192.168.1.xxx`, you are NOT able to
get someone's general location with them. Please also note that public ip-address in 99.9% of cases do not even accurately give good enough
geo-location to really cause you any harm. Some ISP such as my own do not even route through my states or region!

Without further ado go and open up `Properties/launchSettings.json`.

- Step 5 (Optional) We will now grab your devices's local ip-address, open a terminal on Linux (may also work on some BSDs) and use the 
command  `hostname -I | awk '{print $1}'`, on Windows open the cmd and run `ipconfig` and look for "IPv4 address", for MacOS open the
terminal and run `ifconfig` and see if you can find "IPv4 address" (haven't tested it yet, so this is from Google). Copy/remember that adress!

- Step 6 (Optional) Go to the `launchSettings.json` file we opened and under profiles replace every instance of `http://localhost:5156`or `192.168.1.150:8090` 
with `http://{your_address_here}:5156` (you can replace 5156 with whatever port you want, I prefer 8090). Ctrl-C out of the server, rerun `dotnet run`, and 
you're good! 

[Note: *If you're patching an android app! You HAVE TO not add a port like :8090, just remove it or replace it with 80, if you're just doing flash or IOS
you're good, on linux you'll need to run `sudo dotnet run` rather than no sudo!*]

[Note: If you're on Linux you may need to run `chmod +x yt-dlp` and `chmod +x ffmpeg`, this can be done
with a GUI on most File Manangers by right-clicking and "Allow executing file as program" (in Mate it is in 
permissons). For some reason when YouTubeDLSharp downloads the binaires they are not marked as an executable.]

## IOS Setup

- Step 1 Make sure you did the optional setup steps. When you started the server it should have said `Now listening on: {your address}`
make sure to remember that (this is your instance url, it is also shown on the homepage)! 

- Step 2 Make sure to install TubeRepair on your device, https://tuberepair.bag-xml.com/, this does require a jailbroken device!

- Step 3 Make sure your device is on the same network as your computer! If that's the case go to settings and find the TubeRepair options,
in the server input box put `http://{youradress}:{yourport}` DO NOT add a `/` at the end!

- Step 4 Close out of any opened YouTube app, and enjoy!

## Android Setup

- This is a very early WIP, https://github.com/ftde0/yt2009/blob/main/apk_setup.md , and make sure to also replace any
"googleapis.com" request or of the sort with your your ip, and if you see "https" (JUST "HTTPS") near "gdata.youtube.com"
please change it to "http"!

## Android Login

[**IF SIGN IN DOESNT WORK, YOU PROBBALY DIDN"T LINK YOUR ACCOUNT**]

- Step 1 Click on a video that says "Link Your Google Account" or something like that.

- Step 2 In the description click the link!

- Step 3 Go to to google.com/device, and link your device with the code!

- Step 4 Go back to your device, and sign in (make sure to click the first result!).

## Libraries Used / Credits

- https://github.com/Bluegrams/YoutubeDLSharp (Bluegrams)

- System.Text.Json (I know this is baked in, but we don't use Newtonsoft)

- https://github.com/ftde0/yt2009/blob/main/back/proto/ (YT2009, for Protobuffs!)