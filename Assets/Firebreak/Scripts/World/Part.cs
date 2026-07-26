using UnityEngine;

namespace Firebreak
{
    public class Part : MonoBehaviour
    {
        public bool Claimed { get; set; }
        public bool Collected { get; set; }
        public Vector3 Position => transform.position;

        private PartRegistry _registry;

        private void OnEnable()
        {
            _registry = FindAnyObjectByType<PartRegistry>();
            _registry?.Add(this);
        }

        private void OnDisable() => _registry?.Remove(this);
    }
}
