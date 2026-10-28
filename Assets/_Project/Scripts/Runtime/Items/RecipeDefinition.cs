using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Items
{
    public enum RecipeKind { Craft = 0, Upgrade = 1, Dismantle = 2 }

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

        public static string Describe(List<Ingredient> list)
        {
            var parts = new List<string>();
            foreach (var i in list) if (i.item != null) parts.Add($"{i.count} {i.item.displayName}");
            return string.Join(" + ", parts);
        }
    }
}
