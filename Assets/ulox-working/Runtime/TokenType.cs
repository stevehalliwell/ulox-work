﻿namespace ULox
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

        PRINT,

        EOF,        
    }
}