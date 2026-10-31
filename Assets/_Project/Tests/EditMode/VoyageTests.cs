using BrineBattle.Battle;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class VoyageTests
    {
        static ItemDefinition Item(RaftKind kind, Rarity r, ItemType t = ItemType.Raft, int slot = 0)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{kind} {r} {t}";
            i.type = t;
            i.rarity = r;
            i.raftKind = kind;
            i.vesselSlot = slot;
            return i;
        }

        static readonly ItemDefinition Basic = Item(RaftKind.Plain, Rarity.Common);

        static RaftGrid Ready()
        {
            var g = new RaftGrid();
            g.FillSquare(Basic, 1);
            g.Kills = 999;
            return g;
        }

        static Inventory Rich()
        {
            var inv = new Inventory();
            inv.Add(Item(RaftKind.Plain, Rarity.Common, ItemType.Material), 99);
            for (var r = Rarity.Common; r <= Rarity.Mythic; r++) inv.AddEssence(new EssenceKey(r, Elements.Element.None), 99);
            return inv;
        }

        [Test]
        public void HarderDifficultiesNeedMoreTypesAndGiveLessLoot()
        {
            Assert.AreEqual(1, DifficultyRules.TypesToWin(Difficulty.Easy));
            Assert.AreEqual(3, DifficultyRules.TypesToWin(Difficulty.Hard));
            Assert.Greater(DifficultyRules.EnemyScale(Difficulty.Hard), DifficultyRules.EnemyScale(Difficulty.Normal));
            Assert.Less(DifficultyRules.LootScale(Difficulty.Hard), DifficultyRules.LootScale(Difficulty.Easy));
            var rng = new System.Random(4);
            for (int i = 0; i < 50; i++) Assert.AreEqual(3, DifficultyRules.ScaleLoot(3, Difficulty.Easy, rng));
            int hard = 0;
            for (int i = 0; i < 1000; i++) hard += DifficultyRules.ScaleLoot(5, Difficulty.Hard, rng);
            Assert.AreEqual(3000, hard, 150, "about 60% of the loot on Hard");
        }

        static Inventory Richer()
        {
            var inv = new Inventory();
            inv.Add(Item(RaftKind.Plain, Rarity.Common, ItemType.Material), 99999);
            for (var r = Rarity.Common; r <= Rarity.Mythic; r++) inv.AddEssence(new EssenceKey(r, Elements.Element.None), 99999);
            return inv;
        }

        [Test]
        public void TheBossAtTheEndOfATierGatesTheLevelUp()
        {
            var g = Ready();
            g.BossesNeeded = true;
            Assert.IsFalse(BossRules.Due(g), "no boss in the middle of a tier");
            Assert.IsTrue(g.TryUpgrade(Rich()), "Lv 1 → 2 needs no boss");
            g.RestoreEvolution(RaftKind.Plain, 16);
            g.Kills = 999;
            Assert.IsTrue(BossRules.Due(g));
            StringAssert.Contains("boss", g.UpgradeBlocker(Rich()));
            Assert.IsFalse(g.TryUpgrade(Rich()));
            g.MarkBoss(Rarity.Common);
            Assert.IsFalse(BossRules.Due(g));
            Assert.IsTrue(g.TryUpgrade(Rich()));
            Assert.AreEqual(17, g.Level);
            Assert.AreEqual(Rarity.Uncommon, g.Tier);
        }

        [Test]
        public void ShipPartsGateTheShipLevels()
        {
            var g = Ready();
            var bag = Rich();
            g.RestoreEvolution(RaftKind.Plain, EvolutionRules.RaftMax);
            Assert.IsTrue(g.Choose(RaftKind.Aerial));
            g.RestoreEvolution(RaftKind.Aerial, 20);
            var wings = Item(RaftKind.Aerial, Rarity.Uncommon, ItemType.Vessel, 1);
            var tail = Item(RaftKind.Aerial, Rarity.Rare, ItemType.Vessel, 2);
            var hull = Item(RaftKind.Sea, Rarity.Uncommon, ItemType.Vessel, 1);
            bag.Add(wings, 1);
            bag.Add(tail, 1);
            bag.Add(hull, 1);
            g.Kills = 999;
            Assert.AreEqual(1, g.PartPending);
            StringAssert.Contains("Wings", g.UpgradeBlocker(bag));
            Assert.IsNotNull(g.VesselBlocker(hull, bag), "not the picked line");
            Assert.IsNotNull(g.VesselBlocker(tail, bag), "opens at ship Lv 40");
            Assert.IsTrue(g.InstallVessel(wings, bag));
            Assert.AreEqual(0, g.PartPending);
            Assert.IsTrue(g.TryUpgrade(bag));
            Assert.AreEqual(21, g.Level);
            Assert.AreEqual("Wings", VesselRules.PartName(RaftKind.Aerial, 1));
            Assert.AreEqual("Hull Walls", VesselRules.PartName(RaftKind.Sea, 1));
            Assert.AreEqual("Pressure Hull", VesselRules.PartName(RaftKind.Undersea, 1));
            Assert.AreEqual("Sea Track", VesselRules.LegacyName(RaftKind.Sea, 1), "the old asset name stays for saves");
        }

        [Test]
        public void TheClimbGoesRaftFiftyThenShipHundredThenComingSoon()
        {
            var g = Ready();
            var bag = Richer();
            int ups = 0;
            while (!g.AtMax) { g.Kills = 999; Assert.IsTrue(g.TryUpgrade(bag), g.UpgradeBlocker(bag)); ups++; }
            Assert.AreEqual(EvolutionRules.RaftMax, g.Level);
            Assert.AreEqual("Simple Boat", g.FormName);
            StringAssert.Contains("pick", g.UpgradeBlocker(bag));
            Assert.IsTrue(g.Choose(RaftKind.Undersea));
            while (!g.AtMax)
            {
                g.Kills = 999;
                if (g.PartPending > 0)
                {
                    var part = Item(RaftKind.Undersea, VesselRules.RarityOf(g.PartPending), ItemType.Vessel, g.PartPending);
                    bag.Add(part, 1);
                    Assert.IsTrue(g.InstallVessel(part, bag), g.VesselBlocker(part, bag));
                }
                Assert.IsTrue(g.TryUpgrade(bag), g.UpgradeBlocker(bag));
                ups++;
            }
            Assert.AreEqual(EvolutionRules.RaftMax - 1 + EvolutionRules.ShipMax - 1, ups);
            Assert.AreEqual(Rarity.Mythic, g.Tier);
            Assert.IsTrue(g.VesselComplete);
            Assert.AreEqual("Abyss Ark", g.FormName);
            StringAssert.Contains("coming soon", g.UpgradeBlocker(bag));
            Assert.IsFalse(g.TryUpgrade(bag));
        }

        [Test]
        public void EachTypeThatLeavesCountsTowardWinning()
        {
            var v = new Voyage { Difficulty = Difficulty.Normal };
            Assert.AreEqual(1.25f, v.EnemyScale, 1e-4f);
            v.Leave(RaftKind.Aerial);
            Assert.AreEqual(2, v.Sea);
            Assert.IsFalse(v.Won);
            Assert.IsFalse(v.CanPick(RaftKind.Aerial), "a finished type can't be picked again");
            Assert.IsTrue(v.CanPick(RaftKind.Undersea));
            Assert.AreEqual((1f + Voyage.SeaStep) * 1.25f, v.EnemyScale, 1e-4f);
            v.Leave(RaftKind.Undersea);
            Assert.IsTrue(v.Won);
            Assert.IsTrue(v.CanPick(RaftKind.Aerial), "after winning every type is open");

            var c = new Combatant { MaxHp = 100, Hp = 100, Attack = 10, Defense = 4, Speed = 5 };
            Voyage.Scale(c, 1.5f);
            Assert.AreEqual(150, c.MaxHp);
            Assert.AreEqual(15, c.Attack);
            Assert.AreEqual(6, c.Defense);
            Assert.AreEqual(5, c.Speed);
        }
    }
}
