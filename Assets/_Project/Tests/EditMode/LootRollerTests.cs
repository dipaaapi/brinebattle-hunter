using System;
using BrineBattle.Loot;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class LootRollerTests
    {
        [Test]
        public void GuaranteedEntryAlwaysDropsWithinQuantity()
        {
            var rng = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                var drops = LootRoller.Roll(new[] { ("scale", 1f, 2, 4) }, rng);
                Assert.AreEqual(1, drops.Count);
                Assert.That(drops[0].Quantity, Is.InRange(2, 4));
            }
        }

        [Test]
        public void ZeroChanceAndNullNeverDrop()
        {
            var drops = LootRoller.Roll(new[] { ("scale", 0f, 1, 1), ((string)null, 1f, 1, 1) }, new Random(1));
            Assert.IsEmpty(drops);
        }

        [Test]
        public void ChanceRoughlyMatchesRate()
        {
            var rng = new Random(42);
            int hits = 0, n = 10000;
            for (int i = 0; i < n; i++)
                hits += LootRoller.Roll(new[] { ("fang", 0.25f, 1, 1) }, rng).Count;
            Assert.That(hits / (float)n, Is.InRange(0.23f, 0.27f));
        }

        [Test]
        public void SameSeedSameResult()
        {
            var entries = new[] { ("a", 0.5f, 1, 3), ("b", 0.5f, 1, 3) };
            var x = LootRoller.Roll(entries, new Random(7));
            var y = LootRoller.Roll(entries, new Random(7));
            Assert.AreEqual(x.Count, y.Count);
            for (int i = 0; i < x.Count; i++)
            {
                Assert.AreEqual(x[i].Item, y[i].Item);
                Assert.AreEqual(x[i].Quantity, y[i].Quantity);
            }
        }

        [Test]
        public void BadRangeIsFixed()
        {
            var drops = LootRoller.Roll(new[] { ("x", 1f, 0, -5) }, new Random(1));
            Assert.AreEqual(1, drops[0].Quantity);
        }
    }
}
