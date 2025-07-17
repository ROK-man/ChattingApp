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
            Server.Init();
            Server.Start();

            // input process
            String input;
            while(true)
            {
                input = Console.ReadLine()!.ToLower();
                if(string.IsNullOrEmpty(input))
                {
                    break;
                }
            }
        }
    }
}
