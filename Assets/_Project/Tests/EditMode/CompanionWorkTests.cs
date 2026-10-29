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
        public void CrewBuildsTheBestFittingPartNextToTheRaft()
        {
            var plank = Item(ItemType.Raft);
            var plate = Item(ItemType.Raft, Rarity.Rare);
            var net = Item(ItemType.Module);
            var raft = new RaftGrid();
            raft.FillSquare(plank, 1);
            var bag = new Inventory();
            Assert.IsFalse(CompanionWork.CrewBuild(raft, bag, out _), "nothing in the bag");
            bag.Add(net, 1);
            Assert.IsFalse(CompanionWork.CrewBuild(raft, bag, out _), "modules are the player's call");
            bag.Add(plank, 1);
            bag.Add(plate, 1);
            int before = raft.Count;
            Assert.IsTrue(CompanionWork.CrewBuild(raft, bag, out var built));
            Assert.AreEqual(plate, built, "rarest part first");
            Assert.AreEqual(before + 1, raft.Count);
            Assert.AreEqual(0, bag.Count(plate));
            Assert.AreEqual(1, bag.Count(net));
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
