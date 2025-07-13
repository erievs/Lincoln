namespace Lincon {

    public static class HTMLModal {


        // you must use [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/raw-string] to store html

        // also I stoll it from my other project

        // https://codepen.io/jgentes/pen/nrWOmJ

        // we have to stick the style AFTER the html because we need $ support in the html

        // very jank I know

        public static String Home(HttpRequest request, HttpContext context) {
            
            return $"""
        
            <!DOCTYPE html>

            <html data-lt-installed="true">
            <head>
                <meta http-equiv="content-type" content="text/html; charset=UTF-8">
                <meta charset="utf-8">
                <title>Lincoln - The GDATA v2 Reimplementation</title>
            </head>
                <body>

                    <p style="font-size: 36px; text-align: center;">Welcome to Lincoln! </p>

                    <p style="font-size: 18px;  text-align: center;" >Your instance url is <a href=""{request.Scheme}://{request.Host}{request.PathBase}"">{request.Scheme}://{request.Host}{request.PathBase}/</a></p> 
                    
                    <p style="font-size: 18px; text-align: center;">
                        For API endpoints, check out swagger 
                        <a href="{request.Scheme}://{request.Host}{request.PathBase}/swagger/">{request.Scheme}://{request.Host}{request.PathBase}/swagger/</a>
                    </p>

                </body>

            </html>
        
        """ + """ 
        
            <style>
                :root input[type="checkbox"] {
                position : absolute;
                opacity : 0;
                }
                :root input[type="checkbox"].ios-switch + div {
                display : inline-block;
                vertical-align : middle;
                width : 3em;
                height : 1em;
                border : rgba(0, 0, 0, 0.3) solid 1px;
                border-radius : 999px;
                margin : 0 0.5em;
                background : white;
                background-image : linear-gradient(rgba(0, 0, 0, 0.1), transparent), linear-gradient(90deg, hsl(210, 90%, 60%) 50%, transparent 50%);
                background-size : 200% 100%;
                background-position : 100% 0;
                background-origin : border-box;
                background-clip : border-box;
                overflow : hidden;
                transition-duration : 0.4s;
                transition-property : padding, width, background-position, text-indent;
                box-shadow : 0 0.1em 0.1em rgba(0, 0, 0, 0.2) inset, 0 0.45em 0 0.1em rgba(0, 0, 0, 0.05) inset;
                font-size : 150%;
                }
                :root input[type="checkbox"].ios-switch:checked + div {
                padding-left : 2em;
                width : 1em;
                background-position : 0 0;
                }
                :root input[type="checkbox"].ios-switch + div:before {
                content : 'On';
                float : left;
                width : 1.65em;
                height : 1.65em;
                margin : -0.1em;
                border : rgba(0, 0, 0, 0.35) solid 1px;
                border-radius : inherit;
                background : white;
                background-image : linear-gradient(rgba(0, 0, 0, 0.2), transparent);
                box-shadow : 0 0.1em 0.1em 0.1em hsla(0, 0%, 100%, 0.8) inset, 0 0 0.5em rgba(0, 0, 0, 0.3);
                color : white;
                text-shadow : 0 -1px 1px rgba(0, 0, 0, 0.3);
                text-indent : -2.5em;
                }
                :root input[type="checkbox"].ios-switch:active + div:before {
                background-color : #eee;
                }
                :root input[type="checkbox"].ios-switch:focus + div {
                box-shadow : 0 0.1em 0.1em rgba(0, 0, 0, 0.2) inset, 0 0.45em 0 0.1em rgba(0, 0, 0, 0.05) inset, 0 0 0.4em 1px rgba(255, 0, 0, 0.5);
                }
                :root input[type="checkbox"].ios-switch + div:before, :root input[type="checkbox"].ios-switch + div:after {
                font : bold 60%/1.9 sans-serif;
                text-transform : uppercase;
                }
                :root input[type="checkbox"].ios-switch + div:after {
                content : 'Off';
                float : left;
                text-indent : 0.5em;
                color : rgba(0, 0, 0, 0.45);
                text-shadow : none;
                }
                label {
                position : relative;
                display : block;
                padding : 0.8em;
                border : silver solid 1px;
                border-top-width : 0;
                background : white;
                font : bold 110% sans-serif;
                }
                label:first-of-type {
                border-top-width : 1px;
                border-radius : 0.6em 0.6em 0 0;
                }
                label:last-of-type {
                border-radius : 0 0 0.6em 0.6em;
                box-shadow : 0 1px hsla(0, 0%, 100%, 0.8);
                }
                body {
                padding : 1em;
                background : #d2d4dd;
                background-image : linear-gradient(90deg, transparent 30%, rgba(0, 0, 0, 0.02) 45%, rgba(0, 0, 0, 0.02) 55%, transparent 70%);
                background-size : 8px 8px;
                font : 100%/1.5 sans-serif;
                text-shadow : 0 1px 1px white;
                }
            </style>

        """;

        }


    }

}