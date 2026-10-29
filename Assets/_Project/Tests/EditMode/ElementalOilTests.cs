using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class ElementalOilTests
    {
        static ItemDefinition Oil(Element element, int rounds = 3)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{element} Oil";
            i.type = ItemType.Consumable;
            i.element = element;
            i.effect = ConsumableEffect.CoatWeapon;
            i.effectAmount = rounds;
            return i;
        }

        static BattleState Battle(Element monster, Inventory bag, int seed = 1)
        {
            var mon = new Combatant { Name = "Mon", MaxHp = 999, Hp = 999, Attack = 0, Defense = 0, Speed = 1, Habitat = Habitat.Sea, Element = monster };
            return new BattleState(Combatant.Hunter(), mon, 90, new System.Random(seed)) { Bag = bag };
        }

        [Test]
        public void OilChangesTheAttackElementForItsRounds()
        {
            var bag = new Inventory();
            var oil = Oil(Element.Wind, 2);
            bag.Add(oil, 1);
            var s = Battle(Element.Water, bag);
            Assert.AreEqual(Element.None, s.AttackElement);
            Assert.IsTrue(s.PlayItem(oil));
            Assert.AreEqual(0, bag.Count(oil), "used up");
            Assert.AreEqual(Element.Wind, s.AttackElement);
            s.PlayRound(BattleAction.Attack);
            Assert.AreEqual(Element.Wind, s.AttackElement, "1 of 2 rounds used");
            s.PlayRound(BattleAction.Attack);
            Assert.AreEqual(Element.None, s.AttackElement, "worn off");
            StringAssert.Contains("wears off", s.Log[s.Log.Count - 1]);
        }

        [Test]
        public void OilOnlyWhenItChangesSomething()
        {
            var bag = new Inventory();
            var wind = Oil(Element.Wind);
            var none = Oil(Element.None);
            bag.Add(wind, 2);
            bag.Add(none, 1);
            var s = Battle(Element.Water, bag);
            Assert.IsFalse(s.CanUseItem(none), "an oil needs an element");
            Assert.IsTrue(s.PlayItem(wind));
            Assert.IsFalse(s.CanUseItem(wind), "already coated with Wind");
        }

        [Test]
        public void StrongOilHitsHarder()
        {
            var bagA = new Inventory();
            var bagB = new Inventory();
            var oil = Oil(Element.Wind);
            bagB.Add(oil, 1);
            var a = Battle(Element.Water, bagA);
            var b = Battle(Element.Water, bagB);
            a.PlayRound(BattleAction.Brace);
            b.PlayItem(oil);
            a.PlayRound(BattleAction.Attack);
            b.PlayRound(BattleAction.Attack);
            Assert.Less(b.Monster.Hp, a.Monster.Hp);
        }

        [Test]
        public void AutoCoatsWithAStrongOilOnly()
        {
            var bag = new Inventory();
            var wind = Oil(Element.Wind);  // Wind beats Water
            var fire = Oil(Element.Fire);  // Fire is weak against Water
            bag.Add(fire, 1);
            var s = Battle(Element.Water, bag);
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Auto), "a weak oil is worse than none");
            bag.Add(wind, 1);
            Assert.AreEqual(wind, BattleAI.ChooseItem(s, BattleMode.Auto));
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.SemiAuto), "Semi-auto leaves oils to the player");
            s.PlayItem(wind);
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Auto), "already coated");
        }
    }
}
