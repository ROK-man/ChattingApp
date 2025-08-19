using MessageLib;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Chatting_Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Client client = new Client();

            bool logined = await client.LoginAsync();
            if (!logined)
            {
                return;
            }

            Console.WriteLine("Available commands:");
            Console.WriteLine("/help - Show this help message");
            Console.WriteLine("/l - Get friends List");
            Console.WriteLine("/p - Get friend request List");
            Console.WriteLine("/t - Get group List");
            Console.WriteLine("/c \"<name>\" - Create a group");
            Console.WriteLine("/gl \"<name>\" - Get group member list");
            Console.WriteLine("/cp \"<name>\" \"<password>\" - Create a group with password");
            Console.WriteLine("/j \"<name>\" - Join a group");
            Console.WriteLine("/jp \"<name>\" \"<password>\" - Join a group with password");
            Console.WriteLine("/q \"<name>\" - Quit group");
            Console.WriteLine("/w \"<name>\" <message> - Whisper to a user");
            Console.WriteLine("/r \"<name>\" - Request friendship with a user");
            Console.WriteLine("/a \"<name>\" - Accept friendship request from a user");
            Console.WriteLine("/d \"<name>\" - Reject friendship request from a user");
            Console.WriteLine("/k \"<name>\" - Remove a friend");
            Console.WriteLine("/b \"<name>\" - Block a user");
            Console.WriteLine("/u \"<name>\" - Unblock a user");
            Console.WriteLine("/test - Send test messages to all users");

            while (true)
            {
                string input = Console.ReadLine()!;
                if (string.IsNullOrEmpty(input))
                {
                    continue;
                }
                if (input.Equals("/help"))
                {
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("/help - Show this help message");
                    Console.WriteLine("/l - Get friends List");
                    Console.WriteLine("/p - Get friend request List");
                    Console.WriteLine("/t - Get group List");
                    Console.WriteLine("/c \"<name>\" - Create a group");
                    Console.WriteLine("/gl \"<name>\" - Get group member list");
                    Console.WriteLine("/cp \"<name>\" \"<password>\" - Create a group with password");
                    Console.WriteLine("/j \"<name>\" - Join a group");
                    Console.WriteLine("/jp \"<name>\" \"<password>\" - Join a group with password");
                    Console.WriteLine("/q \"<name>\" - Quit group");
                    Console.WriteLine("/w \"<name>\" <message> - Whisper to a user");
                    Console.WriteLine("/r \"<name>\" - Request friendship with a user");
                    Console.WriteLine("/a \"<name>\" - Accept friendship request from a user");
                    Console.WriteLine("/d \"<name>\" - Reject friendship request from a user");
                    Console.WriteLine("/k \"<name>\" - Remove a friend");
                    Console.WriteLine("/b \"<name>\" - Block a user");
                    Console.WriteLine("/u \"<name>\" - Unblock a user");
                    Console.WriteLine("/test - Send test messages to all users");
                    continue;
                }
                if(input.Equals("/t"))
                {
                    client.SendMessage(MessageType.Group, new GroupMessage(GroupType.GetGroupList, client.UserInfo.UserName, string.Empty, string.Empty, string.Empty));
                    continue;
                }

                if (input.Equals("/l"))
                {
                    client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.GetFriendList, client.UserInfo.UserName, string.Empty));
                    continue;
                }

                if (input.Equals("/p"))
                {
                    client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.GetFriendRequestList, client.UserInfo.UserName, string.Empty));
                    continue;
                }

                if(input.StartsWith("/cp"))
                {
                    Console.WriteLine("Create group with password");
                    var match = Regex.Match(input, @"^/cp\s+(""(?<name>[^""]+)""|(?<name>\S+))\s+(""(?<password>[^""]+)""|(?<password>\S+))$");
                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        string password = match.Groups["password"].Value;
                        client.SendMessage(MessageType.Group, new GroupMessage(GroupType.Create, client.UserInfo.UserName, groupName, password, string.Empty));
                    }
                    continue;
                }
                if(input.StartsWith("/c"))
                {
                    var match = Regex.Match(input, @"^/c\s+(""(?<name>[^""]+)""|(?<name>\S+))$");

                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        client.SendMessage(MessageType.Group, 
                            new GroupMessage(GroupType.Create, client.UserInfo.UserName, groupName, string.Empty, string.Empty));
                    }
                    continue;
                }
                if (input.StartsWith("/jp"))
                {
                    var match = Regex.Match(input, @"^/jp\s+(""(?<name>[^""]+)""|(?<name>\S+))\s+(""(?<password>[^""]+)""|(?<password>\S+))$");
                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        string password = match.Groups["password"].Value;
                        client.SendMessage(MessageType.Group, new GroupMessage(GroupType.Join, client.UserInfo.UserName, groupName, password, string.Empty));
                    }
                    continue;
                }

                if (input.StartsWith("/j"))
                {
                    var match = Regex.Match(input, @"^/j\s+(""(?<name>[^""]+)""|(?<name>\S+))$");
                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        client.SendMessage(MessageType.Group, new GroupMessage(GroupType.Join, client.UserInfo.UserName, groupName, string.Empty, string.Empty));
                    }
                    continue;
                }
                if (input.StartsWith("/q"))
                {
                    var match = Regex.Match(input, @"^/q\s+(""(?<name>[^""]+)""|(?<name>\S+))$");
                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        client.SendMessage(MessageType.Group, new GroupMessage(GroupType.Quit, client.UserInfo.UserName, groupName, string.Empty, string.Empty));
                    }
                    continue;
                }
                if (input.StartsWith("/gl"))
                {
                    var match = Regex.Match(input, @"^/gl\s+(""(?<name>[^""]+)""|(?<name>\S+))$");
                    if (match.Success)
                    {
                        string groupName = match.Groups["name"].Value;
                        client.SendMessage(MessageType.Group, new GroupMessage(GroupType.GetMemberList, client.UserInfo.UserName, groupName, string.Empty, string.Empty));
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

                        if (targetName.ToLower() == client.UserInfo.UserName.ToLower())
                        {
                            continue;
                        }
                        client.SendChatting(ChattingType.Whisper, targetName, message);
                    }
                    continue;
                }

                if (input.StartsWith("/r"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/r\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );

                    if (match.Success)
                    {
                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Request, client.UserInfo.UserName, target));
                    }

                    continue;
                }

                if (input.StartsWith("/a"))
                {

                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/a\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );

                    if (match.Success)
                    {

                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Accept, client.UserInfo.UserName, target));
                    }

                    continue;
                }
                if (input.StartsWith("/d"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/d\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );
                    if (match.Success)
                    {
                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Reject, client.UserInfo.UserName, target));
                    }
                    continue;
                }

                if (input.StartsWith("/k"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/k\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );
                    if (match.Success)
                    {
                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Remove, client.UserInfo.UserName, target));
                    }
                    continue;
                }
                if (input.StartsWith("/b"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/b\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );
                    if (match.Success)
                    {
                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Block, client.UserInfo.UserName, target));
                    }
                    continue;
                }
                if (input.StartsWith("/u"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        input,
                        @"^/u\s+(""(?<msg>[^""]+)""|(?<msg>.+))$"
                    );
                    if (match.Success)
                    {
                        string target = match.Groups["msg"].Value;
                        client.SendMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Unblock, client.UserInfo.UserName, target));
                    }
                    continue;
                }


                if (input.Equals("/test"))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        client.SendChatting(ChattingType.All, string.Empty, $"Test Chat #{i}");
                    }
                    continue;
                }

                // all chatting
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop);

                client.SendChatting(ChattingType.All, string.Empty, input);
            }
        }
    }
}
