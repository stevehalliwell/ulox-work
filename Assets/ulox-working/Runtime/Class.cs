using System.Collections.Generic;

namespace ULox
{
    public class Class : ICallable
    {
        private string _name;

        public string Name => _name;

        public Class(string name)
        {
            _name = name;
        }

        public int Arity => 0;

        public object Call(Interpreter interpreter, List<object> args)
        {
            return new Instance(this);
        }

        public override string ToString()
        {
            return "<class " + _name + ">";
        }
    }
}