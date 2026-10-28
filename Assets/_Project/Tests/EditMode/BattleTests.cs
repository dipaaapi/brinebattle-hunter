using System;
using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Monsters;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class BattleTests
    {
        static Combatant Mon(int hp = 30, int atk = 6, int def = 2, int spd = 5, Habitat hab = Habitat.Sea, Element el = Element.None) =>
            new Combatant { Name = "Mon", MaxHp = hp, Hp = hp, Attack = atk, Defense = def, Speed = spd, Habitat = hab, Element = el };

        [Test]
        public void DamageIsAtLeastOne()
        {
            Assert.AreEqual(1, BattleRules.Damage(1, 100, 1f, Element.None, Element.None, false, new Random(1)));
        }

        [Test]
        public void ElementAndBraceScaleDamage()
        {
            int neutral = BattleRules.Damage(20, 0, 1f, Element.Water, Element.Earth, false, new Random(1));
            int strong = BattleRules.Damage(20, 0, 1f, Element.Water, Element.Fire, false, new Random(1));
            int braced = BattleRules.Damage(20, 0, 1f, Element.Water, Element.Earth, true, new Random(1));
            Assert.Greater(strong, neutral);
            Assert.Less(braced, neutral);
        }

        [Test]
        public void HarpoonCostsStaminaAndFallsBackWhenEmpty()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 999), 90, new Random(1));
            Assert.IsTrue(s.CanUse(BattleAction.Harpoon));
            s.PlayRound(BattleAction.Harpoon);
            Assert.AreEqual(BattleRules.StartStamina - BattleRules.HarpoonCost + BattleRules.StaminaPerRound, s.Stamina);
            Assert.IsFalse(s.CanUse(BattleAction.Harpoon));
        }

        [Test]
        public void StaminaNeverExceedsMax()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 999, atk: 0), 999, new Random(1));
            for (int i = 0; i < 10; i++) s.PlayRound(BattleAction.Brace);
            Assert.AreEqual(BattleRules.MaxStamina, s.Stamina);
        }

        [Test]
        public void HunterWinsAgainstWeakMonster()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 10, atk: 1), 90, new Random(2));
            for (int i = 0; i < 20 && s.Outcome == BattleOutcome.Ongoing; i++) s.PlayRound(BattleAction.Attack);
            Assert.AreEqual(BattleOutcome.Won, s.Outcome);
        }

        [Test]
        public void SeaMonstersMostlyHitRaftSkyMonstersMostlyHitHunter()
        {
            float RaftShare(Habitat hab)
            {
                var hunter = Combatant.Hunter();
                hunter.MaxHp = hunter.Hp = 1_000_000;
                hunter.Defense = 0;
                var s = new BattleState(hunter, Mon(hp: 1_000_000, atk: 10, hab: hab), 1_000_000, new Random(4));
                for (int i = 0; i < 2000; i++) s.PlayRound(BattleAction.Attack);
                float raftDmg = s.RaftMaxHp - s.RaftHp, hunterDmg = hunter.MaxHp - hunter.Hp;
                return raftDmg / (raftDmg + hunterDmg);
            }
            Assert.That(RaftShare(Habitat.Sea), Is.InRange(0.65f, 0.75f));
            Assert.That(RaftShare(Habitat.Sky), Is.InRange(0.25f, 0.35f));
        }

        [Test]
        public void NoRoundsAfterBattleEnds()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 1), 90, new Random(1));
            s.PlayRound(BattleAction.Attack);
            Assert.AreEqual(BattleOutcome.Won, s.Outcome);
            Assert.IsFalse(s.PlayRound(BattleAction.Attack));
            Assert.AreEqual(1, s.Round);
        }

        [Test]
        public void FasterMonsterActsFirst()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 1, atk: 5, spd: 99), 90, new Random(1));
            s.PlayRound(BattleAction.Attack);
            StringAssert.StartsWith("Mon", s.Log[0]);
        }

        [Test]
        public void ManualAlwaysWaits()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new Random(1));
            Assert.IsNull(BattleAI.Choose(s, BattleMode.Manual));
        }

        [Test]
        public void SemiAutoWaitsOnImportantTurns()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 999, atk: 0), 90, new Random(1));
            Assert.IsNull(BattleAI.Choose(s, BattleMode.SemiAuto), "harpoon ready");
            s.PlayRound(BattleAction.Harpoon);
            Assert.AreEqual(BattleAction.Attack, BattleAI.Choose(s, BattleMode.SemiAuto));
            s.Hunter.Hp = 5;
            Assert.IsNull(BattleAI.Choose(s, BattleMode.SemiAuto), "low hp");
        }

        [Test]
        public void AutoAlwaysPicksAndFinishesBattle()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new Random(3));
            for (int i = 0; i < 100 && s.Outcome == BattleOutcome.Ongoing; i++)
            {
                var a = BattleAI.Choose(s, BattleMode.Auto);
                Assert.IsTrue(a.HasValue);
                s.PlayRound(a.Value);
            }
            Assert.AreNotEqual(BattleOutcome.Ongoing, s.Outcome);
        }
    }
}
