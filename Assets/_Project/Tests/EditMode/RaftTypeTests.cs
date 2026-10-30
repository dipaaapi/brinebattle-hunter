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
            list.AddRange(Enumerable.Range(0, fortress).Select(_ => Part(RaftKind.Fortress)));
            list.AddRange(Enumerable.Range(0, sky).Select(_ => Part(RaftKind.Sky)));
            list.AddRange(Enumerable.Range(0, beast).Select(_ => Part(RaftKind.Beast)));
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
            Assert.AreEqual(RaftKind.Fortress, t.Main);
            Assert.IsFalse(t.IsHybrid);
            Assert.AreEqual("Fortress", t.Name);
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
            Assert.AreEqual(RaftKind.Fortress, t.Main);
            Assert.AreEqual(RaftKind.Beast, t.Second);
            var f = RaftTypeRules.BonusOf(RaftKind.Fortress);
            var b = RaftTypeRules.BonusOf(RaftKind.Beast);
            Assert.AreEqual(f.RaftHpPercent / 2, t.Bonus.RaftHpPercent);
            Assert.AreEqual(b.Attack / 2, t.Bonus.Attack);
            StringAssert.StartsWith("Hybrid", t.Name);
        }

        [Test]
        public void ModulesAndHabitatsDontCount()
        {
            var parts = Mix(3, 0, 0, 3);
            parts.AddRange(Enumerable.Range(0, 6).Select(_ => Part(RaftKind.Plain, ItemType.Module)));
            Assert.AreEqual(RaftKind.Fortress, RaftTypeRules.Classify(parts).Main);
        }

        [Test]
        public void FortressRaisesRaftHp()
        {
            var g = new RaftGrid();
            g.FillSquare(Part(RaftKind.Fortress), 1);
            // 9 Common parts = 90 HP, +40% for a Fortress raft.
            Assert.AreEqual(126, g.MaxHp);
        }

        [Test]
        public void BonusAppliesToTheHunter()
        {
            var h = RaftTypeRules.Apply(Combatant.Hunter(), RaftTypeRules.BonusOf(RaftKind.Beast));
            var plain = Combatant.Hunter();
            Assert.AreEqual(plain.Attack + 4, h.Attack);
            Assert.AreEqual(plain.Speed + 1, h.Speed);
        }

        [Test]
        public void SpeedNeverDropsBelowOne()
        {
            var slow = Combatant.Hunter();
            slow.Speed = 1;
            Assert.AreEqual(1, RaftTypeRules.Apply(slow, RaftTypeRules.BonusOf(RaftKind.Fortress)).Speed);
        }

        [Test]
        public void BonusTextReadsClearly()
        {
            Assert.AreEqual("+40% raft HP, +3 DEF, −1 SPD", RaftTypeRules.BonusOf(RaftKind.Fortress).ToString());
            Assert.AreEqual("no bonus", default(RaftBonus).ToString());
        }
    }
}
