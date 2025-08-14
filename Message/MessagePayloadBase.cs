namespace MessageLib
{
    public abstract class MessagePayloadBase
    {
        public override abstract string ToString();
        public abstract void Deserialize(byte[] payloadData, int offset, int length);

        public abstract int GetLength();

        public abstract void Serialize(byte[] buffer, int offset);
    }
}
