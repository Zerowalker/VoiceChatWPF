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

    internal class ClientEndpoint : Endpoint, IDisposable
    {
        private const int PortNum = 7500;
        public EventHandler<CustomEventArgs> ButtonEvent;
        public Socket TcpClient;
        private bool _alive;
        private PlaybackEndpoint _playbackEndpoint;
        private RecordEndPoint _recordEndPoint;

        public void Dispose()
        {
            if (_alive)
                CloseConnections();
        }


        /// <summary>
        ///     Connect to the IP written in the TextBox (If Valid)
        /// </summary>
        public void Connect()
        {
            IPAddress ip;
            if (IPAddress.TryParse(VoiceChatViewModel.Adress, out ip))
            {
                _playbackEndpoint = new PlaybackEndpoint();
                Task.Run(() => EstablishConnection(ip));
            }
            else
            {
                MessageBox.Show("IP Adress is Invalid!");
            }
        }

        /// <summary>
        ///     Sends PC name to the Server, Server will then ask User if it accepts the Connection.
        /// </summary>
        /// <returns></returns>
        private void ConfirmConnection()
        {
            //Writes PC Username, and awaits confirmation
            WriteData(TcpClient, GetPcName()):

            var readbyte = new byte[1];
            TcpClient.Receive(readbyte);


            //Checks if byte is 0, if so close the connection else accept
            if (readbyte[0] == 0)
            {
                throw new Exception("Connection Refused");
            }
            MessageBox.Show("Server Accepted");
        }

        /// <summary>
        ///     Try connect with 1 sec Timeout and waits for the Receiver to Accept the connection
        ///     Also starts the Audio Recording and transfer
        /// </summary>
        private void EstablishConnection(IPAddress ip)
        {
            try
            {
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(false, false));
                        //Enable Disconnect Button and Disable Connect Button
                TcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

                IAsyncResult result = TcpClient.BeginConnect(ip, PortNum, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2)); //Try to connect for 2 seconds

                if (!success)
                {
                    throw new Exception("Connection Timeout");
                }

                TcpClient.EndConnect(result);

                ConfirmConnection();
                _recordEndPoint = new RecordEndPoint(TcpClient);
                TcpClient.NoDelay = true;
                if (handler != null)
                    handler(this, new CustomEventArgs(false, true));
                        //Enable Disconnect Button and Disable Connect Button

                BeginRecAndPlayback();
                _alive = true;
            }
            catch (Exception e)
            {
                TcpClient.Close();
                MessageBox.Show("BeginConnect: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(true, false));
                        //Disable Disconnect Button and Enable Connect Button
            }
        }


        /// <summary>
        /// Start Recording and Begin the PlaybackLoop
        /// </summary>
        private void BeginRecAndPlayback()
        {
            _recordEndPoint.StartRecording();
            Task.Factory.StartNew(param => _playbackEndpoint.PlaybackLoop(TcpClient), TaskCreationOptions.LongRunning)
                .ContinueWith(param => CloseConnections());
        }

        /// <summary>
        ///     Close all connections (Null Checks)
        ///     Stop Recording and Playing
        /// </summary>
        public void CloseConnections()
        {

                if (_recordEndPoint != null)
                {
                    _recordEndPoint.Dispose();
                }
                if (TcpClient != null)
                    TcpClient.Close();
                if (_playbackEndpoint != null) _playbackEndpoint.Dispose();
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                if (handler != null)
                    handler(this, new CustomEventArgs(true, false));
                        //Disable Disconnect Button and Enable Connect Button

                _alive = false;

        }
    }
}