using System.Collections.Generic;

namespace ULox
{
    public class Function : ICallable
    {
        private Stmt.Function _declaration;
        private Environment _closure;
        private bool _isInitializer;

        public Function(Stmt.Function declaration, Environment closure, bool isInitializer)
        {
            _declaration = declaration;
            _closure = closure;
            _isInitializer = isInitializer;
        }

        public int Arity => _declaration.parameters.Count;

        public object Call(Interpreter interpreter, List<object> args)
        {
            var environment = new Environment(_closure);
            for (int i = 0; i < _declaration.parameters.Count; i++) {
                environment.Define(_declaration.parameters[i].Lexeme, args[i]);
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
            return new Function(_declaration, env, _isInitializer);
        }

        public override string ToString() => $"<fn {_declaration.name.Lexeme}>";
    }
}