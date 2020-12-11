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

        ASSIGN,
        BANG,
        BANG_EQUAL,
        EQUALITY,
        GREATER,
        GREATER_EQUAL,
        LESS,
        LESS_EQUAL,

        IDENT,
        VAR,
        STRING,
        INT,
        FLOAT,

        FUNCTION,
        CLASS,

        AND,
        OR,
        IF,
        ELSE_IF,
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

        PRINT,

        EOF,        
    }
}