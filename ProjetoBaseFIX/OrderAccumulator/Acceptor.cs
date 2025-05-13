using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace OrderAccumulator
{
    public class Acceptor : MessageCracker, IApplication
    {
        IAcceptor _acceptor;
        public Acceptor()
        {
            var settings = new SessionSettings(".\\configs\\QuickFixSessionServer.cfg");
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            ILogFactory logFactory = new FileLogFactory(settings);
            _acceptor = new ThreadedSocketAcceptor(this, storeFactory, settings, logFactory);

            _acceptor.Start();
        }

        public void Dispose()
        {
            _acceptor?.Stop();
            _acceptor?.Dispose();
        }

        public void FromApp(Message message, SessionID sessionID) => Crack(message, sessionID);
        public void ToApp(Message message, SessionID sessionID) { }
        public void OnMessage(QuickFix.FIX44.NewOrderSingle message, SessionID sessionID) => Session.SendToTarget(OrderRepositoryProcessor.Instance.NewOrder(message), sessionID);

        public void FromAdmin(Message message, SessionID sessionID) { }
        public void ToAdmin(Message message, SessionID sessionID) { }
        public void OnCreate(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
    }
}
