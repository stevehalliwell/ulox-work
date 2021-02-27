namespace ULox
{
    public struct FunctionArguments
    {
        public object[] args;

        public T At<T>(int i) => (T)args[i];
        public object At(int i) => args[i];
        public static FunctionArguments New(params object[] arguments) =>
            new FunctionArguments() { args = arguments}; 
    }
}
