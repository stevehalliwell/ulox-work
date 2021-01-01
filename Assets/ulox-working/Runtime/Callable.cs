using System;

namespace ULox
{
    public class Callable : ICallable
    {
        private readonly Func<Interpreter, object[], object> _func;
        private readonly int _arity;
        public int Arity => _arity;

        public Callable(int arity, Func<Interpreter, object[], object> func)
        {
            _arity = arity;
            _func = func;
        }

        public Callable(int arity, Func<object[], object> func)
        {
            _arity = arity;
            _func = (interp, args) => func(args);
        }

        public Callable(int arity, Action<object[]> func)
        {
            _arity = arity;
            _func = (interp, args) => { func(args); return null; };
        }

        public Callable(Action func)
        {
            _arity = 0;
            _func = (interp, args) => { func(); return null; };
        }

        public Callable(Func<object> func)
        {
            _arity = 0;
            _func = (interp, args) => func();
        }

        public object Call(Interpreter interpreter, object[] args)
        {
            return Interpreter.SantizeObject(_func?.Invoke(interpreter, args));
        }
    }
}
