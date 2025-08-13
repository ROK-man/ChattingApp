using System.Reflection.PortableExecutable;
using System.Text;

namespace Chatting_Server
{
    internal class Message
    {
        public const int HEADER_SIZE = 14;
        public const int MAX_PAYLOAD_SIZE = 2048;
        public MessageHeader? Header { get; set; }
        public MessagePayload? Payload { get; set; }

        public Message()
        {
            Header = new();
            Payload = default;
        }

        public Message(MessageType type, byte flag, MessagePayload payload)
        {
            Payload = payload;
            Header = new(Payload.GetLength(), type, flag);
        }


        public Message MakeChattingMessage(string payload)
        {
            ChattingMessage message = new(payload);
            PackagePayload(MessageType.Chatting, (byte)Chatting.All, message);

            return new Message();
        }

        private Message PackagePayload(MessageType type, byte flag, MessagePayload payload)
        {
            return new Message(type, flag, payload);
        }

        public void SetHeader(byte[] headerData)
        {
            Header!.SetHeader(headerData, 0);
            ProcessHeader();
        }

        public void SetPayload(byte[] payloadData)
        {
            Payload!.SetPayload(payloadData, 0, Header!.Length);
        }

        private void ProcessHeader()
        {
            switch ((MessageType)Header!.Type)
            {
                case MessageType.System:
                    Payload = new SystemMessage();
                    break;
                case MessageType.Chatting:
                    Payload = new ChattingMessage();
                    break;
            }
        }
    }
    internal enum MessageType : byte
    {
        System = 1,
        Chatting = 2,
    }

    internal enum Chatting : byte
    {
        All = 1,
        Whisper = 2,
    }

    internal class MessageHeader
    {
        public int Length { get; set; } // 4 bytes
        public MessageType Type { get; set; } // 1 byte
        public byte Flag { get; set; } // 1 byte
        public long UnixTimeMilli { get; set; } // 8 bytes 

        public MessageHeader()
        {
            Length = 0;
            Type = 0;
            Flag = 0;
            UnixTimeMilli = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public MessageHeader(int length, MessageType type, byte flag)
        {
            Length = length;
            Type = type;
            Flag = flag;
            UnixTimeMilli = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void SetHeader(byte[] headerData, int offset)
        {
            Length = BitConverter.ToInt32(headerData, offset);
            Type = (MessageType)headerData[offset + 4];
            Flag = headerData[offset + 5];
            UnixTimeMilli = BitConverter.ToInt64(headerData, offset + 6);
        }
    }

    internal abstract class MessagePayload
    {
        public override abstract string ToString();
        public abstract void SetPayload(byte[] payloadData, int offset, int length);

        public abstract int GetLength();
    }

    internal class ChattingMessage : MessagePayload
    {
        string Payload { get; set; }

        public ChattingMessage()
        {
            Payload = string.Empty;
        }
        public ChattingMessage(string payload)
        {
            Payload = payload;
        }

        public override void SetPayload(byte[] payloadData, int offset, int length)
        {
            Payload = Encoding.UTF8.GetString(payloadData, 0, length);
        }

        public override string ToString()
        {
            return Payload;
        }

        public override int GetLength()
        {
            return Encoding.UTF8.GetByteCount(Payload);
        }
    }

    internal class SystemMessage : MessagePayload
    {
        string Payload { get; set; }

        public SystemMessage()
        {

        }
        public override void SetPayload(byte[] payloadData, int offset, int length)
        {
            Payload = Encoding.UTF8.GetString(payloadData, 0, length);
        }

        public override string ToString()
        {
            return Payload;
        }
        public override int GetLength()
        {
            return Encoding.UTF8.GetByteCount(Payload);
        }
    }
}
