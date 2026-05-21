using UnityEngine;
using OceanFactory.Core;

namespace OceanFactory.Generation
{
    public class RandomProvider : MonoBehaviour
    {
        public System.Random Rng { get; private set; }

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<RandomProvider>();
        }

        public void Seed(int seed)
        {
            Rng = seed == 0 ? new System.Random() : new System.Random(seed);
        }
    }
}
