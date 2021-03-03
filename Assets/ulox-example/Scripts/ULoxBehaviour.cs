using UnityEngine;

namespace ULox.Demo
{
    /// <summary>
    /// With bind the gameobject is it attached to as 'thisGameObject' inside the local environment
    /// that this script runs in.
    ///
    /// OnCollision, will be called during OnCollisionEnter
    ///  - if it has 0 params it is always called
    /// </summary>
    public class ULoxBehaviour : MonoBehaviour
    {
        [SerializeField] private TextAsset script;
        private ICallable _anonymousOnCollision;
        private ICallable _gameUpdateFunction;
        private ULoxScriptEnvironment _uLoxScriptEnvironment;

        private void Start()
        {
            _uLoxScriptEnvironment = new ULoxScriptEnvironment(
                FindObjectOfType<ULoxSharedEnvironment>().Engine);

            BindToScript();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_anonymousOnCollision != null)
                _uLoxScriptEnvironment.CallFunction(_anonymousOnCollision);
        }

        private void BindToScript()
        {
            _uLoxScriptEnvironment.RunScript(script.text);

            //get our own closure to run in
            _uLoxScriptEnvironment.LocalEnvironemnt.DefineInAvailableSlot("thisGameObject", gameObject);

            var atSlot = _uLoxScriptEnvironment.FetchLocalByName("OnCollision");
            if (atSlot is ICallable slotCallable &&
                slotCallable.Arity == 0)
            {
                _anonymousOnCollision = slotCallable;
            }
            _gameUpdateFunction = _uLoxScriptEnvironment.FetchLocalByName("Update") as ICallable;
        }

        private void Update()
        {
            if (_gameUpdateFunction != null)
            {
                _uLoxScriptEnvironment.AssignLocalByName("dt", Time.deltaTime);
                _uLoxScriptEnvironment.CallFunction(_gameUpdateFunction);
            }
        }
    }
}
