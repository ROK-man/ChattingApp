using MessageLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Client
{
    internal class SocketToken
    {
        public Socket? Socket { get; set; }

        private byte[] m_headerBuffer;  // 14 bytes
        private byte[] m_payloadbuffer;   // 2048 bytes
        private MessageManager? m_messageManager;

        private int m_lengthForReceive;

        private BlockingCollection<Message> m_messageQueue;

        public SocketToken(int bufferSize, BlockingCollection<Message> messageQueue)
        {
            Socket = null;

            m_headerBuffer = new byte[MessageManager.HEADER_SIZE];
            m_payloadbuffer = new byte[MessageManager.MAX_PAYLOAD_SIZE];

            m_lengthForReceive = MessageManager.HEADER_SIZE;
            m_messageManager = new();

            m_messageQueue = messageQueue;
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
                    m_messageQueue.Add(m_messageManager.GetMessage());
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
    }
}
