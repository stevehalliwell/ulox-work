namespace ULox
{
    public enum OpCode : byte
    {
        NONE,
     
        CONSTANT,
        NULL,
        TRUE,
        FALSE,

        POP, 

        DEFINE_GLOBAL,
        FETCH_GLOBAL,
        ASSIGN_GLOBAL,
        FETCH_LOCAL,
        ASSIGN_LOCAL,

        JUMP_IF_FALSE,
        JUMP,
        LOOP,   //this is just jump but negative

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