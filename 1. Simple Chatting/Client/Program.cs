using System.Net;
using System.Text;
namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ChattingClient.Init();
            ChattingClient.Connect(IPAddress.Loopback, 5000);
            ChattingClient.StartListening();

            Thread.Sleep(100);
            ChattingClient.Login();
            while (ChattingClient.IsLogined() == false) { }
            Console.WriteLine("Login Success!!");

            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                ChattingClient.SendMessage(MessageType.Text, MessageTarget.All, input);
            }
        }
    }
}
