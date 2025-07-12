using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        public byte[] Buffer;
        public int m_index;
        public int m_length;
        public int m_offset;
        public int m_year;
        public int m_month;
        public int m_day;
        public int m_hour;
        public int m_minute;
        public int m_second;
        public string m_name;
        public string m_payload;
        public int m_messageLength;
        TokenState m_state;

        public Token()
        {
            m_index = 0;
            Buffer = new byte[4096];
            m_offset = 0;
            m_state = TokenState.HeaderParsing;
        }

        public bool TransferData(byte[] buffer, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Buffer[m_offset + m_index++] = buffer[i + offset];
            }
            m_length += length;

            return ParseData();
        }

        bool ParseData()
        {
            switch (m_state)
            {
                case TokenState.HeaderParsing:
                    for (int i = m_offset; i < m_offset + m_length - 1; i++)
                    {
                        if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                        {
                            string line = Encoding.UTF8.GetString(Buffer, m_offset, i - m_offset);
                            string[] parts = line.Split(' ');
                            Console.WriteLine(line);
                            if (parts[0] == "time:")
                            {
                                m_year = int.Parse(parts[1]);
                                m_month = int.Parse(parts[2]);
                                m_day = int.Parse(parts[3]);
                                m_hour = int.Parse(parts[4]);
                                m_minute = int.Parse(parts[5]);
                                m_second = int.Parse(parts[6]);
                                m_offset = i + 2;
                            }
                            else if (parts[0] == "name:")
                            {
                                m_name = parts[1];
                                m_offset = i + 2;
                            }
                            else if (parts[0] == "length:")
                            {
                                m_messageLength = int.Parse(parts[1]);
                                Console.WriteLine($"{m_messageLength}");
                                m_offset = i + 2;
                            }
                            if (i + 3 < m_offset + m_length)
                            {
                                if (Buffer[i + 2] == '\r' && Buffer[i + 3] == '\n')
                                {
                                    m_offset = i + 4;
                                    m_length = m_index - (m_offset - i);
                                    m_state = TokenState.PayloadParsing;
                                    return ParseData();
                                }
                            }
                        }
                    }
                    break;
                case TokenState.PayloadParsing:
                    if (m_index >= m_messageLength + m_offset)
                    {
                        m_payload = Encoding.UTF8.GetString(Buffer, m_offset, m_messageLength);
                        m_state = TokenState.HeaderParsing;
                        m_index = 0; 
                        m_offset = 0; 
                        m_length = 0;
                        return true;
                    }
                    break;
            }
            return false;
        }

        public string MakeMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{m_hour}:{m_minute}:{m_second}] ");
            sb.Append($"{m_name}: ");
            sb.Append(m_payload);
            return sb.ToString();
        }
    }
}