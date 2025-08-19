namespace Chatting_Server
{
    enum FriendStatus
    {
        Pending,   // 친구 요청
        Accepted,  // 친구
        Blocked,   // 차단
    }

    internal class FriendInfo
    {
        public long UserNo { get; set; } // User number
        public long FriendNo { get; set; } // FriendInfo's user number
        public FriendStatus Status { get; set; } // FriendInfo status (e.g., Pending, Accepted, Blocked)
        public DateTime CreatedAt { get; set; } // Date when the friend relationship was created
    }
}
