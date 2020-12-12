using System.Collections.Generic;

namespace ULox
{
    public class Function : ICallable
    {
        private Stmt.Function _declaration;

        public Function(Stmt.Function declaration)
        {
            _declaration = declaration;
        }

        public int Arity => _declaration.parameters.Count;

        public object Call(Interpreter interpreter, List<object> args)
        {
            var environment = new Environment(interpreter.Globals);
            for (int i = 0; i < _declaration.parameters.Count; i++) {
                environment.Define(_declaration.parameters[i].Lexeme, args[i]);
            }

            try
            {
                interpreter.ExecuteBlock(_declaration.body, environment);
            }
            catch (Interpreter.Return exp)
            {
                return exp.Value;
            }
            return null;
        }

        public override string ToString() => $"<fn {_declaration.name.Lexeme}>";
    }
}