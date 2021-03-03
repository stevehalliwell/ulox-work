using System.Collections.Generic;

namespace ULox
{
    public enum FunctionType { None, Function, Method, Init, }

    public class Function : ICallable
    {
        private string _name;
        private Expr.Function _declaration;
        private IEnvironment _closure;

        public Function(
            string name,
            Expr.Function declaration,
            IEnvironment closure)
        {
            _name = name;
            _declaration = declaration;
            _closure = closure;
        }

        public int Arity => _declaration?.parameters?.Count ?? 0;

        public IReadOnlyList<Token> Params => _declaration.parameters.AsReadOnly();

        public string Name => _name;

        public object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            //if doesn't have locals does it need the new env? since we don't know if we are going to be 
            //  used by a closure, yes we do
            var environment = new Environment(_closure);

            if (_declaration.parameters != null)
            {
                for (int i = 0; i < _declaration.parameters.Count; i++)
                {
                    environment.DefineSlot(_declaration.parameters[i].Lexeme, (short)i, functionArgs.args[i]);
                }
            }

            try
            {
                interpreter.ExecuteBlock(_declaration.body, environment);
            }
            catch (Return exp)
            {
                return exp.Value;
            }

            return null;
        }

        public override string ToString()
        {
            return $"<fn {_name}>";
        }
    }
}
