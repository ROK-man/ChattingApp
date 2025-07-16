using System.Net.Sockets;
using System.Text;

// 현 문제점
// 한 번에 하나의 메시지만 처리됨. (여러개 메시지 쌓이면 꼬임)
// 큐에 메시지 쌓아서 넣을 필요가 있음

namespace Server
{
    enum TokenState
    {
        HeaderParsing,
        PayloadParsing
    }
    class Token
    {
        public Socket? ClientSocket;
        public int ID;

        // buffer
        public byte[] Buffer;
        public int m_index;
        public int m_offset;
        public int m_currentDatalength;
        public int m_MaxLength;

        // message
        Message m_message;
        public Message Message;
        TokenState m_state;

        public Token()
        {
            m_message = new();
            m_index = 0;
            // one total message shoud less than 4KB;
            m_MaxLength = 300;
            Buffer = new byte[m_MaxLength];
            m_offset = 0;
            m_state = TokenState.HeaderParsing;
        }

        public bool TransferData(byte[] buffer, int offset, int length)
        {
            if (length + m_currentDatalength > m_MaxLength)
            {
                Console.WriteLine($"Error: Data length exceeds maximum limit. Current length: {m_currentDatalength}, Attempted length: {length}");
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                Buffer[m_index++] = buffer[i + offset];
                m_index %= m_MaxLength;
            }
            m_currentDatalength += length;

            return ParseData();
        }

        // return true when payload parsed
        bool ParseData()
        {
            while (true)
            {
                string line = getLine();
                if (line.Length == 0)
                {
                    return false;
                }
                if (ProcessLine(line))
                {
                    return true;
                }
            }
        }

        string getLine()
        {
            string line = "";
            byte[] temp = new byte[m_MaxLength]; // Big Problem
            int tempIndex = 0;
            for (int i = m_offset; i != m_index; i = (i + 1) % m_MaxLength)
            {
                temp[tempIndex++] = Buffer[i];
                if (Buffer[i] == '\r' && Buffer[(i + 1) % m_MaxLength] == '\n')
                {
                    line = Encoding.UTF8.GetString(temp, 0, tempIndex);
                    m_offset = (i + 2) % m_MaxLength;
                    m_currentDatalength -= (tempIndex + 1);
                    break;
                }
            }

            return line;
        }

        bool ProcessLine(string line)
        {
            line = line.Trim();

            switch (m_state)
            {
                case TokenState.HeaderParsing:
                    processHeadr(line);
                    break;

                case TokenState.PayloadParsing:
                    processPayload(line);
                    return true;
            }

            return false;
        }

        void processHeadr(string line)
        {
            m_message.ID = ID;

            if (line.Length == 0)
            {
                m_state = TokenState.PayloadParsing;
                return;
            }

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

                m_message.Time = new DateTime(year, month, day, hour, minute, second);
                m_message.UnixTime = unixTime;
            }
            else if (parts[0] == "name:")
            {
                m_message.Name = parts[1];
            }
            else if (parts[0] == "length:")
            {
                m_message.PayloadLength = int.Parse(parts[1]);
            }
        }

        void processPayload(string line)
        {
            m_message.Payload = line.Trim();

            m_state = TokenState.HeaderParsing;
            Message = m_message;
            m_message = new();
            m_message.ID = ID;
        }
        public string MakeMessage()
        {
            Console.WriteLine($"Delay: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_message.UnixTime}ms");
            StringBuilder sb = new StringBuilder();

            sb.Append($"name: {Message.Name}\r\n");
            sb.Append($"time: {Message.Time.Year} {Message.Time.Month} {Message.Time.Day} {Message.Time.Hour} {Message.Time.Minute} {Message.Time.Second} {Message.UnixTime}\r\n");
            sb.Append($"length: {Message.Payload.Length}\r\n"); // string length
            sb.Append("\r\n");

            sb.Append(Message.Payload);
            sb.Append("\r\n");

            return sb.ToString();
        }
    }
}