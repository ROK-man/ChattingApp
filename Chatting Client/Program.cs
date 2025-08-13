using System.Net.Sockets;

namespace Chatting_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            byte[] buffer = new byte[2048];

            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect("127.0.0.1", 5000);

            while (true)
            {
                string input = Console.ReadLine();
                if(string.IsNullOrEmpty(input))
                {
                    break;
                }

                int length = MessageManager.MakeChattingMessage(input!).GetBytes(buffer, 0, 2048);
                client.Send(buffer, length, SocketFlags.None);

            }
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }
}
