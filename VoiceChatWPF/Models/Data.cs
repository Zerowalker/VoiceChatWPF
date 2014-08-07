using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceChatWPF.Models
{
    internal class DataCom
    {
        //Default constructor.
        internal string Name; //Name by which the client logs into the room.
        internal Command cmdCommand; //Command type (login, logout, send message, etc).

        internal DataCom()
        {
            cmdCommand = Command.Null;
            Name = null;
        }

        //Converts the bytes into an object of type Data.
        internal DataCom(byte[] data)
        {
            //The first four bytes are for the Command.
            cmdCommand = (Command) BitConverter.ToInt32(data, 0);

            //The next four store the length of the name.
            int nameLen = BitConverter.ToInt32(data, 4);

            //This check makes sure that strName has been passed in the array of bytes.
            if (nameLen > 0)
                Name = Encoding.UTF8.GetString(data, 8, nameLen);
            else
                Name = null;
        }

        //Converts the Data structure into an array of bytes.
        internal byte[] ToByte()
        {
            var result = new List<byte>();

            //First four are for the Command.
            result.AddRange(BitConverter.GetBytes((int) cmdCommand));

            //Add the length of the name.
            if (Name != null)
                result.AddRange(BitConverter.GetBytes(Name.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name.
            if (Name != null)
                result.AddRange(Encoding.UTF8.GetBytes(Name));

            return result.ToArray();
        }

        internal enum Command
        {
            Invite, //Make a call.
            Bye, //End a call.
            Busy, //User busy.
            Ok, //Response to an invite message. OK is send to indicate that call is accepted.
            Null, //No command.
        }
    }
}