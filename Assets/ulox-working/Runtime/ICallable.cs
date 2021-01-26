using System;

namespace ULox
{
    public interface ICallable
    {
        //todo we've started mixing sending this through param 0 and through the call
        //  this needs to be clarified as external calls shouldn't need to know about
        //  the number of args that are reserved
        Object Call(Interpreter interpreter, FunctionArguments args);

        int Arity { get; }
    }
}
