using System;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class AbsorptionTests
    {
        static ItemDefinition Part(ItemType type = ItemType.Raft, string name = "Plank")
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = name.ToLowerInvariant();
            i.displayName = name;
            i.type = type;
            i.rarity = Rarity.Common;
            return i;
        }

        static RaftGrid Starter()
        {
            var g = new RaftGrid();
            g.FillSquare(Part(), 1);
            return g;
        }

        [Test]
        public void EatsAnEdgePartNeverTheCentre()
        {
            var g = Starter();
            var edible = g.EdibleParts();
            Assert.AreEqual(8, edible.Count);
            Assert.IsFalse(edible.Contains(new Cell(0, 0)));
            var part = g.Absorb(new System.Random(1), out var eaten);
            Assert.IsNotNull(part);
            Assert.AreEqual(8, g.Count);
            Assert.IsFalse(g.Has(eaten));
        }

        [Test]
        public void NeverSplitsTheRaft()
        {
            // A line of 7: only the two ends can go; anything in the middle would split it.
            var g = new RaftGrid();
            var plank = Part();
            g.Restore(Enumerable.Range(-3, 7).Select(x => new System.Collections.Generic.KeyValuePair<Cell, ItemDefinition>(new Cell(x, 0), plank)), Rarity.Common);
            CollectionAssert.AreEquivalent(new[] { new Cell(-3, 0), new Cell(3, 0) }, g.EdibleParts());
        }

        [Test]
        public void SmallRaftIsNeverEaten()
        {
            var g = new RaftGrid();
            var plank = Part();
            g.Restore(Enumerable.Range(0, AbsorbRules.MinParts).Select(x => new System.Collections.Generic.KeyValuePair<Cell, ItemDefinition>(new Cell(x, 0), plank)), Rarity.Common);
            Assert.IsNull(g.Absorb(new System.Random(1), out _));
            Assert.AreEqual(AbsorbRules.MinParts, g.Count);
        }

        [Test]
        public void ModulesAndHabitatsAreNotEaten()
        {
            var g = Starter();
            var module = Part(ItemType.Module, "Net");
            g.Restore(g.Parts.Select(kv => new System.Collections.Generic.KeyValuePair<Cell, ItemDefinition>(kv.Key, kv.Key.X == -1 ? module : kv.Value)).ToList(), Rarity.Common);
            Assert.IsTrue(g.EdibleParts().All(c => c.X != -1));
        }

        [Test]
        public void GorgedMonsterGrowsWithEachPart()
        {
            var m = new Combatant { Name = "Eel", MaxHp = 30, Hp = 30, Attack = 6, Defense = 2, Speed = 5 };
            var g = AbsorbRules.Gorge(m, 2);
            Assert.AreEqual("Gorged Eel", g.Name);
            Assert.AreEqual(42, g.MaxHp);
            Assert.AreEqual(42, g.Hp);
            Assert.AreEqual(8, g.Attack);
            Assert.AreEqual(5, g.Speed);
            Assert.AreEqual(2f, AbsorbRules.StatScale(99), 1e-4);
        }

        [Test]
        public void LogKeepsPartsPerMonsterAndGivesThemBack()
        {
            var log = new GorgedLog();
            var a = Part(name: "A");
            var b = Part(name: "B");
            log.Add("eel", a);
            log.Add("eel", b);
            log.Add("gull", a);
            Assert.AreEqual(2, log.Count);
            Assert.AreEqual(2, log.PartsIn("eel"));
            CollectionAssert.AreEqual(new[] { a, b }, log.Recover("eel"));
            Assert.AreEqual(0, log.PartsIn("eel"));
            Assert.AreEqual(1, log.Count);
            Assert.IsEmpty(log.Recover("eel"));
        }

        [Test]
        public void ReturningMonsterIsTheOldestOrNone()
        {
            var log = new GorgedLog();
            Assert.IsNull(log.PickReturning(new System.Random(1)));
            log.Add("eel", Part());
            log.Add("gull", Part());
            int seen = 0;
            var rng = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                var e = log.PickReturning(rng);
                if (e == null) continue;
                Assert.AreEqual("eel", e.MonsterId);
                seen++;
            }
            Assert.That(seen, Is.InRange(30, 90));
        }

        [Test]
        public void BonusRollsMatchPartsEaten()
        {
            Assert.AreEqual(3, AbsorbRules.BonusRolls(3));
            Assert.AreEqual(0, AbsorbRules.BonusRolls(-1));
        }
    }
}
