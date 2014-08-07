﻿
using System;

namespace VoiceChatWPF.Models
{
    class Model : IDisposable
    {
        public ConnectionEndPoint ConnectionEndPoint;
        public ListeningEndpoint ListeningEndpoint;

        public Model()
        {
            ConnectionEndPoint = new ConnectionEndPoint();
            ListeningEndpoint = new ListeningEndpoint();
            ListeningEndpoint.ConnectHandler += ConnectHandler;
            ListeningEndpoint.DisconnectHandler += DisconnectHandler; 
        }

        private void DisconnectHandler(object sender, ConnectionEvent e)
        {
           ConnectionEndPoint.CloseConnections();
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
