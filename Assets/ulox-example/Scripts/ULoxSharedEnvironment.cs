using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ULox.Demo
{
    /// <summary>
    /// A shared ulox engine to be used by 0-* instances. Allowing for easier sharing of 
    /// common functionality and data or simply to reduce the overhead of multiple ulox engines.
    /// </summary>
    public class ULoxSharedEnvironment : MonoBehaviour
    {
        private LoxEngine loxEngine;
        public LoxEngine LoxEngine => loxEngine;

        void Awake()
        {
            loxEngine = new LoxEngine(
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