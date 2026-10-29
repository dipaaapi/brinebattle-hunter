using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class CompanionGrowthTests
    {
        static Companion Pet(string id = "eel", Rarity rarity = Rarity.Common, Element element = Element.Water) => new Companion
        {
            Id = id, Name = id == "eel" ? "Brinefang Eel" : id == "gull" ? "Gale Gull" : id,
            Element = element, Rarity = rarity, MaxHp = 18, Attack = 4, Defense = 2, Speed = 5,
        };

        static ItemDefinition Item(ItemType type, Rarity rarity = Rarity.Common, Element element = Element.None,
            ConsumableEffect effect = ConsumableEffect.None, int amount = 0)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{type}";
            i.type = type;
            i.rarity = rarity;
            i.element = element;
            i.effect = effect;
            i.effectAmount = amount;
            return i;
        }

        [Test]
        public void FeedingLootGrowsThenEvolves()
        {
            var c = Pet();
            var scale = Item(ItemType.Material, Rarity.Common, Element.Water);
            int hp = c.MaxHp;
            Assert.AreEqual(2, CompanionGrowth.FeedPoints(scale, c), "favourite element counts double");
            for (int i = 0; i < 4; i++) Assert.AreEqual(FeedResult.Grew, CompanionGrowth.Feed(c, scale));
            Assert.AreEqual(FeedResult.Evolved, CompanionGrowth.Feed(c, scale));
            Assert.AreEqual(GrowthStage.Adult, c.Stage);
            Assert.AreEqual(0, c.Growth);
            Assert.Greater(c.MaxHp, hp);
        }

        [Test]
        public void PotionsInfuseUpToTheCap()
        {
            var c = Pet();
            var tonic = Item(ItemType.Consumable, effect: ConsumableEffect.HealHunter, amount: 25);
            var oil = Item(ItemType.Consumable, effect: ConsumableEffect.CoatWeapon, amount: 3);
            Assert.AreEqual(FeedResult.Infused, CompanionGrowth.Feed(c, tonic));
            Assert.AreEqual(21, c.MaxHp);
            Assert.AreEqual(FeedResult.Infused, CompanionGrowth.Feed(c, oil));
            Assert.AreEqual(5, c.Attack);
            CompanionGrowth.Feed(c, tonic);
            Assert.AreEqual(FeedResult.Full, CompanionGrowth.Feed(c, tonic), "Young takes 3 potions");
        }

        [Test]
        public void GearAndRaftPartsAreNotFood()
        {
            Assert.AreEqual(FeedResult.CantEat, CompanionGrowth.Feed(Pet(), Item(ItemType.Weapon)));
            Assert.AreEqual(FeedResult.CantEat, CompanionGrowth.Feed(Pet(), Item(ItemType.Raft)));
        }

        [Test]
        public void SameKindFusionRaisesRarityAndLeavesAnEcho()
        {
            var keep = Pet();
            var source = Pet();
            source.Attack = 8;
            Assert.AreEqual(FuseResult.Upgraded, CompanionGrowth.Fuse(keep, source, false, 0.0));
            Assert.AreEqual(Rarity.Uncommon, keep.Rarity);
            Assert.GreaterOrEqual(keep.Attack, 9 + 8 / 4, "the better attack, boosted, plus a quarter of the source's");
            StringAssert.Contains("Echo of", keep.Echo);
        }

        [Test]
        public void DifferentKindsMakeAHybrid()
        {
            var keep = Pet("eel");
            var source = Pet("gull", Rarity.Rare, Element.Wind);
            Assert.AreEqual(FuseResult.Hybrid, CompanionGrowth.Fuse(keep, source, false, 0.0));
            Assert.AreEqual("Brinefang Gull", keep.Name);
            Assert.AreEqual(Rarity.Rare, keep.Rarity, "keeps the rarer one's rarity");
            Assert.AreEqual(Element.Water, keep.Element, "keeps its own element");
        }

        [Test]
        public void FusionCanFailAndACatalystHelps()
        {
            var keep = Pet();
            var source = Pet("gull");
            Assert.AreEqual(FuseResult.Failed, CompanionGrowth.Fuse(keep, source, false, 0.7));
            Assert.AreEqual("Brinefang Eel", keep.Name, "unchanged on a failure");
            Assert.Greater(CompanionGrowth.FuseChance(keep, source, true), CompanionGrowth.FuseChance(keep, source, false));
            Assert.AreEqual(FuseResult.Hybrid, CompanionGrowth.Fuse(keep, source, true, 0.7));
        }

        [Test]
        public void TreatHealsOrWakesTheCompanionAndAutoUsesIt()
        {
            var treat = Item(ItemType.Consumable, effect: ConsumableEffect.HealCompanion, amount: 20);
            var bag = new Inventory();
            bag.Add(treat, 2);
            var mon = new Combatant { Name = "Mon", MaxHp = 999, Hp = 999, Attack = 0, Defense = 0, Speed = 1, Habitat = Habitat.Sea };
            var s = new BattleState(Combatant.Hunter(), mon, 90, new System.Random(1)) { Bag = bag };
            s.Companion = new Combatant { Name = "Pet", MaxHp = 30, Hp = 30, Attack = 1, Speed = 3 };
            Assert.IsFalse(s.CanUseItem(treat), "no use at full HP");
            s.Companion.Hp = 0;
            Assert.AreEqual(treat, BattleAI.ChooseItem(s, BattleMode.Auto), "Auto wakes a knocked-out companion");
            Assert.IsTrue(s.PlayItem(treat));
            Assert.IsTrue(s.Companion.Alive);
            Assert.AreEqual(1, bag.Count(treat));
        }

        [Test]
        public void LooksCycle()
        {
            Assert.AreEqual("Plain", CompanionGrowth.LookName(0));
            Assert.AreEqual(0, CompanionGrowth.NextLook(CompanionGrowth.Looks.Length - 1));
        }
    }
}
