using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using BrineBattle.Saving;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's raft and ship evolution (2026-10-31): raft Lv 1-50, then a ship line Lv 1-100, then "Coming soon".</summary>
    public class EvolutionTests
    {
        [Test]
        public void TiersSpreadOverTheWholeClimb()
        {
            Assert.AreEqual(Rarity.Common, EvolutionRules.TierOf(RaftKind.Plain, 16));
            Assert.AreEqual(Rarity.Uncommon, EvolutionRules.TierOf(RaftKind.Plain, 17));
            Assert.AreEqual(Rarity.Rare, EvolutionRules.TierOf(RaftKind.Plain, 50));
            Assert.AreEqual(Rarity.Epic, EvolutionRules.TierOf(RaftKind.Aerial, 1));
            Assert.AreEqual(Rarity.Legendary, EvolutionRules.TierOf(RaftKind.Sea, 34));
            Assert.AreEqual(Rarity.Mythic, EvolutionRules.TierOf(RaftKind.Undersea, 100));
            for (var t = Rarity.Common; t <= Rarity.Rare; t++)
                Assert.AreEqual(t, EvolutionRules.TierOf(RaftKind.Plain, EvolutionRules.FirstLevelOf(RaftKind.Plain, t)));
        }

        [Test]
        public void BossesGateTheEndOfEachTierAndPartsGateEveryTwentyShipLevels()
        {
            var gates = Enumerable.Range(1, 50).Where(l => EvolutionRules.BossGate(RaftKind.Plain, l)).ToArray();
            CollectionAssert.AreEqual(new[] { 16, 33, 50 }, gates);
            gates = Enumerable.Range(1, 100).Where(l => EvolutionRules.BossGate(RaftKind.Sea, l)).ToArray();
            CollectionAssert.AreEqual(new[] { 33, 66, 99 }, gates);
            var parts = Enumerable.Range(1, 100).Where(l => EvolutionRules.PartNeeded(RaftKind.Sea, l) > 0).ToArray();
            CollectionAssert.AreEqual(new[] { 20, 40, 60, 80, 99 }, parts);
            Assert.AreEqual(0, EvolutionRules.PartNeeded(RaftKind.Plain, 20), "rafts have no ship parts");
            Assert.AreEqual(99, EvolutionRules.PartOpensAt(5));
        }

        [Test]
        public void ShipLinesAreStrIntDexAndTheirBonusGrows()
        {
            Assert.AreEqual("STR", EvolutionRules.Stat(RaftKind.Sea));
            Assert.AreEqual("INT", EvolutionRules.Stat(RaftKind.Aerial));
            Assert.AreEqual("DEX", EvolutionRules.Stat(RaftKind.Undersea));
            var lv1 = EvolutionRules.Bonus(RaftKind.Undersea, 1);
            var lv100 = EvolutionRules.Bonus(RaftKind.Undersea, 100);
            Assert.Greater(lv100.Speed, lv1.Speed);
            Assert.Greater(EvolutionRules.Bonus(RaftKind.Aerial, 100).Reach, EvolutionRules.Bonus(RaftKind.Aerial, 1).Reach);
            Assert.Greater(EvolutionRules.Bonus(RaftKind.Sea, 50).RaftHpPercent, EvolutionRules.Bonus(RaftKind.Undersea, 50).RaftHpPercent);
        }

        [Test]
        public void TheLookChangesAsItLevels()
        {
            Assert.AreEqual("Plank Raft", EvolutionRules.FormName(RaftKind.Plain, 1));
            Assert.AreEqual("Cabin Raft", EvolutionRules.FormName(RaftKind.Plain, 45));
            Assert.AreEqual("Simple Boat", EvolutionRules.FormName(RaftKind.Plain, 50));
            Assert.AreEqual("Sloop", EvolutionRules.FormName(RaftKind.Sea, 1));
            Assert.AreEqual("Flagship", EvolutionRules.FormName(RaftKind.Sea, 100));
            Assert.AreEqual("Airship", EvolutionRules.FormName(RaftKind.Aerial, 60));
            Assert.AreEqual("Raft Lv 7", EvolutionRules.Label(RaftKind.Plain, 7));
            Assert.AreEqual("Aerial ship Lv 3", EvolutionRules.Label(RaftKind.Aerial, 3));
        }

        [Test]
        public void OldSavesMoveToTheNewLevelsAndLoseTheOldType()
        {
            var g = new RaftGrid();
            SaveGame.MigrateRaft(g, Rarity.Rare, new List<int> { 0, 1 }, true);
            Assert.AreEqual(17, g.Level, "old Lv 3 → raft Lv 17");
            Assert.AreEqual(RaftKind.Plain, g.Chosen);
            Assert.AreEqual(Rarity.Uncommon, g.Tier);
            Assert.IsTrue(g.BossBeaten(Rarity.Common));
            Assert.AreEqual(0, g.VesselCount);
            var top = new RaftGrid();
            SaveGame.MigrateRaft(top, Rarity.Mythic, null, false);
            Assert.AreEqual(41, top.Level);
        }

        [Test]
        public void NewSavesKeepTheLineAndLevel()
        {
            var plank = ScriptableObject.CreateInstance<ItemDefinition>();
            plank.id = "plank";
            plank.type = ItemType.Raft;
            var g = new RaftGrid();
            g.FillSquare(plank, 1);
            g.RestoreEvolution(RaftKind.Aerial, 42);
            var data = SaveGame.Capture(new Inventory(), new Equipment(), null, g, null, new Companions.CompanionRoster(), null, System.DateTime.Now);
            Assert.IsTrue(data.hasEvolution);
            var g2 = new RaftGrid();
            SaveGame.Apply(data, id => id == "plank" ? plank : null, new Inventory(), new Equipment(), null, g2, new Companions.CompanionRoster(), out _);
            Assert.AreEqual(RaftKind.Aerial, g2.Chosen);
            Assert.AreEqual(42, g2.Level);
            Assert.AreEqual(Rarity.Legendary, g2.Tier);
        }
    }
}
