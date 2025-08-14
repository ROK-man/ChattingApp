using MessageLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Client
{
    internal class MessageProcessor
    {
        private BlockingCollection<Message> m_messages;

        public MessageProcessor(BlockingCollection<Message> messages)
        {
            m_messages = messages;
        }

        public void Start()
        {
            Task.Run(() => ProcessMessages());
        }

        private void ProcessMessages()
        {
            foreach (var message in m_messages.GetConsumingEnumerable())
            {
                switch (message.Header!.Type)
                {
                    case MessageType.System:
                        Console.WriteLine("System message received.");
                        break;
                    case MessageType.Chatting:
                        ProcessChatting(message);
                        break;
                    default:
                        Console.WriteLine("Unknown message type.");
                        break;
                }
            }
        }

        private void ProcessChatting(Message message)
        {
            switch ((ChattingType)message.Header!.Flag)
            {
                case ChattingType.All:
                    Console.WriteLine($"Processed message: {message.Payload?.ToString()}\t " +
                        $"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}");
                    break;
                case ChattingType.Whisper:
                    Console.WriteLine($"Whisper received");
                    break;
            }
        }   
    }
}
