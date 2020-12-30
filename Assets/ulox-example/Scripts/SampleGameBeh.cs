using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULox.Demo
{
    public class SampleGameBeh : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private LoxEngine loxEngine;
        private Interpreter interpreter;
        private Resolver resolver;
        private ICallable gameUpdateFunction;
        private ICallable collisionFunction;
        public List<GameObject> availablePrefabs;
        private Dictionary<int, GameObject> createdObjects = new Dictionary<int, GameObject>();
        private int createdCount = 0;

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
                new Callable(3, (args) => SetGameObjectPosition(Convert.ToInt32(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]))));
            loxEngine.SetValue("Reload", 
                new Callable(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
            loxEngine.SetValue("SetGameObjectVelocity",
                new Callable(3, (args) => SetGameObjectVelocity(Convert.ToInt32(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2]))));
            loxEngine.SetValue("DestroyGameObject", 
                new Callable(1, (args) => DestroyGameObject(Convert.ToInt32(args[0]))));


            loxEngine.Run(script.text);

            loxEngine.CallFunction("SetupGame");
            gameUpdateFunction = loxEngine.GetValue("Update") as ICallable;
            collisionFunction = loxEngine.GetValue("OnCollision") as ICallable;
        }

        private void Update()
        {
            loxEngine.SetValue("dt", Time.deltaTime);
            loxEngine.CallFunction(gameUpdateFunction);
        }

        private double CreateGameObject(string name)
        {
            var loc = availablePrefabs.Find(x => x.name == name);
            if (loc != null)
            {
                createdCount++;
                var newGo = Instantiate(loc);
                createdObjects[createdCount] = newGo;
                newGo.AddComponent<CollisionReroute>()
                    .rereouteDestination = CollisionRereouteHandler;
                return createdCount;
            }
            return -1;
        }

        private void CollisionRereouteHandler(Collision arg1, GameObject arg2)
        {
            var arg1ID = -1;
            var arg2ID = -1;

            var res = createdObjects.FirstOrDefault(x => x.Value == arg1.gameObject);
            if (!res.Equals(default)) arg1ID = res.Key;
            res = createdObjects.FirstOrDefault(x => x.Value == arg2);
            if (!res.Equals(default)) arg2ID = res.Key;

            if (arg1ID != -1 && arg2ID != -1)
            {
                loxEngine.CallFunction(collisionFunction, arg1ID, arg2ID);
            }
        }

        private void DestroyGameObject(int id)
        {
            if (createdObjects.TryGetValue(id, out var go))
            {
                //delay to ensure it lives the rest of game loop, remove so the script already thinks it's gone
                Destroy(go,0.1f);
                createdObjects.Remove(id);
            }
        }

        private void SetGameObjectPosition(int id, float x, float y)
        {
            if (createdObjects.TryGetValue(id, out var go))
            {
                go.transform.position = new Vector2(x,y);
            }
        }
        private void SetGameObjectVelocity(int id, float x, float y)
        {
            if (createdObjects.TryGetValue(id, out var go))
            {
                go.GetComponent<Rigidbody>().velocity = new Vector2(x, y);
            }
        }
    }
}
