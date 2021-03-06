using System.Collections.Generic;

namespace ULox
{
    public class EnvironmentStack
    {
        private Stack<IEnvironment> _environmentStack = new Stack<IEnvironment>();
        private Stack<IEnvironment> _availableEnvironments = new Stack<IEnvironment>();
        private Environment globals;

        public EnvironmentStack(Environment globals)
        {
            CurrentFallbackEnvironment = globals;
            PushTarget(CurrentFallbackEnvironment);
        }

        public IEnvironment CurrentEnvironment => _environmentStack.Peek();
        public IEnvironment CurrentFallbackEnvironment { get; set; }

        public IEnvironment PushNew()
        {
            if (_availableEnvironments.Count == 0)
                _availableEnvironments.Push(new Environment(CurrentEnvironment));

            var ret = _availableEnvironments.Pop();
            ret.Reset(CurrentEnvironment);
            _environmentStack.Push(ret);
            return ret;
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
