using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Firebreak.Tests
{
    public class FarmerControllerTests
    {
        private static (FarmerController farmer, InputReader input) MakeFarmer(out GameEvent entered, out GameEvent exited)
        {
            entered = ScriptableObject.CreateInstance<GameEvent>();
            exited = ScriptableObject.CreateInstance<GameEvent>();
            var go = new GameObject("Farmer");
            var input = go.AddComponent<InputReader>();
            var farmer = go.AddComponent<FarmerController>();
            farmer.ConfigureForTests(input, entered, exited, moveSpeed: 5f);
            return (farmer, input);
        }

        [UnityTest]
        public IEnumerator MovesInTight_WhenNotWide()
        {
            var (farmer, input) = MakeFarmer(out _, out _);
            input.SetForTests(new Vector2(1f, 0f), wide: false);
            Vector3 start = farmer.Position;

            yield return null;
            yield return null;

            Assert.Greater((farmer.Position - start).magnitude, 0f);
            Object.Destroy(farmer.gameObject);
        }

        [UnityTest]
        public IEnumerator RootedInWide_DoesNotMove_AndRaisesEnter()
        {
            var (farmer, input) = MakeFarmer(out var entered, out _);
            int enters = 0;
            entered.Register(() => enters++);
            input.SetForTests(new Vector2(1f, 0f), wide: true);
            yield return null;
            Vector3 afterEnter = farmer.Position;

            yield return null;
            yield return null;

            Assert.IsTrue(farmer.IsWide);
            Assert.AreEqual(afterEnter.x, farmer.Position.x, 0.0001f);
            Assert.AreEqual(1, enters);
            Object.Destroy(farmer.gameObject);
        }

        [UnityTest]
        public IEnumerator ReleasingWide_RaisesExit_AndMovesAgain()
        {
            var (farmer, input) = MakeFarmer(out _, out var exited);
            int exits = 0;
            exited.Register(() => exits++);
            input.SetForTests(Vector2.right, wide: true);
            yield return null;
            input.SetForTests(Vector2.right, wide: false);
            yield return null;
            Vector3 start = farmer.Position;
            yield return null;
            yield return null;

            Assert.AreEqual(1, exits);
            Assert.Greater((farmer.Position - start).magnitude, 0f);
            Object.Destroy(farmer.gameObject);
        }
    }
}
