using System;

namespace ULox
{
    //todo  pre parse binding and post parse binding
    //  call function locally or globally
    public class LoxEngine
    {
        private Scanner _scanner;
        private Parser _parser;
        private Resolver _resolver;
        private Interpreter _interpreter;

        public void SetValue(string address, object value)
        {
            var containingEnvironment = _interpreter.AddressToEnvironment(address, out var endToken);

            value = Interpreter.SantizeObject(value);

            if (containingEnvironment != null)
            {
                var existingIndex = containingEnvironment.FindSlot(endToken);
                if (existingIndex >= 0)
                {
                    containingEnvironment.AssignSlot(existingIndex, value);
                }
                else
                {
                    containingEnvironment.Define(endToken, value);
                }
            }
        }

        public object GetValue(string address)
        {
            var containingEnvironment = _interpreter.AddressToEnvironment(address, out var endToken);

            if(containingEnvironment != null)
            {
                return containingEnvironment.FetchObject(containingEnvironment.FindSlot(endToken));
            }

            return null;
        }

        public object CallFunction(string address, params object[] objs)
        {
            if (GetValue(address) is ICallable callable)
                return CallFunction(callable, objs);
            return null;
        }

        public object CallFunction(ICallable callable, params object[] objs)
        {
            Interpreter.SantizeObjects(objs);
            return callable.Call(_interpreter, objs);
        }

        public Class GetClass(string className)
        {
            return GetValue(className) as Class;
        }

        public Instance CreateInstance(string className, params object[] objs)
        {
            return CreateInstance(GetClass(className), objs);
        }

        public Instance CreateInstance(Class @class, params object[] objs)
        {
            Interpreter.SantizeObjects(objs);
            return @class?.Call(_interpreter, objs) as Instance;
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

            SetValue("clock", new Callable(() => System.DateTime.Now.Ticks));
            SetValue("sleep", new Callable(1, (args) => System.Threading.Thread.Sleep((int)(double)args[0])));
            SetValue("abort", new Callable(() => throw new LoxException("abort")));
            SetValue("Rand", new Callable(() => UnityEngine.Random.value));
            SetValue("RandRange", new Callable(2, (args) => UnityEngine.Random.Range((float)(double)args[0], (float)(double)args[1])));

            SetValue("POD", new PODClass(null));
            SetValue("Array", new ArrayClass(null));
            SetValue("List", new ListClass(null));
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
