using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Message
    {
        enum MessageType
        {
            Text,
            Get,
        }

        enum MessageTarget
        {
            All,
        }

        public string Name;
        public DateTime Time;
        public long UnixTime;
        public int PayloadLength;
        public string Payload;

        public static byte[] MakeMessage(string name, string payload)
        {
            string message = "";

            message += $"Type: {MessageType.Text}\r\n";
            message += $"Target: {MessageTarget.All}\r\n";
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
