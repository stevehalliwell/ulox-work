using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class BouncingBallsBehaviour : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private LoxEngine loxEngine;
        private Interpreter interpreter;
        private Resolver resolver;
        private ICallable gameUpdateFunction;
        public List<GameObject> availablePrefabs;

        private void Start()
        {
            interpreter = new Interpreter(Debug.Log);
            resolver = new Resolver(interpreter);
            loxEngine = new LoxEngine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                resolver,
                interpreter,
                Debug.Log);

            loxEngine.SetValue("SetUIText",
                new Callable(1, (args) => text.text = (string)args[0]));
            loxEngine.SetValue("GetKey",
                new Callable(1, (args) => Input.GetKey((string)args[0])));
            loxEngine.SetValue("CreateGameObject",
                new Callable(1, (args) => CreateGameObject((string)args[0])));
            loxEngine.SetValue("SetGameObjectPosition",
                new Callable(3, (args) => ((GameObject)args[0]).transform.position = new Vector2(Convert.ToSingle(args[1]), Convert.ToSingle(args[2]))));
            loxEngine.SetValue("Reload",
                new Callable(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));

            loxEngine.Run(script.text);

            loxEngine.CallFunction("SetupGame");
            gameUpdateFunction = loxEngine.GetValue("Update") as ICallable;
        }

        private void Update()
        {
            loxEngine.SetValue("dt", Time.deltaTime);
            loxEngine.CallFunction(gameUpdateFunction);
        }

        private GameObject CreateGameObject(string name)
        {
            var loc = availablePrefabs.Find(x => x.name == name);
            if (loc != null)
            {
                return Instantiate(loc);
            }
            return null;
        }
    }
}
