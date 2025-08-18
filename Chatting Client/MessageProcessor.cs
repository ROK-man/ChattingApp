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
        private Client m_client;

        public MessageProcessor(BlockingCollection<Message> messages, Client client)
        {
            m_messages = messages;
            m_client = client;
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
                    case MessageType.Login:
                        ProcessLogin(message);
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

        private void ProcessLogin(Message message)
        {
            LoginMessage login = message.Payload as LoginMessage;
            if (login != null && login.Type == LoginType.Success)
            {
                m_client.LoginSuccess(login.Token);
            }
            else
            {
                m_client.LoginFailed();
            }
        }

        private void ProcessChatting(Message message)
        {
            ChattingMessage chat = message.Payload as ChattingMessage;
            switch ((ChattingType)chat.Type)
            {
                case ChattingType.All:
                    Console.WriteLine($"{chat.SenderName}: {chat.Payload}");
                    break;
                case ChattingType.Whisper:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"From {chat.SenderName}: {chat.Payload}");
                    Console.ResetColor();
                    break;
            }
        }
    }
}
