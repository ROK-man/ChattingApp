using System.Text;

namespace MessageLib
{
    public enum GroupType
    {
        Create = 1,
        Join = 2,
        Quit = 3,
        GetMemberList = 4,
        GetGroupList = 5,
        PostMemberList = 6,
        PostGroupList = 7,
    }
    public class GroupMessage : MessagePayloadBase
    {
        public GroupType Type { get; set; } // required, 1 byte
        public bool HavePassword { get; set; }   // 1 byte, true if group has password
        public string Sender { get; set; } // 15 bytes, sender's name  required
        public string GroupName { get; set; } // 15 bytes 
        public string Password { get; set; } // 15 bytes
        public string Content { get; set; } // custom

        public GroupMessage()
        {
            Type = GroupType.Create;
            Sender = string.Empty;
            HavePassword = false;
            GroupName = string.Empty;
            Password = string.Empty;
            Content = string.Empty;
        }

        public GroupMessage(GroupType type, string sender, string groupName, string groupPassword, string content)
        {
            Type = type;
            Sender = sender;
            GroupName = groupName;
            Password = groupPassword;
            Content = content;
            HavePassword = !string.IsNullOrEmpty(groupPassword);
        }


        public override int GetLength()
        {
            int length = 1; // 1 byte for Type
            if (Type == GroupType.PostMemberList || Type == GroupType.PostGroupList)
            {
                length += Encoding.UTF8.GetByteCount(Content);
                return length;
            }

            length += 15; // 15 bytes for Sender
            if (Type != GroupType.GetGroupList)
            {
                length += 15; // 15 bytes for GroupName
                length += 1; // 1 byte for HavePassword
                if (HavePassword)
                {
                    length += 15; // 15 bytes for Password
                }
            }
            return length;
        }
        public override void Deserialize(byte[] payloadData, int offset, int length)
        {
            for(int i=0; i<length; i++)
            {
                Console.Write($"{payloadData[offset + i]} ");
            }
            Type = (GroupType)payloadData[offset++];
            int readLength = 1;
            Console.WriteLine(0);

            if (Type == GroupType.PostMemberList || Type == GroupType.PostGroupList)
            {
                Content = Encoding.UTF8.GetString(payloadData, offset, length - readLength);
                Console.WriteLine(this);
                return;
            }

            Sender = Encoding.UTF8.GetString(payloadData, offset, 15).Trim().TrimEnd('\0').ToLower();
            offset += 15;
            readLength += 15;

            if (Type != GroupType.GetGroupList)
            {
                GroupName = Encoding.UTF8.GetString(payloadData, offset, 15).Trim().TrimEnd('\0');
                offset += 15;
                readLength += 15;

                HavePassword = (payloadData[offset++] == 1); // 1 byte for HavePassword
                readLength++;
                Console.WriteLine(1);
                if (HavePassword)
                {
                    Console.WriteLine(2);

                    Password = Encoding.UTF8.GetString(payloadData, offset, 15).Trim().TrimEnd('\0');
                    offset += 15;
                    readLength += 15;
                }
                else
                {
                    Password = string.Empty;
                }
            }
            else
            {
                GroupName = string.Empty;
            }
            Console.WriteLine(this);
        }

        public override void Serialize(byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)Type;
            if (Type == GroupType.PostMemberList || Type == GroupType.PostGroupList)
            {
                var b = Encoding.UTF8.GetBytes(Content);
                Buffer.BlockCopy(b, 0, buffer, offset, b.Length);
                offset += b.Length;
                return;
            }

            var sender = Encoding.UTF8.GetBytes(Sender.PadRight(15, '\0'));
            Buffer.BlockCopy(sender, 0, buffer, offset, 15);
            offset += 15;

            if (Type != GroupType.GetGroupList)
            {
                var groupName = Encoding.UTF8.GetBytes(GroupName.PadRight(15, '\0'));
                Buffer.BlockCopy(groupName, 0, buffer, offset, 15);
                offset += 15;
            }

            if (HavePassword)
            {
                buffer[offset++] = 1; // indicates that the group has a password
                var groupPassword = Encoding.UTF8.GetBytes(Password.PadRight(15, '\0'));
                Buffer.BlockCopy(groupPassword, 0, buffer, offset, 15);
                offset += 15;
            }
            else
            {
                if(Type != GroupType.GetGroupList)
                {
                    buffer[offset++] = 0; // indicates that the group does not have a password
                }
            }
        }

        public override string ToString()
        {
            return $"GroupMessage: Type={Type}, Sender={Sender}, GroupName={GroupName}, HavePassword={HavePassword}, Password={Password}, Content={Content}";
        }
    }
}
