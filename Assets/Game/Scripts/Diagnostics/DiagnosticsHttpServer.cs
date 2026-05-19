using System;
using System.Net;
using System.Text;
using System.Threading;

namespace Game.Scripts.Diagnostics
{
    public sealed class DiagnosticsHttpServer : IDisposable
    {
        private readonly DiagnosticsManager _manager;
        private readonly DiagnosticsConfig _config;
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;

        public bool IsRunning => _running;
        public string Url { get; private set; }

        public DiagnosticsHttpServer(DiagnosticsManager manager, DiagnosticsConfig config)
        {
            _manager = manager;
            _config = config;
        }

        public bool Start()
        {
            if (_running)
            {
                return true;
            }

            int firstPort = _config.HttpPort;
            int attempts = _config.AllowPortFallback ? Math.Max(1, _config.MaxPortFallbackAttempts) : 1;
            Exception lastException = null;

            for (int i = 0; i < attempts; i++)
            {
                int port = firstPort + i;
                if (TryStartOnPort(port, out lastException))
                {
                    return true;
                }

                if (!_config.AllowPortFallback)
                {
                    break;
                }
            }

            string message = lastException != null ? lastException.Message : "unknown error";
            UnityEngine.Debug.LogWarning("[Diagnostics] HTTP server failed to start on " + _config.BindAddress + ":" + firstPort + ": " + message);
            _running = false;
            return false;
        }

        private bool TryStartOnPort(int port, out Exception exception)
        {
            exception = null;
            HttpListener listener = null;
            try
            {
                listener = new HttpListener();
                string prefix = "http://" + _config.BindAddress + ":" + port + "/";
                listener.Prefixes.Add(prefix);
                listener.Start();
                _listener = listener;
                _config.HttpPort = port;
                Url = prefix.TrimEnd('/');
                _running = true;
                _thread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "DiagnosticsHttpServer"
                };
                _thread.Start();
                UnityEngine.Debug.Log("[Diagnostics] HTTP server listening on " + Url);
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                try
                {
                    if (listener != null)
                    {
                        listener.Close();
                    }
                }
                catch
                {
                }

                return false;
            }
        }

        public void Dispose()
        {
            _running = false;
            try
            {
                if (_listener != null)
                {
                    _listener.Stop();
                    _listener.Close();
                }
            }
            catch
            {
            }

            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join(500);
            }
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleContext(context));
                }
                catch
                {
                    if (!_running)
                    {
                        return;
                    }
                }
            }
        }

        private void HandleContext(HttpListenerContext context)
        {
            try
            {
                if (!IsAuthorized(context.Request))
                {
                    WriteResponse(context.Response, 401, "{\"ok\":false,\"error\":\"unauthorized\"}");
                    return;
                }

                string json = HandleRequest(context.Request, out int statusCode);
                WriteResponse(context.Response, statusCode, json);
            }
            catch (Exception ex)
            {
                WriteResponse(context.Response, 500, "{\"ok\":false,\"error\":\"" + EscapeError(ex.Message) + "\"}");
            }
        }

        private string HandleRequest(HttpListenerRequest request, out int statusCode)
        {
            statusCode = 200;
            string path = request.Url != null ? request.Url.AbsolutePath : string.Empty;
            int seconds = ReadSeconds(request, 10);

            if (path == "/diagnostics/health")
            {
                return _manager.BuildHealthJson();
            }

            if (path == "/diagnostics/current")
            {
                return _manager.BuildCurrentSnapshotJson();
            }

            if (path == "/diagnostics/last")
            {
                return _manager.BuildLastSamplesJson(seconds);
            }

            if (path == "/diagnostics/spikes")
            {
                return _manager.BuildSpikesJson(seconds);
            }

            if (path == "/diagnostics/frame-spikes")
            {
                return _manager.BuildFrameSpikesJson(seconds);
            }

            if (path == "/diagnostics/top/client")
            {
                return _manager.BuildTopScopesJson(DiagnosticsCategories.Client, seconds);
            }

            if (path == "/diagnostics/top/server")
            {
                return _manager.BuildTopScopesJson(DiagnosticsCategories.Server, seconds);
            }

            if (path == "/diagnostics/top/editor")
            {
                return _manager.BuildTopScopesJson(DiagnosticsCategories.Editor, seconds);
            }

            if (path == "/diagnostics/network")
            {
                return _manager.BuildNetworkJson(seconds);
            }

            if (path == "/diagnostics/analyze")
            {
                return _manager.BuildAnalyzeJson(seconds);
            }

            statusCode = 404;
            return "{\"ok\":false,\"error\":\"not_found\"}";
        }

        private bool IsAuthorized(HttpListenerRequest request)
        {
            if (!_config.RequiresToken())
            {
                return true;
            }

            string expected = _config.Token;
            if (string.IsNullOrEmpty(expected))
            {
                return false;
            }

            string token = request.Headers["X-Diagnostics-Token"];
            if (string.IsNullOrEmpty(token))
            {
                token = request.QueryString["token"];
            }

            return string.Equals(token, expected, StringComparison.Ordinal);
        }

        private static int ReadSeconds(HttpListenerRequest request, int fallback)
        {
            string value = request.QueryString["seconds"];
            if (string.IsNullOrEmpty(value))
            {
                value = request.QueryString["last"];
            }

            if (int.TryParse(value, out int parsed))
            {
                if (parsed < 1)
                {
                    return 1;
                }

                if (parsed > 300)
                {
                    return 300;
                }

                return parsed;
            }

            return fallback;
        }

        private static void WriteResponse(HttpListenerResponse response, int statusCode, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(json) ? "{}" : json);
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }

        private static string EscapeError(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
