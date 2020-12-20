using System.Collections.Generic;

namespace ULox
{
    public class Instance
    {
        private Class _class;
        private Dictionary<string, object> fields = new Dictionary<string, object>();

        public Instance(Class @class)
        {
            _class = @class;
        }

        public object Get(Token name)
        {
            if (fields.TryGetValue(name.Lexeme, out object obj))
            {
                return obj;
            }

            var method = _class.FindMethod(name.Lexeme);
            if (method != null) return method.Bind(this);

            throw new InstanceException(name, "Undefined property '" + name.Lexeme + "'.");
        }

        public void Set(Token name, object val) => fields[name.Lexeme] = val;

        public override string ToString() => $"<inst {_class.Name}>";
    }
}
