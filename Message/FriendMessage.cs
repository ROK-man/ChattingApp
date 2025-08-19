
using System.Text;

namespace MessageLib
{
    public enum FriendMessageType : byte
    {
        None = 0,
        Request = 1,                // Friend request
        Accept = 2,                 // Accept friend request
        Reject = 3,                 // Reject friend request
        Remove = 4,                 // Remove friend
        Block = 5,                  // Block user
        Unblock = 6,                // Unblock user
        GetFriendList = 7,          // GetFriendList friend list
        GetFriendRequestList = 8,   // GetFriendRequestList friends
        SendFriendList = 9,         // SendFriendList friends
        SendFriendRequestList = 10, // SendFriendRequestList friends
    }
    public class FriendMessage : MessagePayloadBase
    {
        public FriendMessageType Type { get; set; } // 1 byte
        public string Sender { get; set; }          // 8 bytes
        public string Target { get; set; }          // 8 bytes
        public int Amount { get; set; }             // Number of friends or friend requests, if applicable
        public string Content { get; set; }         // List of friends or friend requests, if applicable
        public FriendMessage()
        {
            Type = FriendMessageType.None;
            Sender = string.Empty;
            Target = string.Empty;
            Amount = 0;
            Content = string.Empty;
        }
        public FriendMessage(FriendMessageType type, string sender, string target)
        {
            Type = type;
            Sender = sender;
            Target = target;
            Amount = 0;
            Content = string.Empty;
        }

        public FriendMessage(FriendMessageType type, string sender, string target, int amount, string content)
        {
            Type = type;
            Sender = sender;
            Target = target;
            Amount = amount;
            Content = content;
        }

        public override void Serialize(byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)Type;

            byte[] senderNameBytes = Encoding.UTF8.GetBytes(Sender.PadRight(15, '\0'));
            Buffer.BlockCopy(senderNameBytes, 0, buffer, offset, 15);
            offset += 15;

            if (Target != string.Empty)
            {
                byte[] targetNameBytes = Encoding.UTF8.GetBytes(Target.PadRight(15, '\0'));
                Buffer.BlockCopy(targetNameBytes, 0, buffer, offset, 15);
                offset += 15;
            }

            if(Type == FriendMessageType.SendFriendList || Type == FriendMessageType.SendFriendRequestList)
            {
                byte[] contentBytes = Encoding.UTF8.GetBytes(Content);
                Buffer.BlockCopy(contentBytes, 0, buffer, offset, contentBytes.Length);
                offset += contentBytes.Length;
            }
        }

        public override void Deserialize(byte[] payloadData, int offset, int length)
        {   
            Type = (FriendMessageType)payloadData[offset++];
            int readLength = 1;

            Sender = Encoding.UTF8.GetString(payloadData, offset, 15);
            offset += 15;
            Sender = Sender.Trim().TrimEnd('\0').ToLower();
            readLength += 15;

            if (!(Type == FriendMessageType.GetFriendList || Type == FriendMessageType.GetFriendRequestList 
                    || Type == FriendMessageType.SendFriendList || Type == FriendMessageType.SendFriendRequestList))
            {
                Target = Encoding.UTF8.GetString(payloadData, offset, 15);
                offset += 15;

                Target = Target.Trim().TrimEnd('\0').ToLower();
                readLength += 15;
            }

            if (Type == FriendMessageType.SendFriendList || Type == FriendMessageType.SendFriendRequestList)
            {
                Content = Encoding.UTF8.GetString(payloadData, offset, length - readLength);
                Content = Content.Trim().TrimEnd('\0');
            }
            else
            {
                Content = string.Empty;
            }
        }

        public override int GetLength()
        {
            int length = 1; // 1 byte for Type
            length += 15; // 15 bytes for UserNo

            if (Target != string.Empty)
            {
                length += 15; // 15 bytes for TargetNo
            }

            length += System.Text.Encoding.UTF8.GetByteCount(Content); // Length of Content 

            return length;
        }

        public override string ToString()
        {
            return $"Type: {Type}, Sender name: {Sender}, Target: {Target}\n{Content}";
        }
    }
}
