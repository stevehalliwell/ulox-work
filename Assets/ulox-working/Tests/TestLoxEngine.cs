using System.Linq;

namespace ULox
{
    public abstract class TestLoxEngine
    {
        public string InterpreterResult { get; private set; } = string.Empty;
        public Engine _engine;
        private Resolver resolver;

        protected void SetResult(string str) => InterpreterResult += str;

        protected TestLoxEngine(params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            var binders = new ILoxEngineLibraryBinder[] { new LoxCoreLibrary(SetResult) };
            resolver = new Resolver();
            _engine = new Engine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                resolver,
                new Interpreter(),
                binders.Concat(loxEngineLibraryBinders).ToArray());
        }

        public virtual void Run(string testString, bool catchAndLogExceptions, bool logWarnings = true, System.Action<string> REPLPrint = null)
        {
            try
            {
                if (REPLPrint != null)
                    _engine.RunREPL(testString, REPLPrint);
                else
                    _engine.Run(testString);

                if (logWarnings)
                {
                    SetResult(
                        string.Join(
                            "\n",
                            resolver.ResolverWarnings.Select(x => $"{x.Token} {x.Message}")
                                   ));
                }
            }
            catch (System.Exception e)
            {
                if (catchAndLogExceptions)
                {
                    SetResult(e.Message);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
