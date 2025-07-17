using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
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
        public int ID;
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

        public byte[] GetBytes()
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
    }
}
