using EletronicTradingModel;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace OrderGenerator.WebImplementation
{
    internal class WebSocketConnection
    {
        private WebSocket _webSocket;
        private CancellationToken _cancellationTokenService;
        public WebSocketConnection(WebSocket webSocket, CancellationToken cancellationTokenService)
        {
            _webSocket = webSocket;
            _cancellationTokenService = cancellationTokenService;

            Task.Run(ProcessMessageReceived);
        }

        private async Task ProcessMessageReceived()
        {
            try
            {
                byte[] buffer = new byte[4096];
                while (_webSocket.State == WebSocketState.Open && !_cancellationTokenService.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenService);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Conexão encerrada", _cancellationTokenService);
                    }
                    else
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        try
                        {
                            Initiator.Instance.SendNewOrder(JsonConvert.DeserializeObject<NewOrder>(receivedMessage), this);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erro ao processar mensagem recebida: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro WebsocketClient: {ex}");
            }
        }

        public void SendMessageClient(string message)
        {
            try
            {
                byte[] responseBuffer = Encoding.UTF8.GetBytes(message);
                _ = _webSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, _cancellationTokenService);
            }
            catch { }
        }
    }
}
