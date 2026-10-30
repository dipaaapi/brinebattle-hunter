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

        [Test]
        public void TheBossOfTheLevelGatesTheLevelUp()
        {
            var g = Ready();
            g.BossesNeeded = true;
            Assert.IsFalse(BossRules.Due(new RaftGrid { BossesNeeded = true }));
            Assert.IsTrue(BossRules.Due(g));
            StringAssert.Contains("boss", g.UpgradeBlocker(Rich()));
            Assert.IsFalse(g.TryUpgrade(Rich()));
            g.MarkBoss(Rarity.Common);
            Assert.IsFalse(BossRules.Due(g));
            Assert.IsTrue(g.TryUpgrade(Rich()));
        }

        [Test]
        public void VehiclePartsOpenOnePerLevelForThePickedType()
        {
            var g = Ready();
            var bag = Rich();
            Assert.IsTrue(g.TryUpgrade(bag, RaftKind.Sky));
            var wings = Item(RaftKind.Sky, Rarity.Uncommon, ItemType.Vessel, 1);
            var tail = Item(RaftKind.Sky, Rarity.Rare, ItemType.Vessel, 2);
            var track = Item(RaftKind.Fortress, Rarity.Uncommon, ItemType.Vessel, 1);
            bag.Add(wings, 1);
            bag.Add(tail, 1);
            bag.Add(track, 1);
            Assert.IsNotNull(g.VesselBlocker(track, bag), "not the picked type");
            Assert.IsNotNull(g.VesselBlocker(tail, bag), "opens at Lv 3");
            Assert.IsTrue(g.InstallVessel(wings, bag));
            Assert.IsTrue(g.HasVessel(1));
            Assert.AreEqual(0, bag.Count(wings));
            Assert.IsFalse(g.InstallVessel(wings, bag), "already on");
            Assert.AreEqual("Wings", VesselRules.PartName(RaftKind.Sky, 1));
            Assert.AreEqual("Sea Track", VesselRules.PartName(RaftKind.Fortress, 1));
        }

        [Test]
        public void AFinishedVehicleLeavesForANewSeaBackAtLevelOne()
        {
            var g = Ready();
            var bag = Rich();
            while (g.Tier < Rarity.Mythic)
            {
                g.Kills = 999;
                Assert.IsTrue(g.TryUpgrade(bag, RaftKind.Beast), g.UpgradeBlocker(bag));
            }
            StringAssert.Contains("vehicle part", g.LeaveBlocker());
            for (int slot = 1; slot <= VesselRules.Parts; slot++)
            {
                var part = Item(RaftKind.Beast, VesselRules.RarityOf(slot), ItemType.Vessel, slot);
                bag.Add(part, 1);
                Assert.IsTrue(g.InstallVessel(part, bag), g.VesselBlocker(part, bag));
            }
            Assert.IsTrue(g.VesselComplete);
            StringAssert.Contains("Lv 6 boss", g.LeaveBlocker());
            g.MarkBoss(Rarity.Mythic);
            Assert.IsTrue(g.CanLeave);

            g.NewSea(Basic, bag);
            Assert.AreEqual(Rarity.Common, g.Tier);
            Assert.AreEqual(RaftKind.Plain, g.Chosen);
            Assert.AreEqual(0, g.VesselCount);
            Assert.IsFalse(g.BossBeaten(Rarity.Mythic));
            Assert.AreEqual(9, g.Count);
        }

        [Test]
        public void EachTypeThatLeavesCountsTowardWinning()
        {
            var v = new Voyage { Difficulty = Difficulty.Normal };
            Assert.AreEqual(1.25f, v.EnemyScale, 1e-4f);
            v.Leave(RaftKind.Sky);
            Assert.AreEqual(2, v.Sea);
            Assert.IsFalse(v.Won);
            Assert.IsFalse(v.CanPick(RaftKind.Sky), "a finished type can't be picked again");
            Assert.IsTrue(v.CanPick(RaftKind.Beast));
            Assert.AreEqual((1f + Voyage.SeaStep) * 1.25f, v.EnemyScale, 1e-4f);
            v.Leave(RaftKind.Beast);
            Assert.IsTrue(v.Won);
            Assert.IsTrue(v.CanPick(RaftKind.Sky), "after winning every type is open");

            var c = new Combatant { MaxHp = 100, Hp = 100, Attack = 10, Defense = 4, Speed = 5 };
            Voyage.Scale(c, 1.5f);
            Assert.AreEqual(150, c.MaxHp);
            Assert.AreEqual(15, c.Attack);
            Assert.AreEqual(6, c.Defense);
            Assert.AreEqual(5, c.Speed);
        }
    }
}
