using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MessageLib;

namespace Chatting_Server
{
    // 접속, 수신, 송신 처리
    internal class Server
    {
        private int m_maxConnections;
        private Socket? m_listenSocket;
        private Stack<SocketAsyncEventArgs>? m_freeSocketArgsPool;

        private BlockingCollection<LappedMessage> m_messages;
        private MessageProcessor m_messageProcessor;

        public Server(IPEndPoint endPoint, int maxConnections = 100)
        {
            m_maxConnections = maxConnections;
            m_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_listenSocket.Bind(endPoint);

            m_freeSocketArgsPool = new Stack<SocketAsyncEventArgs>();

            m_messages = new();
            m_messageProcessor = new MessageProcessor(this, m_messages);
        }

        public void Init()
        {
            for (int i = 0; i < m_maxConnections; i++)
            {
                SocketAsyncEventArgs args = new();

                args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted!);

                SocketToken token = new(2048, m_messages);
                args.UserToken = token;
                token.SetBuffer(args);

                m_freeSocketArgsPool!.Push(args);
            }
        }

        public void Start()
        {
            SocketAsyncEventArgs listeningArgs = new();
            listeningArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted!);
            m_listenSocket!.Listen();

            AcceptStart(listeningArgs);

            m_messageProcessor.Start();
        }

        public void Check()
        {
            Console.WriteLine($"Connected clients: {m_maxConnections - m_freeSocketArgsPool.Count}");
        }

        private void AcceptStart(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;

            if (!m_listenSocket!.AcceptAsync(e))
            {
                AcceptCompleted(e, e);
            }
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (m_freeSocketArgsPool.Count > 0)
                {
                    AcceptConnect(e);
                }
                else
                {
                    Console.WriteLine("Error occured?!, Acceptcompleted");
                }
            }
            else
            {
                Console.WriteLine($"Listen error: {e.SocketError}");
            }
            AcceptStart(e);
        }

        private void AcceptConnect(SocketAsyncEventArgs e)
        {
            Socket? socket = e.AcceptSocket;
            Console.WriteLine($"Connected: {socket!.RemoteEndPoint}");

            var args = m_freeSocketArgsPool!.Pop();

            SocketToken? token = args.UserToken as SocketToken;
            token!.Socket = socket;

            ReceiveStart(args);
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
            else if (e.SocketError == SocketError.ConnectionReset)
            {
                CloseSocketConnect(e);
            }
            else
            {
                Console.WriteLine($"Receive Error occurred. From {e.RemoteEndPoint}: {e.SocketError}");
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
            m_freeSocketArgsPool!.Push(e);
            m_messageProcessor.DisconnectUser(token!);
        }
    }
}
