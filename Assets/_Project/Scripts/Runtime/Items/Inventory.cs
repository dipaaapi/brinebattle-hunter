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

        /// <summary>Salvages every stack that doesn't fit its target tier. Returns essence gained.</summary>
        public int SalvageIncompatible(Func<ItemType, Rarity> tierOf)
        {
            var bad = items.Where(kv => !SalvageRules.IsCompatible(kv.Key.type, kv.Key.rarity, tierOf(kv.Key.type))).ToList();
            int total = 0;
            foreach (var kv in bad) total += Salvage(kv.Key, kv.Value);
            return total;
        }
    }
}
