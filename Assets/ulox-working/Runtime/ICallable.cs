using System;
using System.Collections.Generic;

namespace ULox
{
    public interface ICallable
    {
        Object Call(Interpreter interpreter, List<Object> args);
        int Arity { get; }
    }

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

    public class NativeExpression : ICallable
    {
        private readonly Func<Object> _func;
        public int Arity => 0;

        public NativeExpression(Func<Object> func)
        {
            _func = func;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            return _func?.Invoke();
        }
    }
}