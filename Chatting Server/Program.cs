namespace Chatting_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
	    Console.WriteLine("hello, server!!");

            Server chattingServer = new(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 5000),
                "Host=localhost;Port=5432;Username=postgres;Password=qwer1234;Database=chattingserverdb",
                "mongodb://localhost:27017/", 100);
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
