using System.Net;
using System.Net.Sockets;

namespace Chatting_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Client client = new Client();
            client.Connect(new IPEndPoint(IPAddress.Loopback, 5000));

            while (true)
            {
                string input = Console.ReadLine()!;
                if(string.IsNullOrEmpty(input))
                {
                    break;
                }

                client.SendChatting(input);
            }
        }
    }
}
