namespace Chatting_Server
{
    internal class UserInfo
    {
        public string UserName { get; set; }

        public UserInfo()
        {
            UserName = string.Empty;
        }

        public UserInfo(string name)
        {
            UserName = name;
        }   
    }
}
