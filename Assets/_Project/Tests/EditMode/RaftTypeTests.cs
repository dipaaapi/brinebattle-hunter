using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class RaftTypeTests
    {
        static ItemDefinition Part(RaftKind kind, ItemType type = ItemType.Raft)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{kind} part";
            i.type = type;
            i.rarity = Rarity.Common;
            i.raftKind = kind;
            return i;
        }

        static List<ItemDefinition> Mix(int fortress, int sky, int beast, int plain)
        {
            var list = new List<ItemDefinition>();
            list.AddRange(Enumerable.Range(0, fortress).Select(_ => Part(RaftKind.Sea)));
            list.AddRange(Enumerable.Range(0, sky).Select(_ => Part(RaftKind.Aerial)));
            list.AddRange(Enumerable.Range(0, beast).Select(_ => Part(RaftKind.Undersea)));
            list.AddRange(Enumerable.Range(0, plain).Select(_ => Part(RaftKind.Plain)));
            return list;
        }

        [Test]
        public void AllPlainIsPlain()
        {
            var t = RaftTypeRules.Classify(Mix(0, 0, 0, 9));
            Assert.AreEqual(RaftKind.Plain, t.Main);
            Assert.IsFalse(t.IsHybrid);
            Assert.IsTrue(t.Bonus.IsNone);
        }

        [Test]
        public void HalfOneKindSetsTheType()
        {
            var t = RaftTypeRules.Classify(Mix(5, 0, 0, 4));
            Assert.AreEqual(RaftKind.Sea, t.Main);
            Assert.IsFalse(t.IsHybrid);
            Assert.AreEqual("Sea-level ship (STR)", t.Name);
        }

        [Test]
        public void LessThanHalfStaysPlain()
        {
            Assert.AreEqual(RaftKind.Plain, RaftTypeRules.Classify(Mix(4, 0, 0, 5)).Main);
        }

        [Test]
        public void TooFewPartsStayPlain()
        {
            // 2 of 3 is two thirds, but a type needs at least 3 parts of its kind.
            Assert.AreEqual(RaftKind.Plain, RaftTypeRules.Classify(Mix(0, 2, 0, 1)).Main);
        }

        [Test]
        public void TwoStrongKindsMakeAHybridWithHalfBonuses()
        {
            var t = RaftTypeRules.Classify(Mix(4, 0, 3, 2));
            Assert.IsTrue(t.IsHybrid);
            Assert.AreEqual(RaftKind.Sea, t.Main);
            Assert.AreEqual(RaftKind.Undersea, t.Second);
            var f = RaftTypeRules.BonusOf(RaftKind.Sea);
            var b = RaftTypeRules.BonusOf(RaftKind.Undersea);
            Assert.AreEqual(f.RaftHpPercent / 2, t.Bonus.RaftHpPercent);
            Assert.AreEqual(b.Attack / 2, t.Bonus.Attack);
            StringAssert.StartsWith("Hybrid", t.Name);
        }

        [Test]
        public void ModulesAndHabitatsDontCount()
        {
            var parts = Mix(3, 0, 0, 3);
            parts.AddRange(Enumerable.Range(0, 6).Select(_ => Part(RaftKind.Plain, ItemType.Module)));
            Assert.AreEqual(RaftKind.Sea, RaftTypeRules.Classify(parts).Main);
        }

        [Test]
        public void SeaPartsRaiseRaftHp()
        {
            var g = new RaftGrid();
            g.FillSquare(Part(RaftKind.Sea), 1);
            // 9 Common parts = 90 HP, +30% for Sea level (STR).
            Assert.AreEqual(117, g.MaxHp);
        }

        [Test]
        public void BonusAppliesToTheHunter()
        {
            var h = RaftTypeRules.Apply(Combatant.Hunter(), RaftTypeRules.BonusOf(RaftKind.Undersea));
            var plain = Combatant.Hunter();
            Assert.AreEqual(plain.Attack + 1, h.Attack);
            Assert.AreEqual(plain.Speed + 3, h.Speed, "Undersea (DEX) is the fastest");
        }

        [Test]
        public void SpeedNeverDropsBelowOne()
        {
            var slow = Combatant.Hunter();
            slow.Speed = 1;
            Assert.AreEqual(1, RaftTypeRules.Apply(slow, new RaftBonus { Speed = -3 }).Speed);
        }

        [Test]
        public void BonusTextReadsClearly()
        {
            Assert.AreEqual("+30% raft HP, +2 ATK, +2 DEF", RaftTypeRules.BonusOf(RaftKind.Sea).ToString());
            Assert.AreEqual("no bonus", default(RaftBonus).ToString());
        }
    }
}
