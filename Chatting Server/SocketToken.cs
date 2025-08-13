using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Chatting_Server
{
    internal class SocketToken
    {
        public Socket? Socket { get; set; }
        private byte[]? m_buffer;
        private int m_bufferOffset;
        private int m_bufferSize;
        private int m_bufferCurrentDataLength;

        private int m_bufferIndex; // index for start read
        private byte m_state; // 0 = header, 1 = payload
        private byte[] m_headerbuffer;  // 14 bytes
        private byte[] m_payloadbuffer;   // 2048 bytes
        private Message? m_message;
        public ConcurrentQueue<Message> MessageQueue { get; set; }

        public SocketToken(int bufferSize, ConcurrentQueue<Message> messageQueue)
        {
            Socket = null;
            m_buffer = new byte[bufferSize];
            m_bufferOffset = 0;
            m_bufferSize = bufferSize;
            m_bufferCurrentDataLength = 0;

            m_bufferIndex = 0;
            m_state = 0;
            m_headerbuffer = new byte[14];
            m_payloadbuffer = new byte[2048];
            MessageQueue = messageQueue;
        }

        public void TransferData(byte[] data, int offset, int datalength)
        {
            if (data.Length + m_bufferCurrentDataLength > m_bufferSize)
            {
                // wait
            }
            if (datalength + m_bufferOffset < m_bufferSize)
            {
                Buffer.BlockCopy(data, offset, m_buffer!, m_bufferOffset, datalength);
            }
            else
            {
                Buffer.BlockCopy(data, offset, m_buffer!, m_bufferOffset, m_bufferSize - m_bufferOffset);
                Buffer.BlockCopy(data, offset + m_bufferSize - m_bufferOffset, m_buffer!, 0, datalength - (m_bufferSize - m_bufferOffset));
            }
            m_bufferCurrentDataLength += datalength;
            m_bufferOffset = (m_bufferOffset + datalength) % m_bufferSize;

            ParseData();
        }

        private void ParseData()
        {
            switch(m_state)
            {
                case 0:
                    ParseHeader();
                    if(m_state == 1)
                    {
                        ParsePayload();
                    }
                    break;
                case 1:
                    ParsePayload();
                    break;
            }
        }

        private void ParseHeader()
        {
            if(m_bufferCurrentDataLength < 14)
            {
                return;
            }

            m_message = new();
            Buffer.BlockCopy(m_buffer!, m_bufferIndex, m_headerbuffer, 0, 14);
            m_message.SetHeader(m_headerbuffer);

            m_bufferIndex += 14;
            m_bufferCurrentDataLength -= 14;
            m_state = 1;
        }

        private void ParsePayload()
        {
            if (m_bufferCurrentDataLength < m_message!.Header.Length)
            {
                return;
            }
            Buffer.BlockCopy(m_buffer!, m_bufferIndex, m_payloadbuffer, 0, m_message.Header.Length);
            m_message.SetPayload(m_payloadbuffer);

            //MessageQueue.Enqueue(m_message);
            Console.WriteLine(m_message.Payload);
            Console.WriteLine($"Header Length: {m_message.Header.Length}, Type: {m_message.Header.Type}, Flag: {m_message.Header.Flag}, UnixTimeMilli: {m_message.Header.UnixTimeMilli}");

            m_bufferIndex += m_message.Header.Length;
            m_bufferCurrentDataLength -= m_message.Header.Length;
            m_state = 0;
        }
    }
}
