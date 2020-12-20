using System;
using System.Collections.Generic;

namespace ULox
{
    public interface ICallable
    {
        Object Call(Interpreter interpreter, List<Object> args);

        int Arity { get; }
    }
}
