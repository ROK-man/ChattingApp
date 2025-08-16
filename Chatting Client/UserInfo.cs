using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Client
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
