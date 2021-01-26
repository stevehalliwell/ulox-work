using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    /// <summary>
    /// This demo uses Unity physics for interactions and routes calls back to the script for responses. It also uses explicit
    /// ids to communicate between the harnes and the scripting environment.
    /// </summary>
    public class BreakoutSampleGameBehaviour : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private Engine engine;
        private ICallable gameUpdateFunction;
        private ICallable collisionFunction;
        public List<GameObject> availablePrefabs;
        private Dictionary<int, GameObject> createdObjects = new Dictionary<int, GameObject>();
        private int createdCount = 0;

        private void Start()
        {
            engine = new Engine(
                new Scanner(),
                new Parser() { CatchAndSynch = false },
                new Resolver(),
                new Interpreter(),
                new LoxCoreLibrary(Debug.Log),
                new StandardClasses(),
                new UnityFunctions());

            engine.SetValue("SetUIText",
                new Callable(1, (args) => text.text = args.At<string>(0)));
            engine.SetValue("GetKey",
                new Callable(1, (args) => Input.GetKey(args.At<string>(0))));
            engine.SetValue("CreateGameObject",
                new Callable(1, (args) => CreateGameObject(args.At<string>(0))));
            engine.SetValue("SetGameObjectPosition",
                new Callable(3, (args) => SetGameObjectPosition(Convert.ToInt32(args.At<double>(0)), Convert.ToSingle(args.At<double>(1)), Convert.ToSingle(args.At<double>(2)))));
            engine.SetValue("SetGameObjectVelocity",
                new Callable(3, (args) => SetGameObjectVelocity(Convert.ToInt32(args.At<double>(0)), Convert.ToSingle(args.At<double>(1)), Convert.ToSingle(args.At<double>(2)))));
            engine.SetValue("DestroyGameObject",
                new Callable(1, (args) => DestroyGameObject(Convert.ToInt32(args.At<double>(0)))));

            engine.Run(script.text);

            engine.CallFunction("SetupGame");
            gameUpdateFunction = engine.GetValue("Update") as ICallable;
            collisionFunction = engine.GetValue("OnCollision") as ICallable;
        }

        private void Update()
        {
            engine.SetValue("dt", Time.deltaTime);
            engine.CallFunction(gameUpdateFunction);
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
                engine.CallFunction(collisionFunction, arg1ID, arg2ID);
            }
        }

        private void DestroyGameObject(int id)
        {
            if (createdObjects.TryGetValue(id, out var go))
            {
                //delay to ensure it lives the rest of game loop, remove so the script already thinks it's gone
                Destroy(go, 0.1f);
                createdObjects.Remove(id);
            }
        }

        private void SetGameObjectPosition(int id, float x, float y)
        {
            if (createdObjects.TryGetValue(id, out var go))
            {
                go.transform.position = new Vector2(x, y);
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
