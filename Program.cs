
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using Lincon;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAntiforgery();

builder.Services.AddMvcCore();

builder.Services.AddControllers()
    .AddXmlSerializerFormatters();

// to create db on your other machine
// https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli


builder.Services.AddHttpLogging(logging =>
{   
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.ResponseHeaders.Add("MyResponseHeader");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.CombineLogs = true;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "Lincon";
    config.Title = "Lincon";
    config.Version = "v1";
});


var app = builder.Build();


app.UseHttpLogging();

app.UseOpenApi();

app.UseSwaggerUi(config =>
{
    config.DocumentTitle = "TodoAPI";
    config.Path = "/swagger";
    config.DocumentPath = "/swagger/{documentName}/swagger.json";
    config.DocExpansion = "list";
});


// download/update yt-dlp

await YoutubeDLSharp.Utils.DownloadBinaries(true); // set to false so it auto updates, will be a TINY but slower on startup

Console.WriteLine("Welcome to Lincoln!");

//

// call classes here (read my lips NO new controllers)

// web pages (home and maybe classic login)
HandleWebpages.HandlePages(app);

// device registration crap
DeviceRegistration.HandleDevices(app);

// categories
HandleCategories.Handle(app);

// search
SearchFeed.HandleFeed(app);

// playlist search
PlaylistSnippetsFeed.HandleFeed(app);

// standard feeds
StandardFeeds.HandleFeed(app);

// comments
CommentFeed.HandleFeed(app);

// releated
RelatedFeed.HandleFeed(app);

// user info 
UserFeeds.HandleFeed(app);

// playlists info
PlaylistInfoFeed.HandleFeed(app);

// user uploads
UploadsFeed.HandleFeed(app);

// video file/video playback
GetVideo.HandleVideos(app);

// auth/login stuff
HandleLogin.HandleStuff(app);

// default subs
DefaultSubscriptionsFeed.HandleFeed(app);

// default rec
RecommendationsFeed.HandleFeed(app);

// default new subs videos
NewSubscriptionVideosFeed.HandleFeed(app);

// default subtivty feed
SubtivityFeed.HandleFeed(app);

// defaut watch history
WatchHistoryFeed.HandleFeed(app);

// default favourites feed (aka liked videos)
FavouritesFeed.HandleFeed(app);

// defaut watch later
WatchLaterFeed.HandleFeed(app);

// default playlists
DefaultPlaylists.HandleFeed(app);

// default user interactions
UserInteractions.HandleFeed(app);


app.Run();