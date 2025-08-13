namespace MessageLib
{
    public class Message
    {
        public MessageHeader? Header { get; set; }
        public MessagePayload? Payload { get; set; }

        public Message()
        {
            Header = new();
            Payload = default;
        }

        public Message(MessageHeader header, MessagePayload payload)
        {
            Header = header;
            Payload = payload;
        }
    }
}
