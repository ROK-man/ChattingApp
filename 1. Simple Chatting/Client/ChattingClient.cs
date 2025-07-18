using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    internal class ChattingClient
    {
        private static ChattingClient? m_Client;

        MessageManager m_messageManager;

        Socket m_socket;
        byte[] m_receiveBuffer;

        Semaphore m_semaTrans;
        Semaphore m_semaParse;

        private ChattingClient()
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_receiveBuffer = new byte[4096];
            m_parsingBuffer = new byte[4096];

            m_semaTrans = new(0, 1);
            m_semaParse = new(1, 1);
            m_messageManager = new MessageManager();
        }

        public static void Init()
        {
            if (m_Client == null)
            {
                m_Client = new ChattingClient();
            }
            else
            {
                Console.WriteLine("Chatting client is already initialized.");
            }
        }

        public static void Connect(IPAddress ip, int port)
        {
            m_Client!.m_socket.Connect(ip, port);
            if (m_Client.m_socket.Connected)
            {
                Console.WriteLine($"Chatting server connected {m_Client.m_socket.RemoteEndPoint}");
            }
        }

        public static void StartListening()
        {
            SocketAsyncEventArgs receiveArg = new();
            receiveArg.SetBuffer(m_Client!.m_receiveBuffer, 0, m_Client.m_receiveBuffer.Length);
            receiveArg.Completed += new EventHandler<SocketAsyncEventArgs>(m_Client.ReceiveCompleted);

            Console.WriteLine("Start receiving messages");

            if (!m_Client.m_socket.ReceiveAsync(receiveArg))
            {
                m_Client.ProcessReceive(receiveArg);
            }

            Task.Run(() => m_Client.ParseData());
            m_Client.m_messageManager.StartWork();
        }
        public static void SendMessage(MessageType type, MessageTarget target, string name, string payload)
        {
            m_Client!.m_socket.SendAsync((new Message(type, target, name, payload).ToBytes()));
        }

        public static void ProcessMessage(Message message)
        {
            Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
        }

        void ReceiveCompleted(object? sender, SocketAsyncEventArgs receiveArg)
        {
            ProcessReceive(receiveArg);
        }
        void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                TransferData(e.Buffer!, e.BytesTransferred);
            }
            else if (e.SocketError != SocketError.Success)
            {
                Console.WriteLine($"Socket error occurred: {e.SocketError}");
                CloseConnect();
                return;
            }

            if (!m_socket.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        void CloseConnect()
        {
            m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
            Console.WriteLine("Connection closed.");
        }

        // Buffer
        int m_offset = 0;
        int m_currentDataLength = 0;
        int m_maxLength = 4096;
        int m_index = 0;
        byte[] m_parsingBuffer;

        void TransferData(byte[] buffer, int length)
        {
            if (length + m_currentDataLength > m_maxLength)
            {
                Console.WriteLine($"Error: Data length exceeds maximum limit. Current empty buffer: {m_maxLength - m_currentDataLength}, Received: {length}");
                return;
            }

            m_semaTrans.WaitOne();
            for (int i = 0; i < length; ++i)
            {
                m_parsingBuffer[m_index] = buffer[i];
                m_index = (m_index + 1) % m_maxLength;
            }
            m_currentDataLength += length;
            m_semaParse.Release();
        }

        void ParseData()
        {
            while (true)
            {
                m_semaParse.WaitOne();

                while (true)
                {
                    string line = GetLine();
                    if (String.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    m_messageManager.ParseLine(line);
                }
                m_semaTrans.Release();
            }
        }

        string GetLine()
        {
            string line = "";
            byte[] temp = new byte[m_maxLength]; // Big Problem
            int tempIndex = 0;
            for (int i = m_offset; i != m_index; i = (i + 1) % m_maxLength)
            {
                temp[tempIndex++] = m_parsingBuffer[i];
                if (m_parsingBuffer[i] == '\r' && m_parsingBuffer[(i + 1) % m_maxLength] == '\n')
                {
                    temp[tempIndex++] = (byte)'\n';
                    line = Encoding.UTF8.GetString(temp, 0, tempIndex);
                    m_offset = (i + 2) % m_maxLength;
                    m_currentDataLength -= tempIndex;
                    break;
                }
            }

            return line;
        }
    }
}
