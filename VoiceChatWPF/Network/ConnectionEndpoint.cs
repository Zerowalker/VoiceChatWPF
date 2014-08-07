using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VoiceChatWPF.Models
{
    public class CustomEventArgs : EventArgs
    {
        public CustomEventArgs(bool s, bool s2)
        {
            GetConnectionStatus = s;
            GetDisconnectionStatus = s2;
        }

        public bool GetDisconnectionStatus { get; set; }
        public bool GetConnectionStatus { get; set; }
    }

    internal class ConnectionEndPoint : Endpoint, IDisposable
    {
        private const int PortNum = 7500;
        public EventHandler<CustomEventArgs> ButtonEvent;
        private Socket _tcpClient;
        private Socket TcpServer;
        private Socket _tcpServerClient;
        private bool _alive;
        private readonly PlaybackEndpoint _playbackEndpoint;
        private RecordEndPoint _recordEndPoint;

        public ConnectionEndPoint()
        {
            TcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = new IPEndPoint(IPAddress.Any, PortNum);
            TcpServer.Bind(ep);
            TcpServer.Listen(0);
            _playbackEndpoint = new PlaybackEndpoint();
            
        }

        public void Dispose()
        {
            if (_alive)
                CloseConnections();
        }


        /// <summary>
        ///     Try connect with 1 sec Timeout and waits for the Receiver to Accept the connection
        ///     Also starts the Audio Recording and transfer
        /// </summary>
      internal void EstablishConnection(IPAddress ip)
        {
            try
            {
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(false, false));
                        //Enable Disconnect Button and Disable Connect Button
                _tcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                Task.Run((Action) RV2);

                IAsyncResult result = _tcpClient.BeginConnect(ip, PortNum, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10)); //Try to connect for 2 seconds

                if (!success)
                {
                    throw new Exception("Connection Timeout");
                }

                _tcpClient.EndConnect(result);

                _recordEndPoint = new RecordEndPoint(_tcpClient);
                _tcpClient.NoDelay = true;
                if (handler != null)
                    handler(this, new CustomEventArgs(false, true));
                        //Enable Disconnect Button and Disable Connect Button

                BeginRecording();
                _alive = true;
            }
            catch (Exception e)
            {
                _tcpClient.Close();
                MessageBox.Show("BeginConnect: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(true, false));
                        //Disable Disconnect Button and Enable Connect Button
            }
        }


        private void RV2()
        {
             _tcpServerClient = TcpServer.Accept();
            _playbackEndpoint.PlaybackLoop(ref _tcpServerClient);
            

            //TcpServer.DuplicateAndClose(0);

        }

        /// <summary>
        /// Start Recording and Begin the PlaybackLoop
        /// </summary>
        private void BeginRecording()
        {
            _recordEndPoint.Recording(true);
        }

        /// <summary>
        ///     Close all connections (Null Checks)
        ///     Stop Recording and Playing
        /// </summary>
        public void CloseConnections()
        {

                if (_recordEndPoint != null)
                    _recordEndPoint.Recording(false); ;
                
                if (_tcpClient != null)
                    _tcpClient.Close();
                if (_tcpServerClient != null) _tcpServerClient.Close();

            //if (_playbackEndpoint != null) _playbackEndpoint.Dispose();
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(true, false));
                        //Disable Disconnect Button and Enable Connect Button

                _alive = false;

        }
    }
}