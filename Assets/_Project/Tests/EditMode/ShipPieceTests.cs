using System.Collections.Generic;
using System.Linq;
using BrineBattle.Hunting;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's ask: the raft turns into a boat through real pieces that minions craft and install.</summary>
    public class ShipPieceTests
    {
        static ItemDefinition Item(ShipPiece p)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = p.Id; i.displayName = p.Name; i.type = ItemType.ShipPiece; i.rarity = p.Rarity;
            return i;
        }

        [Test]
        public void PiecesOpenGraduallyFromLv2To49()
        {
            var all = ShipPieceRules.All;
            Assert.AreEqual(22, all.Length);
            Assert.AreEqual(all.Length, all.Select(p => p.Id).Distinct().Count(), "ids are unique");
            Assert.AreEqual(2, all.First().Level);
            Assert.Less(all.Last().Level, EvolutionRules.RaftMax);
            for (int i = 1; i < all.Length; i++)
            {
                int gap = all[i].Level - all[i - 1].Level;
                Assert.That(gap, Is.InRange(2, 3), $"{all[i].Name}: one new piece every 2-3 levels");
            }
            foreach (var p in all) Assert.IsFalse(p.Bonus.IsNone, $"{p.Name} gives a stat");
        }

        [Test]
        public void RarityFollowsTheBand()
        {
            Assert.AreEqual(Rarity.Common, ShipPieceRules.RarityAt(2));
            Assert.AreEqual(Rarity.Common, ShipPieceRules.RarityAt(10));
            Assert.AreEqual(Rarity.Uncommon, ShipPieceRules.RarityAt(11));
            Assert.AreEqual(Rarity.Rare, ShipPieceRules.RarityAt(25));
            Assert.AreEqual(Rarity.Epic, ShipPieceRules.RarityAt(40));
            Assert.AreEqual(Rarity.Legendary, ShipPieceRules.RarityAt(49));
        }

        [Test]
        public void NoPieceGoesOnADeckCell()
        {
            // The deck is where people walk: pieces go on the rim (flat), the sides, the ends, under it or up the mast.
            foreach (var p in ShipPieceRules.All)
                Assert.IsTrue(System.Enum.IsDefined(typeof(PieceSlot), p.Slot), p.Name);
            Assert.AreEqual(PieceSlot.Hull, ShipPieceRules.Find("piece_keel").Slot);
            Assert.AreEqual(PieceSlot.Stern, ShipPieceRules.Find("piece_small_cabin").Slot);
        }

        [Test]
        public void MissingAndNextFollowTheLevel()
        {
            var on = new List<string>();
            Assert.AreEqual(0, ShipPieceRules.Missing(1, on).Count, "nothing is open at Lv 1");
            Assert.AreEqual("piece_lashing_rope", ShipPieceRules.Next(1, on).Id);
            Assert.AreEqual(3, ShipPieceRules.Missing(6, on).Count, "rope, outrigger, arms");
            on.AddRange(ShipPieceRules.FreeFor(6));
            Assert.AreEqual(0, ShipPieceRules.Missing(6, on).Count);
            Assert.AreEqual("piece_deck_trim", ShipPieceRules.Next(6, on).Id);
            Assert.AreEqual(22, ShipPieceRules.FreeFor(EvolutionRules.RaftMax).Count(), "an old Lv 50 save gets the whole set");
            Assert.AreEqual(EvolutionRules.RaftMax, ShipPieceRules.RaftLevel(true, 3), "a ship has passed every piece");
        }

        [Test]
        public void InstallRulesAndGrowth()
        {
            var keel = ShipPieceRules.Find("piece_keel");
            StringAssert.Contains("Opens at raft Lv 20", ShipPieceRules.InstallBlocker(keel, 19, new string[0]));
            Assert.IsNull(ShipPieceRules.InstallBlocker(keel, 20, new string[0]));
            StringAssert.Contains("already", ShipPieceRules.InstallBlocker(keel, 30, new[] { keel.Id }));
            Assert.Greater(ShipPieceRules.Growth(keel, 20), 0f, "shows as soon as it's on");
            Assert.Less(ShipPieceRules.Growth(keel, 20), ShipPieceRules.Growth(keel, 35), "and grows a little every level");
            Assert.AreEqual(1f, ShipPieceRules.Growth(keel, 50));
            var b = ShipPieceRules.Bonus(new[] { "piece_keel", "piece_yard_sail", "piece_low_wall" });
            Assert.AreEqual(3, b.Speed);
            Assert.AreEqual(2, b.Defense);
        }

        [Test]
        public void RecipesUnlockLittleByLittle()
        {
            var keel = ShipPieceRules.Find("piece_keel");
            StringAssert.Contains("Opens at raft Lv 18", ShipPieceRules.MakeBlocker(keel, 10, new string[0]));
            Assert.IsNull(ShipPieceRules.MakeBlocker(keel, 18, new string[0]), "2 levels early, so it's ready in time");
            StringAssert.Contains("Already", ShipPieceRules.MakeBlocker(keel, 30, new[] { keel.Id }));
            Assert.AreEqual(1, ShipPieceRules.All.Count(p => ShipPieceRules.MakeBlocker(p, 1, new string[0]) == null), "at Lv 1 only the first piece can be made");
        }

        [Test]
        public void TheRaftWaitsForItsPiecesAndInstallsThem()
        {
            var plank = ScriptableObject.CreateInstance<ItemDefinition>();
            plank.type = ItemType.Raft;
            var g = new RaftGrid { NeedsPieces = true };
            g.FillSquare(plank, 1);
            g.RestoreEvolution(RaftKind.Plain, 2);
            var bag = new Inventory();
            StringAssert.Contains("install the Lashing Rope", g.UpgradeBlocker(bag));
            var rope = Item(ShipPieceRules.Find("piece_lashing_rope"));
            StringAssert.Contains("Workbench", g.PieceBlocker(rope, bag));
            bag.Add(rope, 1);
            Assert.IsNull(g.PieceBlocker(rope, bag));
            int hp = g.MaxHp;
            Assert.IsTrue(g.InstallPiece(rope, bag));
            Assert.AreEqual(0, bag.Count(rope), "used up");
            Assert.IsTrue(g.HasPiece(rope.id));
            Assert.AreEqual("piece_lashing_rope", g.LastInstalled);
            Assert.GreaterOrEqual(g.MaxHp, hp, "+2% raft HP");
            Assert.IsFalse((g.UpgradeBlocker(bag) ?? "").Contains("install"));
            var trim = Item(ShipPieceRules.Find("piece_deck_trim"));
            bag.Add(trim, 1);
            StringAssert.Contains("Opens at raft Lv 8", g.PieceBlocker(trim, bag));
        }

        [Test]
        public void ShipyardSortsPartsIntoSubTabs()
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.type = ItemType.Raft; Assert.AreEqual(PartTab.Planks, ShipyardRules.TabOf(i));
            i.type = ItemType.Module; Assert.AreEqual(PartTab.AutoLoot, ShipyardRules.TabOf(i));
            i.shape = ModuleShape.L; i.segment = ScriptableObject.CreateInstance<ItemDefinition>();
            Assert.AreEqual(PartTab.Kits, ShipyardRules.TabOf(i));
            i.type = ItemType.Habitat; Assert.AreEqual(PartTab.Habitats, ShipyardRules.TabOf(i));
            i.type = ItemType.ShipPiece; Assert.AreEqual(PartTab.ShipPieces, ShipyardRules.TabOf(i));
            i.type = ItemType.Vessel; Assert.AreEqual(PartTab.ShipParts, ShipyardRules.TabOf(i));
            i.type = ItemType.Weapon; Assert.IsNull(ShipyardRules.TabOf(i));
            Assert.IsTrue(ShipyardRules.OnCells(PartTab.Kits));
            Assert.IsFalse(ShipyardRules.OnCells(PartTab.ShipPieces), "pieces go on their slot, not a cell");
        }
    }
}
