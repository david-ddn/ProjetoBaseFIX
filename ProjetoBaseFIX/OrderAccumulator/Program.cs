using OrderAccumulator;

Console.WriteLine("Iniciando Server");
var acceptor = new Acceptor();
Console.WriteLine("Server Iniciado");
new SemaphoreSlim(0).Wait();