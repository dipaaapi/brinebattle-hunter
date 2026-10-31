using System.Collections.Generic;
using BrineBattle.Companions;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's pick: crafting is minion alchemy; the hunter makes only Common things alone.</summary>
    public class WhoCraftsTests
    {
        static RecipeDefinition Makes(ItemType type, Rarity rarity, RecipeKind kind = RecipeKind.Craft, ModuleShape shape = ModuleShape.Single)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.type = type;
            item.rarity = rarity;
            item.shape = shape;
            if (shape != ModuleShape.Single) item.segment = ScriptableObject.CreateInstance<ItemDefinition>();
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.kind = kind;
            r.outputs = new List<Ingredient> { new Ingredient { item = item, count = 1 } };
            return r;
        }

        static Companion With(MinionSkill skill) => new Companion { Id = skill.ToString(), Name = skill.ToString(), MaxHp = 10, Hp = 10, Skills = { skill } };

        [Test]
        public void TheHunterMakesCommonThingsAlone()
        {
            Assert.IsEmpty(WhoCrafts.Needs(Makes(ItemType.Raft, Rarity.Common)));
            Assert.IsEmpty(WhoCrafts.Needs(Makes(ItemType.Consumable, Rarity.Common)));
            Assert.IsEmpty(WhoCrafts.Needs(Makes(ItemType.Weapon, Rarity.Epic, RecipeKind.Dismantle)), "taking apart is always allowed");
            var maker = WhoCrafts.Maker(Makes(ItemType.Tool, Rarity.Common), new CompanionRoster(), out bool allowed);
            Assert.IsTrue(allowed);
            Assert.IsNull(maker);
        }

        [Test]
        public void RarerThingsNeedTheMatchingMinion()
        {
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Smith, MinionSkill.Tinker }, WhoCrafts.Needs(Makes(ItemType.Weapon, Rarity.Rare)));
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Smith, MinionSkill.Tinker }, WhoCrafts.Needs(Makes(ItemType.Tool, Rarity.Uncommon)));
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Alchemist }, WhoCrafts.Needs(Makes(ItemType.Consumable, Rarity.Uncommon)));
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Builder, MinionSkill.Crewmate }, WhoCrafts.Needs(Makes(ItemType.Raft, Rarity.Uncommon)));
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Builder, MinionSkill.Crewmate }, WhoCrafts.Needs(Makes(ItemType.Vessel, Rarity.Epic)));
            CollectionAssert.AreEquivalent(new[] { MinionSkill.Builder, MinionSkill.Alchemist }, WhoCrafts.Needs(Makes(ItemType.Module, Rarity.Common, shape: ModuleShape.Square)), "every shaped kit, even Common");
            StringAssert.Contains("Alchemist", WhoCrafts.NeedText(Makes(ItemType.Consumable, Rarity.Rare)));
        }

        [Test]
        public void TheStrongestMatchingMinionMakesIt()
        {
            var roster = new CompanionRoster { Habitats = 3 };
            var potion = Makes(ItemType.Consumable, Rarity.Rare);
            Assert.IsNull(WhoCrafts.Maker(potion, roster, out bool allowed));
            Assert.IsFalse(allowed, "no Alchemist yet");
            var alchemist = With(MinionSkill.Alchemist);
            roster.Add(With(MinionSkill.Fighter));
            roster.Add(alchemist);
            Assert.AreEqual(alchemist, WhoCrafts.Maker(potion, roster, out allowed));
            Assert.IsTrue(allowed);
            alchemist.Hp = 0;
            WhoCrafts.Maker(potion, roster, out allowed);
            Assert.IsFalse(allowed, "a knocked-out minion can't craft");
        }
    }
}
