using System;
using BrineBattle.Battle;
using BrineBattle.Encounters;
using BrineBattle.Items;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class ReelGameTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void ZoneNarrowsWithRarityAndSizeAndWidensWithBonus()
        {
            Assert.Greater(ReelGame.ZoneWidth(1, Rarity.Common), ReelGame.ZoneWidth(1, Rarity.Epic));
            Assert.Greater(ReelGame.ZoneWidth(1, Rarity.Common), ReelGame.ZoneWidth(4, Rarity.Common));
            Assert.Greater(ReelGame.ZoneWidth(2, Rarity.Rare, 1f, ReelGame.Bonus(10, true)), ReelGame.ZoneWidth(2, Rarity.Rare));
            Assert.AreEqual(ReelGame.MinZone, ReelGame.ZoneWidth(9, Rarity.Mythic, 0.6f), 1e-5f);
            Assert.Greater(ReelGame.CatchSpeed(3, Rarity.Legendary), ReelGame.CatchSpeed(1, Rarity.Common));
        }

        [Test]
        public void HoldingLiftsTheZoneAndLettingGoSinksIt()
        {
            var g = new ReelGame(0.2f, 0f, BattleMode.Manual, new Random(1));
            float start = g.Zone;
            for (int i = 0; i < 20; i++) g.Step(Dt, true, false);
            Assert.Greater(g.Zone, start);
            float high = g.Zone;
            for (int i = 0; i < 60; i++) g.Step(Dt, false, false);
            Assert.Less(g.Zone, high);
        }

        [Test]
        public void CalmCatchKeptInsideIsCaughtCleanInAFewSeconds()
        {
            // A slow catch with a wide zone, followed closely: the meter fills in a couple of seconds.
            var g = new ReelGame(0.4f, 0.1f, BattleMode.Auto, new Random(2));
            float time = 0f;
            while (!g.Done && time < 10f) { g.Step(Dt, false, false); time += Dt; }
            Assert.IsTrue(g.Caught);
            Assert.IsTrue(g.Clean);
            Assert.That(time, Is.InRange(1.5f, 4f));
        }

        [Test]
        public void DoingNothingLetsItSlipAway()
        {
            var g = new ReelGame(0.12f, 0.6f, BattleMode.Manual, new Random(3));
            float time = 0f;
            while (!g.Done && time < 30f) { g.Step(Dt, false, false); time += Dt; }
            Assert.IsTrue(g.Escaped);
        }

        [Test]
        public void AutoPlaysItForYou()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var g = new ReelGame(ReelGame.ZoneWidth(2, Rarity.Rare), ReelGame.CatchSpeed(2, Rarity.Rare), BattleMode.Auto, new Random(seed));
                float time = 0f;
                while (!g.Done && time < 30f) { g.Step(Dt, false, false); time += Dt; }
                Assert.IsTrue(g.Caught, $"seed {seed}");
            }
        }
    }
}
