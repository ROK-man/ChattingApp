using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server
{
    internal class MessageManager
    {
        Token m_token;

        ConcurrentQueue<Message> m_receivedMessagesQueue;
        SemaphoreSlim m_receivedQueueSemaphore;

        Message m_message;

        bool work;

        public MessageManager(Token token)
        {
            work = false;
            m_receivedMessagesQueue = new();
            m_receivedQueueSemaphore = new SemaphoreSlim(0);

            m_message = new Message(token);
            m_token = token;
        }

        public void StartWork()
        {
            m_message = new(m_token);
            work = true;
            Task.Run(() => ProcessReceivedMessages());
        }

        public void EndWork()
        {
            work = false;
        }

        public void ParseLine(string line)
        {
            if (m_message.ParseLine(line))
            {
                m_receivedMessagesQueue.Enqueue(m_message);
                m_message = new(m_token);
                m_receivedQueueSemaphore.Release();
            }
        }

        void ProcessReceivedMessages()
        {
            Console.WriteLine("Message processing started.");
            while (work)
            {
                m_receivedQueueSemaphore.Wait();

                if (m_receivedMessagesQueue.TryDequeue(out Message? message))
                {
                    ChattingServer.ProcessMessage(message);
                }
            }
        }
    }
}
