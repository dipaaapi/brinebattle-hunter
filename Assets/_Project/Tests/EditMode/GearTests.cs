using System;
using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class GearTests
    {
        static ItemDefinition Gear(ItemType type, Rarity rarity, int power, Element element = Element.None)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{rarity} {type}";
            i.type = type;
            i.rarity = rarity;
            i.element = element;
            i.gearPower = power;
            return i;
        }

        [TestCase(4, 0, 4)]
        [TestCase(4, 1, 5)]
        [TestCase(8, 2, 12)]
        [TestCase(2, 3, 5)]
        [TestCase(4, 99, 14)]
        public void PowerGrowsWithRefine(int basePower, int refine, int expected)
        {
            Assert.AreEqual(expected, GearRules.Power(basePower, refine));
        }

        [Test]
        public void RefineCostGrowsAndStopsAtMax()
        {
            Assert.AreEqual(2, GearRules.RefineCost(0));
            Assert.AreEqual(20, GearRules.RefineCost(9));
            Assert.AreEqual(0, GearRules.RefineCost(GearRules.MaxRefine));
        }

        [Test]
        public void EquipMovesItemOutOfBagAndSwapsBack()
        {
            var inv = new Inventory();
            var gear = new Equipment();
            var spear = Gear(ItemType.Weapon, Rarity.Uncommon, 4, Element.Water);
            var club = Gear(ItemType.Weapon, Rarity.Common, 2);
            inv.Add(spear, 1);
            inv.Add(club, 1);

            Assert.IsTrue(gear.Equip(spear, inv, Rarity.Common));
            Assert.AreEqual(0, inv.Count(spear));
            Assert.AreEqual(spear, gear.Get(GearSlot.Weapon));
            Assert.AreEqual(4, gear.Attack);
            Assert.AreEqual(Element.Water, gear.Element);

            Assert.IsTrue(gear.Equip(club, inv, Rarity.Common));
            Assert.AreEqual(1, inv.Count(spear));
            Assert.AreEqual(0, inv.Count(club));

            Assert.IsTrue(gear.Unequip(GearSlot.Weapon, inv));
            Assert.AreEqual(1, inv.Count(club));
            Assert.IsNull(gear.Get(GearSlot.Weapon));
            Assert.AreEqual(Element.None, gear.Element);
        }

        [Test]
        public void CantEquipMisfitsMissingItemsOrNonGear()
        {
            var inv = new Inventory();
            var gear = new Equipment();
            var buckler = Gear(ItemType.Armor, Rarity.Common, 3);
            var scale = Gear(ItemType.Material, Rarity.Mythic, 9);
            Assert.IsFalse(gear.Equip(buckler, inv, Rarity.Common), "not in the bag");
            inv.Add(buckler, 1);
            inv.Add(scale, 1);
            Assert.IsFalse(gear.Equip(buckler, inv, Rarity.Rare), "below the armor tier");
            Assert.IsFalse(gear.Equip(scale, inv, Rarity.Common), "not gear");
            Assert.IsTrue(gear.Equip(buckler, inv, Rarity.Common));
            Assert.AreEqual(3, gear.Defense);
        }

        [Test]
        public void RefineSpendsEssenceOfItsRarityAndKeepsWhenTakenOff()
        {
            var inv = new Inventory();
            var gear = new Equipment();
            var spear = Gear(ItemType.Weapon, Rarity.Uncommon, 4);
            var junk = Gear(ItemType.Material, Rarity.Common, 0);
            var good = Gear(ItemType.Material, Rarity.Uncommon, 0);
            inv.Add(spear, 1);
            inv.Add(junk, 10);
            inv.Salvage(junk, 10); // 10 Common essence: too low a rarity for an Uncommon spear
            gear.Equip(spear, inv, Rarity.Common);

            Assert.IsFalse(gear.CanRefine(GearSlot.Weapon, inv));
            inv.Add(good, 3);
            inv.Salvage(good, 3); // 6 Uncommon essence
            Assert.IsTrue(gear.Refine(GearSlot.Weapon, inv));  // costs 2
            Assert.IsTrue(gear.Refine(GearSlot.Weapon, inv));  // costs 4
            Assert.IsFalse(gear.Refine(GearSlot.Weapon, inv)); // needs 6, has 0
            Assert.AreEqual(2, gear.RefineOf(spear));
            Assert.AreEqual(6, gear.Attack);
            Assert.AreEqual("Uncommon Weapon +2", gear.Label(GearSlot.Weapon));
            Assert.AreEqual(10, inv.EssenceAtLeast(Rarity.Common));

            gear.Unequip(GearSlot.Weapon, inv);
            gear.Equip(spear, inv, Rarity.Common);
            Assert.AreEqual(6, gear.Attack);
        }

        [Test]
        public void GearFeedsTheHunterInBattle()
        {
            var inv = new Inventory();
            var gear = new Equipment();
            var spear = Gear(ItemType.Weapon, Rarity.Common, 4, Element.Water);
            var buckler = Gear(ItemType.Armor, Rarity.Common, 3, Element.Wind);
            inv.Add(spear, 1);
            inv.Add(buckler, 1);
            gear.Equip(spear, inv, Rarity.Common);
            gear.Equip(buckler, inv, Rarity.Common);

            var bare = Combatant.Hunter();
            var armed = Combatant.Hunter(gear);
            Assert.AreEqual(bare.Attack + 4, armed.Attack);
            Assert.AreEqual(bare.Defense + 3, armed.Defense);
            Assert.AreEqual(Element.Water, armed.Element);
        }
    }
}
