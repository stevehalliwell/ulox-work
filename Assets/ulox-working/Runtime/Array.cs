using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class Array : Instance
    {
        public List<object> objs;

        public Array(int count) : base(null)
        {
            objs = new List<object>();
            for (int i = 0; i < count; i++)
            {
                objs.Add(null);
            }

            base.Set("Get", new Callable(1, (inter, args) => objs[(int)(double)args[0]]));
            base.Set("Set", new Callable(2, (inter, args) => objs[(int)(double)args[0]] = args[1]));
            base.Set("Count", new NativeExpression(() => (double)objs.Count));
        }

        public override void Set(string name, object val)
        {
            throw new RuntimeAccessException(new Token(), "Can't add properties to arrays.");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("<array [");
            foreach (var item in objs)
            {
                sb.Append(item?.ToString() ?? "null");
                sb.Append(",");
            }
            sb.Append("]>");

            return sb.ToString();
        }
    }
}