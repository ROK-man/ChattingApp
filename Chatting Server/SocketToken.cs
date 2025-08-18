using System.Collections.Concurrent;
using System.Net.Sockets;
using MessageLib;

namespace Chatting_Server
{
    // 소켓 상태 처리
    internal class SocketToken
    {
        public Socket? Socket { get; set; }
        public UserInfo? User { get; set; }

        private byte[] m_headerBuffer;  // 14 bytes
        private byte[] m_payloadbuffer;   // 2048 bytes
        private MessageManager? m_messageManager;
        private int m_lengthForReceive;

        private BlockingCollection<LappedMessage> m_messageQueue;

        private Queue<Message> m_sendQueue = new Queue<Message>();
        private SocketAsyncEventArgs m_sendArgs = new SocketAsyncEventArgs();
        bool m_isSending = false;

        public SocketToken(int bufferSize, BlockingCollection<LappedMessage> messageQueue)
        {
            Socket = null;

            m_headerBuffer = new byte[MessageManager.HEADER_SIZE];
            m_payloadbuffer = new byte[MessageManager.MAX_PAYLOAD_SIZE];

            m_lengthForReceive = MessageManager.HEADER_SIZE;
            m_messageManager = new();

            m_messageQueue = messageQueue;

            User = new UserInfo();
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            args.SetBuffer(m_headerBuffer, 0, MessageManager.HEADER_SIZE);
        }

        public void ProcessReceive(SocketAsyncEventArgs args, int length)
        {
            if (length == m_lengthForReceive)
            {
                if (m_messageManager!.ParseData(args.Buffer!))
                {
                    LappedMessage message = new LappedMessage(this, m_messageManager.GetMessage());
                    m_messageQueue.Add(message);

                    m_lengthForReceive = MessageManager.HEADER_SIZE;
                    args.SetBuffer(m_headerBuffer, 0, MessageManager.HEADER_SIZE);
                }
                else
                {
                    m_lengthForReceive = m_messageManager.PayloadLength;   
                    args.SetBuffer(m_payloadbuffer, 0, m_lengthForReceive);
                }
            }
            else
            {
                m_lengthForReceive -= length;
                args.SetBuffer(args.Buffer, args.Offset + length, m_lengthForReceive);
            }
        }

        public void SendMessage(Message message)
        {
            lock (m_sendQueue)
            {
                if (!m_isSending)
                {
                    m_isSending = true;
                    SendStart(message);
                }
                else
                {
                    m_sendQueue.Enqueue(message);
                }
            }
        }

        private void SendStart(Message message)
        {
            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);
            m_sendArgs.SetBuffer(buffer, 0, buffer.Length);

            // should occurr stack overflow
            if (!Socket.SendAsync(m_sendArgs))
            {
                SendCompleted(this, m_sendArgs);
            }
        }

        private void SendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Console.WriteLine($"Send failed: {e.SocketError}");
                return;
            }

            lock (m_sendQueue)
            {
                if (m_sendQueue.Count > 0)
                {
                    Message nextMessage = m_sendQueue.Dequeue();
                    SendStart(nextMessage);
                }
                else
                {
                    m_isSending = false;
                }
            }
        }
    }
}
