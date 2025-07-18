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

            Console.WriteLine("Input your name:");
            var name = Console.ReadLine();
            if (String.IsNullOrEmpty(name) || Encoding.UTF8.GetByteCount(name) > 20)
            {
                name = "Anonymous";
            }

            ChattingClient.Init();
            ChattingClient.Connect(IPAddress.Loopback, 5000);
            ChattingClient.StartListening();

            Thread.Sleep(100);

            while (true)
            {
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }

                ChattingClient.SendMessage(MessageType.Text, MessageTarget.All, name, input);
            }
        }
    }
}
