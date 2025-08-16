using System.Collections.Concurrent;

namespace MessageLib
{
    // Assemble Message receive and Make Message for send
    public class MessageManager
    {
        public const int HEADER_SIZE = 14;
        public const int MAX_PAYLOAD_SIZE = 2048;

        private Message message { get; set; }

        public int PayloadLength
        {
            get
            {
                if (message.Header == null || message.Payload == null)
                    return 0;
                return message.Header.Length;
            }
        }

        // Parse
        private int state;

        public MessageManager()
        {
            message = new();
        }

        public bool ParseData(byte[] data)
        {
            switch (state)
            {
                // header
                case 0:
                    message.Header!.SetHeader(data, 0);
                    state = 1;
                    ProcessHeader();
                    return false;

                // payload
                case 1:
                    message.Payload!.Deserialize(data, 0, message.Header!.Length);
                    state = 0;
                    return true;
            }

            return false;
        }

        private void ProcessHeader()
        {
            switch ((MessageType)message.Header!.Type)
            {
                case MessageType.System:
                    message.Payload = new SystemMessage();
                    break;
                case MessageType.Login:
                    message.Payload = new LoginMessage();
                    break;
                case MessageType.Chatting:
                    message.Payload = new ChattingMessage();
                    break;
            }
        }

        public Message GetMessage()
        {
            return new Message(new MessageHeader(message.Header!.Length, message.Header!.Type, message.Header.Flag), message.Payload!);
        }

        public Message MakeMessage(MessageType type, MessagePayloadBase payload)
        {
            return new Message(new MessageHeader(payload.GetLength(), type, 0), payload);
        }
    }
}
