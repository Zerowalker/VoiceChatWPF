
using System;
using System.Windows.Threading;
using VoiceChatWPF.Events;
using VoiceChatWPF.Network;

namespace VoiceChatWPF.Models
{
    class Model : IDisposable
    {
        public ConnectionEndPoint ConnectionEndPoint;
        public ListeningEndpoint ListeningEndpoint;
        public DispatcherTimer AudioTimer;
        public Model()
        {
            ConnectionEndPoint = new ConnectionEndPoint();
            ListeningEndpoint = new ListeningEndpoint();
            AudioTimer = ConnectionEndPoint.AudioTimer;
            ListeningEndpoint.ConnectHandler += ConnectHandler;
            ListeningEndpoint.DisconnectHandler += DisconnectHandler;
        }

        private void DisconnectHandler(object sender, ConnectionEvent e)
        {
           ConnectionEndPoint.CloseConnections();
            AudioTimer.Stop();
        }

        private void ConnectHandler(object sender, ConnectionEvent connectionEvent)
        {
            ConnectionEndPoint.EstablishConnection(connectionEvent.GetAddress);
        }

        public void Dispose()
        {
            ConnectionEndPoint.Dispose();
            ListeningEndpoint.Dispose();
        }
    }
}
