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

        MessageManager m_messageManager;
        Semaphore m_semaTrans;
        Semaphore m_semaParse;
        bool work = false;

        // buffer
        private byte[] Buffer;
        private int m_index;
        private int m_offset;
        private int m_currentDatalength;
        private int m_MaxLength;


        public Token()
        {
            m_messageManager = new();
            m_semaTrans = new Semaphore(0, 1);
            m_semaParse = new Semaphore(1, 1);

            m_index = 0;
            m_MaxLength = 300;
            Buffer = new byte[m_MaxLength];
            m_offset = 0;
        }

        public void SetId(int id)
        {
            ID = id;
            m_messageManager.ID = id;
        }

        public void Start()
        {
            m_semaTrans = new Semaphore(0, 1);
            m_semaParse = new Semaphore(1, 1);
            Task.Run(() => ParseData());
            work = true;

            m_messageManager.StartWork();
        }

        public void End()
        {
            work = false;

            m_messageManager.EndWork();
        }

        public void TransferData(byte[] buffer, int offset, int length)
        {
            if (length + m_currentDatalength > m_MaxLength)
            {
                Console.WriteLine($"Error: Data length exceeds maximum limit. Current length: {m_currentDatalength}, Attempted length: {length}");
                return;
            }

            m_semaTrans.WaitOne();
            for (int i = 0; i < length; i++)
            {
                Buffer[m_index++] = buffer[i + offset];
                m_index %= m_MaxLength;
            }
            m_currentDatalength += length;
            m_semaParse.Release();
        }

        void ParseData()
        {
            string line;
            while (work)
            {
                m_semaParse.WaitOne();

                while (true)
                {
                    line = GetLine();
                    if (String.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    m_messageManager.ParseLine(line);
                }
                m_semaTrans.Release();
            }
        }

        string GetLine()
        {
            string line = "";
            byte[] temp = new byte[m_MaxLength]; // Big Problem
            int tempIndex = 0;
            for (int i = m_offset; i != m_index; i = (i + 1) % m_MaxLength)
            {
                temp[tempIndex++] = Buffer[i];
                if (Buffer[i] == '\r' && Buffer[(i + 1) % m_MaxLength] == '\n')
                {
                    temp[tempIndex++] = (byte)'\n';
                    line = Encoding.UTF8.GetString(temp, 0, tempIndex);
                    m_offset = (i + 2) % m_MaxLength;
                    m_currentDatalength -= tempIndex;
                    break;
                }
            }

            return line;
        }
    }
}