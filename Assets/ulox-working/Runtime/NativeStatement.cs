using System;
using System.Collections.Generic;

namespace ULox
{
    public class NativeStatement : ICallable
    {
        private readonly Action _action;
        public int Arity => 0;

        public NativeStatement(Action action)
        {
            _action = action;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            _action?.Invoke();
            return null;
        }
    }
}
