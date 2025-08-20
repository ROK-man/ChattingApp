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

        private DBController m_dataBaseController;

        public MessageProcessor(Server server, BlockingCollection<LappedMessage> messages, string postgresql, string mongoDB)
        {
            m_messages = messages;
            m_httpClient = new HttpClient();
            m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            m_dataBaseController = new DBController(postgresql, mongoDB);
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

                    case MessageType.Friend:
                        ProcessFriend(serverMessage);
                        break;

                    case MessageType.Group:
                        ProcessGroup(serverMessage);
                        break;

                    default:
                        Console.WriteLine("Unknown serverMessage type.");
                        Console.WriteLine(serverMessage.Message);
                        break;
                }
            }
        }

        private void ProcessGroup(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            Console.WriteLine(serverMessage.Message.Payload);
            switch (message!.Type)
            {
                case GroupType.Create:
                    ProcessGroupCreate(serverMessage);
                    break;
                case GroupType.Join:
                    ProcessGroupJoin(serverMessage);
                    break;
                case GroupType.Quit:
                    ProcessGroupLeave(serverMessage);
                    break;
                case GroupType.GetGroupList:
                    ProcessGetGroupList(serverMessage);
                    break;
                case GroupType.GetMemberList:
                    ProcessGetMemberList(serverMessage);
                    break;
            }
        }

        private void ProcessGroupCreate(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            GroupInfo? groupInfo = m_dataBaseController.GetGroupInfo(message!.GroupName);

            if (groupInfo != null)
            {
                Console.WriteLine($"GroupType {message.GroupName} already exists.");
                return;
            }

            if (message.HavePassword)
            {
                m_dataBaseController.InsertGroupWithPassword(message.GroupName, message.Password);
            }
            else
            {
                m_dataBaseController.InsertGroup(message.GroupName);
            }

            ProcessGroupJoin(serverMessage);
        }

        private void ProcessGroupJoin(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            GroupInfo? groupInfo = m_dataBaseController.GetGroupInfo(message!.GroupName);
            UserInfo? userInfo = m_dataBaseController.GetUserInfo(message.Sender);
            if (groupInfo == null || userInfo == null)
            {
                Console.WriteLine($"GroupType {message.GroupName} does not exist.");
                return;
            }

            if (message.HavePassword && groupInfo.Password != string.Empty)
            {
                if (message.Password.Equals(groupInfo.Password, StringComparison.OrdinalIgnoreCase) == false)
                {
                    Console.WriteLine($"GroupType {message.GroupName} join failed: Incorrect password.");
                    return;
                }
                if (m_dataBaseController.JoinGroupWithPassword(userInfo.UserNo, groupInfo.GroupNo, message.Password))
                {
                    Console.WriteLine($"User {userInfo.Nickname} joined group {groupInfo.GroupName} successfully.");
                }
                else
                {
                    Console.WriteLine($"User {userInfo.Nickname} failed to join group {groupInfo.GroupName}.");
                }
            }
            else
            {
                if (m_dataBaseController.JoinGroup(userInfo.UserNo, groupInfo.GroupNo))
                {
                    Console.WriteLine($"User {userInfo.Nickname} joined group {groupInfo.GroupName} successfully.");
                }
                else
                {
                    Console.WriteLine($"User {userInfo.Nickname} failed to join group {groupInfo.GroupName}.");
                }
            }
        }

        private void ProcessGroupLeave(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            GroupInfo? groupInfo = m_dataBaseController.GetGroupInfo(message!.GroupName);
            UserInfo? userInfo = m_dataBaseController.GetUserInfo(message.Sender);

            if (groupInfo == null || userInfo == null)
            {
                Console.WriteLine($"GroupType {message.GroupName} does not exist or user {message.Sender} not found.");
                return;
            }

            if (!m_dataBaseController.IsUserInGroup(userInfo!.UserNo, groupInfo!.GroupNo))
            {
                Console.WriteLine($"User {userInfo.Nickname} is not in group {groupInfo.GroupName}.");
                return;
            }

            if (m_dataBaseController.QuitGroup(userInfo.UserNo, groupInfo.GroupNo))
            {
                Console.WriteLine($"User {userInfo.Nickname} left group {groupInfo.GroupName} successfully.");

                if (m_dataBaseController.GetGroupMemberCount(groupInfo.GroupNo) == 0)
                {
                    m_dataBaseController.RemoveGroup(groupInfo.GroupNo);
                    Console.WriteLine($"GroupType {groupInfo.GroupName} has no members left and has been removed.");
                }
            }
            else
            {
                Console.WriteLine($"User {userInfo.Nickname} failed to leave group {groupInfo.GroupName}.");
            }
        }

        private void ProcessGetMemberList(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            GroupInfo? groupInfo = m_dataBaseController.GetGroupInfo(message!.GroupName);
            UserInfo? userInfo = m_dataBaseController.GetUserInfo(message.Sender);

            if (groupInfo == null || userInfo == null)
            {
                Console.WriteLine($"GroupType {message.GroupName} or {userInfo!.Nickname} does not exist");
                return;
            }
            if (!m_dataBaseController.IsUserInGroup(userInfo.UserNo, groupInfo.GroupNo))
            {
                Console.WriteLine($"User {userInfo.Nickname} is not in group {groupInfo.GroupName}." +
                    $"근데도 멤버 조회를 하려고 해!??!!");
                return;
            }

            List<UserInfo> memberList = m_dataBaseController.GetGroupMembers(groupInfo.GroupNo);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Members of group {groupInfo.GroupName}:");
            foreach (var member in memberList)
            {
                Console.WriteLine($"\t{member.Nickname}");
                sb.AppendLine($"\t{member.Nickname}");
            }

            GroupMessage responseMessage = new GroupMessage(GroupType.PostMemberList, message.Sender, message.GroupName, string.Empty, sb.ToString());
            Message response = m_messageManager.MakeMessage(MessageType.Group, responseMessage);
            serverMessage.Token.SendMessage(response);
        }

        private void ProcessGetGroupList(LappedMessage serverMessage)
        {
            GroupMessage? message = serverMessage.Message.Payload as GroupMessage;
            UserInfo? userInfo = m_dataBaseController.GetUserInfo(message.Sender);

            if (userInfo == null)
            {
                Console.WriteLine($"{userInfo.Nickname} dont valid name");
                return;
            }

            List<GroupInfo> groupList = m_dataBaseController.GetGroupList(userInfo.UserNo);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Groups joined by {userInfo.Nickname}:");
            foreach (var group in groupList)
            {
                Console.WriteLine($"\t{group.GroupName} Joined Data: {group.CreatedAt}");
                sb.AppendLine($"\t{group.GroupName} Joined Data: {group.CreatedAt}");
            }

            GroupMessage responseMessage = new GroupMessage(GroupType.PostGroupList, userInfo.Nickname, string.Empty, string.Empty, sb.ToString());
            Message response = m_messageManager.MakeMessage(MessageType.Group, responseMessage);
            serverMessage.Token.SendMessage(response);
        }

        private void ProcessFriend(LappedMessage serverMessage)
        {
            FriendMessage? message = serverMessage.Message.Payload as FriendMessage;
            Console.WriteLine(serverMessage.Message.Payload);
            switch (message!.Type)
            {
                case FriendMessageType.Request:
                    ProcessFriendRequest(serverMessage);
                    break;

                case FriendMessageType.Accept:
                    ProcessFriendAccept(serverMessage);
                    break;

                case FriendMessageType.Reject:
                    ProcessFriendReject(serverMessage);
                    break;

                case FriendMessageType.Remove:
                    ProcessFriendRemove(serverMessage);
                    break;

                case FriendMessageType.Block:
                    ProcessFriendBlcok(serverMessage);
                    break;

                case FriendMessageType.Unblock:
                    ProcessFriendUnblock(serverMessage);
                    break;

                case FriendMessageType.GetFriendList:
                    ProcessGetFriendList(serverMessage);
                    break;

                case FriendMessageType.GetFriendRequestList:
                    ProcessGetFriendRequestList(serverMessage);
                    break;
            }
        }

        private bool GetUserInfo(LappedMessage serverMessage, out UserInfo? sender, out UserInfo? target)
        {
            FriendMessage? message = serverMessage.Message.Payload as FriendMessage;
            if (message.Sender == message.Target)
            {
                sender = null;
                target = null;
                Console.WriteLine($"FriendInfo request failed: Sender and Target are the same ({message.Sender}).");

                return false;
            }

            sender = m_dataBaseController.GetUserInfo(message.Sender);
            target = m_dataBaseController.GetUserInfo(message.Target);
            if (sender == null || target == null)
            {
                Console.WriteLine($"FriendInfo request failed: {message.Sender} or {message.Target} not found.");
                return false;
            }

            return true;
        }

        private void ProcessFriendRequest(LappedMessage serverMessage)
        {
            if (!GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target))
            {
                return;
            }

            FriendInfo? friendInfo = m_dataBaseController.GetFriendInfo(Sender.UserNo, Target.UserNo);
            FriendInfo? friendInfoReverse = m_dataBaseController.GetFriendInfo(Target.UserNo, Sender.UserNo);

            if (friendInfoReverse != null)
            {
                if (friendInfoReverse.Status == FriendStatus.Pending)
                {
                    // 상대방이 친구 요청을 보낸 상태, 수락 처리
                    m_dataBaseController.AcceptFriend(Sender.UserNo, Target.UserNo);
                    m_dataBaseController.AcceptFriend(Target.UserNo, Sender.UserNo);
                    Console.WriteLine($"FriendInfo request from {Sender.Nickname} to {Target.Nickname} accepted");
                    return;
                }
                else
                {
                    return;
                }
            }

            if (friendInfo != null && (friendInfo.Status == FriendStatus.Accepted || friendInfo.Status == FriendStatus.Pending))
            {
                // 이미 친구이거나 친구 요청을 보낸 상태
                return;
            }

            m_dataBaseController.RequestFriend(Sender.UserNo, Target.UserNo);
            Console.WriteLine($"FriendInfo request from {Sender.Nickname} to {Target.Nickname} processed");
        }

        private void ProcessFriendAccept(LappedMessage serverMessage)
        {
            if (!GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target))
            {
                return;
            }

            // 요청자에게 친구 요청 정보가 있는지 확인
            FriendInfo? friendInfo = m_dataBaseController.GetFriendInfo(Target.UserNo, Sender.UserNo);
            if (friendInfo != null && friendInfo.Status == FriendStatus.Pending)
            {
                // 친구 요청 수락
                m_dataBaseController.AcceptFriend(Target.UserNo, Sender.UserNo);
                m_dataBaseController.AcceptFriend(Sender.UserNo, Target.UserNo);
                Console.WriteLine($"FriendInfo request from {Sender.Nickname} to {Target.Nickname} accepted");
            }
            else
            {
                Console.WriteLine($"FriendInfo accept failed: {Sender.Nickname} or {Target.Nickname} not found.");
            }
        }

        private void ProcessFriendReject(LappedMessage serverMessage)
        {
            if (!GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target))
            {
                return;
            }

            FriendInfo friendInfo = m_dataBaseController.GetFriendInfo(Target.UserNo, Sender.UserNo);
            if (friendInfo != null && friendInfo.Status == FriendStatus.Pending)
            {
                // 친구 요청 거절
                m_dataBaseController.RemoveFriendInfo(Target.UserNo, Sender.UserNo);
                Console.WriteLine($"FriendInfo request from {Target.Nickname} to {Sender.Nickname} rejected");
            }
            else
            {
                Console.WriteLine($"FriendInfo reject failed: {Target.Nickname} to {Sender.Nickname} not found.");
            }
        }
        private void ProcessFriendRemove(LappedMessage serverMessage)
        {
            GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target);
            if (Sender == null || Target == null)
            {
                return;
            }

            FriendInfo? friendInfo = m_dataBaseController.GetFriendInfo(Sender.UserNo, Target.UserNo);
            if (friendInfo != null && friendInfo.Status == FriendStatus.Accepted)
            {
                // 친구 관계 해제
                m_dataBaseController.RemoveFriendInfo(Sender.UserNo, Target.UserNo);
                m_dataBaseController.RemoveFriendInfo(Target.UserNo, Sender.UserNo);
                Console.WriteLine($"FriendInfo between {Sender.Nickname} and {Target.Nickname} removed");
            }
            else
            {
                Console.WriteLine($"FriendInfo remove failed: {Sender.Nickname} and {Target.Nickname} not found.");
            }
        }

        private void ProcessFriendBlcok(LappedMessage serverMessage)
        {
            GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target);
            if (Sender == null || Target == null)
            {
                return;
            }

            m_dataBaseController.BlockFriend(Sender.UserNo, Target.UserNo);
            Console.WriteLine($"FriendInfo blocked: {Sender.Nickname} blocked {Target.Nickname}");

            FriendInfo? friendInfo = m_dataBaseController.GetFriendInfo(Target.UserNo, Sender.UserNo);
            if (friendInfo != null && friendInfo.Status == FriendStatus.Accepted)
            {
                m_dataBaseController.RemoveFriendInfo(Target.UserNo, Sender.UserNo);
                Console.WriteLine($"FriendInfo removed due to block: {Target.Nickname} to {Sender.Nickname}");
            }
        }

        private void ProcessFriendUnblock(LappedMessage serverMessage)
        {
            GetUserInfo(serverMessage, out UserInfo? Sender, out UserInfo? Target);
            if (Sender == null || Target == null)
            {
                return;
            }

            FriendInfo? friendInfo = m_dataBaseController.GetFriendInfo(Sender.UserNo, Target.UserNo);
            if (friendInfo != null && friendInfo.Status == FriendStatus.Blocked)
            {
                // 관계 삭제
                m_dataBaseController.RemoveFriendInfo(Sender.UserNo, Target.UserNo);
                Console.WriteLine($"FriendInfo unblocked: {Sender.Nickname} unblocked {Target.Nickname}");
            }
            else
            {
                Console.WriteLine($"FriendInfo unblock failed: {Sender.Nickname} and {Target.Nickname} not found.");
            }
        }

        private void ProcessGetFriendList(LappedMessage serverMessage)
        {
            FriendMessage? message = serverMessage.Message.Payload as FriendMessage;
            UserInfo? user = m_dataBaseController.GetUserInfo(message!.Sender);
            if (user == null)
            {
                Console.WriteLine($"GetFriendList failed: user {message.Sender} not found.");
                return;
            }

            List<UserInfo> friendList = m_dataBaseController.GetFriendList(user.UserNo);
            StringBuilder sb = new StringBuilder();
            foreach (var friend in friendList)
            {
                Console.WriteLine($"Friend: {friend.Nickname} LastLogin: {friend.LastLogin}");
                sb.AppendLine($"{friend.Nickname} Status: {friend.Status} LastLogin: {friend.LastLogin}");
            }

            FriendMessage responseMessage = new FriendMessage(FriendMessageType.SendFriendList, user.Nickname, string.Empty, friendList.Count, sb.ToString());
            Message response = m_messageManager.MakeMessage(MessageType.Friend, responseMessage);
            serverMessage.Token.SendMessage(response);
        }

        private void ProcessGetFriendRequestList(LappedMessage serverMessage)
        {
            FriendMessage? message = serverMessage.Message.Payload as FriendMessage;
            UserInfo? user = m_dataBaseController.GetUserInfo(message!.Sender);
            if (user == null)
            {
                Console.WriteLine($"GetFriendList failed: user {message.Sender} not found.");
                return;
            }

            List<UserInfo> friendList = m_dataBaseController.GetFriendRequests(user.UserNo);
            StringBuilder sb = new StringBuilder();
            foreach (var friend in friendList)
            {
                Console.WriteLine($"Friend: {friend.Nickname} LastLogin: {friend.LastLogin}");
                sb.AppendLine($"{friend.Nickname} LastLogin: {friend.LastLogin}");
            }

            FriendMessage responseMessage = new FriendMessage(FriendMessageType.SendFriendRequestList, user.Nickname, string.Empty, friendList.Count, sb.ToString());
            Message response = m_messageManager.MakeMessage(MessageType.Friend, responseMessage);
            serverMessage.Token.SendMessage(response);
        }

        private async Task ProcessLogin(LappedMessage serverMessage)
        {
            LoginMessage? message = serverMessage.Message.Payload as LoginMessage;
            string? JWT = message!.Token;

            var userInfo = await GetUserInfoFromWeb(JWT);
            if (userInfo != null)
            {
                if (IsAlreadyConnected(userInfo.Nickname))
                {
                    RejectLogin(serverMessage.Token, "Already Connected");
                    return;
                }
                AcceptLogin(userInfo.UserId, userInfo.Nickname, serverMessage.Token);
            }
            else
            {
                RejectLogin(serverMessage.Token, "Wrong Login Info");
            }
        }

        private async Task<UserInfoResponse?> GetUserInfoFromWeb(string jwt)
        {
            m_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
	    Console.WriteLine(1);
            var userInfoResponse = await m_httpClient.GetAsync("http://localhost:7242/api/AccountAPI/userinfo");
	    Console.WriteLine(2);
            if (userInfoResponse.IsSuccessStatusCode)
            {
                return await userInfoResponse.Content.ReadFromJsonAsync<UserInfoResponse>() ?? null;
            }
            else
            {
                var error = await userInfoResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"사용자 정보 요청 실패: {error}");
                return null;
            }
        }

        private bool IsAlreadyConnected(string nickName)
        {
            return m_connectedTokens.ContainsKey(nickName.ToLower().Trim());
        }

        private void RejectLogin(SocketToken token, string errorMessage)
        {
            Message rejectMsg = m_messageManager.MakeMessage(
                MessageType.Login,
                new LoginMessage(LoginType.Reject, errorMessage)
            );
            token.SendMessage(rejectMsg);
        }

        private void AcceptLogin(string id, string nickName, SocketToken token)
        {
            UserInfo? info = m_dataBaseController.GetUserInfo(nickName);
            if (info == null)
            {
                m_dataBaseController.InsertUserInfo(id, nickName);
                Console.WriteLine($"New user registered: {nickName}");
                info = m_dataBaseController.GetUserInfo(nickName);
            }
            token.User = info;

            m_connectedTokens[info.Nickname.ToLower().Trim()] = token;
            Console.WriteLine($"{info.Nickname} connected");

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
            ChattingMessage? m = message.Payload as ChattingMessage;
            UserInfo info = m_dataBaseController.GetUserInfo(m.SenderName);
            m.SenderNo = info.UserNo;
            m.TargetNo = -1; 
            m_dataBaseController.InsertChattingMessage(m);

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
            ChattingMessage? m = message.Payload as ChattingMessage;
            UserInfo info = m_dataBaseController.GetUserInfo(m.SenderName);
            m.SenderNo = info.UserNo;
            m.TargetNo = m_dataBaseController.GetUserInfo(targetNickname).UserNo;
            m_dataBaseController.InsertChattingMessage(m);

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
            if (token.User != null)
            {
                string key = token.User.Nickname.ToLower().Trim();
                if (m_connectedTokens.ContainsKey(key))
                {
                    m_connectedTokens.Remove(key);
                    Console.WriteLine($"User {token.User.Nickname} disconnected.");
                    m_dataBaseController.UpdateLastLogin(token.User.UserNo);
                }
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
