using System;

namespace ULox
{
    public class LoxCoreLibrary : ILoxEngineLibraryBinder
    {
        private Action<string> _logger;

        private void PrintViaLogger(object o) => _logger?.Invoke(o?.ToString() ?? "null");

        public LoxCoreLibrary(Action<string> logger)
        {
            _logger = logger;
        }

        public void BindToEngine(LoxEngine engine)
        {
            //todo printr style print
            engine.SetValue("print", new Callable(1, (args) => PrintViaLogger(args[0])));
            engine.SetValue("printr", new Callable(1, (args) =>
            {
                var sb = new System.Text.StringBuilder();
                PrintRecursive(args[0], sb, string.Empty);
                PrintViaLogger(sb.ToString());
            }));
            engine.SetValue("clock", new Callable(() => System.DateTime.Now.Ticks));
            engine.SetValue("sleep", new Callable(1, (args) => System.Threading.Thread.Sleep((int)(double)args[0])));
            engine.SetValue("abort", new Callable(() => throw new LoxException("abort")));
        }

        private void PrintRecursive(object v, System.Text.StringBuilder sb, string prefix)
        {
            //sb.Append(prefix);
            sb.Append(v?.ToString() ?? "null");
            prefix += "  ";

            if (v is Class vClass)
            {
                foreach (var meth in vClass.ReadOnlyMethods)
                {
                    sb.AppendLine(); 
                    sb.Append(prefix);
                    PrintRecursive(meth.Value, sb, prefix);
                }
            }
            else if (v is Environment vEnv)
            {
                foreach (var val in vEnv.ReadOnlyValueIndicies)
                {
                    sb.AppendLine();
                    sb.Append(prefix);
                    sb.Append(val.Key);
                    sb.Append(" : ");
                    PrintRecursive(vEnv.FetchObject(val.Value), sb, prefix);
                }
            }
        }
    }
}
