using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using VoiceChatWPF.Events;
using VoiceChatWPF.Models;
using VoiceChatWPF.ViewModels;

namespace VoiceChatWPF.Network
{
    internal class ListeningEndpoint : Endpoint, IDisposable
    {
        private const int PortNum = 7600;
        private readonly byte[] _byteData;
        public EventHandler<ButtonHandlerEvent> ButtonEvent;
        public EventHandler<ConnectionEvent> ConnectHandler;
        public EventHandler<ConnectionEvent> DisconnectHandler;
        private bool _disposing;
        private EndPoint _otherPartyEp;
        private IPEndPoint _otherPartyIp; //IP of party we want to make a call.
        private Socket _socketListener;
        private bool _solo;

        public ListeningEndpoint()
        {
            _byteData = new byte[1024];
            StartListening();
        }

        public void Dispose()
        {
            if (_socketListener == null) return;
            _disposing = true;
            if (_otherPartyEp != null) SendMessage(DataCom.Command.Bye, _otherPartyEp);
            _socketListener.Shutdown(SocketShutdown.Both);
            _socketListener.Close();     
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
                    BeginReceive, null);
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
                AllowDisconnect();
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
            _socketListener.EndSendTo(ar);
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

                //Send DropCall signal before disposing
                if (_disposing)
                {
                    _socketListener.SendTo(message, sendToEP);
                    return;
                }
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
        ///     Fire Connection Event via Model
        /// </summary>
        private void FireConnectEvent()
        {
            if (ConnectHandler != null) ConnectHandler(this, new ConnectionEvent(_otherPartyIp.Address));
        }

        /// <summary>
        ///     Fire Disconnect Event via Model
        /// </summary>
        private void FireDisconnectEvent()
        {
            if (DisconnectHandler != null) DisconnectHandler(this, new ConnectionEvent(null));
        }

        /// <summary>
        ///     Fire Event to Disable Connect and Enable Disconnect
        /// </summary>
        private void AllowDisconnect()
        {
            if (ButtonEvent != null) ButtonEvent(this, new ButtonHandlerEvent(false, true));
        }

        /// <summary>
        ///     Fire Event to Enable Connect and Disable Disconnect
        /// </summary>
        private void AllowConnect()
        {
            if (ButtonEvent != null) ButtonEvent(this, new ButtonHandlerEvent(true, false));
        }

        /// <summary>
        ///     Confirms that the connection is to be established
        ///     If user declines it will close and start listening for new connecitons
        /// </summary>
        /// <returns></returns>
        private void BeginReceive(IAsyncResult ar)
        {
            try
            {
            EndPoint receivedFromEp = new IPEndPoint(IPAddress.Any, 0);

            //Get the IP from where we got a message.
            //Exits if Socket is closed as a safe measure when disposing
        
                _socketListener.EndReceiveFrom(ar, ref receivedFromEp);
  


            _otherPartyIp = (IPEndPoint) receivedFromEp;
            //Convert the bytes received into an object of type Data.
            var msgReceived = new DataCom(_byteData);

            //Act according to the received message.
            switch (msgReceived.cmdCommand)
            {
                    //We have an incoming call.
                case DataCom.Command.Invite:
                {
                    //We have no active call.

                    //Ask the user to accept the call or not.
                    if (MessageBox.Show("Call coming from " + msgReceived.Name + ".\r\n\r\nAccept it?", "Accept"
                        , MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        AllowDisconnect();
                        //_receivename = msgReceived.Name;
                        SendMessage(DataCom.Command.Ok, receivedFromEp);
                        _otherPartyEp = receivedFromEp;
                        _otherPartyIp = (IPEndPoint) receivedFromEp;
                        FireConnectEvent();
                        _solo = true;
                    }
                    else
                    {
                        //The call is declined. Send a busy response.
                        SendMessage(DataCom.Command.Busy, receivedFromEp);
                    }
                    break;
                }

                    //OK is received in response to an Invite.
                case DataCom.Command.Ok:
                {
                    //Start a call.
                    if (!_solo)
                        FireConnectEvent();
                    _solo = false;
                    break;
                }

                    //Remote party is busy.
                case DataCom.Command.Busy:
                {
                    AllowConnect();
                    MessageBox.Show("User busy.", "VoiceChat", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    break;
                }

                case DataCom.Command.Bye:
                {
                    //Check if the Bye command has indeed come from the user/IP with which we have
                    //a call established. This is used to prevent other users from sending a Bye, which
                    //would otherwise end the call.
                    if (receivedFromEp.Equals(_otherPartyEp))
                    {
                        AllowConnect();
                        //End the call.
                        FireDisconnectEvent();
                    }
                    break;
                }
            }
            EndPoint ep = new IPEndPoint(IPAddress.Any, PortNum);
            //Get ready to receive more commands.
            _socketListener.BeginReceiveFrom(_byteData, 0, _byteData.Length, SocketFlags.None, ref ep,
                BeginReceive, null);
            }
            catch
            {
            }
        }

        public void DropCall()
        {
            try
            {
                //Send a Bye message to the user to end the call.
                SendMessage(DataCom.Command.Bye, _otherPartyEp);

                FireDisconnectEvent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "VoiceChat-DropCall ()", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}