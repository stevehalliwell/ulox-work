namespace ULox
{
    //todo determine if we can use common constant ops like inc, *-1 etc.
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
        GET_LOCAL,
        SET_LOCAL,
        GET_UPVALUE,
        SET_UPVALUE,
        CLOSE_UPVALUE,

        GET_PROPERTY,
        SET_PROPERTY,

        JUMP_IF_FALSE,
        JUMP,
        LOOP,   //this is just jump but negative

        NOT,
        EQUAL,
        GREATER,
        LESS,

        CALL,
        CLOSURE,

        RETURN,

        CLASS,
        METHOD,
        INVOKE,
        INHERIT,
        GET_SUPER,
        SUPER_INVOKE,

        NEGATE,
        ADD,
        SUBTRACT,
        MULTIPLY,
        DIVIDE,

        PRINT,
    }
}