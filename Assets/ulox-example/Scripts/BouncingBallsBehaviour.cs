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
        private Engine engine;
        private ICallable gameUpdateFunction;
        public List<GameObject> availablePrefabs;

        private void Start()
        {
            engine = new Engine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                new Resolver(),
                new Interpreter(),
                new LoxCoreLibrary(Debug.Log),
                new StandardClasses(),
                new UnityFunctions(availablePrefabs));

            engine.SetValue("SetUIText",
                new Callable(1, (args) => text.text = args.At<string>(0)));
            
            engine.Run(script.text);

            engine.CallFunction("SetupGame");
            gameUpdateFunction = engine.GetValue("Update") as ICallable;
        }

        private void Update()
        {
            engine.SetValue("dt", Time.deltaTime);
            engine.CallFunction(gameUpdateFunction);
        }
    }
}
