using System.Net.Sockets;

namespace MessageLib
{
    public class Message
    {
        public MessageHeader? Header { get; set; }
        public MessagePayloadBase? Payload { get; set; }
        public SocketAsyncEventArgs? SocketArgs { get; set; }

        public Message()
        {
            Header = new();
            Payload = default;
        }

        public Message(MessageHeader header, MessagePayloadBase payload)
        {
            Header = header;
            Payload = payload;
        }

        public int GetByteLength()
        {
            return MessageManager.HEADER_SIZE + Payload.GetLength();
        }

        public void Serialize(byte[] buffer, int offset)
        {
            Header.Serialize(buffer, offset);
            Payload.Serialize(buffer, offset + MessageManager.HEADER_SIZE);
        }
    }
}
