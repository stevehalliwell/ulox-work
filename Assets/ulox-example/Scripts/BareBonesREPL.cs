﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ULox.Demo
{
    /// <summary>
    /// This demo uses lox with REPL with unity ui, with now other frills.
    /// </summary>
    public class BareBonesREPL : MonoBehaviour
    {
        public InputField inputField;

        //text item output
        private Engine engine;

        private void Start()
        {
            engine = new Engine(
                new LoxCoreLibrary(Debug.Log),
                new StandardClassesLibrary(),
                new UnityFunctionsLibrary(new System.Collections.Generic.List<GameObject>()));

            inputField.onEndEdit.AddListener(RunREPLStep);
            StartCoroutine(Reselect());
        }

        private void RunREPLStep(string arg0)
        {
            try
            {
                engine.RunREPL(arg0, Debug.Log);
                inputField.text = string.Empty;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }

            StartCoroutine(Reselect());
        }

        private IEnumerator Reselect()
        {
            yield return null;
            yield return null;
            inputField.ActivateInputField();
        }
    }
}
