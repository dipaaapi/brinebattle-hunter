using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class CompanionTests
    {
        static MonsterDefinition Monster(string id = "eel", Rarity rarity = Rarity.Common)
        {
            var m = ScriptableObject.CreateInstance<MonsterDefinition>();
            m.id = id;
            m.displayName = id;
            m.rarity = rarity;
            m.stats = new MonsterStats { hp = 30, attack = 6, defense = 2, speed = 5 };
            return m;
        }

        static Combatant Target(int hp = 999, int atk = 0) =>
            new Combatant { Name = "Mon", MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1, Habitat = Habitat.Sea };

        [Test]
        public void TamedMonsterKeepsAShareOfItsStats()
        {
            var c = Companion.FromMonster(Monster());
            Assert.AreEqual(18, c.MaxHp);
            Assert.AreEqual(4, c.Attack);
            Assert.AreEqual(1, c.Defense);
            Assert.AreEqual(5, c.Speed, "speed is kept");
        }

        [Test]
        public void TameChanceDropsWithRarity()
        {
            for (var r = Rarity.Common; r < Rarity.Mythic; r++)
                Assert.Greater(CompanionRules.TameChance(r), CompanionRules.TameChance(r + 1));
        }

        [Test]
        public void RosterHasRoomForOnePlusHabitats()
        {
            var roster = new CompanionRoster();
            var a = Companion.FromMonster(Monster("a"));
            var b = Companion.FromMonster(Monster("b"));
            Assert.IsTrue(roster.Add(a));
            Assert.AreEqual(a, roster.Active, "first one is active");
            Assert.IsTrue(roster.Full);
            Assert.IsFalse(roster.Add(b), "no habitat yet");
            roster.Habitats = 1;
            Assert.IsTrue(roster.Add(b));
            Assert.IsTrue(roster.SetActive(b));
            Assert.IsTrue(roster.Release(b));
            Assert.AreEqual(a, roster.Active, "falls back to the one left");
            Assert.IsTrue(roster.Release(a));
            Assert.IsNull(roster.Active);
        }

        [Test]
        public void CompanionAttacksEachRound()
        {
            var alone = new BattleState(Combatant.Hunter(), Target(), 90, new System.Random(1));
            var pair = new BattleState(Combatant.Hunter(), Target(), 90, new System.Random(1));
            pair.Companion = Combatant.FromCompanion(Companion.FromMonster(Monster()));
            for (int i = 0; i < 3; i++) { alone.PlayRound(BattleAction.Attack); pair.PlayRound(BattleAction.Attack); }
            Assert.Less(pair.Monster.Hp, alone.Monster.Hp);
            Assert.IsTrue(pair.Log.Any(l => l.StartsWith("eel attacks:")));
        }

        [Test]
        public void CompanionCanBeKnockedOutAndStopsAttacking()
        {
            var s = new BattleState(Combatant.Hunter(), Target(atk: 200), 9999, new System.Random(3));
            s.Hunter.MaxHp = s.Hunter.Hp = 99999;
            s.Companion = Combatant.FromCompanion(Companion.FromMonster(Monster()));
            for (int i = 0; i < 40 && s.Companion.Alive; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsFalse(s.Companion.Alive, "the monster went for it at some point");
            int before = s.Log.Count(l => l.StartsWith("eel attacks:"));
            s.PlayRound(BattleAction.Brace);
            Assert.AreEqual(before, s.Log.Count(l => l.StartsWith("eel attacks:")));
        }
    }
}
