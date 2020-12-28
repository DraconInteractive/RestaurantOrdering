using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Dracon.WebServer
{
    class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;
        byte[] logo;
        private static IDictionary<string, string> _mimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"},
            };
        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            //logo = File.ReadAllBytes(@"Logo.png");
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Program.Write("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string fName = ctx.Request.Url.AbsolutePath;
                                string ext = Path.GetExtension(fName);
                                //ctx.Response.ContentType = _mimeTypeMappings[ext];

                                if (ext == ".png" || ext == ".jpeg" || ext == ".ico")
                                {
                                    ctx.Response.ContentType = "image/png";
                                    RespondImage(ctx);
                                } else
                                {
                                    ctx.Response.ContentType = "application/json";
                                    RespondText(ctx);
                                }
                                
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        void RespondImage (HttpListenerContext ctx)
        {
            Console.WriteLine("Getting image for " + ctx.Request.RawUrl);
            byte[] buf = Program.SendByteResponse(ctx.Request);
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.ContentType = "image/png";
            Console.WriteLine("Serving image: " + (buf.Length / 1024) + "MB");
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }

        void RespondText (HttpListenerContext ctx)
        {
            Console.WriteLine("Sending Text for " + ctx.Request.RawUrl);
            string rstr = _responderMethod(ctx.Request);
            if (rstr.Substring(0,6) == "<html>")
            {
                ctx.Response.ContentType = "text/html";
            } else
            {
                Console.WriteLine(rstr.Substring(0, 4));
            }
            byte[] buf = Encoding.UTF8.GetBytes(rstr);
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.ContentLength64 = buf.Length;
            Console.WriteLine("Serving Text: " + buf.Length + "KB");
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
        }
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
