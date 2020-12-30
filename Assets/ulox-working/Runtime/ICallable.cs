using System;
using System.Collections.Generic;

namespace ULox
{
    public interface ICallable
    {
        Object Call(Interpreter interpreter, Object[] args);

        int Arity { get; }
    }
}
