namespace Lincon {

    public static class HTMLModal {


        // you must use [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string] to store html

        // https://github.com/jakiestfu/Ratchet-Vine

        // also I stoll it from my other project


        public static String Home(String assetsURL) {
            
            if(assetsURL is null) {assetsURL = "localhost";};

            return $"""
        
            <!DOCTYPE html>

            <html data-lt-installed="true">
            <head>
                <meta http-equiv="content-type" content="text/html; charset=UTF-8">
                <meta charset="utf-8">
            </head>

                <body>

                    <p> Hello world! </p>
                    
                </body>

            </html>
        
        """;

        }


    }

}