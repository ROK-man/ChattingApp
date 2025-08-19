using MessageLib;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Chatting_Client
{
    internal class Client
    {
        Socket m_socket;
        public UserInfo UserInfo = new UserInfo();

        SocketAsyncEventArgs m_receiveArgs;

        MessageManager m_messageManager;
        MessageProcessor m_messageProcessor;

        BlockingCollection<Message> m_messages;
        private TaskCompletionSource<bool> m_loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); // dead lock occurred. 

        public Client()
        {
            m_socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_receiveArgs = new();

            m_messages = new();
            m_messageManager = new();
            m_messageProcessor = new(m_messages, this);

            SocketToken token = new(2048, m_messages);
            m_receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted!);
            m_receiveArgs.UserToken = token;
            token.SetBuffer(m_receiveArgs);
            token.Socket = m_socket;
        }

        public async Task<bool> LoginAsync()
        {
            string? userId, password;
            do
            {
                Console.Write("Input your ID: ");
                userId = Console.ReadLine();
            } while (string.IsNullOrEmpty(userId));
            do
            {
                Console.Write("Input your Password: ");
                password = ReadPassword();
            } while (string.IsNullOrEmpty(password));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var loginData = new { UserID = userId, Password = password };
            var jsonContent = JsonSerializer.Serialize(loginData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                Console.WriteLine("Trying web login...");
                var loginResponse = await httpClient.PostAsync("https://localhost:7242/api/AccountAPI/login", content);
                if (loginResponse.IsSuccessStatusCode)
                {
                    var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
                    Console.WriteLine("Web login Success");
                    var parts = result!.ChatServer.Split(':');

                    string ipString = parts[0];      // "127.0.0.1"
                    int port = int.Parse(parts[1]);  // 5000

                    IPAddress ip = IPAddress.Parse(ipString);
                    Connect(new IPEndPoint(ip, port));

                    return await LoginChattingServerAsync(result.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }

            return false;
        }

        public string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();

            return password;
        }
            
        private void Connect(IPEndPoint endpoint)
        {
            m_socket.Connect(endpoint);

            ReceiveStart(m_receiveArgs);
            m_messageProcessor.Start();
        }

        private async Task<bool> LoginChattingServerAsync(string token)
        {

            Message message = m_messageManager.MakeMessage(MessageType.Login, new LoginMessage(token));

            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);

            m_socket.Send(buffer);

            bool success = await m_loginTcs.Task.ConfigureAwait(false);

            if (success)
            {
                Console.WriteLine("Server Login Success");
                return true;
            }
            else
            {
                Console.WriteLine("Server Login Failed");
                return false;
            }
        }

        public void LoginSuccess(string name)
        {
            UserInfo.UserName = name;
            m_loginTcs.TrySetResult(true);
            Console.WriteLine($"My name: {name}");
        }

        public void LoginFailed()
        {
            Console.WriteLine("Server Login Failed!!!!");

            m_loginTcs.TrySetResult(false);
        }

        private void ReceiveStart(SocketAsyncEventArgs e)
        {
            SocketToken? token = e.UserToken as SocketToken;
            if (!token.Socket.ReceiveAsync(e))
            {
                ReceiveCompleted(e, e);
            }
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                SocketToken? token = e.UserToken as SocketToken;
                token.ProcessReceive(e, e.BytesTransferred);

                ReceiveStart(e);
            }
            else
            {
                Console.WriteLine(e.SocketError);
                CloseSocketConnect(e);
            }
        }
        void CloseSocketConnect(SocketAsyncEventArgs e)
        {
            SocketToken? token = e.UserToken as SocketToken;

            Socket socket = (token!).Socket!;
            Console.WriteLine($"Closing connection: {socket!.RemoteEndPoint}");

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }

            socket.Close();
        }

        public void SendChatting(ChattingType type, string targetName, string payload)
        {
            Message message = m_messageManager.MakeMessage(MessageType.Chatting, new ChattingMessage(type, UserInfo.UserName, targetName, payload));

            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);

            m_socket.Send(buffer);
        }

        public void SendMessage(MessageType type, MessagePayloadBase payload)
        {
            Message message = m_messageManager.MakeMessage(type, payload);

            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);

            m_socket.Send(buffer);
        }

        public void SendTest()
        {
            Message message = m_messageManager.MakeMessage(MessageType.Friend, new FriendMessage(FriendMessageType.Request, "jin", "kim"));

            byte[] buffer = new byte[message.GetByteLength()];
            message.Serialize(buffer, 0);

            m_socket.Send(buffer);
        }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public string Token { get; set; }
        public string ChatServer { get; set; }
    }
}
