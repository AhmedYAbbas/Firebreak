using UnityEngine;

namespace Firebreak
{
    public class FarmerController : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 720f;
        [SerializeField] private Transform _cameraTransform;
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
            move = Vector2.ClampMagnitude(move, 1f);
            Vector3 dir = MoveDirection(move);

            transform.position += dir * (_moveSpeed * Time.deltaTime);

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, _turnSpeed * Time.deltaTime);
            }
        }

        // Input mapped onto the ground plane, relative to the camera's facing when
        // one is assigned. Falls back to world axes (W = +Z) so headless tests hold.
        private Vector3 MoveDirection(Vector2 move)
        {
            if (_cameraTransform == null)
                return new Vector3(move.x, 0f, move.y);

            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            return camRight * move.x + camForward * move.y;
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
