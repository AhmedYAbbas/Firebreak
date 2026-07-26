using UnityEngine;

namespace Firebreak
{
    public class Sprinkler : MonoBehaviour
    {
        [SerializeField] private int _requiredParts = 2;
        [SerializeField] private GameEvent _onActivated;

        public int RequiredParts => _requiredParts;
        public int Deposited { get; private set; }
        public bool IsActive { get; private set; }

        public bool Deposit(int count)
        {
            if (IsActive)
                return false;

            Deposited += Mathf.Max(0, count);
            if (Deposited >= _requiredParts)
            {
                IsActive = true;
                _onActivated?.Raise();
                return true;
            }

            return false;
        }

        public void ConfigureForTests(int required, GameEvent onActivated)
        {
            _requiredParts = required;
            _onActivated = onActivated;
        }
    }
}
