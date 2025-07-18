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

        public Socket? Socket;

        int temp = 0;

        public MessageManager()
        {
            m_receivedMessagesQueue = new();
            m_receivedQueueSemaphore = new SemaphoreSlim(0);

            m_currentMessage = new Message();
        }
        public void StartWork()
        {
            temp = 1;
            Task.Run(() => ProcessReceivedMessages());
        }

        public void EndWork()
        {
            temp = 0;
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
            while (temp == 1)
            {
                m_receivedQueueSemaphore.Wait();

                if (m_receivedMessagesQueue.TryDequeue(out Message message))
                {
                    Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
                    message.ID = ID;
                    Server.ProcessMessage(message);
                }
            }
        }

    }
}
