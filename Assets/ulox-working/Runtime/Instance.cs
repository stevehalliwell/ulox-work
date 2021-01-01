namespace ULox
{
    public class Instance : Environment
    {
        private Class _class;

        public Instance(Class @class, IEnvironment enclosing)
            : base(enclosing)
        {
            _class = @class;
        }

        public virtual object Get(Token name)
        {
            if (values.TryGetValue(name.Lexeme, out object obj))
            {
                return obj;
            }

            var method = _class?.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new InstanceException(name, "Undefined property '" + name.Lexeme + "'.");
        }

        public virtual void Set(string name, object val) => values[name] = val;

        public override string ToString() => $"<inst {_class.Name}>";
    }
}
