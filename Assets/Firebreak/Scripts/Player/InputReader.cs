using UnityEngine;
using UnityEngine.InputSystem;

namespace Firebreak
{
    public class InputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _wide;

        private bool _overridden;
        private Vector2 _testMove;
        private bool _testWide;

        public Vector2 MoveInput => _overridden ? _testMove :
            (_move != null && _move.action != null ? _move.action.ReadValue<Vector2>() : Vector2.zero);

        public bool WideHeld => _overridden ? _testWide :
            (_wide != null && _wide.action != null && _wide.action.IsPressed());

        public void OnEnable()
        {
            _move?.action?.Enable();
            _wide?.action?.Enable();
        }

        private void OnDisable()
        {
            _move?.action?.Disable();
            _wide?.action?.Disable();
        }

        public void SetForTests(Vector2 move, bool wide)
        {
            _overridden = true;
            _testMove = move;
            _testWide = wide;
        }
    }
}
