using System;
using System.Net.Sockets;
using System.Text;

namespace VoiceChatWPF.Models
{
    internal class Endpoint
    {
        public  void ReadData(Socket stream, out byte[] readBytes)
        {
            readBytes = new byte[sizeof(ushort)];
            stream.Receive(readBytes);
            int length = BitConverter.ToUInt16(readBytes, 0);
            readBytes = new byte[length];
            stream.Receive(readBytes);
        }

        public void WriteData(Socket stream, byte[] readBytes)
        {
            byte[] bytelength = BitConverter.GetBytes((ushort)readBytes.Length);
            stream.Send(bytelength);
            stream.Send(readBytes);
        }

        public  byte[] GetPcName()
        {
            string pcName = Environment.UserName;
            return Encoding.ASCII.GetBytes(pcName);
        }

        public  void TimeCheck(byte[] data , int bytesRecorded)
    {
     
    }
    }
}
