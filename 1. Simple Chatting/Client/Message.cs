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

        int HeadrparsingCompleted = 0;

        // Header
        public string Name;
        public DateTime Time;
        public long UnixTime;
        public int PayloadLength;

        // Payload
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

        public bool ParseLine(string line)
        {
            switch (HeadrparsingCompleted)
            {
                case 0:
                    ParseHeader(line);
                    return false;
                case 1:

                    ParsePayload(line);
                    return true;
            }
            return false;
        }

        void ParseHeader(string line)
        {
            if (line == "\r")
            {
                HeadrparsingCompleted = 1;
                return;
            }
            line = line.Trim();
            string[] parts = line.Split(' ');

            if (parts[0] == "time:")
            {
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
            Console.WriteLine($"[{Time.Minute:00}:{Time.Second:00}] {Name}: {Payload}");
            HeadrparsingCompleted = 0;
        }
    }
}
