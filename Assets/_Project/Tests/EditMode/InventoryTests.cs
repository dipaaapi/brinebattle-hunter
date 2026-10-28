using BrineBattle.Elements;
using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class InventoryTests
    {
        static ItemDefinition Item(ItemType type, Rarity rarity, Element element = Element.None)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{rarity} {type}";
            i.type = type;
            i.rarity = rarity;
            i.element = element;
            return i;
        }

        [TestCase(ItemType.Raft, Rarity.Uncommon, Rarity.Epic, false)]
        [TestCase(ItemType.Raft, Rarity.Epic, Rarity.Epic, true)]
        [TestCase(ItemType.Weapon, Rarity.Mythic, Rarity.Common, true)]
        [TestCase(ItemType.Material, Rarity.Common, Rarity.Mythic, true)]
        [TestCase(ItemType.Consumable, Rarity.Common, Rarity.Mythic, true)]
        public void Compatibility(ItemType type, Rarity item, Rarity tier, bool expected)
        {
            Assert.AreEqual(expected, SalvageRules.IsCompatible(type, item, tier));
        }

        [Test]
        public void YieldGrowsWithRarityAndEssenceIsZero()
        {
            Assert.AreEqual(1, SalvageRules.EssenceYield(ItemType.Material, Rarity.Common));
            Assert.AreEqual(6, SalvageRules.EssenceYield(ItemType.Weapon, Rarity.Mythic));
            Assert.AreEqual(0, SalvageRules.EssenceYield(ItemType.Essence, Rarity.Mythic));
        }

        [Test]
        public void AddAndRemoveStack()
        {
            var inv = new Inventory();
            var scale = Item(ItemType.Material, Rarity.Common);
            inv.Add(scale, 3);
            inv.Add(scale, 2);
            Assert.AreEqual(5, inv.Count(scale));
            Assert.IsFalse(inv.Remove(scale, 6));
            Assert.IsTrue(inv.Remove(scale, 5));
            Assert.AreEqual(0, inv.Count(scale));
            Assert.IsTrue(inv.IsEmpty);
        }

        [Test]
        public void SalvageMakesEssenceOfRarityAndElement()
        {
            var inv = new Inventory();
            var plate = Item(ItemType.Raft, Rarity.Rare, Element.Earth);
            inv.Add(plate, 4);
            Assert.AreEqual(6, inv.Salvage(plate, 2));
            Assert.AreEqual(2, inv.Count(plate));
            Assert.AreEqual(6, inv.EssenceCount(new EssenceKey(Rarity.Rare, Element.Earth)));
        }

        [Test]
        public void SalvageMoreThanOwnedDoesNothing()
        {
            var inv = new Inventory();
            var plate = Item(ItemType.Raft, Rarity.Rare);
            inv.Add(plate, 1);
            Assert.AreEqual(0, inv.Salvage(plate, 2));
            Assert.AreEqual(1, inv.Count(plate));
        }

        [Test]
        public void SalvageIncompatibleOnlyTouchesMisfits()
        {
            var inv = new Inventory();
            var lowRaft = Item(ItemType.Raft, Rarity.Common);
            var goodRaft = Item(ItemType.Raft, Rarity.Epic);
            var mat = Item(ItemType.Material, Rarity.Common);
            inv.Add(lowRaft, 3);
            inv.Add(goodRaft, 1);
            inv.Add(mat, 5);
            int gained = inv.SalvageIncompatible(t => Rarity.Rare);
            Assert.AreEqual(3, gained);
            Assert.AreEqual(0, inv.Count(lowRaft));
            Assert.AreEqual(1, inv.Count(goodRaft));
            Assert.AreEqual(5, inv.Count(mat));
        }

        [Test]
        public void ChangedFires()
        {
            var inv = new Inventory();
            int n = 0;
            inv.Changed += () => n++;
            inv.Add(Item(ItemType.Material, Rarity.Common), 1);
            Assert.AreEqual(1, n);
        }
    }
}
