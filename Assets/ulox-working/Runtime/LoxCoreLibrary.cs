namespace ULox
{
    public class LoxCoreLibrary : ILoxEngineLibraryBinder
    {
        public void BindToEngine(LoxEngine engine)
        {
            //todo print probably should be part of standard lib and include printr style print
            engine.SetValue("clock", new Callable(() => System.DateTime.Now.Ticks));
            engine.SetValue("sleep", new Callable(1, (args) => System.Threading.Thread.Sleep((int)(double)args[0])));
            engine.SetValue("abort", new Callable(() => throw new LoxException("abort")));
        }
    }
}
