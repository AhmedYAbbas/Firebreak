using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    public class FlockController : MonoBehaviour
    {
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private PartRegistry _partRegistry;
        [SerializeField] private Crow _crowPrefab;
        [SerializeField] private int _crowCount = 16;
        [SerializeField] private float _seekRadius = 20f;
        [SerializeField] private float _orbitRadius = 2.5f;
        [SerializeField] private float _crowMoveSpeed = 12f;

        private readonly List<Crow> _crows = new();

        public int CarriedParts { get; private set; }
        public int LivingCrows => _crows.FindAll((c) => c != null && c.State != CrowState.Dead).Count;

        private void Start()
        {
            if (_crows.Count == 0)
                SpawnCrows();
        }

        private void Update()
        {
            bool wide = _farmer != null && _farmer.IsWide;

            for (int i = 0; i < _crows.Count; i++)
            {
                var crow = _crows[i];
                if (crow == null || crow.State == CrowState.Dead)
                    continue;

                crow.SetOrbit(_farmer.Position, OrbitOffset(i));

                if (wide)
                {
                    if (crow.State == CrowState.Orbiting && _partRegistry != null)
                    {
                        var part = _partRegistry.FindNearestUnclaimed(crow.transform.position, _seekRadius);
                        if (part != null)
                            crow.AssignSeek(part);
                    }
                }
                else
                {
                    crow.Recall(_farmer.transform);
                }

                if (!wide && crow.CarryingPart && Vector3.Distance(crow.transform.position, _farmer.Position) <= _orbitRadius + 0.5f)
                {
                    CarriedParts++;
                    ClearCarry(crow);
                }
            }
        }

        private void SpawnCrows()
        {
            for (int i = 0; i < _crowCount; i++)
            {
                Crow crow = _crowPrefab != null ? Instantiate(_crowPrefab, OrbitPoint(i), Quaternion.identity, transform) :
                    new GameObject($"Crow_{i}").AddComponent<Crow>();

                if (_crowPrefab == null)
                    crow.transform.SetParent(transform);
                _crows.Add(crow);
            }
        }

        private Vector3 OrbitPoint(int i)
        {
            float ang = (i / Mathf.Max(1f, _crowCount)) * Mathf.PI * 2f;
            return _farmer.Position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * _orbitRadius;
        }

        private static void ClearCarry(Crow crow)
        {
            crow.SetOrbit(crow.transform.position, Vector3.zero);
            //crow.SendMessage("OnDeposited", SendMessageOptions.DontRequireReceiver);
            crow.ForceDropCarry();
        }

        private Vector3 OrbitOffset(int i)
        {
            float ang = (i / Mathf.Max(1f, (float)_crows.Count)) * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(ang), 0.3f, Mathf.Sin(ang)) * _orbitRadius;
        }

        public int TakeCarriedParts()
        {
            int n = CarriedParts;
            CarriedParts = 0;
            return n;
        }

        public IReadOnlyList<Crow> Crows => _crows;

        public void ConfigureForTests(FarmerController farmer, PartRegistry registry, int crowCount, float seekRadius, float moveSpeed)
        {
            _farmer = farmer;
            _partRegistry = registry;
            _crowCount = crowCount;
            _seekRadius = seekRadius;
            _crowMoveSpeed = moveSpeed;
            _crowPrefab = null;
            SpawnCrows();
        }
    }
}
