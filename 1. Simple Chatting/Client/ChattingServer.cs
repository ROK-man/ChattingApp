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
    internal class ChattingServer
    {
        MessageManager m_messageManager;

        IPAddress m_iP;
        int m_port;
        Socket m_socket;
        byte[] m_receiveBuffer;

        Semaphore m_semaphore;

        public ChattingServer(IPAddress ip, int port)
        {
            m_iP = ip;
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_receiveBuffer = new byte[4096];
            m_parsingBuffer = new byte[4096];

            m_semaphore = new(1, 1);
            m_messageManager = new MessageManager();
        }

        public void Connect()
        {
            m_socket.Connect(m_iP, m_port);
            if (m_socket.Connected)
            {
                Console.WriteLine($"Chatting server connected {m_socket.RemoteEndPoint}");
                m_messageManager.Socket = m_socket;
            }
        }

        public void StartListening()
        {
            SocketAsyncEventArgs receiveArg = new();
            receiveArg.SetBuffer(m_receiveBuffer, 0, m_receiveBuffer.Length);
            receiveArg.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);

            Console.WriteLine("Start receiving messages");

            if (!m_socket.ReceiveAsync(receiveArg))
            {
                ProcessReceive(receiveArg);
            }

            Task.Run(() => ParseData());
            m_messageManager.StartWork();
        }

        void ReceiveCompleted(object sender, SocketAsyncEventArgs receiveArg)
        {
            ProcessReceive(receiveArg);
        }
        void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                TransferData(e.Buffer, e.BytesTransferred);
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

            m_semaphore.WaitOne();
            for (int i = 0; i < length; ++i)
            {
                m_parsingBuffer[m_index] = buffer[i];
                m_index = (m_index + 1) % m_maxLength;
            }
            m_currentDataLength += length;
            m_semaphore.Release();
        }

        void ParseData()
        {
            while (true)
            {
                m_semaphore.WaitOne();

                while (true)
                {
                    string line = GetLine();
                    if (String.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    m_messageManager.ParseLine(line);
                }
                m_semaphore.Release();
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
                    line = Encoding.UTF8.GetString(temp, 0, tempIndex);
                    m_offset = (i + 2) % m_maxLength;
                    m_currentDataLength -= (tempIndex + 1);
                    break;
                }
            }

            return line;
        }

        public void SendMessage(MessageType type, MessageTarget target, string name, string payload)
        {
            m_messageManager.Send(type, target, name, payload);
        }
    }
}
