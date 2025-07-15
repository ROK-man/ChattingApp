using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Message
    {
        public enum MessageType
        {
            Text,
            Video,
            Image,
        }
        public enum To
        {
            All,
        }
        public MessageType type;
        public To to;
        public string name;
        public DateTime timestamp;
        public long unixTime;
        public int length;
        public string payload;
    }
}
