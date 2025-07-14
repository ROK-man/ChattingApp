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

        // buffer
        public byte[] Buffer;
        public int m_index;
        public int m_offset;

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
            Buffer = new byte[100];
            m_offset = 0;
            m_state = TokenState.HeaderParsing;
        }

        public bool TransferData(byte[] buffer, int offset, int length)
        {
            Console.WriteLine($"TransferData: {length} bytes");
            if (length > Buffer.Length - m_index)
            {
                for (int i = 0; i < length; i++)
                {
                    Buffer[m_index++] = buffer[i + offset];
                    m_index %= Buffer.Length;
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    Buffer[m_index++] = buffer[i + offset];
                }
                m_index %= Buffer.Length;
            }

            return ParseData();
        }

        // return true when payload parsed
        bool ParseData()
        {
            switch (m_state)
            {
                case TokenState.HeaderParsing:
                    if (m_offset < m_index)
                    {
                        for (int i = m_offset; i < m_index - 1; i++)
                        {
                            if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                            {
                                i += 1;
                                string line = Encoding.UTF8.GetString(Buffer, m_offset, i - m_offset);
                                m_offset = i + 1;
                                // crlf crlf
                                if (ProcessLine(line))
                                {
                                    return ParseData();
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = m_offset; i < Buffer.Length - 1; i++)
                        {
                            if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                            {
                                i += 1;
                                string line = Encoding.UTF8.GetString(Buffer, m_offset, i - m_offset);
                                m_offset = i + 1;

                                m_offset %= Buffer.Length;
                                // crlf crlf
                                if (ProcessLine(line))
                                {
                                    return ParseData();
                                }
                            }
                        }
                        if (m_offset == 0)
                        {
                            return ParseData();
                        }

                        // \r\n 끼어있거나, 0에 있거나, 데이터 잘려있는 상황
                        if (Buffer[Buffer.Length - 1] == '\r' && Buffer[0] == '\n')
                        {
                            string line = Encoding.UTF8.GetString(Buffer, m_offset, Buffer.Length - m_offset);
                            m_offset = 1;
                            ProcessLine(line);

                            return ParseData();
                        }
                        else if (Buffer[0] == 'r' && Buffer[1] == '\n')
                        {

                            string line = Encoding.UTF8.GetString(Buffer, m_offset, Buffer.Length - m_offset);
                            m_offset = 2;
                            if (ProcessLine(line))
                            {
                                return ParseData();
                            }
                        }
                        else
                        {
                            byte[] temp = new byte[4096];
                            int tempIndex = 0;
                            for (int i = m_offset; i < Buffer.Length; i++)
                            {
                                temp[tempIndex++] = Buffer[i];
                            }
                            for (int i = 0; i < m_index - 1; i++)
                            {
                                if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                                {
                                    string line = Encoding.UTF8.GetString(temp, 0, tempIndex);
                                    Console.WriteLine($"Test3: {line}");

                                    m_offset = i + 2;
                                    ProcessLine(line);

                                    return ParseData();

                                }
                                else
                                {
                                    temp[tempIndex++] = Buffer[i];
                                }
                            }
                        }
                    }
                    break;

                case TokenState.PayloadParsing:
                    if (m_offset < m_index)
                    {
                        for (int i = m_offset; i < m_index - 1; i++)
                        {
                            if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                            {
                                m_payload = Encoding.UTF8.GetString(Buffer, m_offset, i - m_offset);
                                m_state = TokenState.HeaderParsing;
                                m_offset = i + 2;
                                return true;
                            }
                        }
                    }
                    else
                    {
                        byte[] temp = new byte[4096];
                        int tempIndex = 0;
                        for (int i = m_offset; i < Buffer.Length; i++)
                        {
                            temp[tempIndex++] = Buffer[i];
                        }
                        for (int i = 0; i < m_index - 1; i++)
                        {
                            if (Buffer[i] == '\r' && Buffer[i + 1] == '\n')
                            {
                                m_payload = Encoding.UTF8.GetString(temp, 0, tempIndex);
                                m_payload = m_payload.Trim();
                                m_state = TokenState.HeaderParsing;
                                m_offset = i + 2;
                                return true;
                            }
                            else
                            {
                                temp[tempIndex++] = Buffer[i];
                            }
                        }
                    }
                    break;
            }

            return false;
        }

        bool ProcessLine(string line)
        {
            line = line.Trim();

            // crlf crlf
            if (line.Length == 0)
            {
                m_state = TokenState.PayloadParsing;
                return true;
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

            return false;
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