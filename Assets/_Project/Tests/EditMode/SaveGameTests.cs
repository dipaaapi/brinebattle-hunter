using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;
using BrineBattle.Rafts;
using BrineBattle.Saving;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class SaveGameTests
    {
        static ItemDefinition Item(string id, ItemType type, int power = 0)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = id;
            i.displayName = id;
            i.type = type;
            i.gearPower = power;
            return i;
        }

        [Test]
        public void SaveAndLoadGivesBackTheSameGame()
        {
            var plank = Item("plank", ItemType.Raft);
            var net = Item("net", ItemType.Module);
            var spear = Item("spear", ItemType.Weapon, 4);
            var tonic = Item("tonic", ItemType.Consumable);
            var all = new[] { plank, net, spear, tonic };

            var bag = new Inventory();
            bag.Add(tonic, 3);
            bag.Add(spear, 1);
            bag.AddEssence(new EssenceKey(Rarity.Rare, Element.Earth), 7);
            var gear = new Equipment();
            gear.Equip(spear, bag, Rarity.Common);
            bag.AddEssence(new EssenceKey(Rarity.Common, Element.None), 20);
            gear.Refine(GearSlot.Weapon, bag);
            var harpoon = new Durability(20, 13);
            var raft = new RaftGrid();
            raft.FillSquare(plank, 1);
            raft.Place(new Cell(2, 0), net);
            raft.Kills = 4;
            var pets = new CompanionRoster();
            pets.Add(new Companion { Id = "gull", Name = "Gale Gull", Element = Element.Wind, Habitat = Habitat.Sky, Rarity = Rarity.Common, Role = CompanionRole.Crew, MaxHp = 12 });
            pets.Add(new Companion { Id = "eel", Name = "Brinefang Eel", Role = CompanionRole.Familiar, MaxHp = 18, Attack = 4 });

            var data = SaveGame.Capture(bag, gear, harpoon, raft,
                new[] { new KeyValuePair<Cell, int>(new Cell(2, 0), 9) }, pets, "Monsters:eel|Items:|Pets:eel", new DateTime(2026, 10, 29, 12, 0, 0));

            var bag2 = new Inventory();
            var gear2 = new Equipment();
            var harpoon2 = new Durability(20);
            var raft2 = new RaftGrid();
            var pets2 = new CompanionRoster();
            var missing = SaveGame.Apply(data, id => all.FirstOrDefault(i => i.id == id), bag2, gear2, harpoon2, raft2, pets2, out var modules);

            CollectionAssert.IsEmpty(missing);
            Assert.AreEqual(3, bag2.Count(tonic));
            Assert.AreEqual(0, bag2.Count(spear), "the spear is equipped, not in the bag");
            Assert.AreEqual(bag.EssenceAtLeast(Rarity.Common), bag2.EssenceAtLeast(Rarity.Common));
            Assert.AreEqual(spear, gear2.Get(GearSlot.Weapon));
            Assert.AreEqual(1, gear2.RefineOf(spear));
            Assert.AreEqual(gear.Attack, gear2.Attack);
            Assert.AreEqual(13, harpoon2.Current);
            Assert.AreEqual(raft.Count, raft2.Count);
            Assert.AreEqual(net, raft2.Parts[new Cell(2, 0)]);
            Assert.AreEqual(4, raft2.Kills, "kills toward the next raft tier");
            Assert.AreEqual(9, modules[new Cell(2, 0)]);
            Assert.AreEqual(2, pets2.All.Count);
            Assert.AreEqual(CompanionRole.Crew, pets2.All[0].Role);
            Assert.AreEqual("Brinefang Eel", pets2.Active.Name);
            Assert.AreEqual("Monsters:eel|Items:|Pets:eel", data.collection);
            Assert.AreEqual("2026-10-29 12:00", data.savedAt);
        }

        [Test]
        public void UnknownItemsAreSkippedAndReported()
        {
            var data = new SaveData();
            data.items.Add(new SaveData.Stack { id = "gone", count = 2 });
            var bag = new Inventory();
            var missing = SaveGame.Apply(data, id => null, bag, new Equipment(), null, new RaftGrid(), new CompanionRoster(), out _);
            CollectionAssert.AreEqual(new[] { "gone" }, missing);
            Assert.IsTrue(bag.IsEmpty);
        }
    }
}
