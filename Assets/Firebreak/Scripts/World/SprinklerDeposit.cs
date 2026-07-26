using UnityEngine;

namespace Firebreak
{
    /// <summary>Trigger volume on a sprinkler. When the farmer enters carrying
    /// parts (via the flock), deposits them into the sprinkler.</summary>
    [RequireComponent(typeof(Sprinkler))]
    public class SprinklerDeposit : MonoBehaviour
    {
        [SerializeField] private FlockController _flock;
        private Sprinkler _sprinkler;

        private void Awake() => _sprinkler = GetComponent<Sprinkler>();

        private void OnTriggerEnter(Collider other) => TryDeposit(other.GetComponentInParent<FarmerController>());

        private void OnTriggerStay(Collider other) => TryDeposit(other.GetComponentInParent<FarmerController>());

        private void TryDeposit(FarmerController farmer)
        {
            if (farmer == null || _flock == null || _sprinkler.IsActive)
                return;

            if (_flock.CarriedParts <= 0)
                return;

            _sprinkler.Deposit(_flock.TakeCarriedParts());
        }
    }
}
