using System;

namespace ULox
{
    public interface ICallable
    {
        Object Call(Interpreter interpreter, Object[] args);

        int Arity { get; }
    }
}
