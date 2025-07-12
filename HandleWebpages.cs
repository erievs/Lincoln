
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Lincon {

    public static class HandleWebpages {

        public static void HandlePages(WebApplication app) {

      
            app.MapGet("/", (HttpRequest request, HttpContext context) =>
            {
                var assets = $"http://{context.Request.Host}/cdn/web";        

                return Results.Content(HTMLModal.Home(assets), "text/html");
                                
            });


        }

    }

}