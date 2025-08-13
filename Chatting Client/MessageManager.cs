using System.Reflection.PortableExecutable;
using System.Text;

namespace Chatting_Client
{
    internal class MessageManager
    {
        public const int HEADER_SIZE = 14;
        public const int MAX_PAYLOAD_SIZE = 2048;
        public MessageHeader? Header { get; set; }
        public MessagePayload? Payload { get; set; }

        public MessageManager()
        {
            Header = new();
            Payload = default;
        }

        public MessageManager(MessageType type, byte flag, MessagePayload payload)
        {
            Payload = payload;
            Header = new(Payload.GetLength(), type, flag);
        }

        public int GetBytes(byte[] buffer, int offset, int bufferLength)
        {
            if(bufferLength < Payload!.GetLength() + HEADER_SIZE)
            {
                return 0;
            }

            Header!.GetBytes(buffer, offset);
            Payload.GetBytes(buffer, offset);

            return HEADER_SIZE + Payload.GetLength();
        }

        public static MessageManager MakeChattingMessage(string payload)
        {
            ChattingMessage message = new(payload);
            return PackagePayload(MessageType.Chatting, (byte)Chatting.All, message);
        }

        private static MessageManager PackagePayload(MessageType type, byte flag, MessagePayload payload)
        {
            return new MessageManager(type, flag, payload);
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

        public void GetBytes(byte[] buffer, int offset)
        {
            BitConverter.GetBytes(Length).CopyTo(buffer, offset);
            buffer[4] = (byte)Type;
            buffer[5] = (byte)Flag;
            BitConverter.GetBytes(UnixTimeMilli).CopyTo(buffer, offset + 6);
        }
    }

    internal abstract class MessagePayload
    {
        public override abstract string ToString();
        public abstract void SetPayload(byte[] payloadData, int offset, int length);

        public abstract int GetLength();

        public abstract void GetBytes(byte[] buffer, int offset);
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

        public override void GetBytes(byte[] buffer, int offset)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(Payload);
            int payloadLength = Math.Min(payloadBytes.Length, MessageManager.MAX_PAYLOAD_SIZE);
            Array.Copy(payloadBytes, 0, buffer, offset + 14, payloadLength);
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
        public override void GetBytes(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }
    }
}
