using System.Net;
using System.Text;
namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
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

                if (input.StartsWith("/w"))
                {
                    string[] parts = input.Split(' ', 3);
                    {
                        Message message = new Message(MessageType.Text, MessageTarget.Whisper, ChattingClient.UserName, parts[2]);
                        message.TargetName = parts[1];
                        ChattingClient.SendMessage(message);
                    }
                }
                else

                    ChattingClient.SendMessage(MessageType.Text, MessageTarget.All, input);
            }
        }
    }
}
