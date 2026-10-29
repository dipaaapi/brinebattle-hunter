using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class RaftGridTests
    {
        static ItemDefinition Part(Rarity r, ItemType t = ItemType.Raft)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{r} part";
            i.type = t;
            i.rarity = r;
            return i;
        }

        static RaftGrid Starter()
        {
            var g = new RaftGrid();
            g.FillSquare(Part(Rarity.Common), 1);
            return g;
        }

        [Test]
        public void StarterIsNinePartsSizeOne()
        {
            var g = Starter();
            Assert.AreEqual(9, g.Count);
            Assert.AreEqual(1, g.Size);
            Assert.AreEqual(90, g.MaxHp);
            Assert.AreEqual(1, g.Radius);
        }

        [TestCase(9, 1)]
        [TestCase(12, 1)]
        [TestCase(13, 2)]
        [TestCase(17, 3)]
        [TestCase(81, 10)]
        [TestCase(1, 1)]
        public void SizeThresholds(int parts, int size)
        {
            Assert.AreEqual(size, RaftRules.SizeFor(parts));
        }

        [Test]
        public void PlaceRules()
        {
            var g = Starter();
            var p = Part(Rarity.Common);
            Assert.AreEqual(PlaceResult.Occupied, g.Place(new Cell(0, 0), p));
            Assert.AreEqual(PlaceResult.NotAdjacent, g.Place(new Cell(3, 3), p));
            Assert.AreEqual(PlaceResult.OutOfBounds, g.Place(new Cell(9, 0), p));
            Assert.AreEqual(PlaceResult.NotARaftPart, g.Place(new Cell(2, 0), Part(Rarity.Common, ItemType.Weapon)));
            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(2, 0), p));
            Assert.AreEqual(10, g.Count);
        }

        [Test]
        public void FourPartsGrowTheRaft()
        {
            var g = Starter();
            var p = Part(Rarity.Common);
            for (int z = -1; z <= 2; z++) Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(2, z), p));
            Assert.AreEqual(2, g.Size);
        }

        [Test]
        public void RemoveKeepsRaftConnected()
        {
            var g = Starter();
            var p = Part(Rarity.Common);
            g.Place(new Cell(2, 0), p);
            g.Place(new Cell(3, 0), p);
            Assert.AreEqual(RemoveResult.WouldSplit, g.CanRemove(new Cell(2, 0)));
            Assert.AreEqual(RemoveResult.Ok, g.Remove(new Cell(3, 0), out var removed));
            Assert.AreSame(p, removed);
            Assert.AreEqual(RemoveResult.Empty, g.CanRemove(new Cell(3, 0)));
        }

        [Test]
        public void CannotRemoveLastPart()
        {
            var g = new RaftGrid();
            g.Place(new Cell(0, 0), Part(Rarity.Common));
            Assert.AreEqual(RemoveResult.LastPart, g.CanRemove(new Cell(0, 0)));
        }

        [Test]
        public void OpenCellsAreTheRing()
        {
            Assert.AreEqual(12, System.Linq.Enumerable.Count(Starter().OpenCells()));
        }

        [Test]
        public void UpgradeCostsEssenceAndBlocksLowParts()
        {
            var g = Starter();
            var inv = new Inventory();
            Assert.IsFalse(g.TryUpgrade(inv));

            var crab = Part(Rarity.Rare);
            crab.element = Element.Earth;
            inv.Add(crab, 10);
            inv.Salvage(crab, 10); // 30 Rare essence
            inv.Add(Part(Rarity.Common, ItemType.Material), 6);
            Assert.IsFalse(g.TryUpgrade(inv), "no kills yet");
            g.Kills = RaftRules.KillsToUpgrade(Rarity.Common);
            Assert.IsTrue(g.TryUpgrade(inv));
            Assert.AreEqual(Rarity.Uncommon, g.Tier);
            Assert.AreEqual(20, inv.EssenceAtLeast(Rarity.Common));
            Assert.AreEqual(1, inv.MaterialCount(), "5 materials spent");
            Assert.AreEqual(0, g.Kills, "kills count again from the upgrade");
            Assert.AreEqual(PlaceResult.BelowTier, g.Place(new Cell(2, 0), Part(Rarity.Common)));
            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(2, 0), Part(Rarity.Uncommon)));
            Assert.Greater(g.MaxHp, 90);
        }

        [Test]
        public void UpgradeSaysWhatIsMissing()
        {
            var g = Starter();
            var inv = new Inventory();
            g.Kills = 2;
            string why = g.UpgradeBlocker(inv);
            StringAssert.Contains("kill 3 more monsters", why);
            StringAssert.Contains("5 more monster materials", why);
            StringAssert.Contains("10 more Common+ essence", why);
            Assert.AreEqual(10, RaftRules.KillsToUpgrade(Rarity.Uncommon));
            Assert.AreEqual(10, RaftRules.MaterialsToUpgrade(Rarity.Uncommon));
        }

        [Test]
        public void HalfTheLeftEdgeNeedsModulesBeforeTheRaftGrows()
        {
            var g = Starter();
            g.ModulesToGrow = RaftRules.ModuleShareToGrow;
            var plank = Part(Rarity.Common);
            var net = Part(Rarity.Common, ItemType.Module);
            Assert.AreEqual(2, g.ModulesNeeded, "3 rows: half rounds up to 2");
            Assert.AreEqual(PlaceResult.NeedsModules, g.Place(new Cell(2, 0), plank));

            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(-2, 0), net), "modules can always go on");
            Assert.AreEqual(PlaceResult.NeedsModules, g.CanPlace(new Cell(2, 0), plank), "1 of 3 rows");
            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(-2, 1), net));
            Assert.AreEqual((2, 3), g.UpstreamModules());
            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(2, 0), plank));

            // A module on the right doesn't guard the left edge.
            var h = Starter();
            h.ModulesToGrow = RaftRules.ModuleShareToGrow;
            h.Place(new Cell(2, 0), net);
            h.Place(new Cell(2, 1), net);
            Assert.IsFalse(h.ModulesReady);
            StringAssert.Contains("2 more auto-loot modules on the left edge", h.UpgradeBlocker(new Inventory()));
        }

        [Test]
        public void GuideShowsWhereLeftEdgeModulesGo()
        {
            var g = Starter();
            var net = Part(Rarity.Common, ItemType.Module);
            CollectionAssert.AreEqual(new[] { new Cell(-2, 0), new Cell(-2, -1), new Cell(-2, 1) }, g.UpstreamGaps(), "middle row first");
            g.Place(new Cell(-2, 0), net);
            CollectionAssert.AreEqual(new[] { new Cell(-2, -1), new Cell(-2, 1) }, g.UpstreamGaps());
        }

        [Test]
        public void PartHpScalesWithRarityAndTier()
        {
            Assert.AreEqual(10, RaftRules.PartHp(Rarity.Common, Rarity.Common));
            Assert.AreEqual(30, RaftRules.PartHp(Rarity.Rare, Rarity.Common));
            Assert.AreEqual(12, RaftRules.PartHp(Rarity.Common, Rarity.Uncommon));
            Assert.AreEqual(0, RaftRules.UpgradeCost(Rarity.Mythic));
        }

        [Test]
        public void SpendEssenceIsAllOrNothingAndRespectsRarity()
        {
            var inv = new Inventory();
            var common = Part(Rarity.Common);
            var rare = Part(Rarity.Rare);
            inv.Add(common, 5); inv.Salvage(common, 5); // 5 Common
            inv.Add(rare, 2); inv.Salvage(rare, 2);     // 6 Rare
            Assert.IsFalse(inv.SpendEssence(Rarity.Uncommon, 7));
            Assert.AreEqual(11, inv.EssenceAtLeast(Rarity.Common));
            Assert.IsTrue(inv.SpendEssence(Rarity.Common, 7)); // 5 Common first, then 2 Rare
            Assert.AreEqual(4, inv.EssenceAtLeast(Rarity.Common));
            Assert.AreEqual(4, inv.EssenceAtLeast(Rarity.Rare));
        }
    }
}
