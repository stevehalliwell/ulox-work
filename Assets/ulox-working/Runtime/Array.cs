using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class Array : Instance
    {
        public List<object> objs;
        public bool IsList { get; protected set; } = false;

        public Array(int count, bool asList, Class @class, IEnvironment enclosing)
            : base(@class, enclosing)
        {
            IsList = asList;
            objs = new List<object>();
            for (int i = 0; i < count; i++)
            {
                objs.Add(null);
            }

            base.Set("Get", new Callable(1, (args) => objs[(int)args.At<double>(0)]));
            base.Set("Set", new Callable(2, (args) => objs[(int)args.At<double>(0)] = args.At<double>(1)));
            base.Set("Count", new Callable(() => (double)objs.Count));
            if (IsList)
            {
                base.Set("Add", new Callable(1, (args) => objs.Add(args.At(0))));
                base.Set("Remove", new Callable(1, (args) => objs.Remove(args.At(0))));
                base.Set("RemoveAt", new Callable(1, (args) => objs.RemoveAt((int)args.At<double>(0))));
            }
        }

        public static Array CreateArray(int count, Class @class, IEnvironment enclosing)
        {
            return new Array(count, false, @class, enclosing);
        }

        public static Array CreateList(Class @class, IEnvironment enclosing)
        {
            return new Array(0, true, @class, enclosing);
        }

        public override void Set(string name, object val)
        {
            throw new RuntimeAccessException(new Token(TokenType.NONE, null, null, -1, -1), "Can't add properties to arrays.");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(IsList ? "<list [" : "<array [");
            foreach (var item in objs)
            {
                sb.Append(item?.ToString() ?? Interpreter.NulIdentifier);
                sb.Append(",");
            }
            sb.Append("]>");

            return sb.ToString();
        }
    }
}
