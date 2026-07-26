using UnityEngine;

namespace Firebreak
{
    public enum CrowState { Orbiting, Seeking, Carrying, Dead }

    public class Crow : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 12f;
        [SerializeField] private float _turnLerp = 6f;
        [SerializeField] private float _arriveDistance = 0.4f;
        [SerializeField] private float _wanderNoise = 0.6f;

        public CrowState State { get; private set; } = CrowState.Orbiting;
        public bool CarryingPart { get; private set; }

        private Vector3 _orbitCenter;
        private Vector3 _orbitOffset;
        private Part _targetPart;
        private Transform _returnTo;
        private Vector3 _velocity;
        private float _noiseSeed;

        private void Awake() => _noiseSeed = Random.value * 100f;

        private void Update()
        {
            if (State == CrowState.Dead)
                return;

            Vector3 target = ResolveTarget();
            SteerToward(target);

            if (State == CrowState.Seeking && _targetPart != null && Flat(transform.position, _targetPart.Position) <= _arriveDistance)
            {
                CarryingPart = true;
                _targetPart.Collected = true;
                _targetPart.gameObject.SetActive(false);
                _targetPart = null;
                State = CrowState.Carrying;
            }
        }

        public void SetOrbit(Vector3 center, Vector3 offset)
        {
            // Only updates the orbit anchor. State transitions (Orbiting) are
            // owned by the field initializer, Recall, and ForceDropCarry — this
            // must NOT reset state, or it would clobber Seeking every frame.
            _orbitCenter = center;
            _orbitOffset = offset;
        }

        public void AssignSeek(Part part)
        {
            if (State == CrowState.Dead || part == null)
                return;

            _targetPart = part;
            part.Claimed = true;
            State = CrowState.Seeking;
        }

        public void Recall(Transform returnTo)
        {
            if (State == CrowState.Dead)
                return;

            _returnTo = returnTo;
            if (State == CrowState.Seeking)
            {
                if (_targetPart != null && !CarryingPart)
                    _targetPart.Claimed = false;

                _targetPart = null;
                State = CrowState.Orbiting;
            }
        }

        public void Kill()
        {
            State = CrowState.Dead;
            if (_targetPart != null && !CarryingPart)
                _targetPart.Claimed = false;

            gameObject.SetActive(false);
        }

        public void ForceDropCarry()
        {
            CarryingPart = false;
            if (State == CrowState.Carrying)
                State = CrowState.Orbiting;
        }

        private Vector3 ResolveTarget()
        {
            switch (State)
            {
                case CrowState.Seeking when _targetPart != null:
                    return _targetPart.Position;
                case CrowState.Carrying:
                    return _returnTo != null ? _returnTo.position : _orbitCenter + _orbitOffset;
                default:
                    Vector3 nudge = new Vector3(Mathf.PerlinNoise(_noiseSeed, Time.time) - 0.5f, 0f, Mathf.PerlinNoise(Time.time, _noiseSeed) - 0.5f) * _wanderNoise;
                    return _orbitCenter + _orbitOffset + nudge;
            }
        }

        private void SteerToward(Vector3 target)
        {
            Vector3 to = target - transform.position;
            Vector3 desired = to.sqrMagnitude > 0.0001f ? to.normalized * _moveSpeed : Vector3.zero;
            _velocity = Vector3.Lerp(_velocity, desired, _turnLerp * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;
            if (_velocity.sqrMagnitude > 0.01f)
                transform.forward = _velocity.normalized;
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
