using System.Net;
using System.Net.WebSockets;

namespace OrderGenerator.WebImplementation
{
    internal class WebSocketServer
    {
        #region Singleton
        private static SemaphoreSlim _instanceSemaphore = new SemaphoreSlim(1);
        private static WebSocketServer _instance;
        public static WebSocketServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instanceSemaphore.Wait();
                    try
                    {
                        if (_instance == null)
                        {
                            _instance = new WebSocketServer();
                        }
                    }
                    finally
                    {
                        _instanceSemaphore.Release();
                    }
                }
                return _instance;
            }
        }
        #endregion


        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private WebSocketServer()
        {
            _ = StartAsync(_cancellationTokenSource.Token);
        }

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            var host = "http://localhost:8081/ws/";
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add(host);
            httpListener.Start();
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                    new WebSocketConnection(wsContext.WebSocket, cancellationToken);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

    }
}
