using UnityEngine;

namespace Firebreak
{
    /// <summary>The advancing fire. Modeled as an unburned interior circle
    /// centered on the map that shrinks as intensity rises. A crow is "caught"
    /// when it is outside that interior while the flock is Wide.</summary>
    public class FireRing : MonoBehaviour
    {
        [SerializeField] private RunManager _run;
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private FlockController _flock;
        [SerializeField] private GameEvent _crowDied;
        [SerializeField] private GameEvent _fireReachedHouse;

        [SerializeField] private float _startRadius = 50f;
        [SerializeField] private float _houseRadius = 5f;

        private float _testIntensity = -1f;
        private bool _houseReached;

        private float Intensity =>
            _testIntensity >= 0f ? _testIntensity : (_run != null ? _run.Intensity : 0f);

        public float CurrentSafeRadius =>
            Mathf.Lerp(_startRadius, _houseRadius, Mathf.Clamp01(Intensity));

        public bool IsCaught(Vector3 worldPos)
        {
            Vector3 c = transform.position;
            c.y = 0f;
            worldPos.y = 0f;
            return Vector3.Distance(worldPos, c) > CurrentSafeRadius;
        }

        private void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing)
                return;

            if (!_houseReached && CurrentSafeRadius <= _houseRadius + 0.001f)
            {
                _houseReached = true;
                _fireReachedHouse?.Raise();
                return;
            }

            if (_farmer != null && _farmer.IsWide && _flock != null)
            {
                foreach (var crow in _flock.Crows)
                {
                    if (crow == null || crow.State == CrowState.Dead)
                        continue;

                    if (IsCaught(crow.transform.position))
                    {
                        crow.Kill();
                        _crowDied?.Raise();
                    }
                }
            }
        }

        public void ConfigureForTests(float startRadius, float houseRadius)
        {
            _startRadius = startRadius;
            _houseRadius = houseRadius;
        }

        public void SetIntensityForTests(float intensity) => _testIntensity = intensity;
    }
}
