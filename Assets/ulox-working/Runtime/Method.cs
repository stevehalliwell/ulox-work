namespace ULox
{
    public class Method : IFunction
    {
        private Instance _boundInstance;
        private Function _function;

        public Method(Instance boundInstance, Function function)
        {
            _boundInstance = boundInstance;
            _function = function;
        }

        public int Arity => _function.Arity;

        public bool IsGetter => _function.IsGetter;

        public object Call(Interpreter interpreter, FunctionArguments args)
        {
            args.@this = _boundInstance;
            return _function.Call(interpreter, args);
        }
    }
}
