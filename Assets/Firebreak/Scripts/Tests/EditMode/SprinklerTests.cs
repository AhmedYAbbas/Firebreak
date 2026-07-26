using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class SprinklerTests
    {
        private Sprinkler MakeSprinkler(int required, GameEvent evt)
        {
            var s = new GameObject("Sprinkler").AddComponent<Sprinkler>();
            s.ConfigureForTests(required, evt);
            return s;
        }

        [Test]
        public void Deposit_BelowRequired_DoesNotActivate()
        {
            var s = MakeSprinkler(2, ScriptableObject.CreateInstance<GameEvent>());
            bool activated = s.Deposit(1);
            Assert.IsFalse(activated);
            Assert.IsFalse(s.IsActive);
        }

        [Test]
        public void Deposit_ReachingRequired_ActivatesOne_AndRaisesEvent()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int raised = 0;
            evt.Register(() => raised++);
            var s = MakeSprinkler(2, evt);

            s.Deposit(1);
            bool activated = s.Deposit(1);

            Assert.IsTrue(activated);
            Assert.IsTrue(s.IsActive);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Deposit_AfterActive_ReturnsFalse_AndDoesNotRaiseAgain()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int raised = 0;
            evt.Register(() => raised++);
            var s = MakeSprinkler(1, evt);

            s.Deposit(1);
            bool again = s.Deposit(1);

            Assert.IsFalse(again);
            Assert.AreEqual(1, raised);
        }
    }
}
