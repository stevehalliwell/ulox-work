﻿using System.Collections.Generic;
using ULox.ByteCode;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    /// <summary>
    /// This demo uses bytecode lox to move objects, unity just draws things.
    /// </summary>
    public class BouncingBallsByteCodeSystem : MonoBehaviour
    {
        public TextAsset script;
        public Text text;
        private ByteCodeInterpreterEngine engine;
        private Value gameUpdateFunction;
        public List<GameObject> availablePrefabs;

        private void Start()
        {
            engine = new ByteCodeInterpreterEngine();

            engine.AddLibrary(new ByteCodeCoreLibrary(Debug.Log));
            engine.AddLibrary(new ByteCodeStandardClassesLibrary());
            engine.AddLibrary(new UnityByteCodeLibrary(availablePrefabs));

            engine.VM.SetGlobal("SetUIText", Value.New((vm, args) =>
            {
                text.text = vm.GetArg(1).val.asString;
                return Value.Null();
            }));

            engine.Run(script.text);

            engine.VM.CallFunction(engine.VM.GetGlobal("SetupGame"),0);
            gameUpdateFunction = engine.VM.GetGlobal("Update");

            Debug.Log(engine.Disassembly);
        }

        private void Update()
        {
            engine.VM.SetGlobal("dt", Value.New(Time.deltaTime));
            engine.VM.CallFunction(gameUpdateFunction,0);
        }
    }
}
