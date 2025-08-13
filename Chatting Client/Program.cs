using System.Net.Sockets;

namespace Chatting_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                string input = Console.ReadLine();

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect("127.0.0.1", 5000);

                client.Send(new Message().MakeMessage(input));

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }
    }
}
