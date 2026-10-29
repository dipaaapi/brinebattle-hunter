using BrineBattle.Elements;
using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class MasteryTests
    {
        [Test]
        public void LevelsNeedMorePointsEachTime()
        {
            Assert.AreEqual(1, AlchemyMastery.LevelFor(0));
            Assert.AreEqual(1, AlchemyMastery.LevelFor(9));
            Assert.AreEqual(2, AlchemyMastery.LevelFor(10));
            Assert.AreEqual(3, AlchemyMastery.LevelFor(30));
            Assert.AreEqual(AlchemyMastery.MaxLevel, AlchemyMastery.LevelFor(100000));
        }

        [Test]
        public void LevelUpEventFiresForEveryLevel()
        {
            var m = new AlchemyMastery();
            int ups = 0;
            m.LeveledUp += _ => ups++;
            m.Add(30);
            Assert.AreEqual(3, m.Level);
            Assert.AreEqual(2, ups);
        }

        [Test]
        public void HintsGetMoreAccurate()
        {
            Assert.AreEqual(0.6f, AlchemyMastery.HintAccuracy(1), 1e-4);
            Assert.Greater(AlchemyMastery.HintAccuracy(5), AlchemyMastery.HintAccuracy(4));
            Assert.LessOrEqual(AlchemyMastery.HintAccuracy(AlchemyMastery.MaxLevel), 0.96f);
        }

        static RecipeDefinition Makes(Rarity rarity, RecipeKind kind = RecipeKind.Craft)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.rarity = rarity;
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.kind = kind;
            r.outputs.Add(new Ingredient { item = item, count = 1 });
            return r;
        }

        [Test]
        public void RarerRecipesNeedMoreMastery()
        {
            Assert.AreEqual(1, AlchemyMastery.Needed(Makes(Rarity.Common)));
            Assert.AreEqual(2, AlchemyMastery.Needed(Makes(Rarity.Uncommon)));
            Assert.AreEqual(4, AlchemyMastery.Needed(Makes(Rarity.Epic)));
            Assert.AreEqual(6, AlchemyMastery.Needed(Makes(Rarity.Mythic)));
            Assert.AreEqual(1, AlchemyMastery.Needed(Makes(Rarity.Mythic, RecipeKind.Dismantle)));
            var m = new AlchemyMastery();
            Assert.IsTrue(m.Knows(Makes(Rarity.Common)));
            Assert.IsFalse(m.Knows(Makes(Rarity.Uncommon)));
        }

        [Test]
        public void OldSavesStartAtTheirRaftLevel()
        {
            var m = new AlchemyMastery();
            m.Restore(AlchemyMastery.StartingPoints(Rarity.Rare));
            Assert.IsTrue(m.Knows(Makes(Rarity.Rare)));
            Assert.IsFalse(m.Knows(Makes(Rarity.Epic)));
            Assert.IsTrue(m.Knows(Makes(Rarity.Uncommon)));
        }

        [Test]
        public void CombineEssenceFromLevelThree()
        {
            var inv = new Inventory();
            var key = new EssenceKey(Rarity.Common, Element.Fire);
            inv.AddEssence(key, 7);
            var m = new AlchemyMastery();
            Assert.IsFalse(m.Combine(inv, key), "too low a level");
            m.Restore(AlchemyMastery.PointsFor(AlchemyMastery.CombineLevel));
            Assert.IsTrue(m.Combine(inv, key));
            Assert.AreEqual(2, inv.EssenceCount(key));
            Assert.AreEqual(1, inv.EssenceCount(new EssenceKey(Rarity.Uncommon, Element.Fire)));
            Assert.IsFalse(m.Combine(inv, key), "only 2 left");
        }
    }
}
