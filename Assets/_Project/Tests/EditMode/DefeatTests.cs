using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class DefeatTests
    {
        static ItemDefinition Item(ItemType type, Rarity rarity)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = $"{type}_{rarity}";
            item.type = type;
            item.rarity = rarity;
            return item;
        }

        [Test]
        public void WavesTakeOnlyAFewCommonMaterials()
        {
            var inv = new Inventory();
            ItemDefinition scale = Item(ItemType.Material, Rarity.Common), rare = Item(ItemType.Material, Rarity.Rare),
                plank = Item(ItemType.Raft, Rarity.Common), spear = Item(ItemType.Weapon, Rarity.Common);
            inv.Add(scale, 10);
            inv.Add(rare, 2);
            inv.Add(plank, 2);
            inv.Add(spear, 1);
            var lost = DefeatRules.WashAway(inv, new System.Random(1));
            int total = 0;
            foreach (var (item, count) in lost) { Assert.AreEqual(scale, item); total += count; }
            Assert.AreEqual(DefeatRules.WashedAway, total);
            Assert.AreEqual(10 - DefeatRules.WashedAway, inv.Count(scale));
            Assert.AreEqual(2, inv.Count(rare));
            Assert.AreEqual(2, inv.Count(plank));
            Assert.AreEqual(1, inv.Count(spear));
        }

        [Test]
        public void NothingToTakeMeansNothingLost()
        {
            var inv = new Inventory();
            inv.Add(Item(ItemType.Material, Rarity.Uncommon), 5);
            Assert.IsEmpty(DefeatRules.WashAway(inv, new System.Random(1)));
        }

        static Combatant Fighter(int hp, int atk, int def) =>
            new Combatant { MaxHp = hp, Hp = hp, Attack = atk, Defense = def, Element = Element.None };

        [Test]
        public void DangerWhenTheMonsterWinsTheRace()
        {
            var hunter = Fighter(60, 9, 3);
            Assert.IsFalse(DefeatRules.Danger(hunter, Fighter(20, 6, 2)));
            Assert.IsTrue(DefeatRules.Danger(hunter, Fighter(200, 30, 10)));
            // A pet in battle can turn it around.
            Assert.IsTrue(DefeatRules.Danger(hunter, Fighter(80, 12, 2)));
            Assert.IsFalse(DefeatRules.Danger(hunter, Fighter(80, 12, 2), Fighter(40, 12, 2)));
        }
    }
}
