namespace ULox
{
    public class PODClass : Class
    {
        public PODClass(Environment enclosing) 
            : base(null, "POD", null, null, null, enclosing, null, null)
        {
        }
    }

    public class ArrayClass : Class
    {
        public ArrayClass(Environment enclosing)
            : base(null, "Array", null, null, null, enclosing, null, null) { }

        public override int Arity => 1 + Function.StartingParamSlot;

        public override object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            return Array.CreateArray((int)functionArgs.At<double>(0), this, interpreter.CurrentEnvironment);
        }
    }

    public class ListClass : Class
    {
        public ListClass(Environment enclosing)
            : base(null, "List", null, null, null, enclosing, null, null) { }

        public override int Arity => 0 + Function.StartingParamSlot;

        public override object Call(Interpreter interpreter, FunctionArguments functionArgs)
        {
            return Array.CreateList(this, interpreter.CurrentEnvironment);
        }
    }
}
