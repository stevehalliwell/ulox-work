﻿namespace ULox
{
    public class EngineFunctions : ILoxEngineLibraryBinder
    { 
        public void BindToEngine(Engine engine)
        {
            engine.SetValue("RunScript", 
                new Callable(1,(args) => engine.Run(args.At<string>(0))));
            engine.SetValue("RunScriptInLocalSandbox",
                new Callable(1, (interp, args) =>
                {
                    var curEnclosing = interp.CurrentEnvironment.Enclosing;
                    interp.CurrentEnvironment.Enclosing = null;
                    engine.Run(args.At<string>(0));
                    interp.CurrentEnvironment.Enclosing = curEnclosing;
                }));
            engine.SetValue("PushLocalEnvironment", 
                new Callable(0, (Interpreter interp, FunctionArguments args)
                    => interp.EnvironmentStack.PushNew()));
            engine.SetValue("PopLocalEnvironment",
                new Callable(1, (Interpreter interp, FunctionArguments args)
                    => interp.EnvironmentStack.PopTarget(args.At(0) as IEnvironment)));
        }
    }
}
