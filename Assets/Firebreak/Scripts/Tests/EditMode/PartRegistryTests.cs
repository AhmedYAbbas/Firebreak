using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Firebreak.Tests
{
    public class PartRegistryTests
    {
        private PartRegistry _registry;
        private readonly System.Collections.Generic.List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            _registry = new GameObject("Registry").AddComponent<PartRegistry>();
            _spawned.Add(_registry.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                Object.DestroyImmediate(go);

            _spawned.Clear();
        }

        private Part MakePart(Vector3 pos)
        {
            var go = new GameObject("Part");
            go.transform.position = pos;
            _spawned.Add(go);
            return go.AddComponent<Part>();
        }

        [Test]
        public void FindNearestUnclaimed_ReturnsClosestWithinRadius()
        {
            _registry.Add(MakePart(new Vector3(5f, 0f, 0f)));
            var near = MakePart(new Vector3(1f, 0f, 0f));
            _registry.Add(near);

            var result =  _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.AreSame(near, result);
        }

        [Test]
        public void FinNearestUnclaimed_IgnoresClaimed()
        {
            var near = MakePart(new Vector3(1f, 0f, 0f));
            near.Claimed = true;
            _registry.Add(near);
            var far = MakePart(new Vector3(4f, 0f, 0f));
            _registry.Add(far);

            var result = _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.AreSame(far, result);
        }

        [Test]
        public void FindNearestUnclaimeed_ReturnsNull_WhenNoneInRadius()
        {
            _registry.Add(MakePart(new Vector3(50f, 0f, 0f)));

            var result = _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.IsNull(result);
        }

        [Test]
        public void UnclaimedCount_ExcludesClaimedAndCollected()
        {
            var p1 = MakePart(Vector3.zero);
            _registry.Add(p1);

            var p2 = MakePart(Vector3.one);
            p2.Claimed = true;
            _registry.Add(p2);

            var p3 = MakePart(Vector3.up);
            p3.Collected = true;
            _registry.Add(p3);
            
            Assert.AreEqual(1, _registry.UnclaimedCount);
        }
    }
}
