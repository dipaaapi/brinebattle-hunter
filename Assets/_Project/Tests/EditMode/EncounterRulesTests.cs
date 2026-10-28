using System;
using BrineBattle.Encounters;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class EncounterRulesTests
    {
        [Test]
        public void RollMatchesWeights()
        {
            var rng = new Random(3);
            int[] counts = new int[3];
            int n = 20000;
            for (int i = 0; i < n; i++) counts[(int)EncounterRules.RollState(5, 3, 2, rng)]++;
            Assert.That(counts[0] / (float)n, Is.InRange(0.48f, 0.52f));
            Assert.That(counts[1] / (float)n, Is.InRange(0.28f, 0.32f));
            Assert.That(counts[2] / (float)n, Is.InRange(0.18f, 0.22f));
        }

        [Test]
        public void SingleWeightAlwaysWins()
        {
            var rng = new Random(1);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(DriftState.Alive, EncounterRules.RollState(0, 0, 1, rng));
        }

        [Test]
        public void ZeroWeightsFallBackToDead()
        {
            Assert.AreEqual(DriftState.Dead, EncounterRules.RollState(0, -1, 0, new Random(1)));
        }

        [Test]
        public void PerfectHintIsAlwaysTrue()
        {
            var rng = new Random(9);
            foreach (DriftState s in Enum.GetValues(typeof(DriftState)))
                for (int i = 0; i < 50; i++)
                    Assert.AreEqual(s, EncounterRules.Hint(s, 1f, rng));
        }

        [Test]
        public void ZeroAccuracyHintIsAlwaysWrong()
        {
            var rng = new Random(9);
            foreach (DriftState s in Enum.GetValues(typeof(DriftState)))
                for (int i = 0; i < 50; i++)
                    Assert.AreNotEqual(s, EncounterRules.Hint(s, 0f, rng));
        }

        [Test]
        public void HintAccuracyRoughlyMatches()
        {
            var rng = new Random(5);
            int right = 0, n = 10000;
            for (int i = 0; i < n; i++)
                if (EncounterRules.Hint(DriftState.Weakened, 0.7f, rng) == DriftState.Weakened) right++;
            Assert.That(right / (float)n, Is.InRange(0.68f, 0.72f));
        }

        [TestCase(1, 1, true)]
        [TestCase(3, 2, true)]
        [TestCase(1, 3, false)]
        public void CanFightBySize(int raft, int monster, bool expected)
        {
            Assert.AreEqual(expected, EncounterRules.CanFight(raft, monster));
        }

        [Test]
        public void OnlyAliveIsGated()
        {
            Assert.IsTrue(EncounterRules.IsGated(DriftState.Alive, 1, 3));
            Assert.IsFalse(EncounterRules.IsGated(DriftState.Weakened, 1, 3));
            Assert.IsFalse(EncounterRules.IsGated(DriftState.Dead, 1, 3));
            Assert.IsFalse(EncounterRules.IsGated(DriftState.Alive, 3, 3));
        }

        [Test]
        public void AggressionDecidesAutoKillAndStaysInRange()
        {
            Assert.IsTrue(EncounterRules.IsAggressive(0.5f));
            Assert.IsFalse(EncounterRules.IsAggressive(0.49f));
            var rng = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                float a = EncounterRules.RollAggression(0.95f, rng);
                Assert.That(a, Is.InRange(0.8f, 1f));
            }
        }
    }
}
