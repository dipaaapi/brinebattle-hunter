using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Elements;

namespace BrineBattle.Items
{
    public enum GearSlot { Weapon = 0, Armor = 1, Accessory = 2 }

    /// <summary>Gear power and refine (+) numbers (GDD §9). Plain C# for EditMode tests.</summary>
    public static class GearRules
    {
        public const int MaxRefine = 10;

        public static bool IsGear(ItemType t) => t == ItemType.Weapon || t == ItemType.Armor || t == ItemType.Accessory;

        public static GearSlot SlotOf(ItemType t) => t == ItemType.Armor ? GearSlot.Armor : t == ItemType.Accessory ? GearSlot.Accessory : GearSlot.Weapon;

        /// <summary>An accessory's power (with its +) splits between attack (the larger half) and defense.</summary>
        public static int AccessoryAttack(int power) => (Math.Max(0, power) + 1) / 2;
        public static int AccessoryDefense(int power) => Math.Max(0, power) / 2;

        /// <summary>What each + adds: a quarter of the base power, at least 1.</summary>
        public static int RefineStep(int basePower) => Math.Max(1, basePower / 4);

        /// <summary>Base power plus <see cref="RefineStep"/> per refine level (0-<see cref="MaxRefine"/>).</summary>
        public static int Power(int basePower, int refine) =>
            Math.Max(0, basePower) + RefineStep(basePower) * Math.Max(0, Math.Min(MaxRefine, refine));

        /// <summary>Essence to go from +n to +(n+1): 2 × (n+1), of the item's rarity or higher. 0 when maxed.</summary>
        public static int RefineCost(int current) => current >= MaxRefine ? 0 : 2 * (Math.Max(0, current) + 1);
    }

    /// <summary>
    /// The hunter's weapon and armor slots (GDD §9). Equipping takes the item out of the bag;
    /// unequipping or swapping puts it back. Refine levels are kept per item kind, so a refined
    /// piece keeps its + when taken off (one copy per kind until saves track single items).
    /// </summary>
    public class Equipment
    {
        readonly Dictionary<GearSlot, ItemDefinition> slots = new Dictionary<GearSlot, ItemDefinition>();
        readonly Dictionary<ItemDefinition, int> refines = new Dictionary<ItemDefinition, int>();

        public event Action Changed;
        /// <summary>Essence saved per refine (a Mystic companion, Phase 7). Cost never drops below 1.</summary>
        public int RefineDiscount { get; set; }

        public ItemDefinition Get(GearSlot slot) => slots.TryGetValue(slot, out var item) ? item : null;

        public int RefineOf(ItemDefinition item) => item != null && refines.TryGetValue(item, out var n) ? n : 0;

        public int PowerOf(GearSlot slot)
        {
            var item = Get(slot);
            return item == null ? 0 : GearRules.Power(item.gearPower, RefineOf(item));
        }

        public int Attack => PowerOf(GearSlot.Weapon) + GearRules.AccessoryAttack(PowerOf(GearSlot.Accessory));
        public int Defense => PowerOf(GearSlot.Armor) + GearRules.AccessoryDefense(PowerOf(GearSlot.Accessory));

        /// <summary>
        /// The weapon's element is the hunter's attack element; with a neutral weapon an accessory's
        /// element counts (it also gives sun/moon affinity, Idea Round 4). Nothing = neutral.
        /// </summary>
        public Element Element
        {
            get
            {
                var w = Get(GearSlot.Weapon)?.element ?? Element.None;
                return w != Element.None ? w : Get(GearSlot.Accessory)?.element ?? Element.None;
            }
        }

        /// <summary>Gear that is in the bag and fits the slot's tier (rarity ≥ tier).</summary>
        public bool CanEquip(ItemDefinition item, Inventory inv, Rarity tier) =>
            item != null && GearRules.IsGear(item.type) && inv.Count(item) > 0
            && SalvageRules.IsCompatible(item.type, item.rarity, tier);

        /// <summary>Takes one from the bag into its slot; whatever was there goes back to the bag.</summary>
        public bool Equip(ItemDefinition item, Inventory inv, Rarity tier)
        {
            if (!CanEquip(item, inv, tier)) return false;
            var slot = GearRules.SlotOf(item.type);
            var old = Get(slot);
            inv.Remove(item, 1);
            if (old != null) inv.Add(old, 1);
            slots[slot] = item;
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(GearSlot slot, Inventory inv)
        {
            var item = Get(slot);
            if (item == null) return false;
            slots.Remove(slot);
            inv.Add(item, 1);
            Changed?.Invoke();
            return true;
        }

        public int RefineCost(GearSlot slot)
        {
            var item = Get(slot);
            if (item == null) return 0;
            int cost = GearRules.RefineCost(RefineOf(item));
            return cost == 0 ? 0 : Math.Max(1, cost - RefineDiscount);
        }

        public bool CanRefine(GearSlot slot, Inventory inv)
        {
            var item = Get(slot);
            int cost = RefineCost(slot);
            return item != null && cost > 0 && inv.EssenceAtLeast(item.rarity) >= cost;
        }

        /// <summary>+1 on the equipped piece, paid with essence of its rarity or higher. Never fails for now (Alchemy Mastery later).</summary>
        public bool Refine(GearSlot slot, Inventory inv)
        {
            if (!CanRefine(slot, inv)) return false;
            var item = Get(slot);
            if (!inv.SpendEssence(item.rarity, RefineCost(slot))) return false;
            refines[item] = RefineOf(item) + 1;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Refine level per item kind that has one (for saving).</summary>
        public IEnumerable<KeyValuePair<ItemDefinition, int>> Refines => refines;

        /// <summary>Puts gear and refine levels back as saved, without touching the bag.</summary>
        public void Restore(ItemDefinition weapon, ItemDefinition armor, IEnumerable<KeyValuePair<ItemDefinition, int>> refineLevels, ItemDefinition accessory = null)
        {
            slots.Clear();
            refines.Clear();
            if (weapon != null) slots[GearSlot.Weapon] = weapon;
            if (armor != null) slots[GearSlot.Armor] = armor;
            if (accessory != null) slots[GearSlot.Accessory] = accessory;
            foreach (var kv in refineLevels)
                if (kv.Key != null) refines[kv.Key] = Math.Max(0, Math.Min(GearRules.MaxRefine, kv.Value));
            Changed?.Invoke();
        }

        /// <summary>Passives from both slots, no repeats (the same passive twice doesn't stack).</summary>
        public IEnumerable<GearPassive> Passives =>
            slots.Values.SelectMany(GearSkillRules.Passives).Distinct();

        public IEnumerable<GearActive> Actives =>
            slots.Values.SelectMany(GearSkillRules.Actives).Distinct();

        /// <summary>"Eel-Fang Spear +2" or "(none)".</summary>
        public string Label(GearSlot slot)
        {
            var item = Get(slot);
            if (item == null) return "(none)";
            int r = RefineOf(item);
            return r > 0 ? $"{item.displayName} +{r}" : item.displayName;
        }
    }
}
