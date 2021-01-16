using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//todo factor out of the monobeh
//todo there will be a need to get access to the global from a local environment, so user scripts can 
//  add shared functionality

namespace ULox.Demo
{
    /// <summary>
    /// Creates a local environment for the script to run inside, allowing for multiple objects
    /// to share the same script and treat their vars as local to their script instance, because 
    /// they are.
    /// 
    /// With bind the gameobject is it attached to as 'thisGameObject' inside the local environment
    /// that this script runs in.
    /// 
    /// OnCollision, will be called during OnCollisionEnter
    ///  - if it has 0 params it is always called
    /// </summary>
    public class ULoxBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset script;
        private IEnvironment _ourEnvironment;
        private ICallable _anonymousOnCollision;
        private ULoxSharedEnvironment uLoxSharedEnvironment;

        void Start()
        {
            //find shared ulox instance
            uLoxSharedEnvironment = FindObjectOfType<ULoxSharedEnvironment>();

            BindToScript();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_anonymousOnCollision != null)
                CallInOurEnvironment(_anonymousOnCollision);
        }

        private void BindToScript()
        {
            var engine = uLoxSharedEnvironment.LoxEngine;

            //get our own closure to run in
            _ourEnvironment = engine.Interpreter.PushNewEnvironemnt();
            engine.Run(script.text);
            _ourEnvironment.DefineInAvailableSlot("thisGameObject", gameObject);

            var atSlot = _ourEnvironment.FetchObjectByName("OnCollision");
            if (atSlot is ICallable slotCallable &&
                slotCallable.Arity == 0)
            {
                _anonymousOnCollision = slotCallable;
            }

            engine.Interpreter.PopSpecificEnvironemnt(_ourEnvironment);
        }

        private void CallInOurEnvironment(ICallable callable)
        {
            uLoxSharedEnvironment.LoxEngine.Interpreter.PushEnvironemnt(_ourEnvironment);
            uLoxSharedEnvironment.LoxEngine.CallFunction(callable);
            uLoxSharedEnvironment.LoxEngine.Interpreter.PopSpecificEnvironemnt(_ourEnvironment);
        }
    }
}