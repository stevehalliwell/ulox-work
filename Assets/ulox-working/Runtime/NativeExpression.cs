using System;
using System.Collections.Generic;

namespace ULox
{
    public class NativeExpression : ICallable
    {
        private readonly Func<object> _func;
        public int Arity => 0;

        public NativeExpression(Func<object> func)
        {
            _func = func;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            return _func?.Invoke();
        }
    }
}
