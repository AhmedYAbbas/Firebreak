using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class FireRingTests
    {
        private FireRing MakeRing(float start, float house)
        {
            var r = new GameObject("Fire").AddComponent<FireRing>();
            r.transform.position = Vector3.zero; // map center
            r.ConfigureForTests(start, house);
            return r;
        }

        [Test]
        public void SafeRadius_ShrinksFromStartToHouse_AsIntensityRises()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0f);
            Assert.AreEqual(50f, r.CurrentSafeRadius, 0.01f);
            r.SetIntensityForTests(1f);
            Assert.AreEqual(5f, r.CurrentSafeRadius, 0.01f);
            Object.DestroyImmediate(r.gameObject);
        }

        [Test]
        public void IsCaught_TrueOutsideSafeRadius()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0.5f); // radius = 27.5
            Assert.IsFalse(r.IsCaught(new Vector3(10f, 0f, 0f)));
            Assert.IsTrue(r.IsCaught(new Vector3(40f, 0f, 0f)));
            Object.DestroyImmediate(r.gameObject);
        }

        [Test]
        public void IsCaught_IgnoresHeight()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0f); // radius 50
            Assert.IsFalse(r.IsCaught(new Vector3(3f, 20f, 3f)));
            Object.DestroyImmediate(r.gameObject);
        }
    }
}
