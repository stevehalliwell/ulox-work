using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ULox
{
    public class UnityFunctions : ILoxEngineLibraryBinder
    {
        private List<GameObject> _availablePrefabs;
     
        public UnityFunctions() { }
        
        public UnityFunctions(List<GameObject> availablePrefabs)
        {
            _availablePrefabs = availablePrefabs;
        }

        public void BindToEngine(Engine engine)
        {
            engine.SetValue("Rand", 
                new Callable(() => UnityEngine.Random.value));
            engine.SetValue("RandRange", 
                new Callable(2, (args) => UnityEngine.Random.Range((float)(double)args[0], (float)(double)args[1])));
            engine.SetValue("GetKey", 
                new Callable(1, (args) => UnityEngine.Input.GetKey((string)args[0])));
            engine.SetValue("DestroyUnityObject", 
                new Callable(1, (args) => UnityEngine.Object.Destroy((UnityEngine.Object)args[0])));
            engine.SetValue("ReloadScene", 
                new Callable(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
            engine.SetValue("CreateFromPrefab", 
                new Callable(1, (args) => CreateFromPrefab((string)args[0])));
            engine.SetValue("SetGameObjectPosition",
                new Callable(3, (args) =>
                {
                    Transform trans = null;
                    if(args[0] is GameObject go)
                        trans = go.transform;
                    else if (args[0] is Component comp)
                        trans = comp.transform;

                    trans.position = new Vector3(
                            Convert.ToSingle(args[1]),
                            Convert.ToSingle(args[2]),
                            args.Length > 3 ? Convert.ToSingle(args[3]) : 0);
                }));
        }

        private GameObject CreateFromPrefab(string name)
        {
            var loc = _availablePrefabs.Find(x => x.name == name);
            if (loc != null)
            {
                return UnityEngine.Object.Instantiate(loc);
            }
            return null;
        }
    }
}
