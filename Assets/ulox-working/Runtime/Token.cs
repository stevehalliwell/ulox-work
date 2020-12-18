namespace ULox
{
    public class TokenException : LoxException
    {
        public TokenException(Token token, string msg)
            : base(token.TokenType,token.Line,token.Character,msg)
        { }
    }

    public class LoxException : System.Exception
    {
        public LoxException(TokenType tokenType, int line, int character, string msg)
            : base($"{tokenType}|{line}:{character} {msg}")
        { }
    }
    public readonly struct Token
    {
        public readonly TokenType TokenType;
        public readonly string Lexeme;
        public readonly object Literal;
        public readonly int Line;
        public readonly int Character;

        public Token(TokenType tokenType,
                     string lexeme,
                     object literal,
                     int line,
                     int character)
        {
            this.TokenType = tokenType;
            this.Lexeme = lexeme;
            this.Literal = literal;
            this.Line = line;
            this.Character = character;
        }

        public override string ToString()
        {
            return $"{Line}:{Character} - {TokenType} {Lexeme}";
        }
    }
}