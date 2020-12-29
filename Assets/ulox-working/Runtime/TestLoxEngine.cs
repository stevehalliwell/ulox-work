using System.Linq;

namespace ULox
{
    public class TestLoxEngine
    {
        public string InterpreterResult { get; private set; }
        private LoxEngine loxEngine;
        private Resolver resolver;
        private Interpreter interpreter;

        private void SetResult(string str) => InterpreterResult += str;

        public TestLoxEngine()
        {
            interpreter = new Interpreter(SetResult);
            resolver = new Resolver(interpreter);
            loxEngine = new LoxEngine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                resolver,
                interpreter,
                SetResult);
        }

        public void Run(string testString, bool catchAndLogExceptions)
        {
            try
            {
                loxEngine.Run(testString);

                SetResult(
                    string.Join(
                        "\n",
                        resolver.ResolverWarnings.Select(x => $"{x.Token} {x.Message}")
                               ));
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
