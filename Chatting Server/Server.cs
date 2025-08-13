using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;

namespace Chatting_Server
{
    internal class Server
    {
        private int m_maxConnections;
        private Socket? m_listenSocket;
        private Stack<SocketAsyncEventArgs>? m_freeSocketArgsPool;

        private List<Socket>? m_connectedSockets;
        private int m_numConnections;

        private ConcurrentQueue<Message> m_messages;

        public Server(IPEndPoint endPoint, int maxConnections = 100)
        {
            m_maxConnections = maxConnections;
            m_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_listenSocket.Bind(endPoint);

            m_freeSocketArgsPool = new Stack<SocketAsyncEventArgs>();

            m_connectedSockets = new List<Socket>();
            m_numConnections = 0;

            m_messages = new();
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
                if (m_numConnections < m_maxConnections)
                {
                    Socket? socket = e.AcceptSocket;
                    Console.WriteLine($"Connected: {socket!.RemoteEndPoint}");
                    var args = m_freeSocketArgsPool!.Pop();
                    SocketToken? token = args.UserToken as SocketToken;
                    token!.Socket = socket;

                    m_connectedSockets!.Add(socket!);
                    ReceiveStart(args);
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

            m_connectedSockets!.Remove(socket);
            socket.Close();
            m_freeSocketArgsPool!.Push(e);
        }
    }
}
