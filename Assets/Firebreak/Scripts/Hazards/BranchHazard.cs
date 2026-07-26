using UnityEngine;

namespace Firebreak
{
    /// <summary>Falling-branch danger. Active only while the farmer is rooted in
    /// Wide past the grace period. Telegraph → strike around the farmer's fixed
    /// position; a hit is an instant fail. Frequency scales with intensity.</summary>
    public class BranchHazard : MonoBehaviour
    {
        [SerializeField] private RunManager _run;
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private GameEvent _farmerStruck;
        [SerializeField] private GameObject _telegraphPrefab; // optional visual

        [SerializeField] private float _gracePeriod = 1.5f;
        [SerializeField] private float _minInterval = 0.4f;   // at intensity 1
        [SerializeField] private float _maxInterval = 2.5f;   // at intensity 0
        [SerializeField] private float _telegraphDelay = 0.8f;
        [SerializeField] private float _spawnRadius = 3.5f;
        [SerializeField] private float _strikeRadius = 1.2f;

        private float _heldTime;
        private float _nextTelegraphIn;
        private bool _strikePending;
        private float _strikeIn;
        private Vector3 _pendingStrikePos;
        private GameObject _activeTelegraph;

        public static bool IsHit(Vector3 strikePos, Vector3 farmerPos, float strikeRadius)
        {
            strikePos.y = 0f;
            farmerPos.y = 0f;
            return Vector3.Distance(strikePos, farmerPos) <= strikeRadius;
        }

        public float TelegraphInterval(float intensity)
        {
            return Mathf.Lerp(_maxInterval, _minInterval, Mathf.Clamp01(intensity));
        }

        private void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing)
                return;

            bool rooted = _farmer != null && _farmer.IsWide;
            if (!rooted)
            {
                ResetState();
                return;
            }

            _heldTime += Time.deltaTime;
            if (_heldTime < _gracePeriod)
                return; // short commands are always safe

            if (_strikePending)
            {
                _strikeIn -= Time.deltaTime;
                if (_strikeIn <= 0f)
                {
                    _strikePending = false;
                    if (_activeTelegraph != null)
                        Destroy(_activeTelegraph);

                    if (IsHit(_pendingStrikePos, _farmer.Position, _strikeRadius))
                        _farmerStruck?.Raise();
                }
                return;
            }

            _nextTelegraphIn -= Time.deltaTime;
            if (_nextTelegraphIn <= 0f)
            {
                Vector2 r = Random.insideUnitCircle * _spawnRadius;
                _pendingStrikePos = _farmer.Position + new Vector3(r.x, 0f, r.y);
                _strikePending = true;
                _strikeIn = _telegraphDelay;
                _nextTelegraphIn = TelegraphInterval(_run.Intensity);
                if (_telegraphPrefab != null)
                    _activeTelegraph = Instantiate(_telegraphPrefab, _pendingStrikePos, Quaternion.identity);
            }
        }

        private void ResetState()
        {
            _heldTime = 0f;
            _nextTelegraphIn = 0f;
            _strikePending = false;
            if (_activeTelegraph != null)
                Destroy(_activeTelegraph);
        }

        public void ConfigureForTests(float grace, float minInterval, float maxInterval, float strikeRadius)
        {
            _gracePeriod = grace;
            _minInterval = minInterval;
            _maxInterval = maxInterval;
            _strikeRadius = strikeRadius;
        }
    }
}
