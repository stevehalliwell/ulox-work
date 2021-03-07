using System.Collections.Generic;

namespace ULox
{
    public class EnvironmentStack
    {
        private Stack<IEnvironment> _environmentStack = new Stack<IEnvironment>();
        private Stack<IEnvironment> _availableEnvironments = new Stack<IEnvironment>();
        private Environment _globals;

        public EnvironmentStack(Environment globals)
        {
            _globals = globals;
            CurrentFallbackEnvironment = globals;
            PushTarget(CurrentFallbackEnvironment);
        }

        public IEnvironment CurrentEnvironment => _environmentStack.Peek();
        public IEnvironment CurrentFallbackEnvironment { get; set; }

        public IEnvironment GetLocalEnvironment()
        {
            if (_availableEnvironments.Count == 0)
                _availableEnvironments.Push(new Environment(null));

            var res = _availableEnvironments.Pop();
            res.Reset(CurrentEnvironment);
            return res;
        }

        public IEnvironment GetGlobalEnvironment()
        {
            if (_availableEnvironments.Count == 0)
                _availableEnvironments.Push(new Environment(null));

            var res = _availableEnvironments.Pop();
            res.Reset(_globals);
            return res;
        }

        public IEnvironment PushNew()
        {
            var env = GetLocalEnvironment();
            _environmentStack.Push(env);
            return env;
        }

        public void PushTarget(IEnvironment env)
        {
            _environmentStack.Push(env);
        }

        public bool PopTarget(IEnvironment env)
        {
            if (_environmentStack.Peek() == env)
            {
                _environmentStack.Pop();
                return true;
            }
            return false;
        }

        public IEnvironment Pop()
        {
            var res = _environmentStack.Pop();
            _availableEnvironments.Push(res);
            return res;
        }
    }
}
