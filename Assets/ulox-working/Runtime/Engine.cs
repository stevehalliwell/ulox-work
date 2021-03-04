using System;
using System.Collections.Generic;
using System.Linq;

namespace ULox
{
    public class Engine
    {
        private Interpreter _interpreter;
        private Parser _parser;
        private Resolver _resolver;
        private Scanner _scanner;

        public Engine(
            params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            _scanner = new Scanner();
            _parser = new Parser();
            _resolver = new Resolver();
            _interpreter = new Interpreter();

            foreach (var binder in loxEngineLibraryBinders)
            {
                binder.BindToEngine(this);
            }
        }

        public Interpreter Interpreter => _interpreter;

        public List<ResolverWarning> ResolverWarnings => _resolver.ResolverWarnings;

        public IEnvironment AddressToEnvironment(string address, out string lastTokenLexeme)
        {
            var parts = address.Split('.');
            lastTokenLexeme = parts.Last();
            IEnvironment returnEnvironment = _interpreter.Globals;

            for (int i = 0; i < parts.Length - 1 && returnEnvironment != null; i++)
            {
                returnEnvironment = returnEnvironment.Fetch(parts[i], false) as IEnvironment;
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
            return callable.Call(_interpreter, FunctionArguments.New(objs));
        }

        public Instance CreateInstance(string className, params object[] objs)
        {
            return CreateInstance(GetClass(className), objs);
        }

        public Instance CreateInstance(Class @class, params object[] objs)
        {
            Interpreter.SantizeObjects(objs);
            return @class?.Call(_interpreter, FunctionArguments.New(objs)) as Instance;
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
                return containingEnvironment.Fetch(endToken, false);
            }

            return null;
        }

        public void Run(string text)
        {
            _scanner.Reset();
            var tokens = _scanner.Scan(text);
            _parser.Reset();
            var statements = _parser.Parse(tokens);

            _resolver.Reset();
            _resolver.Resolve(statements);

            _interpreter.Interpret(statements);
        }

        public void RunREPL(string text, Action<string> output)
        {
            _scanner.Reset();
            var tokens = _scanner.Scan(text);
            _parser.Reset();
            var statements = _parser.Parse(tokens);

            _resolver.Reset();
            _resolver.Resolve(statements);

            _interpreter.REPLInterpret(statements, output);
        }

        public void SetValue(string address, object value)
        {
            var containingEnvironment = AddressToEnvironment(address, out var endToken);

            var val = Interpreter.SantizeObject(value);

            if (containingEnvironment != null)
            {
                containingEnvironment.Assign(endToken, val, true, false);
            }
        }
    }
}
