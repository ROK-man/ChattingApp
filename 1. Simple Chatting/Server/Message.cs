using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Message
    {
        public enum MessageType
        {
            Text,
            Get,
        }

        public enum MessageTarget
        {
            All,
        }

        // temp
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

        public static byte[] MakeMessage(Message.MessageType type, Message.MessageTarget target, string name, string payload)
        {
            string message = "";

            message += $"Type: {type}\r\n";
            message += $"Target: {target}\r\n";
            message += $"name: {name}\r\n";
            message += $"time: {DateTime.Now.Year} {DateTime.Now.Month} {DateTime.Now.Day} " +
                $"{DateTime.Now.Hour} {DateTime.Now.Minute} {DateTime.Now.Second} {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\r\n";
            message += $"length: {payload.Length}\r\n";
            message += "\r\n";
            message += $"{payload}\r\n";

            return System.Text.Encoding.UTF8.GetBytes(message);
        }
    }
}
