using System.Net;
using System.Net.Sockets;
namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

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

            while (true)
            {
                string? message = Console.ReadLine();
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }
                byte[] buffer = new byte[1024];

                System.Text.Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 0);

                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
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
                Console.WriteLine($"Received message: {message}");
            }
            else
            {
                Console.WriteLine("Error receiving data.");
            }

            if (!((Socket)e.UserToken).ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }
    }
}
