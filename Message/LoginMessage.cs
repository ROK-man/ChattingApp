namespace MessageLib
{
    public class LoginMessage : MessagePayloadBase
    {
        public string Token { get; set; }

        public LoginMessage()
        {
            Token = string.Empty;
        }
        public LoginMessage(string token)
        {
            Token = token;
        }
        public override void Serialize(byte[] buffer, int offset)
        {
            var tokenBytes = System.Text.Encoding.UTF8.GetBytes(Token);
            Buffer.BlockCopy(tokenBytes, 0, buffer, offset, tokenBytes.Length);
        }
        public override void Deserialize(byte[] payloadData, int offset, int length)
        {
            Token = System.Text.Encoding.UTF8.GetString(payloadData, offset, length);
        }

        public override int GetLength()
        {
            return System.Text.Encoding.UTF8.GetByteCount(Token);
        }

        public override string ToString()
        {
            return Token;
        }
    }
}
