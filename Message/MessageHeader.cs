using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public enum MessageType : byte
    {
        System = 1,
        Login = 2,
        Chatting = 3,
    }
    public class MessageHeader
    {

        public int Length { get; set; } // 4 bytes
        public MessageType Type { get; set; } // 1 byte
        public byte Flag { get; set; } // 1 byte
        public long UnixTimeMilli { get; set; } // 8 bytes 

        public MessageHeader()
        {
            Length = 0;
            Type = 0;
            Flag = 0;
            UnixTimeMilli = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public MessageHeader(int length, MessageType type, byte flag)
        {
            Length = length;
            Type = type;
            Flag = flag;
            UnixTimeMilli = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void SetHeader(byte[] headerData, int offset)
        {
            Length = BitConverter.ToInt32(headerData, offset);
            Type = (MessageType)headerData[offset + 4];
            Flag = headerData[offset + 5];
            UnixTimeMilli = BitConverter.ToInt64(headerData, offset + 6);
        }

        public void Serialize(byte[] buffer, int offset)
        {
            BitConverter.GetBytes(Length).CopyTo(buffer, offset);
            buffer[4] = (byte)Type;
            buffer[5] = (byte)Flag;
            BitConverter.GetBytes(UnixTimeMilli).CopyTo(buffer, offset + 6);
        }

        public override string ToString()
        {
            return $"Length: {Length}, Type: {Type}, Flag: {Flag}, UnixTimeMilli: {UnixTimeMilli}";
        }
    }
}
