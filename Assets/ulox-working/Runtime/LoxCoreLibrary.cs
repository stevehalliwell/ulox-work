using System;

namespace ULox
{
    public class LoxCoreLibrary : ILoxEngineLibraryBinder
    {
        private Action<string> _logger;

        public LoxCoreLibrary(Action<string> logger)
        {
            _logger = logger;
        }

        public void BindToEngine(LoxEngine engine)
        {
            //todo printr style print
            engine.SetValue("print", new Callable(1, (args) =>
            {
                var obj = args[0];
                _logger?.Invoke(obj?.ToString() ?? "null");
            }));
            engine.SetValue("clock", new Callable(() => System.DateTime.Now.Ticks));
            engine.SetValue("sleep", new Callable(1, (args) => System.Threading.Thread.Sleep((int)(double)args[0])));
            engine.SetValue("abort", new Callable(() => throw new LoxException("abort")));
        }
    }
}
