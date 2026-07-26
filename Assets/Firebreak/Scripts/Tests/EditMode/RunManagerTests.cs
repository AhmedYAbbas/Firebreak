using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class RunManagerTests
    {
        private RunManager MakeManager(int requiredSprinklers, out GameEvent won, out GameEvent lost)
        {
            won = ScriptableObject.CreateInstance<GameEvent>();
            lost = ScriptableObject.CreateInstance<GameEvent>();
            var curve = ScriptableObject.CreateInstance<EscalationCurve>();
            curve.SetForTests(AnimationCurve.Linear(0f, 0f, 1f, 1f), 100f);
            var rm = new GameObject("RunManager").AddComponent<RunManager>();
            rm.ConfigureForTests(curve, requiredSprinklers, won, lost);
            return rm;
        }

        [Test]
        public void ActivatingAllRequiredSprinklers_Wins()
        {
            var rm = MakeManager(2, out var won, out var lost);
            int wonCount = 0, lostCount = 0;
            won.Register(() => wonCount++);
            lost.Register(() => lostCount++);

            rm.NotifySprinklerActivated();
            Assert.AreEqual(RunPhase.Playing, rm.Phase);
            rm.NotifySprinklerActivated();

            Assert.AreEqual(RunPhase.Won, rm.Phase);
            Assert.AreEqual(1, wonCount);
            Assert.AreEqual(0, lostCount);
        }

        [Test]
        public void FarmerStruck_Loses()
        {
            var rm = MakeManager(2, out var won, out var lost);
            int lostCount = 0;
            lost.Register(() => lostCount++);

            rm.NotifyFarmerStruck();

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
        }

        [Test]
        public void FireReachedHouse_Loses()
        {
            var rm = MakeManager(2, out _, out var lost);
            int lostCount = 0;
            lost.Register(() => lostCount++);

            rm.NotifyFireReachedHouse();

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
        }

        [Test]
        public void AfterLoss_FurtherNotifications_DoNotChangePhaseOrReRaise()
        {
            var rm = MakeManager(1, out var won, out var lost);
            int wonCount = 0, lostCount = 0;
            won.Register(() => wonCount++);
            lost.Register(() => lostCount++);

            rm.NotifyFarmerStruck();
            rm.NotifySprinklerActivated();

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
            Assert.AreEqual(0, wonCount);
        }

        [Test]
        public void Intensity_ReflectsElapsedThroughCurve()
        {
            var rm = MakeManager(1, out _, out _);
            rm.SetElapsedForTests(50f);
            Assert.AreEqual(0.5f, rm.Intensity, 0.01f);
        }
    }
}
