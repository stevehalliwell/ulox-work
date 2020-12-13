using System;
using System.Collections.Generic;

namespace ULox
{
    public class Class : ICallable
    {
        private string _name;
        private Dictionary<string, Function> _methods;

        public string Name => _name;

        public Class(string name, Dictionary<string, Function> methods)
        {
            _name = name;
            _methods = methods;
        }

        public int Arity => FindMethod("init")?.Arity ?? 0;

        public object Call(Interpreter interpreter, List<object> args)
        {
            var instance = new Instance(this);
            
            var initializer = FindMethod("init");
            if (initializer != null)
            {
                initializer.Bind(instance).Call(interpreter, args);
            }

            return instance;
        }

        public override string ToString()
        {
            return "<class " + _name + ">";
        }

        public Function FindMethod(string lexeme)
        {
            _methods.TryGetValue(lexeme, out Function func);
            return func;
        }
    }
}