using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class Server
    {
        private static Server m_server = new(100);

        static int ID = 1;

        private int m_bufferSize;
        private int m_maxConnections;
        private int m_currentConnections;

        private SocketBufferManager m_bufferManager;
        private Stack<SocketAsyncEventArgs> m_freeArgsPool;
        private List<SocketAsyncEventArgs> m_connectedSocketArgs;

        private object m_lock;

        private Socket m_listeningSocket;

        private Server(int maxConnections)
        {
            m_bufferSize = 1024;
            m_maxConnections = maxConnections;
            m_currentConnections = 0;

            m_bufferManager = new SocketBufferManager(2 * m_maxConnections * m_bufferSize, m_bufferSize);
            m_freeArgsPool = new Stack<SocketAsyncEventArgs>(2 * m_maxConnections);
            m_connectedSocketArgs = [];

            m_lock = new();

            m_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public static void Init()
        {
            if(m_server == null)
            {
                m_server = new(100);
            }

            m_server.m_bufferManager.InitBuffer();

            SocketAsyncEventArgs eventArg;
            Token token;
            for (int i = 0; i < 2 * m_server.m_maxConnections; i++)
            {
                eventArg = new SocketAsyncEventArgs();
                eventArg.Completed += new EventHandler<SocketAsyncEventArgs>(m_server.IO_Completed);

                token = new Token();
                eventArg.UserToken = token;

                m_server.m_bufferManager.SetBuffer(eventArg);

                m_server.m_freeArgsPool.Push(eventArg);
            }
        }

        public static void Start()
        {
            m_server.m_listeningSocket.Bind(new IPEndPoint(IPAddress.Loopback, 5000));

            Console.WriteLine("Server is listening on port 5000, Loopback address");

            m_server.m_listeningSocket.Listen();

            SocketAsyncEventArgs acceptArg = new();
            acceptArg.Completed += new EventHandler<SocketAsyncEventArgs>(m_server.AcceptEventArg_Completed);

            m_server.StartAccept(acceptArg);
        }

        public static void ProcessMessage(Message message)
        {
            m_server.SendAsync(message);
        }

        public void SendAsync(Message message)
        {
            m_server.BroadCastMessage(message);
            switch(message.Type)
            {
                case MessageType.Text:
                    if(message.Target == MessageTarget.All)
                    {
                    }
                    break;
            }
        }

        void StartAccept(SocketAsyncEventArgs acceptArg)
        {
            acceptArg.AcceptSocket = null;

            if (!m_listeningSocket.AcceptAsync(acceptArg))
            {
                AcceptEventArg_Completed(m_listeningSocket, acceptArg);
            }
        }

        void AcceptEventArg_Completed(object? sender, SocketAsyncEventArgs e)
        {
            if (m_currentConnections >= m_maxConnections)
            {
                Console.WriteLine("Max connections reached.");
                e.AcceptSocket!.Close();
            }
            else
            {
                Console.WriteLine($"Client connected: {e.AcceptSocket!.RemoteEndPoint}");
                ProcessAccept(e);
            }

            StartAccept(e);
        }

        void ProcessAccept(SocketAsyncEventArgs e)
        {
            m_currentConnections++;
            if(m_freeArgsPool.Count > 0 && e.AcceptSocket != null)
            { 
                SocketAsyncEventArgs receiveEventArg = m_freeArgsPool.Pop();

                Token token = (Token)receiveEventArg.UserToken!;

                token.ClientSocket = e.AcceptSocket;
                token.SetId(ID++);
                token.Start();

                lock (m_lock)
                {
                    m_connectedSocketArgs.Add(receiveEventArg);
                }

                if (!e.AcceptSocket.ReceiveAsync(receiveEventArg))
                {
                    ProcessReceive(receiveEventArg);
                }
            }
            else
            {
                Console.WriteLine("Error: No free SocketAsyncEventArgs available.");
            }
        }

        void IO_Completed(object? sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
            }
        }

        void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                Token token = (Token)e.UserToken!;
                token.TransferData(e.Buffer!, e.Offset, e.BytesTransferred);

                if (!(token).ClientSocket!.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        void ProcessSend(SocketAsyncEventArgs e)
        {
            m_freeArgsPool.Push(e);
            m_bufferManager.SetBuffer(e);
        }

        void BroadCastMessage(Message message)
        {
            byte[] buffer = message.ToBytes();
            lock(m_lock)
            {
                foreach (var Arg in m_connectedSocketArgs)
                {
                    Token token = (Token)Arg.UserToken!;
                    if (token.ID == message.ID)
                    {
                        continue;
                    }

                    SocketAsyncEventArgs sendEventArg = m_freeArgsPool.Pop();

                    m_bufferManager.FreeBuffer(sendEventArg);
                    sendEventArg.SetBuffer(buffer, 0, buffer.Length);

                    Socket socket = token.ClientSocket!;

                    if (!socket.SendAsync(sendEventArg))
                    {
                        ProcessSend(sendEventArg);
                    }
                }
            }
        }

        void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Token token = (Token)e.UserToken!;
            token.End();

            Console.WriteLine($"Closing connection: {(token).ClientSocket!.RemoteEndPoint}");
            Socket socket = (token).ClientSocket;

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }

            catch (Exception) { }
            socket.Close();
            lock(m_lock)
            {
                m_connectedSocketArgs.Remove(e);
            }
            m_currentConnections--;
            m_freeArgsPool.Push(e);
        }
    }
}
