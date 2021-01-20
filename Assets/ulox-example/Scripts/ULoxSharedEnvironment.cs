using UnityEngine;

namespace ULox.Demo
{
    /// <summary>
    /// A shared ulox engine to be used by 0-* instances. Allowing for easier sharing of
    /// common functionality and data or simply to reduce the overhead of multiple ulox engines.
    /// </summary>
    public class ULoxSharedEnvironment : MonoBehaviour
    {
        private Engine _engine;
        public Engine Engine => _engine;

        private void Awake()
        {
            _engine = new Engine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                new Resolver(),
                new Interpreter(),
                new LoxCoreLibrary(Debug.Log),
                new StandardClasses(),
                new UnityFunctions());
        }
    }
}
