using System.Net.Sockets;

namespace Server
{
    internal class MessageProcessor
    {
        List<SocketAsyncEventArgs> m_socketArgs;
        object m_lock;

        public MessageProcessor(List<SocketAsyncEventArgs> socketArgs, object socketLock)
        {
            m_socketArgs = socketArgs;
            m_lock = socketLock;
        }

        public void ProcessMessage(Message message)
        {
            switch(message.Type)
            {
                case MessageType.Text:
                    ProcessText(message);
                    break;
                case MessageType.Login:
                    ProcessLogin(message);
                    break;
            }
        }

        void ProcessText(Message message)
        {
            switch(message.Target)
            {
                case MessageTarget.All:
                    Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
                    BroadcastMessage(message);
                    break;
                case MessageTarget.Whisper:
                    SendWhisper(message);
                    break;
            }
        }

        void BroadcastMessage(Message message)
        {
            byte[] buffer = message.ToBytes();
            lock (m_lock)
            {
                foreach (var Arg in m_socketArgs)
                {
                    Token token = (Token)Arg.UserToken!;
                    if (token.Name == message.Name || token.Name == string.Empty)
                    {
                        continue;
                    }
                    Socket socket = token.ClientSocket!;

                    ChattingServer.SendMessage(socket, message);
                }
            }
        }

        void SendWhisper(Message message)
        {
            lock (m_lock)
            {
                foreach(var Arg in m_socketArgs)
                {
                    Token token = (Token)Arg.UserToken;
                    if(token.Name == message.TargetName)
                    {
                        ChattingServer.SendMessage(token.ClientSocket, message);
                    }
                }
            }
        }

        void ProcessLogin(Message message)
        {
            bool success = true;
            lock (m_lock)
            {
                foreach (var Arg in m_socketArgs)
                {
                    Token token = (Token) Arg.UserToken;
                    if(token.Name == message.Name)
                    {
                        message.Payload = "Failed";
                        success = false;
                        ChattingServer.SendMessage(message.Token.ClientSocket, message);
                    }
                }
                if(success)
                {
                    message.Token.Name = message.Name;
                    ChattingServer.SendMessage(message.Token.ClientSocket, message);
                    Console.WriteLine($"{message.Name} logined");
                }
            }

        }
    }
}
