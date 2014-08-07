using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using VoiceChatWPF.Models;
using VoiceChatWPF.ViewModels;

public class ConnectionEvent : EventArgs
{
    public ConnectionEvent(IPAddress address)
    {
        GetAddress = address;

    }

    public IPAddress GetAddress { get; set; }
}

namespace VoiceChatWPF
{
    internal class ListeningEndpoint : Endpoint, IDisposable
    {
        private const int PortNum = 7500;
        private readonly byte[] _byteData;
        public EventHandler<ConnectionEvent> ConnectHandler;
        public EventHandler<ConnectionEvent> DisconnectHandler;
        public EventHandler<CustomEventArgs> ButtonEvent;
        private EndPoint _otherPartyEp;
        private IPEndPoint _otherPartyIp; //IP of party we want to make a call.
        private string _receivename;
        private Socket _socketListener;
        private Model _model;
        private bool Solo;
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
                if (_socketListener != null)
                    _socketListener.Close();
                
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
        /// Fire Connection Event via Model
        /// </summary>
        private void FireConnectEvent()
        {
            try
            {
                var connect = ConnectHandler;
                if (connect != null)
                    connect(this, new ConnectionEvent(_otherPartyIp.Address));
            }
            catch (Exception e)
            {
                MessageBox.Show("FireConnectEvent: " + e.Message);
            }
        }
        /// <summary>
        /// Fire Disconnect Event via Model
        /// </summary>
        private void FireDisconnectEvent()
        {

            
            var connect = DisconnectHandler;
            if (connect != null)
                connect(this, new ConnectionEvent(_otherPartyIp.Address));
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
                var handler = ButtonEvent;

                EndPoint receivedFromEp = new IPEndPoint(IPAddress.Any, 0);


                //Get the IP from where we got a message.
                _socketListener.EndReceiveFrom(ar, ref receivedFromEp);
                _otherPartyIp = (IPEndPoint)receivedFromEp;
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
                            SendMessage(DataCom.Command.Ok, receivedFromEp);
                            _otherPartyEp = receivedFromEp;
                            _otherPartyIp = (IPEndPoint) receivedFromEp;
                            FireConnectEvent();
                            Solo = true;
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

                        if (handler != null)
                            handler(this, new CustomEventArgs(false, true));
                        //Start a call.
                        if(!Solo)
                        FireConnectEvent();

                     
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
                        if (receivedFromEp.Equals(_otherPartyEp))
                        {
                            //End the call.
                            FireDisconnectEvent();
                        }
                        break;
                    }
                }
                EndPoint ep = new IPEndPoint(IPAddress.Any, PortNum);
                //Get ready to receive more commands.
                _socketListener.BeginReceiveFrom(_byteData, 0, _byteData.Length, SocketFlags.None, ref ep,
                 ConfirmConnection, null);
            }
            catch (Exception ex)
            {
               MessageBox.Show("ReceiveFrom: " + ex.Message);
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
        public void Dispose()
        {
            if (_socketListener != null) _socketListener.Close();
        }
    }
}