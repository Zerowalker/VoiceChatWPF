using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly Socket _socketServer;
        public EventHandler<CustomEventArgs> ButtonEvent;
        private bool _alive;
        private PlaybackEndpoint _playbackEndpoint;
        private RecordEndPoint _recordEndPoint;
        private Socket _socketClient;

        public ConnectionEndPoint()
        {
            _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = new IPEndPoint(IPAddress.Any, PortNum);
            _socketServer.Bind(ep);
            _socketServer.Listen(0);
        }

        public void Dispose()
        {
            if (_alive)
                CloseConnections();
            if (_playbackEndpoint != null) _playbackEndpoint.Dispose();
            if (_recordEndPoint != null) _recordEndPoint.Dispose();
            if (_socketServer != null) _socketServer.Close();
        }


        /// <summary>
        ///     Try connect with 1 sec Timeout and waits for the Receiver to Accept the connection
        ///     Also starts the Audio Recording and transfer
        /// </summary>
        internal void EstablishConnection(IPAddress ip)
        {
            try
            {
                _alive = true;
                _playbackEndpoint = new PlaybackEndpoint();
                _socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                Task.Factory.StartNew(param => BeginPlayback(), CancellationToken.None,
                    TaskCreationOptions.LongRunning);

                IAsyncResult result = _socketClient.BeginConnect(ip, PortNum, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2)); //Try to connect for 2 seconds

                if (!success)
                {
                    throw new Exception("Connection Timeout");
                }

                _socketClient.EndConnect(result);

                _recordEndPoint = new RecordEndPoint(_socketClient);


                BeginRecording();
                _alive = true;
            }
            catch (Exception e)
            {
                _socketClient.Close();
                //Disable Disconnect Button and Enable Connect Button
                if (ButtonEvent != null) ButtonEvent(this, new CustomEventArgs(true, false));
            }
        }

        /// <summary>
        ///     Start Playbackloop, constantly reading from Socket to BufferedWaveProvider
        /// </summary>
        private void BeginPlayback()
        {
            //Make a TCP client which is disposed when the method ends
            using (Socket client = _socketServer.Accept())
                _playbackEndpoint.PlaybackLoop(client);
        }

        /// <summary>
        ///     Start Recording from WaveInEvent to BlockingCollection
        /// </summary>
        private void BeginRecording()
        {
            _recordEndPoint.Recording(true, false);
        }

        /// <summary>
        ///     Close all connections (Null Checks)
        ///     Stop Recording and Playing
        /// </summary>
        public void CloseConnections()
        {
            if (!_alive) return;
            _alive = false;

            if (_recordEndPoint != null)
            {
                //Stop Recording SendStream
                _recordEndPoint.Recording(false);
                //Stop Recording WasapiLoopBack
                _recordEndPoint.Recording(true, true);
            }

            if (_playbackEndpoint != null) _playbackEndpoint.Dispose();

            if (_socketClient != null)
            {
                _socketClient.Shutdown(SocketShutdown.Both);
                _socketClient.Close();
            }


            //Disable Disconnect Button and Enable Connect Button
            if (ButtonEvent != null) ButtonEvent(this, new CustomEventArgs(true, false));
        }
    }
}