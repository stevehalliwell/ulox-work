namespace ULox
{
    public enum OpCode : byte
    {
        NONE,
     
        CONSTANT,
        NULL,
        TRUE,
        FALSE,

        NOT,
        EQUAL,
        GREATER,
        LESS,

        RETURN,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,

        PRINT,
    }
}