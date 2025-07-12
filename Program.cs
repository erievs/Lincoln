
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using Lincon;

var builder = WebApplication.CreateBuilder(args);

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

await YoutubeDLSharp.Utils.DownloadYtDlp();
await YoutubeDLSharp.Utils.DownloadFFmpeg();

Console.WriteLine("Welcome to Lincoln!");

//

// call classes here (read my lips NO new controllers)

// web pages (home and maybe classic login)
HandleWebpages.HandlePages(app);

// device registration crap
DeviceRegistration.HandleDevices(app);

// search
SearchFeed.HandleFeed(app);

// video file/video playback
GetVideo.HandleVideos(app);


app.Run();