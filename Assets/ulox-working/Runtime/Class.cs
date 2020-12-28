using System.Collections.Generic;

namespace ULox
{
    public class Class : Instance, ICallable
    {
        private string _name;
        private Dictionary<string, Function> _methods;
        private Class _superclass;
        private List<Stmt.Var> _vars;

        public string Name => _name;

        public Class(
            Class metaClass,
            string name,
            Class superclass,
            Dictionary<string, Function> methods,
            List<Stmt.Var> fields)
            : base(metaClass)
        {
            _name = name;
            _methods = methods;
            _superclass = superclass;
            _vars = fields;
        }

        public int Arity => FindMethod("init")?.Arity ?? 0;

        public object Call(Interpreter interpreter, List<object> args)
        {
            var instance = new Instance(this);

            foreach (var item in _vars)
            {
                instance.Set(item.name.Lexeme, interpreter.Evaluate(item.initializer));
            }

            var initializer = FindMethod("init");
            if (initializer != null)
            {
                initializer.Bind(instance).Call(interpreter, args);
            }

            return instance;
        }

        public Function FindMethod(string lexeme)
        {
            if (_methods.TryGetValue(lexeme, out Function func))
                return func;

            if (_superclass != null)
                return _superclass.FindMethod(lexeme);

            return null;
        }

        public override string ToString() => $"<class {_name}>";
    }
}