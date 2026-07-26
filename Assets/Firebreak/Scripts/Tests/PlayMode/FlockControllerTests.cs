using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Firebreak.Tests
{
    public class FlockControllerTests
    {
        [UnityTest]
        public IEnumerator Crow_SeekingReachesPart_BecomesCarrying()
        {
            var crow = new GameObject("Crow").AddComponent<Crow>();
            crow.transform.position = Vector3.zero;
            var partGo = new GameObject("Part");
            partGo.transform.position = new Vector3(1f, 0f, 0f);
            var part = partGo.AddComponent<Part>();

            crow.AssignSeek(part);
            Assert.AreEqual(CrowState.Seeking, crow.State);

            float timeout = 3f;
            while (crow.State == CrowState.Seeking && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(CrowState.Carrying, crow.State);
            Assert.IsTrue(crow.CarryingPart);
            Assert.IsTrue(part.Claimed);

            Object.Destroy(crow.gameObject);
            Object.Destroy(partGo);
        }

        [UnityTest]
        public IEnumerator Kill_SetsDeadState()
        {
            var crow = new GameObject("Crow").AddComponent<Crow>();
            crow.Kill();
            yield return null;
            Assert.AreEqual(CrowState.Dead, crow.State);
            Object.Destroy(crow.gameObject);
        }

        [UnityTest]
        public IEnumerator Wide_AssignsPartToCrow_ThenRecallReturnsCarried()
        {
            var registry = new GameObject("Registry").AddComponent<PartRegistry>();
            var partGo = new GameObject("Part");
            partGo.transform.position = new Vector3(2f, 0f, 0f);
            var part = partGo.AddComponent<Part>();
            registry.Add(part);

            var farmerGo = new GameObject("Farmer");
            var input = farmerGo.AddComponent<InputReader>();
            var farmer = farmerGo.AddComponent<FarmerController>();
            farmer.ConfigureForTests(input, ScriptableObject.CreateInstance<GameEvent>(), ScriptableObject.CreateInstance<GameEvent>(), 5f);

            var flock = farmerGo.AddComponent<FlockController>();
            flock.ConfigureForTests(farmer, registry, crowCount: 1, seekRadius: 20f, moveSpeed: 12f);

            input.SetForTests(Vector2.zero, wide: true);

            float timeout = 4f;
            while (flock.CarriedParts == 0 && flock.LivingCrows > 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            input.SetForTests(Vector2.zero, wide: false);
            yield return null;

            Assert.GreaterOrEqual(flock.CarriedParts, 1);

            Object.Destroy(farmerGo);
            Object.Destroy(registry.gameObject);
            Object.Destroy(partGo);
        }
    }
}
