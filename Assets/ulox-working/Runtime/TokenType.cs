namespace ULox
{
    public enum TokenType
    {
        OPEN_PAREN,
        CLOSE_PAREN,
        OPEN_BRACE,
        CLOSE_BRACE,
        COMMA,
        DOT,
        MINUS,
        PLUS,
        END_STATEMENT,
        SLASH,
        STAR,
        PERCENT,
        MINUS_EQUAL,
        PLUS_EQUAL,
        SLASH_EQUAL,
        STAR_EQUAL,
        PERCENT_EQUAL,
        INCREMENT,
        DECREMENT,

        ASSIGN,
        BANG,
        BANG_EQUAL,
        EQUALITY,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,
        QUESTION,
        COLON,

        IDENTIFIER,
        VAR,
        STRING,
        INT,
        FLOAT,

        FUNCTION,
        CLASS,

        AND,
        OR,
        IF,
        ELSE,
        WHILE,
        FOR,

        LOOP,
        RETURN,

        BREAK,
        CONTINUE,
        TRUE,
        FALSE,
        NULL,
        THIS,
        SUPER,
        GET,
        SET,
        GETSET,

        PRINT,

        EOF,

        NONE,
    }
}