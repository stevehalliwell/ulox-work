using System.Collections.Generic;

namespace ULox
{
    public class FunctionArguments
    {
        public List<object> args;

        public T At<T>(int i) => (T)args[i];
        public object At(int i) => args[i];
        public static FunctionArguments New(params object[] arguments) =>
            new FunctionArguments() { args = new List<object>(arguments)};
        public static FunctionArguments New(List<object> arguments) =>
            new FunctionArguments() { args = arguments };
    }
}
