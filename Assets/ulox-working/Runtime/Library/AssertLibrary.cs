namespace ULox
{
    public class AssertLibrary : ILoxEngineLibraryBinder
    {
        public void BindToEngine(Engine engine)
        {
            engine.SetValue("Assert", new Instance(null, engine.Interpreter.CurrentEnvironment));

            engine.SetValue("Assert.AreEqual",
                new Callable(3, (interp, args) =>
                {
                    var lhs = args.At(0);
                    var rhs = args.At(1);
                    if (!Interpreter.IsEqual(lhs, rhs))
                        throw new AssertException($"{args.At(2)} : {lhs} does not equal {rhs}");
                }));

            engine.SetValue("Assert.AreApproxEqual",
                new Callable(3, (interp, args) =>
                {
                    var lhs = args.At<double>(0);
                    var rhs = args.At<double>(1);
                    var dif = lhs - rhs;
                    var squareDif = dif * dif;
                    if (squareDif > 1e-16)
                        throw new AssertException($"{args.At(2)} : {lhs} and {rhs} are {dif} apart.");
                }));
        }
    }
}
