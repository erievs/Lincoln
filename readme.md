## Lincoln ##

Free the tubes!

## Progress ##

- [?] Device: Registration

- [*] Search: Working

- [O] Standard Feeds: Not Started

- [*] Comments: Started

- [O] User Info: Not started

- [?] Get Video: Started 

- [O] Client Login: Not Started

- [O] IOS v1.xx Login: Not Started
 
- [O] Default Feeds: Not started

- [O] Ratings: Not Started
 
## Progress Atlas

- [*] Means that the feed works, however is lacking pagination. 

- [^] Means working to some degree, however is lacking in some very important way.

- [?] Means it has been implemented to some degree, however hasen't been tested.

- [O] Means the end point does not work, or is not started.

- [C] Means end point is cancelled.

## Suppored Platfroms/Future Supported Platforms

- IOS (Classic through 2.2.0)

- YouTube Wii

- YouTube Leanback (2012-2014)

- Leanback Lite v3

- YouTube Windows 7 Gadget

- Android (1.xx through 5.xx)

- Windows Phone Offical YouTube App

- And More!

# What Is This?

This is a reimplementation of YouTube's old GDATA v2 API, which was YouTube primary API from
roughly 2008 through 2013/2014. 

# Setup

[**THIS IS VERY EARLY DEVELOPMENT, YOU HAVE BEEN WARNED**]

- Step 1 `git clone https://github.com/erievs/Lincoln`

- Step 2 `cd Lincoln`

- Step 3 `dotnet Run`

[Note: If you're on Linux you may need to run `chmod +x yt-dlp` and `chmod +x ffmpeg`, this can be done
with a GUI on most File Manangers by right-clicking and "Allow executing file as program" (in Mate it is in 
permissons). For some reason when YouTubeDLSharp downloads the binaires they are not marked as an executable.]

## Libraries Used

- https://github.com/Bluegrams/YoutubeDLSharp (Bluegrams)

- System.Text.Json (I know this is baked in, but we don't use Newtonsoft)