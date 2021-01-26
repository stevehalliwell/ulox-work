using System;

namespace ULox
{
    public interface ICallable
    {
        Object Call(Interpreter interpreter, FunctionArguments args);

        int Arity { get; }
    }
}
