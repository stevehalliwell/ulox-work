using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    /// <summary>
    /// This demo uses lox to move objects, unity just draws things.
    /// This demo also uses direct gameobject referenes in script rather than ids.
    /// </summary>
    public class BouncingBallsBehaviour : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private LoxEngine loxEngine;
        private ICallable gameUpdateFunction;
        public List<GameObject> availablePrefabs;

        private void Start()
        {
            loxEngine = new LoxEngine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                new Resolver(),
                new Interpreter(),
                new LoxCoreLibrary(Debug.Log),
                new StandardClasses(),
                new UnityFunctions(availablePrefabs));

            loxEngine.SetValue("SetUIText",
                new Callable(1, (args) => text.text = (string)args[0]));
            
            loxEngine.Run(script.text);

            loxEngine.CallFunction("SetupGame");
            gameUpdateFunction = loxEngine.GetValue("Update") as ICallable;
        }

        private void Update()
        {
            loxEngine.SetValue("dt", Time.deltaTime);
            loxEngine.CallFunction(gameUpdateFunction);
        }
    }
}
