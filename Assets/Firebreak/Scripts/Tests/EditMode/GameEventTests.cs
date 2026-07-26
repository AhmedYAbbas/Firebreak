using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class GameEventTests
    {
        [Test]
        public void Raise_InvokesAllRegisteredListeners()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int a = 0, b = 0;
            System.Action la = () => a++;
            System.Action lb = () => b++;
            evt.Register(la);
            evt.Register(lb);

            evt.Raise();

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void Unregister_StopsDelivery()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int count = 0;
            System.Action l = () => count++;
            evt.Register(l);
            evt.Unregister(l);

            evt.Raise();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Register_IsDempotent()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int count = 0;
            System.Action l = () => count++;
            evt.Register(l);
            evt.Register(l);

            evt.Raise();

            Assert.AreEqual(1, count);
        }
    }
}
