namespace MessageLib
{
    public abstract class MessagePayloadBase
    {
        public abstract void Serialize(byte[] buffer, int offset);
        public abstract void Deserialize(byte[] payloadData, int offset, int length);
        public abstract int GetLength();
        public override abstract string ToString();

    }
}
