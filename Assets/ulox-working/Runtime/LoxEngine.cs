using System;

namespace ULox
{
    //todo easier user access to vars, funcs, and classes
    public class LoxEngine
    {
        private Scanner _scanner;
        private Parser _parser;
        private Resolver _resolver;
        private Interpreter _interpreter;

        public void SetValue(string address, object value)
        {
            value = SantizeObject(value);

            if (_interpreter.Globals.Exists(address))
                _interpreter.Globals.Assign(address, value);
            else
                _interpreter.Globals.Define(address, value);
        }

        public object GetValue(string address)
        {
            return _interpreter.Globals.Get(address);
        }

        public object CallFunction(string address, params object[] objs)
        {
            if (GetValue(address) is ICallable callable)
                return CallFunction(callable, objs);
            return null;
        }

        public object CallFunction(ICallable callable, params object[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i] = SantizeObject(objs[i]);
            }
            
            return callable.Call(_interpreter, objs);
        }

        public object SantizeObject(object o)
        {
            if (o is int || o is float)
                return Convert.ToDouble(o);
            return o;
        }

        private Action<string> _logger;

        public LoxEngine(
            Scanner scanner,
            Parser parser,
            Resolver resolver,
            Interpreter interpreter,
            Action<string> logger)
        {
            _scanner = scanner;
            _parser = parser;
            _resolver = resolver;
            _interpreter = interpreter;
            _logger = logger;

            _interpreter.Globals.Define("clock", new Callable(0, (inter, args) => System.DateTime.Now.Ticks));
            _interpreter.Globals.Define("abort", new Callable(0, (inter, args) => throw new LoxException("abort")));
            _interpreter.Globals.Define("Array", new Callable(1, (inter, args) => new Array((int)(double)args[0])));
        }

        public void Run(string text)
        {
            _scanner.Reset();
            _scanner.Scan(text);
            _parser.Reset();
            var statements = _parser.Parse(_scanner.Tokens);

            _resolver.Reset();
            _resolver.Resolve(statements);

            _interpreter.Interpret(statements);
        }
    }
}
