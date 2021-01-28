using UnityEngine;

namespace ULox.Demo
{
    public class CreateBouncingBalls : MonoBehaviour
    {
        [SerializeField] private GameObject bouncingBallPrefab;

        private void Start()
        {
            var numBallsToSpawn = 100;

            for (var i = 0; i < numBallsToSpawn; i += 1)
            {
                Instantiate(bouncingBallPrefab);
            }
        }
    }
}
