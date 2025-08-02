
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Lincon {

    public static class HandleWebpages {

        public static void HandlePages(WebApplication app) {
            
            app.MapGet("/", (HttpRequest request, HttpContext context) =>
            {
                var assets = $"http://{context.Request.Host}/assets/";

                return Results.Content(HTMLModal.Home(request, context), "text/html");
                
            });

        }

    }

}