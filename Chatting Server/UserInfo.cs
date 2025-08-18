namespace Chatting_Server
{
    internal class UserInfo
    {
        public long UserNo { get; set; }
        public string UserID { get; set; }
        public string Nickname { get; set; }
        public bool Status { get; set; } // true: online, false: offline
        public DateTime? LastLogin { get; set; }
        public bool Banned { get; set; } // true: banned, false: not banned
        public DateTime RegisterDate { get; set; }

        public UserInfo()
        {
            UserNo = 0;
            UserID = string.Empty;
            Nickname = string.Empty;
            Status = false;
            LastLogin = DateTime.MinValue;
            Banned = false;
            RegisterDate = DateTime.MinValue;
        }

        public UserInfo(string name)
        {
            Nickname = name;
        }

        public override string ToString()
        {
            return $"UserNo: {UserNo}, UserID: {UserID}, Nickname: {Nickname}, Status: {(Status ? "Online" : "Offline")}, LastLogin: {LastLogin}, Banned: {(Banned ? "Yes" : "No")}, RegisterDate: {RegisterDate}";
        }
    }
}
