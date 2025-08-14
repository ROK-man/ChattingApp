using MessageLib;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Chatting_Server
{
    internal record LappedMessage(SocketToken Token, Message Message);

    internal class MessageProcessor
    {
        private Server m_server;
        private BlockingCollection<LappedMessage> m_messages;

        public MessageProcessor(Server server, BlockingCollection<LappedMessage> messages)
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
            foreach (var serverMessage in m_messages.GetConsumingEnumerable())
            {
                Message message = serverMessage.Message;
                switch (message.Header!.Type)
                {
                    case MessageType.System:
                        Console.WriteLine("System serverMessage received.");
                        break;
                    case MessageType.Chatting:
                        ProcessChatting(message);
                        break;
                    default:
                        Console.WriteLine("Unknown serverMessage type.");
                        break;
                }
            }
        }

        private void ProcessChatting(Message message)
        {
            ChattingMessage? chat = message.Payload as ChattingMessage;
            switch (chat!.Type)
            {
                case ChattingType.All:
                    Console.WriteLine($"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}\t" +
                        $"Processed serverMessage: {message.Payload?.ToString()}");
                    m_server.SendAllChatting(message);
                    break;

                case ChattingType.Whisper:
                    Console.WriteLine($"To {chat.TargetName} {chat.Payload}\t" +
                        $"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}");
                    break;
            }
        }
    }
}
