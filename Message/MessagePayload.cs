using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageLib
{
    public abstract class MessagePayload
    {
        public override abstract string ToString();
        public abstract void SetPayload(byte[] payloadData, int offset, int length);

        public abstract int GetLength();

        public abstract void GetBytes(byte[] buffer, int offset);
    }
}
