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

            _interpreter.Globals.Define("clock", new NativeExpression(() => System.DateTime.Now.Ticks));
            _interpreter.Globals.Define("abort", new NativeStatement(() => throw new LoxException("abort")));
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
