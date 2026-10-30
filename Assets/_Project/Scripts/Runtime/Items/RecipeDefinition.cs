using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Items
{
    public enum RecipeKind { Craft = 0, Upgrade = 1, Dismantle = 2 }

    /// <summary>
    /// Workbench tab a recipe shows under (Ed's ask): gear, the player's potions and oils, things for
    /// pets, the raft. Dismantle recipes live in the Salvage Bin, not the workbench.
    /// </summary>
    public enum RecipeCategory { Gear = 0, Player = 1, Pet = 2, Raft = 3, Dismantle = 4 }

    /// <summary>Sub-tab inside a workbench tab (Ed's ask: hammers, planks and auto-loot apart; weapon, armor, accessory apart).</summary>
    public enum RecipeSub
    {
        Weapon = 0, Armor = 1, Accessory = 2,
        Potions = 10, Oils = 11,
        Treats = 20, Habitats = 21,
        Parts = 30, Modules = 31, Hammers = 32,
        Dismantle = 40,
    }

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

        public static readonly string[] CategoryLabels = { "Gear", "Player", "Pet", "Raft", "Dismantle" };

        /// <summary>What each workbench tab is for.</summary>
        public static readonly string[] CategoryHints =
        {
            "Weapons, armor and accessories. Equip them from the Bag (B).",
            "Potions for you and the raft, and oils for your weapon.",
            "Treats for your pets and habitats where they live.",
            "Planks and parts, auto-loot modules, and hammers.",
            "Take crafted things apart to get materials back (Salvage Bin).",
        };

        /// <summary>The sub-tabs of each tab, in order.</summary>
        public static RecipeSub[] SubsOf(RecipeCategory c)
        {
            switch (c)
            {
                case RecipeCategory.Gear: return new[] { RecipeSub.Weapon, RecipeSub.Armor, RecipeSub.Accessory };
                case RecipeCategory.Player: return new[] { RecipeSub.Potions, RecipeSub.Oils };
                case RecipeCategory.Pet: return new[] { RecipeSub.Treats, RecipeSub.Habitats };
                case RecipeCategory.Raft: return new[] { RecipeSub.Parts, RecipeSub.Modules, RecipeSub.Hammers };
                default: return new[] { RecipeSub.Dismantle };
            }
        }

        public static string Label(RecipeSub s) => s == RecipeSub.Parts ? "Planks & parts" : s == RecipeSub.Modules ? "Auto-loot" : s.ToString();

        /// <summary>Dismantle recipes by kind; the rest by what the first output is.</summary>
        public static RecipeSub SubOf(RecipeDefinition r)
        {
            if (r.kind == RecipeKind.Dismantle) return RecipeSub.Dismantle;
            var item = r.outputs.Count > 0 ? r.outputs[0].item : null;
            if (item == null) return RecipeSub.Parts;
            switch (item.type)
            {
                case ItemType.Weapon: return RecipeSub.Weapon;
                case ItemType.Armor: return RecipeSub.Armor;
                case ItemType.Accessory: return RecipeSub.Accessory;
                case ItemType.Consumable:
                    if (item.effect == ConsumableEffect.CoatWeapon) return RecipeSub.Oils;
                    return item.effect == ConsumableEffect.HealCompanion ? RecipeSub.Treats : RecipeSub.Potions;
                case ItemType.Habitat: return RecipeSub.Habitats;
                case ItemType.Module: return RecipeSub.Modules;
                case ItemType.Tool: return RecipeSub.Hammers;
                default: return RecipeSub.Parts;
            }
        }

        public static RecipeCategory CategoryOf(RecipeSub s) =>
            s <= RecipeSub.Accessory ? RecipeCategory.Gear
            : s <= RecipeSub.Oils ? RecipeCategory.Player
            : s <= RecipeSub.Habitats ? RecipeCategory.Pet
            : s <= RecipeSub.Hammers ? RecipeCategory.Raft
            : RecipeCategory.Dismantle;

        public static RecipeCategory CategoryOf(RecipeDefinition r) => CategoryOf(SubOf(r));

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
