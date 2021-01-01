using UnityEngine;

namespace ULox.Demo
{
    public class CollisionReroute : MonoBehaviour
    {
        public System.Action<Collision, GameObject> rereouteDestination;

        private void OnCollisionEnter(Collision collision)
        {
            rereouteDestination?.Invoke(collision, gameObject);
        }
    }
}
