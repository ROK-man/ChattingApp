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
        private byte[] Buffer;
        private int m_index;
        private int m_offset;
        private int m_currentDatalength;
        private int m_MaxLength;

        MessageManager m_messageManager;

        public Token()
        {
            m_index = 0;
            // one total message shoud less than 4KB;
            m_MaxLength = 300;
            Buffer = new byte[m_MaxLength];
            m_offset = 0;

            m_messageManager = new();
        }

        public void SetId(int id)
        {
            ID = id;
            m_messageManager.ID = id;
        }

        public void Start()
        {
            m_messageManager.StartWork();
        }

        public void End()
        {
            m_messageManager.EndWork();
        }

        public void TransferData(byte[] buffer, int offset, int length)
        {
            if (length + m_currentDatalength > m_MaxLength)
            {
                Console.WriteLine($"Error: Data length exceeds maximum limit. Current length: {m_currentDatalength}, Attempted length: {length}");
            }

            for (int i = 0; i < length; i++)
            {
                Buffer[m_index++] = buffer[i + offset];
                m_index %= m_MaxLength;
            }
            m_currentDatalength += length;

            ParseData();
        }

        // return true when payload parsed
        void ParseData()
        {
            string line;
            while ((line = getLine()).Length != 0)
            {
                m_messageManager.ParseLine(line);
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
    }
}