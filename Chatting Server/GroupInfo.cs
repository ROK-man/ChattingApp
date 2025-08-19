namespace Chatting_Server
{
    internal class GroupInfo
    {
        public long GroupNo { get; set; } // GroupType number
        public string GroupName { get; set; } // GroupType name
        public string Password { get; set; } // GroupType password (if any) 평문 저장, null(empty) able, 변경 불가
        public DateTime CreatedAt { get; set; } // Date when the group was created
        public GroupInfo()
        {
            GroupNo = 0;
            GroupName = string.Empty;
            Password = string.Empty;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
