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
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Target: {Target}");
            sb.AppendLine($"name: {Name}");
            sb.AppendLine($"time: {Time.Year} {Time.Month} {Time.Day} {Time.Hour} {Time.Minute} {Time.Second} {UnixTime}");
            sb.AppendLine($"length: {PayloadLength}");
            sb.AppendLine();
            sb.AppendLine(Payload);
            return Encoding.UTF8.GetBytes(sb.ToString());
        }


    }
}
