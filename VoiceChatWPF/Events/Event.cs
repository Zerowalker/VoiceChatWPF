using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VoiceChatWPF.Events
{
    public class ButtonHandlerEvent : EventArgs
    {
        public ButtonHandlerEvent(bool s, bool s2)
        {
            GetConnectionStatus = s;
            GetDisconnectionStatus = s2;
        }

        public bool GetDisconnectionStatus { get; set; }
        public bool GetConnectionStatus { get; set; }


    }
    public class ConnectionEvent : EventArgs
    {
        public ConnectionEvent(IPAddress address)
        {
            GetAddress = address;
        }

        public IPAddress GetAddress { get; set; }
    }
    public class AudioDeviceEvent : EventArgs
    {
        public AudioDeviceEvent(int device , int bufferlength)
        {
           DeviceNumber = device;
            BufferLength = bufferlength;
        }

        public int DeviceNumber { get; set; }
        public int BufferLength { get; set; }
    }
}