using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Chatting_Server
{
    internal class SocketToken
    {
        public Socket? Socket { get; set; }

        private byte m_state; // 0 = header, 1 = payload
        private byte[] m_headerBuffer;  // 14 bytes
        private byte[] m_payloadbuffer;   // 2048 bytes
        private Message? m_message;
        public ConcurrentQueue<Message> MessageQueue { get; set; }

        private int m_lengthForReceive;

        public SocketToken(int bufferSize, ConcurrentQueue<Message> messageQueue)
        {
            Socket = null;

            m_state = 0;
            m_headerBuffer = new byte[Message.HEADER_SIZE];
            m_payloadbuffer = new byte[Message.MAX_PAYLOAD_SIZE];
            MessageQueue = messageQueue;

            m_lengthForReceive = Message.HEADER_SIZE;
            m_message = new();
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            args.SetBuffer(m_headerBuffer, 0, Message.HEADER_SIZE);
        }

        public void ProcessReceive(SocketAsyncEventArgs args, int length)
        {
            if(length == m_lengthForReceive)
            {
                switch(m_state)
                {
                    case 0:
                        ParseHeader();

                        if (m_message!.Header.Length == 0)
                        {
                            Console.WriteLine("0 detected");
                            args.SetBuffer(m_headerBuffer, 0, Message.HEADER_SIZE);
                            m_lengthForReceive = Message.HEADER_SIZE;
                            m_state = 0;
                            return;
                        }

                        args.SetBuffer(m_payloadbuffer, 0, m_message!.Header.Length);
                        m_lengthForReceive = m_message.Header.Length;
                        break;
                    case 1:
                        ParsePayload();

                        args.SetBuffer(m_headerBuffer, 0, Message.HEADER_SIZE);
                        m_lengthForReceive = Message.HEADER_SIZE;
                        break;
                }
            }
            else
            {
                m_lengthForReceive -= length;
                switch(m_state)
                {
                    case 0:
                        args.SetBuffer(m_headerBuffer, Message.HEADER_SIZE - m_lengthForReceive, m_lengthForReceive);
                        break;
                    case 1:
                        args.SetBuffer(m_payloadbuffer, m_message!.Header.Length - m_lengthForReceive, m_lengthForReceive);
                        break;
                }
            }
        }

        private void ParseHeader()
        {
            m_message!.SetHeader(m_headerBuffer);

            m_state = 1;;
        }

        private void ParsePayload()
        {
            m_message.SetPayload(m_payloadbuffer);

            //MessageQueue.Enqueue(m_message);
            Console.WriteLine(m_message.Payload);
            //Console.WriteLine($"Header Length: {m_message.Header.Length}, Type: {m_message.Header.Type}, " +
            //    $"Flag: {m_message.Header.Flag}, UnixTimeMilli: {m_message.Header.UnixTimeMilli}");
            m_state = 0;

            m_message = new();
        }
    }
}
