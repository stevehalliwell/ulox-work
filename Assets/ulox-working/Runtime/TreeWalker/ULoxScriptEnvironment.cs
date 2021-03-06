﻿namespace ULox
{
    /// <summary>
    /// Creates a local environment for the script to run inside, allowing for multiple objects
    /// to share the same script and treat their vars as local to their script instance, because 
    /// they are.
    /// 
    /// Globals will still be found and global callables still used, but globals will not be able
    /// to access local environment vars, there is no way for them to do so. If that is desirable
    /// you will need to write global callables with args that can be passed in from the local env.
    /// 
    /// More over global env doesn't track the local envs being made so can can't be automatically,
    /// or accidentally bridged.
    /// </summary>
    public class ULoxScriptEnvironment
    {
        private Engine _engine;
        private IEnvironment _ourEnvironment;

        public Engine SharedEngine => _engine;
        public IEnvironment LocalEnvironemnt => _ourEnvironment;

        public ULoxScriptEnvironment(Engine engine)
        {
            _engine = engine;
            _ourEnvironment = engine.Interpreter.EnvironmentStack.PushNew();
            engine.Interpreter.EnvironmentStack.PopTarget(_ourEnvironment);
        }

        public void RunScript(string script)
        {
            _engine.Interpreter.EnvironmentStack.PushTarget(_ourEnvironment);
            _engine.Run(script);
            _engine.Interpreter.EnvironmentStack.PopTarget(_ourEnvironment);
        }

        public void CallFunction(ICallable callable, params object[] objs)
        {
            _engine.Interpreter.EnvironmentStack.PushTarget(_ourEnvironment);
            _engine.CallFunction(callable, objs);
            _engine.Interpreter.EnvironmentStack.PopTarget(_ourEnvironment);
        }

        public object FetchLocalByName(string name)
        {
            return _ourEnvironment.Fetch(name, false);
        }

        public void AssignLocalByName(string name, object val)
        {
            _ourEnvironment.Assign(name, Interpreter.SantizeObject(val), true, false);
        }
    }
}
