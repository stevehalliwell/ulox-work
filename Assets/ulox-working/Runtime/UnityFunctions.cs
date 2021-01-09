namespace ULox
{
    public class UnityFunctions : ILoxEngineLibraryBinder
    {
        public void BindToEngine(LoxEngine engine)
        {
            engine.SetValue("Rand", new Callable(() => UnityEngine.Random.value));
            engine.SetValue("RandRange", new Callable(2, (args) => UnityEngine.Random.Range((float)(double)args[0], (float)(double)args[1])));
        }
    }
}
