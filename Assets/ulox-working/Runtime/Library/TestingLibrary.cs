namespace ULox
{
    public class TestingLibrary : ILoxEngineLibraryBinder
    {
        private bool disableAutoThrow;

        public TestingLibrary(bool disableAutoThrowOnFailingSuites)
        {
            disableAutoThrow = disableAutoThrowOnFailingSuites;
        }

        public void BindToEngine(Engine engine)
        {
            engine.Interpreter.TestSuiteManager.AutoThrowOnFailingSuite = !disableAutoThrow;

            engine.SetValue("GenerateTestingReport",
                new Callable(0, (interp, args) => interp.TestSuiteManager.GenerateReport()));

            engine.SetValue("HasAnyTestFailed",
                new Callable(0, (interp, args) => interp.TestSuiteManager.HasFailures));

            engine.SetValue("ValidateTesting",
                new Callable(0, (interp, args) =>
                {
                    if (interp.TestSuiteManager.HasFailures)
                        throw new PanicException(interp.TestSuiteManager.GenerateReport());
                }));
        }
    }
}
