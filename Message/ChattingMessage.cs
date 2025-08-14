using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public enum ChattingType : byte
    {
        None = 0,
        All = 1,
        Whisper = 2,
    }

    public class ChattingMessage : MessagePayloadBase
    {
        public ChattingType Type { get; set; }
        public string Payload { get; set; }

        public string TargetName { get; set; }  // 15 bytes or 0 

        public ChattingMessage()
        {
            Type = ChattingType.None;
            Payload = string.Empty;
            TargetName = string.Empty;
        }
        public ChattingMessage(ChattingType type, string targetName, string payload)
        {
            Type = type;
            TargetName = targetName;
            Payload = payload;
        }

        public override void Deserialize(byte[] payloadData, int offset, int length)
        {
            int readLength = 0;
            Type = (ChattingType)payloadData[offset++];
            readLength++;

            if (Type == ChattingType.Whisper)
            {
                TargetName = Encoding.UTF8.GetString(payloadData, offset, 15);
                offset += 15;
                readLength += 15;
            }
            else
            {
                TargetName = string.Empty;
            }
            Payload = Encoding.UTF8.GetString(payloadData, offset, length - readLength);
        }

        public override string ToString()
        {
            return Payload;
        }

        public override int GetLength()
        {
            int length = Encoding.UTF8.GetByteCount(Payload);
            length += 1; // type
            length += TargetName.Equals(string.Empty) ? 0 : 15;

            return length;
        }

        public override void Serialize(byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)Type;

            if(!TargetName.Equals(string.Empty))
            {
                byte[] targetNameBytes = Encoding.UTF8.GetBytes(TargetName);
                Array.Copy(targetNameBytes, 0, buffer, offset, Math.Min(targetNameBytes.Length, 15));
                offset += 15;
            }

            byte[] payloadBytes = Encoding.UTF8.GetBytes(Payload);
            Array.Copy(payloadBytes, 0, buffer, offset, payloadBytes.Length);
            offset += payloadBytes.Length;
        }
    }
}
