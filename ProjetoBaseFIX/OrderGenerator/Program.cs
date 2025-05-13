using OrderGenerator;
using OrderGenerator.WebImplementation;

Console.WriteLine("Iniciando Client");
var initiator = Initiator.Instance;
var websocketInstance = WebSocketServer.Instance;
var httpServerStaticFiles = StaticFilesHttpServer.Instance;
Console.WriteLine("Client Iniciado");
new SemaphoreSlim(0).Wait();