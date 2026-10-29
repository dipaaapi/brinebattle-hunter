using BrineBattle.Companions;
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
    }
}
