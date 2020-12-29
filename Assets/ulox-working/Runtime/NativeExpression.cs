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

    public class Callable : ICallable
    {
        private readonly Func<Interpreter, List<object>, object> _func;
        private readonly int _arity;
        public int Arity => _arity;

        public Callable(int arity, Func<Interpreter, List<object>, object> func)
        {
            _arity = arity;
            _func = func;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            return _func?.Invoke(interpreter, args);
        }
    }
}
