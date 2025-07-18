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
            BroadcastMessage(message);
        }

        void BroadcastMessage(Message message)
        {
            byte[] buffer = message.ToBytes();
            lock (m_lock)
            {
                foreach (var Arg in m_socketArgs)
                {
                    Token token = (Token)Arg.UserToken!;
                    if (token.ID == message.ID)
                    {
                        continue;
                    }

                    Socket socket = token.ClientSocket!;

                    ChattingServer.SendMessage(socket, message);
                }
            }
        }
    }
}
