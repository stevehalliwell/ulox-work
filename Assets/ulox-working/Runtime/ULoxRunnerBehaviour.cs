using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ULox
{
    public class ULoxRunnerBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset text;

        void Start()
        {
            var interp = new Interpreter(Debug.Log);
            var loxEngine = new LoxEngine(
                new Scanner(Debug.Log),
                new Parser(),
                new Resolver(interp),
                interp,
                Debug.Log); ;

            loxEngine.Run(text.text);
        }
    }
}