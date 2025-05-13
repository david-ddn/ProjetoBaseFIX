using EletronicTradingModel;
using EletronicTradingModel.Enum;
using Newtonsoft.Json;
using OrderGenerator.WebImplementation;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using System.Collections.Concurrent;

namespace OrderGenerator
{
    public class Initiator : MessageCracker, IApplication, IDisposable
    {
        #region Singleton
        private static SemaphoreSlim _instanceSemaphore = new SemaphoreSlim(1);
        private static Initiator _instance;
        public static Initiator Instance
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
                            _instance = new Initiator();
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

        IInitiator _initiator;
        SessionID _sessionID;
        ConcurrentDictionary<string, WebSocketConnection> _pedingOrderResult;
        private Initiator()
        {
            _pedingOrderResult = new();
            var settings = new SessionSettings(".\\configs\\QuickFixSessionClient.cfg");
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            _initiator = new SocketInitiator(this, storeFactory, settings, logFactory);
            _initiator.Start();
            _sessionID = _initiator.GetSessionIDs().First();
        }

        public void Dispose()
        {
            _pedingOrderResult?.Clear();
            _initiator?.Stop();
            _initiator?.Dispose();
        }

        void IApplication.FromApp(Message message, SessionID sessionID) => Crack(message, sessionID);
        public void OnMessage(QuickFix.FIX44.ExecutionReport message, SessionID sessionID)
        {
            var clOrdID = message.GetString(ClOrdID.TAG);
            if (_pedingOrderResult.TryRemove(clOrdID, out var connection))
            {
                if (message.GetChar(OrdStatus.TAG) == OrdStatus.NEW)
                {
                    connection.SendMessageClient(JsonConvert.SerializeObject(new ResultReport()
                    {
                        IsAccepted = true,
                        Message = $"Ordem aceita. ID:{clOrdID}",
                        ClOrdID = clOrdID
                    }));
                }
                else
                {
                    connection.SendMessageClient(JsonConvert.SerializeObject(new ResultReport()
                    {
                        IsAccepted = false,
                        Message = $"{message.GetString(Text.TAG)}. ID:{clOrdID}",
                        ClOrdID = clOrdID
                    }));
                }
            }
        }

        internal void SendNewOrder(NewOrder newOrder, WebSocketConnection webSocketConnection)
        {
            if (_initiator.IsLoggedOn)
            {
                string clOrdID = DateTime.Now.Ticks.ToString();
                _pedingOrderResult[clOrdID] = webSocketConnection;
                Session.SendToTarget(new QuickFix.FIX44.NewOrderSingle()
                {
                    ClOrdID = new ClOrdID(clOrdID),
                    Symbol = new Symbol(newOrder.Symbol),
                    Side = new Side(newOrder.Side == OrderSide.Buy ? Side.BUY : Side.SELL),
                    OrdType = new OrdType(OrdType.LIMIT),
                    Price = new Price(newOrder.Price),
                    OrderQty = new OrderQty(newOrder.OrderQty),
                    TransactTime = new TransactTime(DateTime.Now),
                }, _sessionID);
            }
            else
            {
                webSocketConnection.SendMessageClient(JsonConvert.SerializeObject(new ResultReport()
                {
                    IsAccepted = false,
                    Message = "Problema de conexão com a bolsa.",
                }));
            }
        }

        void IApplication.ToApp(Message message, SessionID sessionID) { }
        void IApplication.FromAdmin(Message message, SessionID sessionID) { }
        void IApplication.ToAdmin(Message message, SessionID sessionID) { }
        public void OnCreate(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
    }
}
