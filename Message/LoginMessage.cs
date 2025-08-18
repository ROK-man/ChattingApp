namespace MessageLib
{
    public enum LoginType : byte
    {
        None = 0,
        TryLogin = 1,
        Success = 2,
        Reject = 3,
    }
    public class LoginMessage : MessagePayloadBase
    {
        public LoginType Type { get; set; }
        public string Token { get; set; }

        public LoginMessage()
        {
            Type = LoginType.None;
            Token = string.Empty;
        }
        public LoginMessage(string token)
        {
            Type = LoginType.TryLogin;
            Token = token;
        }

        public LoginMessage(LoginType type)
        {
            Type = type;
            Token = string.Empty;
        }

        public LoginMessage(LoginType type, string token)
        {
            Type = type;
            Token = token;
        }
        public override void Serialize(byte[] buffer, int offset)
        {
            buffer[offset++] = (byte)Type;

            var tokenBytes = System.Text.Encoding.UTF8.GetBytes(Token);
            Buffer.BlockCopy(tokenBytes, 0, buffer, offset, tokenBytes.Length);
        }
        public override void Deserialize(byte[] payloadData, int offset, int length)
        {
            int readLength = 0;
            Type = (LoginType)payloadData[offset++];
            readLength++;

            Token = System.Text.Encoding.UTF8.GetString(payloadData, offset, length - readLength);
        }

        public override int GetLength()
        {
            return 1 + System.Text.Encoding.UTF8.GetByteCount(Token);
        }

        public override string ToString()
        {
            return Token;
        }
    }
}
