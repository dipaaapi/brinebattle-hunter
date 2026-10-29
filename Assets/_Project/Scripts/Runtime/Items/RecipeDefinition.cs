using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Items
{
    public enum RecipeKind { Craft = 0, Upgrade = 1, Dismantle = 2 }

    /// <summary>Workbench tab a recipe shows under.</summary>
    public enum RecipeCategory { Gear = 0, Potions = 1, Oils = 2, Raft = 3, Dismantle = 4 }

    [Serializable]
    public class Ingredient
    {
        public ItemDefinition item;
        [Min(1)] public int count = 1;
    }

    /// <summary>
    /// Turns some items into others (idea U-16): craft a module from raft parts, upgrade it to the
    /// next level, or dismantle it back into parts. Crafting proper (alchemy, gear) comes in Phase 6.
    /// </summary>
    [CreateAssetMenu(menuName = "BrineBattle/Recipe", fileName = "Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public RecipeKind kind;
        public List<Ingredient> inputs = new List<Ingredient>();
        public List<Ingredient> outputs = new List<Ingredient>();
    }

    /// <summary>Plain C# rules for recipes, for EditMode tests.</summary>
    public static class CraftRules
    {
        public static bool CanCraft(Inventory inv, RecipeDefinition r)
        {
            if (r == null || r.inputs.Count == 0) return false;
            foreach (var i in r.inputs)
                if (i.item == null || inv.Count(i.item) < i.count) return false;
            return true;
        }

        /// <summary>All or nothing: takes the inputs and adds the outputs.</summary>
        public static bool Craft(Inventory inv, RecipeDefinition r)
        {
            if (!CanCraft(inv, r)) return false;
            foreach (var i in r.inputs) inv.Remove(i.item, i.count);
            foreach (var o in r.outputs) inv.Add(o.item, o.count);
            return true;
        }

        public static readonly string[] CategoryLabels = { "Gear", "Potions", "Oils", "Raft", "Dismantle" };

        /// <summary>What each workbench tab is for.</summary>
        public static readonly string[] CategoryHints =
        {
            "Weapons and armor. Equip them from the Bag (B).",
            "Heal the hunter or repair the raft in battle.",
            "Coat your weapon: attacks take the oil's element for a few rounds.",
            "Raft modules and their upgrades. Take a module off the raft to upgrade it.",
            "Take crafted things apart to get materials back.",
        };

        /// <summary>Dismantle recipes by kind; the rest by what the first output is.</summary>
        public static RecipeCategory CategoryOf(RecipeDefinition r)
        {
            if (r.kind == RecipeKind.Dismantle) return RecipeCategory.Dismantle;
            var item = r.outputs.Count > 0 ? r.outputs[0].item : null;
            if (item == null) return RecipeCategory.Raft;
            switch (item.type)
            {
                case ItemType.Weapon:
                case ItemType.Armor:
                    return RecipeCategory.Gear;
                case ItemType.Consumable:
                    return item.effect == ConsumableEffect.CoatWeapon ? RecipeCategory.Oils : RecipeCategory.Potions;
                default:
                    return RecipeCategory.Raft;
            }
        }

        /// <summary>How many more items the bag needs before this recipe can be made (0 = ready). Ed: show what is missing.</summary>
        public static int Missing(Inventory inv, RecipeDefinition r)
        {
            int missing = 0;
            foreach (var i in r.inputs)
                if (i.item != null) missing += System.Math.Max(0, i.count - inv.Count(i.item));
            return missing;
        }

        /// <summary>Workbench order: ready recipes first, then the ones closest to ready.</summary>
        public static List<RecipeDefinition> ByReadiness(Inventory inv, IEnumerable<RecipeDefinition> recipes)
        {
            var list = new List<RecipeDefinition>(recipes);
            var order = new Dictionary<RecipeDefinition, int>();
            for (int n = 0; n < list.Count; n++) order[list[n]] = n;
            list.Sort((a, b) =>
            {
                int c = Missing(inv, a).CompareTo(Missing(inv, b));
                return c != 0 ? c : order[a].CompareTo(order[b]);
            });
            return list;
        }

        public static string Describe(List<Ingredient> list)
        {
            var parts = new List<string>();
            foreach (var i in list) if (i.item != null) parts.Add($"{i.count} {i.item.displayName}");
            return string.Join(" + ", parts);
        }
    }
}
