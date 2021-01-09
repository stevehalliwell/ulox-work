using System.Linq;

namespace ULox
{
    public abstract class TestLoxEngine
    {
        public string InterpreterResult { get; private set; }
        public LoxEngine loxEngine;
        private Resolver resolver;
        public Interpreter Interpreter { get; private set; }

        private void SetResult(string str) => InterpreterResult += str;

        protected TestLoxEngine(params ILoxEngineLibraryBinder[] loxEngineLibraryBinders)
        {
            var binders = new ILoxEngineLibraryBinder[] { new LoxCoreLibrary(SetResult) };
            Interpreter = new Interpreter();
            resolver = new Resolver(Interpreter);
            loxEngine = new LoxEngine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                resolver,
                Interpreter,
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
