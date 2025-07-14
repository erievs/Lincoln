
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Lincon {

    public static class HandleCategories {

        public static void Handle(WebApplication app) {

      
            app.MapGet("/schemas/2007/categories.cat", (HttpRequest request, HttpContext context) =>
            {
                
                string filePath = Path.Combine(Environment.CurrentDirectory, "assets" , $"categories.cat");

                return Results.Stream(new FileStream(filePath, FileMode.Open));  
                                
            });


        }

    }

}