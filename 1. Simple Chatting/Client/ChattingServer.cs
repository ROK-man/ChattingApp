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
        IPAddress m_iP;
        int m_port;
        Socket m_socket;
        byte[] m_receiveBuffer;

        SemaphoreSlim m_semaphore;

        public ChattingServer(IPAddress ip, int port)
        {
            m_iP = ip;
            m_port = port;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_receiveBuffer = new byte[4096];
            m_parsingBuffer = new byte[4096];

            m_semaphore = new(0);
        }

        public void Connect()
        {
            m_socket.Connect(m_iP, m_port);
            Console.WriteLine($"Chatting server connected {m_socket.RemoteEndPoint}");
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
            Console.WriteLine("Parsing thread started.");
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
        int m_state = 0; // 0: Header, 1: Payload
        Message message = new();

        void TransferData(byte[] buffer, int length)
        {
            if (length + m_currentDataLength > m_maxLength)
            {
                Console.WriteLine($"Error: Data length exceeds maximum limit. Current empty buffer: {m_maxLength - m_currentDataLength}, Received: {length}");
                return;
            }

            for (int i = 0; i < length; ++i)
            {
                m_parsingBuffer[m_index] = buffer[i];
                m_index = (m_index + 1) % m_maxLength;
            }
            m_currentDataLength += length;
            m_semaphore.Release();
        }

        async Task ParseData()
        {
            while (true)
            {
                await m_semaphore.WaitAsync();

                while (true)
                {
                    string line = GetLine();
                    if (String.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    ParseLine(line);
                }
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

        void ParseLine(string line)
        {
            switch (m_state)
            {
                case 0:
                    ParseHeader(line);
                    break;
                case 1:

                    ParsePayload(line);
                    break;
            }
        }

        void ParseHeader(string line)
        {
            if (line == "\r")
            {
                m_state = 1;
                return;
            }
            line = line.Trim();
            string[] parts = line.Split(' ');

            if (parts[0] == "time:")
            {
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                int day = int.Parse(parts[3]);
                int hour = int.Parse(parts[4]);
                int minute = int.Parse(parts[5]);
                int second = int.Parse(parts[6]);
                long unixTime = long.Parse(parts[7]);

                message.Time = new DateTime(year, month, day, hour, minute, second);
                message.UnixTime = unixTime;
            }
            else if (parts[0] == "name:")
            {
                message.Name = parts[1];
            }
            else if (parts[0] == "length:")
            {
                message.PayloadLength = int.Parse(parts[1]);
            }
        }

        void ParsePayload(string line)
        {
            message.Payload = line.Trim();
            Console.WriteLine($"[{message.Time.Minute:00}:{message.Time.Second:00}] {message.Name}: {message.Payload}");
            message = new();
            m_state = 0;
        }

        public void SendMessage(string name, string payload)
        {
            if (string.IsNullOrEmpty(name) || Encoding.UTF8.GetByteCount(name) > 20)
            {
                name = "Anonymous";
            }
            m_socket.Send(Message.MakeMessage(name, payload));
        }
    }
}
