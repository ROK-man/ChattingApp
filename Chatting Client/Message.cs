using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Client
{
    internal class Message
    {
        public Messageheader Header { get; set; }
        public string Payload { get; set; } 

        public Message()
        {
            Header = new();
            Payload = string.Empty;
        }

        public void SetHeader(byte[] headerData)
        {
            Header!.Length = BitConverter.ToInt32(headerData, 0);
            Header.Type = headerData[4];
            Header.Flag = headerData[5];
            Header.UnixTimeMilli = BitConverter.ToInt64(headerData, 6);
        }

        public void SetPayload(byte[] payloadData)
        {
            Payload = Encoding.UTF8.GetString(payloadData, 0, Header.Length);
        }
        public byte[] MakeMessage(string input)
        {
            Header = new();
            Header.Length = Encoding.UTF8.GetByteCount(input);
            Header.UnixTimeMilli = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            byte[] data = new byte[14 + Header.Length];

            BitConverter.GetBytes(Header.Length).CopyTo(data, 0);
            data[4] = Header.Type;
            data[5] = Header.Flag;
            BitConverter.GetBytes(Header.UnixTimeMilli).CopyTo(data, 6);

            Encoding.UTF8.GetBytes(input, 0, input.Length, data, 14);
            return data;
        }
    }

    internal class Messageheader
    {
        public int Length { get; set; } // 4 bytes
        public byte Type { get; set; } // 1 byte
        public byte Flag { get; set; } // 1 byte
        public long UnixTimeMilli { get; set; } // 8 bytes 

        public Messageheader()
        {
            Length = 0;
            Type = 2;
            Flag = 1;
            UnixTimeMilli = 0;
        }
    }
}
