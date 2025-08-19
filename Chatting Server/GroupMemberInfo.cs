using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatting_Server
{
    internal class GroupMemberInfo
    {
        public long GroupNo { get; set; } // GroupType number
        public long UserNo { get; set; } // User number
        public DateTime JoinedAt { get; set; } // Date when the user joined the group
        public GroupMemberInfo()
        {
            GroupNo = 0;
            UserNo = 0;
            JoinedAt = DateTime.UtcNow;
        }
    }
}
