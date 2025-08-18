using MessageLib;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
namespace Chatting_Server
{
    internal record LappedMessage(SocketToken Token, Message Message);

    internal class MessageProcessor
    {
        private BlockingCollection<LappedMessage> m_messages;
        private HttpClient m_httpClient;

        private Dictionary<string, SocketToken> m_connectedTokens = new();
        private MessageManager m_messageManager = new MessageManager();

        private UserInfoDB m_userInfoDB;

        public MessageProcessor(Server server, BlockingCollection<LappedMessage> messages)
        {
            m_messages = messages;
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            m_userInfoDB = new UserInfoDB("Host=localhost;Port=5432;Username=postgres;Password=qwer1234;Database=chattingserver");
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

                AcceptLogin(userInfo.UserId, userInfo.Nickname, serverMessage.Token);
            }
            else
            {
                var error = await userInfoResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"사용자 정보 요청 실패: {error}");
            }
        }

        private void AcceptLogin(string id, string nickName, SocketToken token)
        {
            UserInfo? info = m_userInfoDB.GetUserInfo(nickName);
            if (info == null)
            {
                m_userInfoDB.InsertUserInfo(id, nickName);
                Console.WriteLine($"New user registered: {nickName}");
                info = m_userInfoDB.GetUserInfo(nickName);
            }
            token.User = info;
            Console.WriteLine($"{info.Nickname} connected");

            //if (m_connectedTokens.ContainsKey(info.Nickname.ToLower().Trim()))
            //{
            //    Message rejectMsg = m_messageManager.MakeMessage(
            //        MessageType.TryLogin,
            //        new LoginMessage(LoginType.None, nickName)
            //    );
            //    token.SendMessage(rejectMsg);
            //    Console.WriteLine($"TryLogin rejected: {nickName} is already connected.");
            //    return;
            //}
            m_connectedTokens[info.Nickname.ToLower().Trim()] = token;


            Message message = m_messageManager.MakeMessage(MessageType.Login, new LoginMessage(LoginType.Success, token.User.Nickname));
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
                    SendWhisper(chat.TargetName, message);
                    break;
            }
        }

        private void BroadcastMessage(Message message)
        {
            foreach (var kv in m_connectedTokens)
            {
                SocketToken token = kv.Value;
                if (token.Socket != null && token.Socket.Connected)
                {
                    token.SendMessage(message);
                }
            }
        }

        private void SendWhisper(string targetNickname, Message message)
        {
            if (m_connectedTokens.TryGetValue(targetNickname.Trim().TrimEnd('\0').ToLower(), out var targetToken))
            {
                if (targetToken.Socket != null && targetToken.Socket.Connected)
                {
                    targetToken.SendMessage(message);
                }
            }
            else
            {
                Console.WriteLine($"Whisper failed: user {targetNickname} not found.");
            }
        }

        public void DisconnectUser(SocketToken token)
        {
            if (token.User != null && m_connectedTokens.ContainsKey(token.User.Nickname))
            {
                m_connectedTokens.Remove(token.User.Nickname);
                Console.WriteLine($"User {token.User?.Nickname} disconnected.");
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
