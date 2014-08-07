using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VoiceChatWPF.Models;

public class QuestionEvent : EventArgs
{
    public QuestionEvent(byte[] username)
    {
        Username = username;
    }

    public bool Value { get; set; }
    public byte[] Username { get; set; }
}
namespace VoiceChatWPF
{
    internal class ListeningEndpoint : Endpoint, IDisposable
    {

        private const int PortNum = 7500;
        public bool Accepted;
        public EventHandler<QuestionEvent> AskHandler;
        public EventHandler<CustomEventArgs> ButtonEvent;
        public Stopwatch ChatDuration = new Stopwatch();
        private bool _isDisposing;
        private PlaybackEndpoint _playEndpoint;
        private RecordEndPoint _recordEndPoint;
        private Socket _serverSocket;
        private Socket _tcpListener;
        public ListeningEndpoint()
        {
            StartListening();
        }

        public void Dispose()
        {
            
            CloseServer();
        }


        public void StartListening()
        {
            try
            {
                _tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var ep = new IPEndPoint(IPAddress.Any, PortNum);
                _tcpListener.Bind(ep);
                _playEndpoint = new PlaybackEndpoint();
                _tcpListener.Listen(0);
                _tcpListener.BeginAccept(WaitForConnection, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\nProbably duplicate instance, Application will be closed",
                    "Duplicated Instance?", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        ///     Confirms that the connection is to be established
        ///     If user declines it will close and start listening for new connecitons
        /// </summary>
        /// <returns></returns>
        private bool ConfirmConnection()
        {
            byte[] recieverName;
            ReadData(_serverSocket, out recieverName);
            if (AskHandler != null)
            {
                EventHandler<QuestionEvent> handler = AskHandler;
                handler(null, new QuestionEvent(recieverName));
            }


            var checkByte = new byte[1];


            if (Accepted)
            {
                checkByte[0] = 1;
                _serverSocket.Send(checkByte); // Sends 1 to accept Connection
                _tcpListener.Close();
                _serverSocket.Close();
                return true;
            }

            checkByte[0] = 0;
            _serverSocket.Send(checkByte); //Sends 0 to deny Connection
            _serverSocket.Close();
            _tcpListener.BeginAccept(WaitForConnection, _serverSocket);
            //Begin listening for new connections.
            return false;
        }

        /// <summary>
        ///     Makes the Receiving TCP Client, and awaits Confirmation
        ///     If accepted it starts the PlaybackLoop
        /// </summary>
        /// <param name="asyncResult"></param>
        private void WaitForConnection(IAsyncResult asyncResult)
        {
            try
            {
                EventHandler<CustomEventArgs> handler = ButtonEvent;
                _serverSocket = _tcpListener.EndAccept(asyncResult);

                if (!ConfirmConnection())
                {
                    return;
                }

                if (handler != null)
                    handler(this, new CustomEventArgs(false, true));

                _recordEndPoint = new RecordEndPoint(_serverSocket);
                Task.Factory.StartNew(param => { _recordEndPoint.StartRecording(); _playEndpoint.PlaybackLoop(_serverSocket); }, TaskCreationOptions.LongRunning)
                    .ContinueWith(param =>
                    {
                        if (_isDisposing) return;
                        ChatDuration.Stop();
                        CloseServer();
                        StartListening();
                    }, CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception e)
            {
                Console.Write(@"WaitForConnection: " + e.Message);
            }
        }


        public void CloseServer()
        {
            if (_playEndpoint != null) _playEndpoint.Dispose();
            if (_recordEndPoint != null) _recordEndPoint.Dispose();
            if (_serverSocket != null) _serverSocket.Close();
            if (_tcpListener != null) _tcpListener.Close();

   
            EventHandler<CustomEventArgs> handler = ButtonEvent;
            if (handler != null)
                handler(this, new CustomEventArgs(false, false));
        }
    }
}