using MessageLib;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
namespace Chatting_Server
{
    internal record LappedMessage(SocketToken Token, Message Message);

    internal class MessageProcessor
    {
        private BlockingCollection<LappedMessage> m_messages;
        private HttpClient m_httpClient;

        private List<SocketToken> m_connectedTokens = new List<SocketToken>();
        private MessageManager m_messageManager = new MessageManager();

        public MessageProcessor(Server server, BlockingCollection<LappedMessage> messages)
        {
            m_messages = messages;
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
                switch (serverMessage.Message.Header!.Type)
                {
                    case MessageType.System:
                        Console.WriteLine("System serverMessage received.");
                        break;

                    case MessageType.Login:
                        _ = ProcessLogin(serverMessage);
                        break;

                    case MessageType.Chatting:
                        ProcessChatting(serverMessage);
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
            string? JWT = message!.Token;

            m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JWT);
            var userInfoResponse = await m_httpClient.GetAsync("https://localhost:7242/api/AccountAPI/userinfo");
            if (userInfoResponse.IsSuccessStatusCode)
            {
                var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<UserInfoResponse>();
                Console.WriteLine($"사용자 ID: {userInfo.UserId}");
                Console.WriteLine($"사용자 이름: {userInfo.Nickname}");

                serverMessage.Token.User.UserName = userInfo.Nickname;
                SendLoginSuccess(serverMessage);
                m_connectedTokens.Add(serverMessage.Token);
            }
            else
            {
                var error = await userInfoResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"사용자 정보 요청 실패: {error}");
            }
        }
        private void SendLoginSuccess(LappedMessage serverMessage)
        {
            SocketToken? token = serverMessage.Token;

            Message message = m_messageManager.MakeMessage(MessageType.Login, new LoginMessage(LoginType.Success, token.User.UserName));
            token.SendMessage(message);
        }

        private void ProcessChatting(LappedMessage serverMessage)
        {
            Message message = serverMessage.Message;
            ChattingMessage? chat = message.Payload as ChattingMessage;

            switch (chat!.Type)
            {
                case ChattingType.All:
                    Console.WriteLine($"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}\t" +
                        $"{chat.SenderName}: {chat}");
                    BroadcastMessage(message);
                    break;

                case ChattingType.Whisper:
                    Console.WriteLine($"To {chat.TargetName} {chat.Payload}\t" +
                        $"ping: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - message.Header!.UnixTimeMilli}");
                    break;
            }
        }

        private void BroadcastMessage(Message message)
        {
            foreach (SocketToken token in m_connectedTokens)
            {
                if (token.Socket != null && token.Socket.Connected)
                {
                    token.SendMessage(message);
                }
            }
        }

        public void DisconnectUser(SocketToken token)
        {
            if (m_connectedTokens.Contains(token))
            {
                m_connectedTokens.Remove(token);
                Console.WriteLine($"User {token.User?.UserName} disconnected.");
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
