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
    
        static ItemDefinition Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.type = ItemType.Material;
            return item;
        }

        static RecipeDefinition Needs(params (ItemDefinition item, int count)[] inputs)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            foreach (var (item, count) in inputs) r.inputs.Add(new Ingredient { item = item, count = count });
            return r;
        }

        [Test]
        public void MissingCountsWhatTheBagStillNeeds()
        {
            ItemDefinition plank = Item("plank"), fang = Item("fang");
            var inv = new Inventory();
            inv.Add(plank, 1);
            var r = Needs((plank, 2), (fang, 3));
            Assert.AreEqual(4, CraftRules.Missing(inv, r));
            inv.Add(plank, 5);
            inv.Add(fang, 3);
            Assert.AreEqual(0, CraftRules.Missing(inv, r));
        }

        [Test]
        public void ReadyRecipesComeFirst()
        {
            ItemDefinition plank = Item("plank"), fang = Item("fang");
            var inv = new Inventory();
            inv.Add(plank, 2);
            RecipeDefinition far = Needs((fang, 5)), close = Needs((plank, 3)), ready = Needs((plank, 2)), alsoReady = Needs((plank, 1));
            var sorted = CraftRules.ByReadiness(inv, new[] { far, close, ready, alsoReady });
            CollectionAssert.AreEqual(new[] { ready, alsoReady, close, far }, sorted);
        }
    }
}
