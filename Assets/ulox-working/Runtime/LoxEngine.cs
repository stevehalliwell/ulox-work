using System;
using System.Linq;

namespace ULox
{
    public class LoxEngine
    {
        private Interpreter _interpreter;
        private Parser _parser;
        private Resolver _resolver;
        private Scanner _scanner;

        public LoxEngine(
            Scanner scanner,
            Parser parser,
            Resolver resolver,
            Interpreter interpreter,
            params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            _scanner = scanner;
            _parser = parser;
            _resolver = resolver;
            _interpreter = interpreter;

            foreach (var binder in loxEngineLibraryBinders)
            {
                binder.BindToEngine(this);
            }
        }

        public IEnvironment AddressToEnvironment(string address, out string lastTokenLexeme)
        {
            var parts = address.Split('.');
            lastTokenLexeme = parts.Last();
            IEnvironment returnEnvironment = _interpreter.Globals;

            for (int i = 0; i < parts.Length - 1 && returnEnvironment != null; i++)
            {
                returnEnvironment = returnEnvironment.FetchObject(returnEnvironment.FindSlot(parts[i])) as IEnvironment;
            }

            return returnEnvironment;
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

        public Instance CreateInstance(string className, params object[] objs)
        {
            return CreateInstance(GetClass(className), objs);
        }

        public Instance CreateInstance(Class @class, params object[] objs)
        {
            Interpreter.SantizeObjects(objs);
            return @class?.Call(_interpreter, objs) as Instance;
        }

        public Class GetClass(string className)
        {
            return GetValue(className) as Class;
        }

        public object GetValue(string address)
        {
            var containingEnvironment = AddressToEnvironment(address, out var endToken);

            if (containingEnvironment != null)
            {
                return containingEnvironment.FetchObject(containingEnvironment.FindSlot(endToken));
            }

            return null;
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

        public void SetValue(string address, object value)
        {
            var containingEnvironment = AddressToEnvironment(address, out var endToken);

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
                    containingEnvironment.DefineInAvailableSlot(endToken, value);
                }
            }
        }
    }
}
