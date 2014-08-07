using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VoiceChatWPF.Models;
using VoiceChatWPF.ViewModels;

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
        private readonly byte[] _byteData;
        public EventHandler<QuestionEvent> AskHandler;
        public EventHandler<CustomEventArgs> ButtonEvent;
        private EndPoint _otherPartyEp;
        private IPEndPoint _otherPartyIp; //IP of party we want to make a call.
        private string _receivename;
        private Socket _socketListener;

        public ListeningEndpoint()
        {
            _byteData = new byte[1024];
            StartListening();
        }


        public void StartListening()
        {
            try
            {
                //Initialize new Socket
                _socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //Binds Socket to AnyIP at specified Port
                EndPoint ep = new IPEndPoint(IPAddress.Any, PortNum);
                _socketListener.Bind(ep);
                //Start listen for any data from specified Address/Port and launch event when received
                _socketListener.BeginReceiveFrom(_byteData, 0, _byteData.Length, SocketFlags.None, ref ep,
                    ConfirmConnection, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\nProbably duplicate instance, Application will be closed",
                    "Duplicated Instance?", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        ///     Connect to the IP written in the TextBox (If Valid)
        /// </summary>
        public void Connect()
        {
            IPAddress ip;
            if (IPAddress.TryParse(VoiceChatViewModel.Adress, out ip))
            {
                _otherPartyEp = new IPEndPoint(ip, PortNum);
                SendMessage(DataCom.Command.Invite, _otherPartyEp);
            }
            else
            {
                MessageBox.Show("IP Adress is Invalid!");
            }
        }


        private void OnSend(IAsyncResult ar)
        {
            try
            {
                _socketListener.EndSendTo(ar);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "VoiceChat-OnSend ()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SendMessage(DataCom.Command cmd, EndPoint sendToEP)
        {
            try
            {
                //  Create the message to send.
                var msgToSend = new DataCom
                {
                    Name = Convert.ToString(Environment.UserName, CultureInfo.InvariantCulture),
                    cmdCommand = cmd
                };

                //Name of the user.
                byte[] message = msgToSend.ToByte();

                //Send the message asynchronously.
                _socketListener.BeginSendTo(message, 0, message.Length, SocketFlags.None, sendToEP,
                    OnSend, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "VoiceChat-SendMessage ()", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Confirms that the connection is to be established
        ///     If user declines it will close and start listening for new connecitons
        /// </summary>
        /// <returns></returns>
        private void ConfirmConnection(IAsyncResult ar)
        {
            try
            {
                EventHandler<CustomEventArgs> handler = ButtonEvent;

                EndPoint receivedFromEP = new IPEndPoint(IPAddress.Any, 0);


                //Get the IP from where we got a message.
                _socketListener.EndReceiveFrom(ar, ref receivedFromEP);

                //Convert the bytes received into an object of type Data.
                var msgReceived = new DataCom(_byteData);

                //Act according to the received message.
                switch (msgReceived.cmdCommand)
                {
                        //We have an incoming call.
                    case DataCom.Command.Invite:
                    {
                        //We have no active call.
                        if (handler != null)
                            handler(this, new CustomEventArgs(false, true));
                        //Ask the user to accept the call or not.
                        if (MessageBox.Show("Call coming from " + msgReceived.Name + ".\r\n\r\nAccept it?", "Accept"
                            , MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            _receivename = msgReceived.Name;
                            SendMessage(DataCom.Command.Ok, receivedFromEP);
                            _otherPartyEp = receivedFromEP;
                            _otherPartyIp = (IPEndPoint) receivedFromEP;
                            //ICall();
                        }
                        else
                        {
                            //The call is declined. Send a busy response.
                            SendMessage(DataCom.Command.Busy, receivedFromEP);


                            //_tcpClient.Close();
                            //_tcpClient = new TcpClient();
                        }
                        break;
                    }

                        //OK is received in response to an Invite.
                    case DataCom.Command.Ok:
                    {
                        if (handler != null)
                            handler(this, new CustomEventArgs(true, false));
                        //Start a call.

                        //ICall();
                        break;
                    }

                        //Remote party is busy.
                    case DataCom.Command.Busy:
                    {
                        MessageBox.Show("User busy.", "VoiceChat", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        break;
                    }

                    case DataCom.Command.Bye:
                    {
                        //Check if the Bye command has indeed come from the user/IP with which we have
                        //a call established. This is used to prevent other users from sending a Bye, which
                        //would otherwise end the call.
                        if (receivedFromEP.Equals(_otherPartyEp))
                        {
                            //End the call.

                            //UninitializeCall();
                        }
                        break;
                    }
                }
                //Get ready to receive more commands.
                _socketListener.BeginReceiveFrom(_byteData, 0, _byteData.Length, SocketFlags.None, ref receivedFromEP,
                    ConfirmConnection, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveFrom: " + ex.Message);
            }
        }

        public void Dispose()
        {
            if (_socketListener != null) _socketListener.Close();
        }
    }
}