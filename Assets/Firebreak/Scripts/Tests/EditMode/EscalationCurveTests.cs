using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class EscalationCurveTests
    {
        private static EscalationCurve LinearCurve(float runLength)
        {
            var c = ScriptableObject.CreateInstance<EscalationCurve>();
            c.SetForTests(AnimationCurve.Linear(0f, 0f, 1f, 1f), runLength);
            return c;
        }

        [Test]
        public void Evaluate_AtStart_IsZer()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(0f, c.Evaluate(0f), 0.001f);
        }

        [Test]
        public void Evaluate_AtEnd_IsOne()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(1f, c.Evaluate(300f), 0.001f);
        }

        [Test]
        public void Evaluate_PastEnd_ClampsToOne()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(1f, c.Evaluate(999f), 0.001f);
        }

        [Test]
        public void Evaluate_InMonotonicNonDecreasing_ForRisingCurve()
        {
            var c = LinearCurve(300f);
            float prev = -1f;
            for (float t = 0f; t <= 300f; t += 15f)
            {
                float v = c.Evaluate(t);
                Assert.GreaterOrEqual(v, prev);
                prev = v;
            }
        }
    }
}
