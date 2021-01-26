using System;

namespace ULox
{
    public class Callable : ICallable
    {
        private readonly Func<Interpreter, FunctionArguments, object> _func;
        private readonly int _arity;
        public int Arity => _arity + Function.StartingParamSlot;

        public Callable(int arity, Func<Interpreter, FunctionArguments, object> func)
        {
            _arity = arity;
            _func = func;
        }

        public Callable(int arity, Func<FunctionArguments, object> func)
        {
            _arity = arity;
            _func = (interp, funcArgs) => func(funcArgs);
        }

        public Callable(int arity, Action<Interpreter> func)
        {
            _arity = arity;
            _func = (interp, funcArgs) => { func(interp); return null; };
        }

        public Callable(int arity, Action<Interpreter, FunctionArguments> func)
        {
            _arity = arity;
            _func = (interp, funcArgs) => { func(interp, funcArgs); return null; };
        }

        public Callable(int arity, Action<FunctionArguments> func)
        {
            _arity = arity;
            _func = (interp, funcArgs) => { func(funcArgs); return null; };
        }

        public Callable(Action func)
        {
            _arity = 0;
            _func = (interp, funcArgs) => { func(); return null; };
        }

        public Callable(Func<object> func)
        {
            _arity = 0;
            _func = (interp, funcArgs) => func();
        }

        public object Call(Interpreter interpreter, FunctionArguments funcArgs)
        {
            return Interpreter.SantizeObject(_func?.Invoke(interpreter, funcArgs));
        }
    }
}
