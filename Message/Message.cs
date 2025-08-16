using System.Net.Sockets;
using System.Text;

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

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Header: {Header}");
            sb.AppendLine($"Payload: {Payload}");
            return sb.ToString();
        }
        public void SetLength()
        {
            if (Header != null && Payload != null)
            {
                Header.Length = Payload.GetLength();
            }
        }
    }
}
