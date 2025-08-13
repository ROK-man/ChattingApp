using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public enum Chatting : byte
    {
        All = 1,
        Whisper = 2,
    }

    public class ChattingMessage : MessagePayload
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
}
