using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Server
{
    internal class Message
    {
        public Messageheader Header { get; set; }
        public String Payload { get; set; } 

        public Message()
        {
            Header = new();
            Payload = string.Empty;
        }

        public void SetHeader(byte[] headerData)
        {
            Header.Length = (headerData[0] << 24) | (headerData[1] << 16) | (headerData[2] << 8) | headerData[3];
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
