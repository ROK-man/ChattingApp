using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Server server = new Server(100);
            server.Init();
            server.Start();

            String input;
            while(true)
            {
                input = Console.ReadLine().ToLower();
                if(input == "connections")
                {
                    Console.WriteLine($"Current args pool size: {server.m_freeArgsPool.Count()}");
                    Console.WriteLine($"Current connections: {server.CurrentConnections}");
                }
            }
        }
    }
}
