using System;
using BrineBattle.Battle;
using BrineBattle.Encounters;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class TensionTests
    {
        [Test]
        public void ZoneShrinksAndSpeedGrowsWithSize()
        {
            Assert.Greater(TensionRules.ZoneWidth(1), TensionRules.ZoneWidth(3));
            Assert.AreEqual(TensionRules.MinZone, TensionRules.ZoneWidth(50));
            Assert.Greater(TensionRules.Speed(5), TensionRules.Speed(1));
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(0.5f, 1f, 0.5f)]
        [TestCase(1.5f, 1f, 0.5f)]
        [TestCase(2f, 1f, 0f)]
        public void MarkerBounces(float time, float speed, float expected)
        {
            Assert.AreEqual(expected, TensionRules.Marker(time, speed), 1e-5f);
        }

        [Test]
        public void MarkerStaysInBar()
        {
            for (float t = 0f; t < 20f; t += 0.037f)
                Assert.That(TensionRules.Marker(t, 1.7f), Is.InRange(0f, 1f));
        }

        [Test]
        public void HitInsideZoneOnly()
        {
            Assert.IsTrue(TensionRules.IsHit(0.5f, 0.4f, 0.2f));
            Assert.IsFalse(TensionRules.IsHit(0.7f, 0.4f, 0.2f));
        }

        [Test]
        public void ZoneStartFitsBar()
        {
            var rng = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                float w = TensionRules.ZoneWidth(1);
                float s = TensionRules.RandomZoneStart(w, rng);
                Assert.That(s, Is.InRange(0f, 1f - w));
            }
        }

        static Combatant Mon(int spd) => new Combatant { Name = "Mon", MaxHp = 999, Hp = 999, Attack = 1, Speed = spd };

        [Test]
        public void CleanReelLetsHunterStrikeFirstAgainstFasterMonster()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(99), 90, new Random(1)) { FirstRound = Initiative.Hunter };
            s.PlayRound(BattleAction.Attack);
            StringAssert.StartsWith("Attack", s.Log[0]);
        }

        [Test]
        public void BadReelLetsMonsterStrikeFirstAgainstSlowerMonster()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(1), 90, new Random(1)) { FirstRound = Initiative.Monster };
            s.PlayRound(BattleAction.Attack);
            StringAssert.StartsWith("Mon", s.Log[0]);
        }

        [Test]
        public void InitiativeOnlyAffectsRoundOne()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(1), 90, new Random(1)) { FirstRound = Initiative.Monster };
            s.PlayRound(BattleAction.Attack);
            int before = s.Log.Count;
            s.PlayRound(BattleAction.Attack);
            StringAssert.StartsWith("Attack", s.Log[before]);
        }
    }
}
