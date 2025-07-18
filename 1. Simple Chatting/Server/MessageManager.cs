using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Server
{
    internal class MessageManager
    {
        public int ID;

        ConcurrentQueue<Message> m_receivedMessagesQueue;
        SemaphoreSlim m_receivedQueueSemaphore;

        Message m_currentMessage;

        bool work;

        public MessageManager()
        {
            work = false;
            m_receivedMessagesQueue = new();
            m_receivedQueueSemaphore = new SemaphoreSlim(0);

            m_currentMessage = new Message();
        }

        public void StartWork()
        {
            work = true;
            Task.Run(() => ProcessReceivedMessages());
        }

        public void EndWork()
        {
            work = false;
        }

        public void ParseLine(string line)
        {
            if (m_currentMessage.ParseLine(line))
            {
                m_receivedMessagesQueue.Enqueue(m_currentMessage);
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
                    Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
                    message.ID = ID;
                    ChattingServer.ProcessMessage(message);
                }
            }
        }
    }
}
