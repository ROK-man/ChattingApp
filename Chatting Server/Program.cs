namespace Chatting_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Server chattingServer = new(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 5000), 100);
            chattingServer.Init();
            chattingServer.Start();

            while(true)
            {
                Console.ReadLine();
            }
        }
    }
}
