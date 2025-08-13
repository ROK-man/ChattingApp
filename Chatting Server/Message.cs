using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Server
{
    internal class Message
    {
        public const int HEADER_SIZE = 14;
        public const int MAX_PAYLOAD_SIZE = 2048;
        public Messageheader Header { get; set; }
        public string Payload { get; set; } 

        public Message()
        {
            Header = new();
            Payload = string.Empty;
        }

        public void SetHeader(byte[] headerData)
        {
            Header.Length = BitConverter.ToInt32(headerData, 0);
            Header.Type = headerData[4];
            Header.Flag = headerData[5];
            Header.UnixTimeMilli = BitConverter.ToInt64(headerData, 6);
        }

        public void SetPayload(byte[] payloadData)
        {
            Payload = Encoding.UTF8.GetString(payloadData, 0, Header.Length);
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
            Type = 0;
            Flag = 0;
            UnixTimeMilli = 0;
        }
    }
}
