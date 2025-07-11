using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Token
    {
        public Socket ClientSocket;
        public byte[] Buffer;
        public int m_index;
        public int m_length;

        public Token()
        {
            m_index = 0;
            Buffer = new byte[4096];
        }

        public void TransferData(byte[] buffer, int offset, int length)
        {
            for(int i=0; i<length; i++)
            {
                Buffer[m_index++] = buffer[i + offset];
            }
            m_length += length;
        }
    }
    internal class Server
    {
        Socket m_listeningSocket;
        public Stack<SocketAsyncEventArgs> m_freeArgsPool;
        List<Socket> m_connectedSockets;
        object m_lock;
        int m_maxConnections;
        private int m_currentConnections;
        public int CurrentConnections => m_currentConnections; 
        BufferManager m_bufferManager;
        int m_bufferSize;

        public Server(int maxConnections)
        {
            m_bufferSize = 1024;
            m_maxConnections = maxConnections;
            m_currentConnections = 0;
            m_bufferManager = new BufferManager(2 * m_maxConnections * m_bufferSize, m_bufferSize);
            m_freeArgsPool = new Stack<SocketAsyncEventArgs>(2 * m_maxConnections);
            m_connectedSockets = new();
            m_lock = new object();
        }

        public void Init()
        {
            m_bufferManager.InitBuffer();
            SocketAsyncEventArgs eventArg;
            Token token;

            for (int i = 0; i < 2 * m_maxConnections; i++)
            {
                eventArg = new SocketAsyncEventArgs();
                eventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);

                token = new Token();
                eventArg.UserToken = token;

                m_bufferManager.SetBuffer(eventArg);
                m_freeArgsPool.Push(eventArg);
            }
        }

        public void Start()
        {
            m_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_listeningSocket.Bind(new IPEndPoint(IPAddress.Loopback, 5000));

            Console.WriteLine("Server is listening on port 5000, Loopback address");

            m_listeningSocket.Listen();

            SocketAsyncEventArgs acceptArg = new();
            acceptArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);

            StartAccept(acceptArg);
        }

        void StartAccept(SocketAsyncEventArgs acceptArg)
        {
            acceptArg.AcceptSocket = null;
            if (!m_listeningSocket.AcceptAsync(acceptArg))
            {
                AcceptEventArg_Completed(m_listeningSocket, acceptArg);
            }
        }

        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (m_currentConnections >= m_maxConnections)
            {
                Console.WriteLine("Max connections reached. Rejecting new connection.");
                e.AcceptSocket.Close();
            }
            else
            {
                Console.WriteLine($"Client connected: {e.AcceptSocket.RemoteEndPoint}");
                ProcessAccept(e);
            }

            StartAccept(e);
        }

        void ProcessAccept(SocketAsyncEventArgs e)
        {
            m_currentConnections++;
            if(m_freeArgsPool.Count > 0)
            { 
                SocketAsyncEventArgs receiveEventArg = m_freeArgsPool.Pop();
                ((Token)receiveEventArg.UserToken).ClientSocket = e.AcceptSocket;
                lock(m_lock)
                {
                    m_connectedSockets.Add(e.AcceptSocket);
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

        void IO_Completed(object sender, SocketAsyncEventArgs e)
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
                Token token = (Token)e.UserToken; 
                token.TransferData(e.Buffer, e.Offset, e.BytesTransferred);

                TryDataProcess(token);

                if (!((Token)e.UserToken).ClientSocket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        void TryDataProcess(Token token)
        {
            String message = System.Text.Encoding.UTF8.GetString(token.Buffer, 0, token.m_length);
            Console.WriteLine($"Received message: {message}");

            BroadCastMessage(message);
            token.m_length = 0;
            token.m_index = 0;
        }

        void ProcessSend(SocketAsyncEventArgs e)
        {
            m_freeArgsPool.Push(e);
            Console.WriteLine($"To {((Token)e.UserToken).ClientSocket.RemoteEndPoint}: Send completed.");
            m_bufferManager.SetBuffer(e);
        }

        void BroadCastMessage(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return;
            }
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            lock(m_lock)
            {
                foreach (Socket socket in m_connectedSockets)
                {
                    SocketAsyncEventArgs sendEventArg = m_freeArgsPool.Pop();

                    m_bufferManager.FreeBuffer(sendEventArg);
                    sendEventArg.SetBuffer(buffer, 0, buffer.Length);

                    ((Token)sendEventArg.UserToken).ClientSocket = socket;
                    if (!socket.SendAsync(sendEventArg))
                    {
                        ProcessSend(sendEventArg);
                    }
                }
            }
        }

        void CloseClientSocket(SocketAsyncEventArgs e)
        {
            Console.WriteLine($"Closing connection: {((Token)e.UserToken).ClientSocket.RemoteEndPoint}");
            Socket socket = ((Token)e.UserToken).ClientSocket;

            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }

            catch (Exception) { }
            socket.Close();
            lock(m_lock)
            {
                m_connectedSockets.Remove(socket);
            }
            m_currentConnections--;
            m_freeArgsPool.Push(e);
        }
    }

    internal class BufferManager
    {
        int m_numBytes;
        byte[] m_buffer;
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int numBytes, int bufferSize)
        {
            m_numBytes = numBytes;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
            m_currentIndex = 0;
        }

        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else if (m_currentIndex <= m_numBytes - m_bufferSize)
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            else
            {
                Console.WriteLine("Buffer assign Error!!");
            }
             return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
