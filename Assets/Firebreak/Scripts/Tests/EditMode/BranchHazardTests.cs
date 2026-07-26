using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class BranchHazardTests
    {
        private BranchHazard MakeHazard()
        {
            var h = new GameObject("Branches").AddComponent<BranchHazard>();
            h.ConfigureForTests(grace: 1.5f, minInterval: 0.4f, maxInterval: 2.5f, strikeRadius: 1.2f);
            return h;
        }

        [Test]
        public void IsHit_TrueWithinStrikeRadius()
        {
            Assert.IsTrue(BranchHazard.IsHit(new Vector3(0.5f, 0f, 0f), Vector3.zero, 1.2f));
            Assert.IsFalse(BranchHazard.IsHit(new Vector3(3f, 0f, 0f), Vector3.zero, 1.2f));
        }

        [Test]
        public void TelegraphInterval_ShorterAtHigherIntensity()
        {
            var h = MakeHazard();
            float low = h.TelegraphInterval(0f);
            float high = h.TelegraphInterval(1f);
            Assert.Greater(low, high);
            Object.DestroyImmediate(h.gameObject);
        }

        [Test]
        public void TelegraphInterval_ClampedToRange()
        {
            var h = MakeHazard();
            Assert.AreEqual(2.5f, h.TelegraphInterval(0f), 0.001f);
            Assert.AreEqual(0.4f, h.TelegraphInterval(1f), 0.001f);
            Object.DestroyImmediate(h.gameObject);
        }
    }
}
