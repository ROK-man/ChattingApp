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

            Console.WriteLine("Input your name:");
            var name = Console.ReadLine();
            if (String.IsNullOrEmpty(name) || Encoding.UTF8.GetByteCount(name) > 20)
            {
                name = "Anonymous";
            }


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
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if (input == "test")
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        socket.Send(Message.MakeMessage(name, $"Test message{i}"));
                        Thread.Sleep(1);
                    }
                    continue;
                }

                socket.Send(Message.MakeMessage(name, input));
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
                string message = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
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
    }
}
