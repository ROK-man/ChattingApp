using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public class SystemMessage : MessagePayload
    {
        string Payload { get; set; }

        public SystemMessage()
        {
            Payload = string.Empty;
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
