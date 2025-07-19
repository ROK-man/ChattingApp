using System.Net.Sockets;

namespace Client
{
    internal class MessageManager
    {
        Queue<Message> m_receivedMessagesQueue;
        SemaphoreSlim m_receivedQueueSemaphore;

        Message m_currentMessage;

        public MessageManager()
        {
            m_receivedMessagesQueue = new();
            m_receivedQueueSemaphore = new SemaphoreSlim(0);

            m_currentMessage = new Message();
        }
        public void StartWork()
        {
            Task.Run(() => ProcessReceivedMessages());
        }

        public void ParseLine(string line)
        {
            //Console.WriteLine(line);
            if (m_currentMessage.ParseLine(line))
            {
                AddMessageToReceivedQueue(m_currentMessage);
                m_currentMessage = new();
            }
        }

        void AddMessageToReceivedQueue(Message message)
        {
            m_receivedMessagesQueue.Enqueue(message);
            m_receivedQueueSemaphore.Release();
        }

        void ProcessReceivedMessages()
        {
            Console.WriteLine("Message processing started.");
            while (true)
            {
                m_receivedQueueSemaphore.Wait();
                while (m_receivedMessagesQueue.Count != 0)
                {
                    Message message = m_receivedMessagesQueue.Dequeue();
                    ChattingClient.ProcessMessage(message);
                }
            }
        }
    }
}
