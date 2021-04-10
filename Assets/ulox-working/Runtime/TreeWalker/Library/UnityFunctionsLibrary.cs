using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace ULox
{
    public class UnityFunctionsLibrary : ILoxEngineLibraryBinder
    {
        private List<GameObject> _availablePrefabs;

        public UnityFunctionsLibrary(List<GameObject> availablePrefabs)
        {
            _availablePrefabs = availablePrefabs;
        }

        public void BindToEngine(Engine engine)
        {
            engine.SetValue("Rand", 
                new Callable(() => UnityEngine.Random.value));
            engine.SetValue("RandRange", 
                new Callable(2, (args) => UnityEngine.Random.Range(
                    Convert.ToSingle(args.At<double>(0)),
                    Convert.ToSingle(args.At<double>(1)))));
            engine.SetValue("GetKey", 
                new Callable(1, (args) => UnityEngine.Input.GetKey(args.At<string>(0))));
            engine.SetValue("DestroyUnityObject", 
                new Callable(1, (args) => UnityEngine.Object.Destroy(args.At<UnityEngine.Object>(0))));
            engine.SetValue("ReloadScene", 
                new Callable(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
            engine.SetValue("CreateFromPrefab", 
                new Callable(1, (args) => CreateFromPrefab(args.At<string>(0))));
            engine.SetValue("SetGameObjectPosition",
                new Callable(4, (args) =>
                {
                    Transform trans = null;
                    var arg0 = args.At(0);
                    if (arg0 is GameObject go)
                        trans = go.transform;
                    else if (arg0 is Component comp)
                        trans = comp.transform;

                    if(trans == null)
                        throw new LibraryException($"Unable to SetGameObjectPosition in '{nameof(UnityFunctionsLibrary)}'. " +
                            $"Provided arg 0 is not a gameobject or component: '{arg0?.ToString() ?? "null"}'.");

                    trans.position = new Vector3(
                            Convert.ToSingle(args.At<double>(1)),
                            Convert.ToSingle(args.At<double>(2)),
                            Convert.ToSingle(args.At<double>(3)));
                }));
        }

        private GameObject CreateFromPrefab(string name)
        {
            var loc = _availablePrefabs.Find(x => x.name == name);
            if (loc != null)
                return UnityEngine.Object.Instantiate(loc);

            throw new LibraryException($"Unable to find prefab in '{nameof(UnityFunctionsLibrary)}' named '{name}'.");
        }
    }
}
