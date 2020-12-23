namespace ULox
{
    public class LoxException : System.Exception
    {
        public LoxException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}")
        { }
    }

    public class TokenException : LoxException
    {
        public TokenException(Token token, string msg)
            : base(token.TokenType, token.Line, token.Character, msg)
        { }
    }

    public class ScannerException : LoxException
    {
        public ScannerException(TokenType tokenType, int line, int character, string msg)
            : base(tokenType, line, character, msg) { }
    }

    public class InstanceException : TokenException
    {
        public InstanceException(Token name, string msg) : base(name, msg)
        {
        }
    }

    public class EnvironmentException : TokenException
    {
        public EnvironmentException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class ResolverException : TokenException
    {
        public ResolverException(Token token, string msg) : base(token, msg)
        {
        }
    }

    public class ParseException : TokenException
    {
        public ParseException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class RuntimeTypeException : TokenException
    {
        public RuntimeTypeException(Token token, string msg)
            : base(token, msg)
        { }
    }

    public class RuntimeCallException : RuntimeTypeException
    {
        public RuntimeCallException(Token token, string msg) : base(token, msg)
        {
        }
    }

    public class RuntimeAccessException : RuntimeTypeException
    {
        public RuntimeAccessException(Token token, string msg) : base(token, msg)
        {
        }
    }
}