using MessageLib;
using System.Net;
using System.Net.Sockets;

namespace Chatting_Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Client client = new Client();

            await client.LoginAsync();

            while (true)
            {
                string input = Console.ReadLine()!;
                if (string.IsNullOrEmpty(input))
                {
                    break;
                }

                if (input.Equals("test"))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        client.SendChatting(ChattingType.All, string.Empty, $"Test Chat #{i}");
                    }
                    continue;
                }

                if (input.StartsWith("/w"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(input, @"^/w\s+(""(?<name>[^""]+)""|(?<name>\S+))\s+(?<msg>.+)$");

                    if (match.Success)
                    {
                        string targetName = match.Groups["name"].Value;
                        string message = match.Groups["msg"].Value;

                        client.SendChatting(ChattingType.Whisper, targetName, message);
                    }
                }
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop);

                    client.SendChatting(ChattingType.All, string.Empty, input);
                }
            }
        }
    }
}
