using MessageLib;
using System.Collections.Concurrent;

namespace Chatting_Server
{
    internal class MessageProcessor
    {
        private Server m_server;
        private BlockingCollection<Message> m_messages;

        public MessageProcessor(Server server, BlockingCollection<Message> messages)
        {
            m_messages = messages;
            m_server = server;
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
            switch((ChattingType)message.Header!.Flag)
            {
                case ChattingType.All:
                    Console.WriteLine($"Processed message: {message.Payload?.ToString()}\t " +
                        $"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}");
                    m_server.SendAllChatting(message);
                    break;

                case ChattingType.Whisper:
                    Console.WriteLine($"Whisper received");
                    break;
            }
        }
    }
}
