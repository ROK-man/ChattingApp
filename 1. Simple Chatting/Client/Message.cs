using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public enum MessageType
    {
        None,
        Text,
        Get,
    }

    public enum MessageTarget
    {
        None,
        All,
    }
    internal class Message
    {
        bool m_isParsingHeader = true;

        // Header
        public MessageType Type = MessageType.Text;
        public MessageTarget Target = MessageTarget.All;
        public string? Name;
        public DateTime Time;
        public long UnixTime;
        public int PayloadLength;

        // Payload
        public string? Payload;

        public Message()
        {
            Type = MessageType.None;
            Target = MessageTarget.None;
        }

        public Message(MessageType type, MessageTarget target, string name, string payload)
        {
            Type = type;
            Target = target;
            Name = name;
            Time = DateTime.Now;
            UnixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PayloadLength = payload.Length;
            Payload = payload;
        }

        public byte[] ToBytes()
        {
            var sb = new StringBuilder();
            sb.Append($"Type: {Type}\r\n");
            sb.Append($"Target: {Target}\r\n");
            sb.Append($"name: {Name}\r\n");
            sb.Append($"time: {Time.Year} {Time.Month} {Time.Day} {Time.Hour} {Time.Minute} {Time.Second} {UnixTime}\r\n");
            sb.Append($"length: {PayloadLength}\r\n");
            sb.Append("\r\n");
            sb.Append(Payload);
            sb.Append("\r\n");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public bool ParseLine(string line)
        {
            if(m_isParsingHeader)
            {
                ParseHeader(line);
                return false;
            }
            else
            {
                ParsePayload(line);
                return true;
            }
        }

        void ParseHeader(string line)
        {
            if (line == "\r")
            {
                m_isParsingHeader = false;
                return;
            }
            line = line.Trim();
            string[] parts = line.Split(' ');

            if (parts[0] == "time:")
            {
                if (parts.Length != 8)
                {
                    Console.WriteLine("Error: Invalid time format in header.");
                    Console.WriteLine($"Received: {line}");
                    return;
                }
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                int day = int.Parse(parts[3]);
                int hour = int.Parse(parts[4]);
                int minute = int.Parse(parts[5]);
                int second = int.Parse(parts[6]);
                long unixTime = long.Parse(parts[7]);

                Time = new DateTime(year, month, day, hour, minute, second);
                UnixTime = unixTime;
            }
            else if (parts[0] == "name:")
            {
                Name = parts[1];
            }
            else if (parts[0] == "length:")
            {
                PayloadLength = int.Parse(parts[1]);
            }
        }

        void ParsePayload(string line)
        {
            Payload = line.Trim();
            if (Payload.Length == PayloadLength)
            {
                m_isParsingHeader = true;
            }
        }
    }
}
