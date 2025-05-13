using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net.WebSockets;
namespace OrderGenerator.WebImplementation
{
    internal class StaticFilesHttpServer
    {

        private static SemaphoreSlim _instanceSemaphore = new SemaphoreSlim(1);
        private static StaticFilesHttpServer _instance;
        public static StaticFilesHttpServer Instance
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
                            _instance = new StaticFilesHttpServer();
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

        private CancellationTokenSource _cancelationToken;
        private List<WebSocketConnection> _webSocketConnections;
        private StaticFilesHttpServer()
        {
            _cancelationToken = new CancellationTokenSource();
            _webSocketConnections = new List<WebSocketConnection>();
            string url = "http://localhost:8080";
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot("wwwroot")
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .UseUrls(url)
                .Configure(app =>
                {
                    app.UseDefaultFiles();
                    app.UseStaticFiles();
                })
                .Build();

            host.Start();

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }

            Console.WriteLine($"Para abrir a interface web, acesse: {url}");
        }
    }
}