using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Idea Round 2: durability (C-4) and auto-loot modules (U-13).</summary>
    public class ModuleTests
    {
        static ItemDefinition Item(ItemType t, Rarity r = Rarity.Common, string name = "x")
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = name;
            i.type = t;
            i.rarity = r;
            return i;
        }

        [Test]
        public void DurabilityBreaksAtZeroAndStopsWorking()
        {
            var d = new Durability(3);
            Assert.IsTrue(d.Use(2));
            Assert.IsTrue(d.Use(5));
            Assert.AreEqual(0, d.Current);
            Assert.IsTrue(d.Broken);
            Assert.IsFalse(d.Use());
            d.Repair(10);
            Assert.AreEqual(3, d.Current);
            Assert.IsTrue(d.Full);
        }

        [Test]
        public void ModuleScalingMatchesTable()
        {
            Assert.AreEqual(30, ModuleRules.MaxDurability(1, Rarity.Common));
            Assert.AreEqual(160, ModuleRules.MaxDurability(5, Rarity.Mythic));
            Assert.AreEqual(1.5f, ModuleRules.Range(1));
            Assert.AreEqual(20f, ModuleRules.Interval(1));
            Assert.AreEqual(0, ModuleRules.Hooks(1));
            Assert.AreEqual(1, ModuleRules.RepairCost(1, Rarity.Common));
            Assert.AreEqual(16, ModuleRules.RepairCost(4, Rarity.Epic));
            Assert.IsTrue(ModuleRules.RepairUsesMaterials(1));
            Assert.IsFalse(ModuleRules.RepairUsesEssence(1));
            Assert.IsTrue(ModuleRules.RepairUsesMaterials(3) && ModuleRules.RepairUsesEssence(3));
            Assert.IsFalse(ModuleRules.RepairUsesMaterials(4));
        }

        [Test]
        public void ModuleCanBePlacedOnRaftButRespectsTier()
        {
            var g = new RaftGrid();
            g.FillSquare(Item(ItemType.Raft), 1);
            Assert.AreEqual(PlaceResult.Ok, g.Place(new Cell(2, 0), Item(ItemType.Module)));
            Assert.AreEqual(PlaceResult.NotARaftPart, g.CanPlace(new Cell(-2, 0), Item(ItemType.Material)));
        }

        [Test]
        public void SpendMaterialsLowestRarityFirstAllOrNothing()
        {
            var inv = new Inventory();
            var common = Item(ItemType.Material, Rarity.Common, "a");
            var rare = Item(ItemType.Material, Rarity.Rare, "b");
            inv.Add(common, 2);
            inv.Add(rare, 2);
            inv.Add(Item(ItemType.Raft), 5);
            Assert.AreEqual(4, inv.MaterialCount());
            Assert.IsFalse(inv.SpendMaterials(5));
            Assert.AreEqual(4, inv.MaterialCount());
            Assert.IsTrue(inv.SpendMaterials(3));
            Assert.AreEqual(0, inv.Count(common));
            Assert.AreEqual(1, inv.Count(rare));
        }

        [Test]
        public void HarpoonWearsAndCantBeUsedBroken()
        {
            var monster = new Combatant { Name = "M", MaxHp = 500, Hp = 500, Attack = 1, Defense = 0, Speed = 1 };
            var s = new BattleState(Combatant.Hunter(), monster, 100, new System.Random(1)) { Weapon = new Durability(1) };
            for (int i = 0; i < 3 && s.Stamina < BattleRules.HarpoonCost; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsTrue(s.CanUse(BattleAction.Harpoon));
            s.PlayRound(BattleAction.Harpoon);
            Assert.IsTrue(s.Weapon.Broken);
            s.PlayRound(BattleAction.Brace);
            Assert.IsFalse(s.CanUse(BattleAction.Harpoon));
        }

        [Test]
        public void RecipeIsAllOrNothingAndRoundTrips()
        {
            var plank = Item(ItemType.Raft, name: "plank");
            var net = Item(ItemType.Module, name: "net");
            var craft = ScriptableObject.CreateInstance<RecipeDefinition>();
            craft.inputs.Add(new Ingredient { item = plank, count = 3 });
            craft.outputs.Add(new Ingredient { item = net, count = 1 });
            var dismantle = ScriptableObject.CreateInstance<RecipeDefinition>();
            dismantle.inputs.Add(new Ingredient { item = net, count = 1 });
            dismantle.outputs.Add(new Ingredient { item = plank, count = 2 });

            var inv = new Inventory();
            inv.Add(plank, 2);
            Assert.IsFalse(CraftRules.Craft(inv, craft));
            Assert.AreEqual(2, inv.Count(plank));
            inv.Add(plank, 1);
            Assert.IsTrue(CraftRules.Craft(inv, craft));
            Assert.AreEqual(0, inv.Count(plank));
            Assert.AreEqual(1, inv.Count(net));
            Assert.IsTrue(CraftRules.Craft(inv, dismantle));
            Assert.AreEqual(2, inv.Count(plank));
            Assert.AreEqual(0, inv.Count(net));
        }
    }
}
