using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net.Sockets;

namespace Client
{
    internal class MessageManager
    {
        Queue<Message> m_receivedMessagesQueue;
        SemaphoreSlim m_receivedQueueSemaphore;
        Queue<Message> m_sendMessagesQueue;
        object lockObject = new();

        Message m_currentMessage;
        int m_currentParsingState; // 0: Header, 1: Payload

        public Socket Socket;

        public MessageManager()
        {
            m_receivedMessagesQueue = new();
            m_receivedQueueSemaphore = new SemaphoreSlim(0);
            m_sendMessagesQueue = new();
            m_currentParsingState = 0;

            m_currentMessage = new Message();
        }
        public void StartWork()
        {
            Task.Run(() => ProcessReceivedMessages());
            Task.Run(() => ProcessSendMessage());
        }

        public void Send(MessageType type, MessageTarget target, string name, string payload)
        {
            Message message = new(type, target, name, payload);

            lock (lockObject)
            {
                m_sendMessagesQueue.Enqueue(message);
            }
        }

        void ProcessSendMessage()
        {
            Message? message;
            while (true)
            {
                message = null;
                lock (lockObject)
                {
                    if (m_sendMessagesQueue.Count > 0)
                    {
                        message = m_sendMessagesQueue.Dequeue();
                    }
                }
                if (message != null)
                {
                    Socket.Send(message.ToBytes());
                }
            }
        }

        public bool ParseLine(string line)
        {
            switch (m_currentParsingState)
            {
                case 0:
                    ParseHeader(line);
                    return false;
                case 1:

                    ParsePayload(line);
                    return true;
            }
            return false;
        }

        void ParseHeader(string line)
        {
            if (line == "\r")
            {
                m_currentParsingState = 1;
                return;
            }
            line = line.Trim();
            string[] parts = line.Split(' ');

            if (parts[0] == "time:")
            {
                if (parts.Length != 8)
                {
                    Console.WriteLine("Error: Invalid time format in header.");
                    Console.WriteLine($"Received: {line}");
                    return;
                }
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                int day = int.Parse(parts[3]);
                int hour = int.Parse(parts[4]);
                int minute = int.Parse(parts[5]);
                int second = int.Parse(parts[6]);
                long unixTime = long.Parse(parts[7]);

                m_currentMessage.Time = new DateTime(year, month, day, hour, minute, second);
                m_currentMessage.UnixTime = unixTime;
            }
            else if (parts[0] == "name:")
            {
                m_currentMessage.Name = parts[1];
            }
            else if (parts[0] == "length:")
            {
                m_currentMessage.PayloadLength = int.Parse(parts[1]);
            }
        }

        void ParsePayload(string line)
        {
            m_currentMessage.Payload = line.Trim();
            if (m_currentMessage.Payload.Length == m_currentMessage.PayloadLength)
            {
                m_currentParsingState = 0;
                AddMessageToReceivedQueue(m_currentMessage);
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
                    ProcessMessage(message);
                }
            }
        }

        void ProcessMessage(Message message)
        {
            Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
        }
    }
}
