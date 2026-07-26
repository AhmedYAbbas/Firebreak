using UnityEngine;

namespace Firebreak
{
    public class FarmerController : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private GameEvent _wideEntered;
        [SerializeField] private GameEvent _wideExited;

        public Vector3 Position => transform.position;
        public bool IsWide { get; private set; }

        private void Update()
        {
            bool wantWide = _input != null && _input.WideHeld;

            if (wantWide && !IsWide)
            {
                IsWide = true;
                _wideEntered?.Raise();
            }
            else if (!wantWide && IsWide)
            {
                IsWide = false;
                _wideExited?.Raise();
            }

            if (IsWide)
                return;

            Vector2 move = _input != null ? _input.MoveInput : Vector2.zero;
            Vector3 delta = new Vector3(move.x, 0f, move.y) * (_moveSpeed * Time.deltaTime);
            transform.position += delta;
        }

        public void ConfigureForTests(InputReader input, GameEvent entered, GameEvent exited, float moveSpeed)
        {
            _input = input;
            _wideEntered = entered;
            _wideExited = exited;
            _moveSpeed = moveSpeed;
        }
    }
}
