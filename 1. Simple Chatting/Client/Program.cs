using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("Input your m_name:");
            var name = Console.ReadLine() ?? "default name";


            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Loopback, 5000);

            SocketAsyncEventArgs receiveArg = new SocketAsyncEventArgs();
            byte[] receiveBuffer = new byte[1024];
            receiveArg.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            receiveArg.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveEventArg_Completed);
            receiveArg.UserToken = socket;

            if (!socket.ReceiveAsync(receiveArg))
            {
                ProcessReceive(receiveArg);
            }

            byte[] buffer = new byte[1024];

            while (true)
            {
                string? message = Console.ReadLine();
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }
                if (message == "test")
                {
                    for (int i = 0; i < 100; i++)
                    {
                        message = Test();
                        System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);
                        socket.Send(buffer, 0, message.Length, SocketFlags.None);
                    }
                    continue;
                }

                message = PackageMessage(name, message);

                System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);

                socket.Send(buffer, 0, System.Text.Encoding.UTF8.GetByteCount(message), SocketFlags.None);
            }
        }

        static void ReceiveEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        static void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                string message = System.Text.Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                Console.WriteLine($"{message}");
            }
            else
            {
                Console.WriteLine("Error receiving data.");
                Socket socket = (Socket)e.UserToken;
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return;
            }

            if (!((Socket)e.UserToken).ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        static string PackageMessage(string name, string payload)
        {
            string message = "";
            message += $"time: {DateTime.Now.Year} {DateTime.Now.Month} {DateTime.Now.Day} " +
                $"{DateTime.Now.Hour} {DateTime.Now.Minute} {DateTime.Now.Second} {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\r\n";
            message += $"name: {name}\r\n";
            message += $"length: {Encoding.UTF8.GetByteCount(payload)}\r\n";

            message += "\r\n";
            message += payload;
            message += "\r\n";
            return message;
        }

        static string Test()
        {
            Console.WriteLine("Test");
            string message = "";

            message += "time: 2025 1 1 12 20 30 1704091234567\r\n";
            message += "name: test\r\n";
            message += "length: 5\r\n";

            message += "\r\n";

            message += "Hello";
            message += "\r\n";

            return message;
        }
    }
}
