using System;
using System.Collections.Generic;

namespace BrineBattle.Loot
{
    /// <summary>One rolled drop. Generic so tests run without ScriptableObjects.</summary>
    public readonly struct Drop<T>
    {
        public readonly T Item;
        public readonly int Quantity;
        public Drop(T item, int quantity) { Item = item; Quantity = quantity; }
    }

    /// <summary>Independent per-entry rolls: each entry drops with its own chance.</summary>
    public static class LootRoller
    {
        public static List<Drop<T>> Roll<T>(IEnumerable<(T item, float chance, int min, int max)> entries, Random rng)
        {
            var drops = new List<Drop<T>>();
            foreach (var (item, chance, min, max) in entries)
            {
                if (item == null || chance <= 0f) continue;
                if (chance < 1f && rng.NextDouble() >= chance) continue;
                int lo = Math.Max(1, min);
                int hi = Math.Max(lo, max);
                drops.Add(new Drop<T>(item, rng.Next(lo, hi + 1)));
            }
            return drops;
        }
    }
}
