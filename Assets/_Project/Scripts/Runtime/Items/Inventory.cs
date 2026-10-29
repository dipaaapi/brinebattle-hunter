using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Elements;

namespace BrineBattle.Items
{
    /// <summary>Monster Essence is a currency keyed by rarity and element (GDD §11).</summary>
    public readonly struct EssenceKey : IEquatable<EssenceKey>
    {
        public readonly Rarity Rarity;
        public readonly Element Element;
        public EssenceKey(Rarity rarity, Element element) { Rarity = rarity; Element = element; }
        public bool Equals(EssenceKey o) => Rarity == o.Rarity && Element == o.Element;
        public override bool Equals(object obj) => obj is EssenceKey o && Equals(o);
        public override int GetHashCode() => ((int)Rarity * 397) ^ (int)Element;
        public override string ToString() => $"{Rarity} {(Element == Element.None ? "Neutral" : Element.ToString())} Essence";
    }

    /// <summary>The hunter's bag: item stacks plus Monster Essence.</summary>
    public class Inventory
    {
        readonly Dictionary<ItemDefinition, int> items = new Dictionary<ItemDefinition, int>();
        readonly Dictionary<EssenceKey, int> essence = new Dictionary<EssenceKey, int>();

        public event Action Changed;

        public IEnumerable<KeyValuePair<ItemDefinition, int>> Items =>
            items.OrderBy(kv => kv.Key.type).ThenByDescending(kv => kv.Key.rarity).ThenBy(kv => kv.Key.displayName);

        public IEnumerable<KeyValuePair<EssenceKey, int>> Essence =>
            essence.OrderByDescending(kv => kv.Key.Rarity).ThenBy(kv => kv.Key.Element);

        public int Count(ItemDefinition item) => item != null && items.TryGetValue(item, out var n) ? n : 0;
        public int EssenceCount(EssenceKey key) => essence.TryGetValue(key, out var n) ? n : 0;
        public bool IsEmpty => items.Count == 0 && essence.Count == 0;

        /// <summary>Empties the bag (loading a save).</summary>
        public void Clear()
        {
            items.Clear();
            essence.Clear();
            Changed?.Invoke();
        }

        public void AddEssence(EssenceKey key, int amount)
        {
            if (amount <= 0) return;
            essence[key] = EssenceCount(key) + amount;
            Changed?.Invoke();
        }

        public void Add(ItemDefinition item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            items[item] = Count(item) + quantity;
            Changed?.Invoke();
        }

        public bool Remove(ItemDefinition item, int quantity)
        {
            int have = Count(item);
            if (quantity <= 0 || have < quantity) return false;
            if (have == quantity) items.Remove(item);
            else items[item] = have - quantity;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Total count of Material items (any rarity).</summary>
        public int MaterialCount() => items.Where(kv => kv.Key.type == ItemType.Material).Sum(kv => kv.Value);

        /// <summary>Spends materials, lowest rarity first. All or nothing.</summary>
        public bool SpendMaterials(int amount)
        {
            if (amount <= 0) return true;
            if (MaterialCount() < amount) return false;
            foreach (var item in items.Keys.Where(i => i.type == ItemType.Material).OrderBy(i => i.rarity).ThenBy(i => i.displayName).ToList())
            {
                int take = Math.Min(amount, items[item]);
                if (take == items[item]) items.Remove(item);
                else items[item] -= take;
                amount -= take;
                if (amount == 0) break;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Turns items into essence of the item's rarity and element. Returns essence gained.</summary>
        public int Salvage(ItemDefinition item, int quantity)
        {
            int per = item != null ? SalvageRules.EssenceYield(item.type, item.rarity) : 0;
            if (per == 0 || !Remove(item, quantity)) return 0;
            var key = new EssenceKey(item.rarity, item.element);
            int gained = per * quantity;
            essence[key] = EssenceCount(key) + gained;
            Changed?.Invoke();
            return gained;
        }

        /// <summary>Total essence with rarity ≥ <paramref name="minRarity"/>, any element.</summary>
        public int EssenceAtLeast(Rarity minRarity) =>
            essence.Where(kv => kv.Key.Rarity >= minRarity).Sum(kv => kv.Value);

        /// <summary>
        /// Spends essence of rarity ≥ <paramref name="minRarity"/>, lowest rarity first.
        /// All or nothing: returns false and spends nothing if there isn't enough.
        /// </summary>
        public bool SpendEssence(Rarity minRarity, int amount)
        {
            if (amount <= 0) return true;
            if (EssenceAtLeast(minRarity) < amount) return false;
            foreach (var key in essence.Keys.Where(k => k.Rarity >= minRarity).OrderBy(k => k.Rarity).ThenBy(k => k.Element).ToList())
            {
                int take = Math.Min(amount, essence[key]);
                if (take == essence[key]) essence.Remove(key);
                else essence[key] -= take;
                amount -= take;
                if (amount == 0) break;
            }
            Changed?.Invoke();
            return true;
        }

        /// <summary>Salvages every stack that doesn't fit its target tier. Returns essence gained.</summary>
        public int SalvageIncompatible(Func<ItemType, Rarity> tierOf, Func<ItemDefinition, bool> keep = null)
        {
            var bad = items.Where(kv => !SalvageRules.IsCompatible(kv.Key.type, kv.Key.rarity, tierOf(kv.Key.type))
                && (keep == null || !keep(kv.Key))).ToList();
            int total = 0;
            foreach (var kv in bad) total += Salvage(kv.Key, kv.Value);
            return total;
        }
    }
}
