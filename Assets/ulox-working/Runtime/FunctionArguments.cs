namespace ULox
{
    public struct FunctionArguments
    {
        public Instance @this;
        public object[] args;

        public T At<T>(int i) => (T)args[i];
        public object At(int i) => args[i];
        public int Count => args.Length;

        public static FunctionArguments New() { return new FunctionArguments(); }
        public static FunctionArguments New(object[] arguments) { return new FunctionArguments() { args = arguments}; }
        public static FunctionArguments New(Instance theThis) { return new FunctionArguments() { @this = theThis}; }
        public static FunctionArguments New(Instance theThis, object[] arguments) { return new FunctionArguments() { @this = theThis, args = arguments }; }
    }
}
