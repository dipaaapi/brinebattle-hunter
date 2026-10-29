using BrineBattle.Items;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class RecipeCategoryTests
    {
        static RecipeDefinition Recipe(RecipeKind kind, ItemType type, ConsumableEffect effect = ConsumableEffect.None)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.type = type;
            item.effect = effect;
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.kind = kind;
            r.outputs.Add(new Ingredient { item = item, count = 1 });
            return r;
        }

        [Test]
        public void RecipesSortIntoWorkbenchTabs()
        {
            Assert.AreEqual(RecipeCategory.Gear, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Weapon)));
            Assert.AreEqual(RecipeCategory.Gear, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Armor)));
            Assert.AreEqual(RecipeCategory.Potions, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Consumable, ConsumableEffect.HealHunter)));
            Assert.AreEqual(RecipeCategory.Potions, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Consumable, ConsumableEffect.RepairRaft)));
            Assert.AreEqual(RecipeCategory.Oils, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Consumable, ConsumableEffect.CoatWeapon)));
            Assert.AreEqual(RecipeCategory.Raft, CraftRules.CategoryOf(Recipe(RecipeKind.Craft, ItemType.Module)));
            Assert.AreEqual(RecipeCategory.Raft, CraftRules.CategoryOf(Recipe(RecipeKind.Upgrade, ItemType.Module)));
            Assert.AreEqual(RecipeCategory.Dismantle, CraftRules.CategoryOf(Recipe(RecipeKind.Dismantle, ItemType.Material)));
        }

        [Test]
        public void EveryTabHasALabelAndHint()
        {
            int tabs = System.Enum.GetValues(typeof(RecipeCategory)).Length;
            Assert.AreEqual(tabs, CraftRules.CategoryLabels.Length);
            Assert.AreEqual(tabs, CraftRules.CategoryHints.Length);
        }
    }
}
