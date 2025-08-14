using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MessageLib;

namespace Chatting_Client
{
    internal class Client
    {
        Socket m_socket;

        SocketAsyncEventArgs m_receiveArgs;

        MessageManager m_messageManager;
        MessageProcessor m_messageProcessor;

        BlockingCollection<Message> m_messages;

        public Client()
        {
            m_socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_receiveArgs = new();

            m_messages = new();
            m_messageManager = new();
            m_messageProcessor = new(m_messages);

            SocketToken token = new(2048, m_messages);
            m_receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted!);
            m_receiveArgs.UserToken = token;
            token.SetBuffer(m_receiveArgs);
            token.Socket = m_socket;
        }

        public void Connect(IPEndPoint endpoint)
        {
            m_socket.Connect(endpoint);

            ReceiveStart(m_receiveArgs);

            m_messageProcessor.Start();
        }

        private void ReceiveStart(SocketAsyncEventArgs e)
        {
            SocketToken? token = e.UserToken as SocketToken;
            if (!token.Socket.ReceiveAsync(e))
            {
                ReceiveCompleted(e, e);
            }
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                SocketToken? token = e.UserToken as SocketToken;
                token.ProcessReceive(e, e.BytesTransferred);

                ReceiveStart(e);
            }
            else
            {
                Console.WriteLine(e.SocketError);
                CloseSocketConnect(e);
            }
        }
        void CloseSocketConnect(SocketAsyncEventArgs e)
        {
            SocketToken? token = e.UserToken as SocketToken;

            Socket socket = (token!).Socket!;
            Console.WriteLine($"Closing connection: {socket!.RemoteEndPoint}");

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }

            socket.Close();
        }

        public void SendChatting(string payload)
        {
            Message message = m_messageManager.MakeMessage(MessageType.Chatting, new ChattingMessage(payload));
            message.Header!.Flag = (byte)ChattingType.All;

            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);

            m_socket.Send(buffer);
        }
    }
}
