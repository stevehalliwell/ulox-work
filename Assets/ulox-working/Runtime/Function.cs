using System.Collections.Generic;

namespace ULox
{
    public class Function : ICallable
    {
        private string _name;
        private Expr.Function _declaration;
        private Environment _closure;
        private bool _isInitializer;

        public Function(
            string name,
            Expr.Function declaration,
            Environment closure,
            bool isInitializer)
        {
            _name = name;
            _declaration = declaration;
            _closure = closure;
            _isInitializer = isInitializer;
        }

        public int Arity => _declaration.parameters.Count;

        public bool IsGetter => _declaration.parameters == null;

        public object Call(Interpreter interpreter, object[] args)
        {
            var environment = new Environment(_closure);
            if (_declaration.parameters != null)
            {
                for (int i = 0; i < _declaration.parameters.Count; i++)
                {
                    environment.Define(_declaration.parameters[i].Lexeme, args[i]);
                }
            }

            try
            {
                interpreter.ExecuteBlock(_declaration.body, environment);
            }
            catch (Interpreter.Return exp)
            {
                if (_isInitializer) return _closure.GetAtDirect(0, "this");

                return exp.Value;
            }

            if (_isInitializer) return _closure.GetAtDirect(0, "this");

            return null;
        }

        public Function Bind(Instance instance)
        {
            var env = new Environment(_closure);
            env.Define("this", instance);
            return new Function(_name, _declaration, env, _isInitializer);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_name))
                return $"<fn {_name}>";
            return "<fn>";
        }
    }
}
