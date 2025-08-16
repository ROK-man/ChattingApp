namespace MessageLib
{
    public enum LoginType : byte
    {
        None = 0,
        Login = 1,
        Success = 2,
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
            Type = LoginType.Login;
            Token = token;
        }

        public LoginMessage(LoginType type, string token)
        {
            Type = type;
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
