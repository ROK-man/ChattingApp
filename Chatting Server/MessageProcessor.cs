using MessageLib;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Net.Sockets;

namespace Chatting_Server
{
    internal record LappedMessage(SocketToken Token, Message Message);

    internal class MessageProcessor
    {
        private Server m_server;
        private BlockingCollection<LappedMessage> m_messages;
        private HttpClient m_httpClient;

        public MessageProcessor(Server server, BlockingCollection<LappedMessage> messages)
        {
            m_messages = messages;
            m_server = server;
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void Start()
        {
            Task.Run(() => ProcessMessages());
        }

        private void ProcessMessages()
        {
            foreach (var serverMessage in m_messages.GetConsumingEnumerable())
            {
                Message message = serverMessage.Message;
                switch (message.Header!.Type)
                {
                    case MessageType.System:
                        Console.WriteLine("System serverMessage received.");
                        break;

                    case MessageType.Login:
                        _ = ProcessLogin(serverMessage);
                        Console.WriteLine("Login message received,");
                        break;

                    case MessageType.Chatting:
                        ProcessChatting(message);
                        break;

                    default:
                        Console.WriteLine("Unknown serverMessage type.");
                        break;
                }
            }
        }

        private async Task ProcessLogin(LappedMessage serverMessage)
        {
            LoginMessage? message = serverMessage.Message.Payload as LoginMessage;
            string? token = message!.Token;

            m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var userInfoResponse = await m_httpClient.GetAsync("https://localhost:7242/api/AccountAPI/userinfo");
            if (userInfoResponse.IsSuccessStatusCode)
            {
                var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<UserInfoResponse>();
                Console.WriteLine($"사용자 ID: {userInfo.UserId}");
                Console.WriteLine($"사용자 이름: {userInfo.Nickname}");
            }
            else
            {
                var error = await userInfoResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"사용자 정보 요청 실패: {error}");
            }

            m_server.SendLoginSuccess(serverMessage);
        }

        private void ProcessChatting(Message message)
        {
            ChattingMessage? chat = message.Payload as ChattingMessage;
            switch (chat!.Type)
            {
                case ChattingType.All:
                    Console.WriteLine($"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}\t" +
                        $"Processed serverMessage: {message.Payload?.ToString()}");
                    m_server.SendAllChatting(message);
                    break;

                case ChattingType.Whisper:
                    Console.WriteLine($"To {chat.TargetName} {chat.Payload}\t" +
                        $"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}");
                    break;
            }
        }
        public class UserInfoResponse
        {
            public string UserId { get; set; }
            public string Nickname { get; set; }
            public string Email { get; set; }
        }
    }
}
