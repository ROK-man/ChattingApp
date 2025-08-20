namespace Chatting_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Server chattingServer = new(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 5000),
                "Host=localhost;Port=5432;Username=postgres;Password=qwer1234;Database=chattingserver",
                "mongodb://localhost:27017/");
            chattingServer.Init();
            chattingServer.Start();

            string input;
            while(true)
            {
                input = Console.ReadLine()!.ToLower();
                if(string.IsNullOrEmpty(input))
                {
                    continue;
                }

                if(input.Equals("check"))
                {
                    chattingServer.Check();
                }
            }
        }
    }
}
