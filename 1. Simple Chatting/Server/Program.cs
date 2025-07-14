using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // console init
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // server init
            Server server = new Server(100);
            server.Init();
            server.Start();

            // input process
            String input;
            while(true)
            {
                input = Console.ReadLine().ToLower();
            }
        }
    }
}
