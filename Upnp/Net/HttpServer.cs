using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using HttpListener = Mono.Net.HttpListener;
using HttpListenerContext = Mono.Net.HttpListenerContext;
using HttpListenerRequest = Mono.Net.HttpListenerRequest;
using HttpListenerResponse = Mono.Net.HttpListenerResponse;

namespace Upnp.Net
{

    
    class HttpServer
    {
        class Route
        {
            public string Method { get; set; }
            public string Url { get; set; }
            public Action<HttpListenerRequest, HttpListenerResponse> Action { get; set; }
        }

        private readonly List<Route> _routes;
        private readonly HttpListener _listener;

        public HttpServer(params string[] prefixes)
        {
            _listener = new HttpListener();
            _routes = new List<Route>();

            foreach (var prefix in prefixes)
                _listener.Prefixes.Add(prefix);
        }

        public virtual void Start()
        {
            _listener.Start();
            _listener.BeginGetContext(OnGetContext, null);
        }

        public void AddRoute(string method, string url, Action<HttpListenerRequest, HttpListenerResponse> action)
        {
            lock (this._routes)
            {
                this._routes.Add(new Route{Action = action, Method = method.ToUpper(), Url = url});
            }
        }

        protected virtual void HandleContext(HttpListenerContext context)
        {
            //context.Request.Url
            var req = context.Request;
            var res = context.Response;

            List<Route> routes;
            lock (_routes)
            {
                routes = new List<Route>(this._routes);
            }
            var matchingUrl = false;
            foreach (var route in routes.Where(route => route.Url == req.RawUrl))
            {
                matchingUrl = true;

                if (route.Method == req.HttpMethod.ToUpper())
                {
                    route.Action(req, res);
                    return;
                }
            }

            if (matchingUrl)
            {
                //Allow: GET, HEAD, PUT
                var allowedMethods = string.Join(", ", routes.Where(route => route.Url == req.RawUrl).Select(r => r.Method).Distinct());
                req.Headers["Allow"] = allowedMethods;
                res.StatusCode = 405;
                res.StatusDescription = "Method Not Allowed";
                res.Close();
            }
            
            res.StatusCode = 404;
            res.StatusDescription = "Not Found";
            res.Close();
        }

        public virtual void Stop()
        {
            lock (_listener)
            {
                _listener.Stop();
            }
        }

        public virtual void Dispose()
        {
            lock (_listener)
            {
                _listener.Close();
            }
        }


        void OnGetContext(IAsyncResult asyncResult)
        {
            lock (_listener)
            {
                if (!_listener.IsListening)
                    return;

                var context = _listener.EndGetContext(asyncResult);

                HandleContext(context);

                try
                {
                    // FIXME this is a bug in Mono
                    context.Response.OutputStream.Close();
                    context.Response.Close();
                }
                catch (ObjectDisposedException)
                {
                    // If we already completed the context in HandleContext we may get this exception, just ignore it...
                }

                _listener.BeginGetContext(OnGetContext, null);
            }
        }
    }
}
