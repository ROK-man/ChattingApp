using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public enum ChattingType : byte
    {
        All = 1,
        Whisper = 2,
    }

    public class ChattingMessage : MessagePayloadBase
    {
        public ChattingType Type { get; set; }
        public string Payload { get; set; }

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
            Type = (ChattingType)payloadData[0];
            Payload = Encoding.UTF8.GetString(payloadData, offset + 1, length - 1);
        }

        public override string ToString()
        {
            return Payload;
        }

        public override int GetLength()
        {
            int length = Encoding.UTF8.GetByteCount(Payload);
            length += 1; // type

            return length;
        }

        public override void Serialize(byte[] buffer, int offset)
        {
            buffer[offset + 1] = (byte)Type;

            byte[] payloadBytes = Encoding.UTF8.GetBytes(Payload);
            int payloadLength = Math.Min(payloadBytes.Length, MessageManager.MAX_PAYLOAD_SIZE);
            Array.Copy(payloadBytes, 0, buffer, offset + 1, payloadLength);
        }
    }
}
