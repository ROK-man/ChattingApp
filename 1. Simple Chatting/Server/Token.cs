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
        public Socket ClientSocket;

        // buffer
        public byte[] Buffer;
        public int m_index;
        public int m_offset;
        public int m_currentDatalength;

        public int m_MaxLength;

        // header
        public int m_year;
        public int m_month;
        public int m_day;
        public int m_hour;
        public int m_minute;
        public int m_second;
        public long m_unixTime;

        public string m_name;
        public int m_messageLength;

        // payload
        public string m_payload;
        TokenState m_state;

        public Token()
        {
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
            if (line.Length == 0)
            {
                m_state = TokenState.PayloadParsing;
                return;
            }

            string[] parts = line.Split(' ');

            if (parts[0] == "time:")
            {
                m_year = int.Parse(parts[1]);
                m_month = int.Parse(parts[2]);
                m_day = int.Parse(parts[3]);
                m_hour = int.Parse(parts[4]);
                m_minute = int.Parse(parts[5]);
                m_second = int.Parse(parts[6]);
                m_unixTime = long.Parse(parts[7]);
            }
            else if (parts[0] == "name:")
            {
                m_name = parts[1];
            }
            else if (parts[0] == "length:")
            {
                m_messageLength = int.Parse(parts[1]);
            }
        }

        void processPayload(string line)
        {
            m_payload = line.Trim();
            m_state = TokenState.HeaderParsing;
        }
        public string MakeMessage()
        {
            Console.WriteLine($"Delay: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_unixTime}ms");
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{m_hour:00}:{m_minute:00}:{m_second:00}] ");
            sb.Append($"{m_name}: ");
            sb.Append(m_payload);

            return sb.ToString();
        }
    }
}