using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Namotion.Reflection;
using YoutubeDLSharp;
using System.Text.RegularExpressions;

namespace Lincon
{

    public static class DeviceRegistration
    {

        public static void HandleDevices(WebApplication app)
        {
            
   
            // IOS classic, layer 1
            app.MapPost("/youtube/accounts/applelogin1", (HttpRequest request) => {
                
                Guid uuid1 = Guid.NewGuid();
                string stringedUUID1 = uuid1.ToString();

                Guid uuid2 = Guid.NewGuid();
                string stringedUUID2 = uuid2.ToString();

                Regex.Replace(stringedUUID1, "/-/g", "");
                Regex.Replace(stringedUUID2, "/-/g", "");

                Console.WriteLine($"\nAppleLogin1: r2={stringedUUID1}, hmackr2={stringedUUID2}");

                return Results.Content($"r2={stringedUUID1}\nhmackr2={stringedUUID2}");
            });


            // IOS classic, layer 1
            app.MapPost("/youtube/accounts/applelogin2", (HttpRequest request) =>  {

                Guid uuid = Guid.NewGuid();

                string stringedUUID = uuid.ToString();

                Regex.Replace(stringedUUID, "/-/g", "");

                Console.WriteLine($"\nAppleLogin2: Auth={stringedUUID}");

                return Results.Content($"Auth={stringedUUID}");

            });


            // Android/Google IOS 
            app.MapPost("/youtube/accounts/registerDevice", (HttpRequest request) =>  {
                
                Guid uuid = Guid.NewGuid();

                string stringedUUID = uuid.ToString();

                Regex.Replace(stringedUUID, "/-/g", "");

                Console.WriteLine($"\nRegisterDevice: DeviceId={stringedUUID}, DeviceKey={stringedUUID}");

                return Results.Content($"DeviceId={stringedUUID}\nDeviceKey={stringedUUID}");

            });


        }

    }

}