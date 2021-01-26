namespace ULox
{
    public class Function : IFunction
    {
        private string _name;
        private Expr.Function _declaration;
        private IEnvironment _closure;
        private bool _isInitializer;
        public const short StartingParamSlot = 1;

        public Function(
            string name,
            Expr.Function declaration,
            IEnvironment closure,
            bool isInitializer)
        {
            _name = name;
            _declaration = declaration;
            _closure = closure;
            _isInitializer = isInitializer;
        }

        public int Arity => _declaration?.parameters?.Count ?? Function.StartingParamSlot;

        public bool IsGetter => _declaration.parameters == null;

        public object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            //if doesn't have locals does it need the new env?
            var environment = new Environment(_closure);
            //if we haven't been given a valid this, see if we already have one
            environment.DefineSlot(
                Class.ThisIdentifier, 
                Class.ThisSlot, 
                functionArgs.@this == null ? 
                    _closure.FetchObject(Class.ThisSlot) : 
                    functionArgs.@this);
            
            if (_declaration.parameters != null)
            {
                for (int i = Function.StartingParamSlot; i < _declaration.parameters.Count; i++)
                {
                    environment.DefineSlot(_declaration.parameters[i].Lexeme, (short)i, functionArgs.args[i - Function.StartingParamSlot]);
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

            if (_isInitializer) return _closure.FetchObject(Class.ThisSlot);

            return null;
        }

        public Method Bind(Instance instance)
        {
            //prior to expecting this, there was an empty enclosing env with a this in it, resolver expected it too
            //var env = new Environment(_closure);
            //env.DefineSlot(Class.ThisIdentifier, Class.ThisSlot, instance);
            //return new Method(instance, new Function(_name, _declaration, env, _isInitializer));
            return new Method(instance, this);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(_name))
                return $"<fn {_name}>";
            return "<fn>";
        }
    }
}
