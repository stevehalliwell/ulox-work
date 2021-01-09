using System.Linq;

namespace ULox
{
    public abstract class TestLoxEngine
    {
        public string InterpreterResult { get; private set; } = string.Empty;
        public LoxEngine loxEngine;
        private Resolver resolver;

        private void SetResult(string str) => InterpreterResult += str;

        protected TestLoxEngine(params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            var binders = new ILoxEngineLibraryBinder[] { new LoxCoreLibrary(SetResult) };
            resolver = new Resolver();
            loxEngine = new LoxEngine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                resolver,
                new Interpreter(),
                binders.Concat(loxEngineLibraryBinders).ToArray());
        }

        public void Run(string testString, bool catchAndLogExceptions, bool logWarnings = true)
        {
            try
            {
                loxEngine.Run(testString);

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
