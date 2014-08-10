using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using VoiceChatWPF.Audio;
using VoiceChatWPF.Events;
using VoiceChatWPF.Models;

namespace VoiceChatWPF.Network
{


    internal class ConnectionEndPoint : Endpoint, IDisposable
    {
        private const int PortNum = 7500;
        private readonly Socket _socketServer;
        public EventHandler<ButtonHandlerEvent> ButtonEvent;
        public  EventHandler<AudioDeviceEvent> DeviceHandler; 
        private bool _alive;
        private PlaybackEndpoint _playbackEndpoint;
        public RecordEndPoint _recordEndPoint;
        private Socket _socketClient;
        private int _device;
        private int _bufferLength = 10;
        public DispatcherTimer AudioTimer;
        public ConnectionEndPoint()
        {
            DeviceHandler += OnDeviceChange;
            _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = new IPEndPoint(IPAddress.Any, PortNum);
            _socketServer.Bind(ep);
            _socketServer.Listen(0);

            AudioTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10),
            };
        }

        /// <summary>
        /// If Device or Buffer length is changed, reset Recording to re-initialize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="audioDeviceEvent"></param>
        private void OnDeviceChange(object sender, AudioDeviceEvent e)
        {
            _device = e.DeviceNumber;
            _bufferLength = e.BufferLength;
            if (_recordEndPoint != null) _recordEndPoint.Reinitialize(e.DeviceNumber,e.BufferLength);
        }


        public void Dispose()
        {
            if (_alive)
                CloseConnections();
            if (_playbackEndpoint != null) _playbackEndpoint.Dispose();
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
                AudioTimer.Start();
                BeginRecording();
            }
            catch (Exception)
            {
                _socketClient.Close();
                //Disable Disconnect Button and Enable Connect Button
                if (ButtonEvent != null) ButtonEvent(this, new ButtonHandlerEvent(true, false));
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
            _recordEndPoint.Reinitialize(_device,_bufferLength);
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
                //Stop Recording SendStream and  WasapiLoopBack
                _recordEndPoint.Recording(false);
                _recordEndPoint.Dispose();
                _recordEndPoint = null;
            }

            if (_playbackEndpoint != null) _playbackEndpoint.Dispose();

            if (_socketClient != null)
            {
                _socketClient.Shutdown(SocketShutdown.Both);
                _socketClient.Close();
            }


            //Disable Disconnect Button and Enable Connect Button
            if (ButtonEvent != null) ButtonEvent(this, new ButtonHandlerEvent(true, false));
        }
    }
}