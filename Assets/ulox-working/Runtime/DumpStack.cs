using System.Collections.Generic;
using System.Text;

namespace ULox
{
    public class DumpStack
    {
        private StringBuilder sb = new StringBuilder();

        public string Generate(Stack<Value> valueStack)
        {
            var copy = new Stack<Value>(valueStack);

            while (copy.Count > 0)
            {
                var value = copy.Pop();
                sb.Append(value.ToString());
                if (copy.Count != 0)
                    sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
