namespace ULox
{
    public class Instance : Environment
    {
        private Class _class;
        public Class Class => _class;

        public Instance(Class @class, IEnvironment enclosing)
            : base(enclosing)
        {
            _class = @class;
        }

        public object GetMethod(Token name)
        {
            var method = _class?.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new InstanceException(name, "Undefined method '" + name.Lexeme + "'.");
        }

        public virtual void Set(string name, object val)
        {
            if (valueIndicies.TryGetValue(name, out var index))
            {
                objectList[index] = val;
            }
            else
            {
                DefineInAvailableSlot(name, val);
            }
        }

        public override string ToString() => $"<inst {_class.Name}>";
    }
}
