using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Monsters;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class CompanionWorkTests
    {
        static ItemDefinition Item(ItemType type, Rarity rarity = Rarity.Common, int power = 4)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = $"{type}{rarity}";
            i.type = type;
            i.rarity = rarity;
            i.gearPower = power;
            return i;
        }

        [Test]
        public void CrewInstallsModulesUpstreamAndGrowsTheRaftDownstream()
        {
            var plank = Item(ItemType.Raft);
            var plate = Item(ItemType.Raft, Rarity.Rare);
            var net = Item(ItemType.Module);
            var raft = new RaftGrid();
            raft.FillSquare(plank, 1);   // cells -1..1
            var bag = new Inventory();
            Assert.IsFalse(CompanionWork.CrewBuild(raft, bag, out _), "nothing in the bag");

            bag.Add(net, 1);
            bag.Add(plank, 1);
            bag.Add(plate, 1);
            Assert.IsTrue(CompanionWork.CrewBuild(raft, bag, out var first));
            Assert.AreEqual(net, first, "modules first");
            Assert.AreEqual(net, raft.Parts[new Cell(-2, 0)], "on the upstream (left) edge, middle row");

            Assert.IsTrue(CompanionWork.CrewBuild(raft, bag, out var second));
            Assert.AreEqual(plate, second, "then the rarest part");
            Assert.AreEqual(plate, raft.Parts[new Cell(2, 0)], "grows on the downstream (right) side");
            Assert.AreEqual(0, bag.Count(net));
            Assert.AreEqual(1, bag.Count(plank));
        }

        [Test]
        public void MysticRefinesTheLowerPieceItCanAfford()
        {
            var spear = Item(ItemType.Weapon);
            var shield = Item(ItemType.Armor);
            var bag = new Inventory();
            bag.Add(spear, 1);
            bag.Add(shield, 1);
            var gear = new Equipment();
            gear.Equip(spear, bag, Rarity.Common);
            gear.Equip(shield, bag, Rarity.Common);
            Assert.IsNull(CompanionWork.MysticPick(gear, bag), "no essence");
            bag.AddEssence(new EssenceKey(Rarity.Common, Element.None), 50);
            gear.Refine(GearSlot.Weapon, bag);
            Assert.AreEqual(GearSlot.Armor, CompanionWork.MysticPick(gear, bag), "armor is behind");
        }

        static RecipeDefinition Recipe(ItemDefinition input, ItemDefinition output)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.kind = RecipeKind.Craft;
            r.inputs.Add(new Ingredient { item = input, count = 1 });
            r.outputs.Add(new Ingredient { item = output, count = 1 });
            return r;
        }

        [Test]
        public void MysticRestocksPotionsBeforeOils()
        {
            var mat = Item(ItemType.Material);
            var tonic = Item(ItemType.Consumable);
            tonic.id = "tonic";
            tonic.effect = ConsumableEffect.HealHunter;
            var oil = Item(ItemType.Consumable);
            oil.id = "oil";
            oil.effect = ConsumableEffect.CoatWeapon;
            var recipes = new[] { Recipe(mat, oil), Recipe(mat, tonic) };
            var bag = new Inventory();
            Assert.IsNull(CompanionWork.MysticCraftPick(recipes, bag, 2), "no materials");
            bag.Add(mat, 5);
            Assert.AreEqual(tonic, CompanionWork.MysticCraftPick(recipes, bag, 2).outputs[0].item);
            bag.Add(tonic, 2);
            Assert.AreEqual(oil, CompanionWork.MysticCraftPick(recipes, bag, 2).outputs[0].item, "tonics are stocked");
            bag.Add(oil, 2);
            Assert.IsNull(CompanionWork.MysticCraftPick(recipes, bag, 2));
        }

        [Test]
        public void CrewRepairsAtTheSetPercentage()
        {
            var d = new Durability(30, 20);
            Assert.IsFalse(CompanionWork.CrewShouldRepair(d, 0.5f));
            d.Set(15);
            Assert.IsTrue(CompanionWork.CrewShouldRepair(d, 0.5f));
            d.Set(30);
            Assert.IsFalse(CompanionWork.CrewShouldRepair(d, 1f), "full needs nothing");
        }

        [Test]
        public void MysticHealsTheMostHurtAllyInBattle()
        {
            var mon = new Combatant { Name = "Mon", MaxHp = 999, Hp = 999, Attack = 0, Speed = 1, Habitat = Habitat.Sea };
            var s = new BattleState(Combatant.Hunter(), mon, 90, new System.Random(1));
            s.Companion = new Combatant { Name = "Pet", MaxHp = 20, Hp = 20, Attack = 1, Speed = 1 };
            s.SupportHeal = CompanionRules.MysticBattleHeal(Rarity.Common);
            s.Hunter.Hp = 30;
            s.Companion.Hp = 5;
            s.PlayRound(BattleAction.Brace);
            Assert.IsTrue(s.Log.Any(l => l.StartsWith("Your mystic heals Pet")), "the pet is lower (25% vs 50%)");
            Assert.IsTrue(s.Events.Any(e => e.Kind == BattleEventKind.Heal && e.To == BattleActor.Companion && e.Text == "Mystic"));
        }
    }
}
