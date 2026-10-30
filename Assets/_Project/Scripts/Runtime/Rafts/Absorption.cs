using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Absorption (Phase 8, GDD §6; numbers tunable): when the hunter loses a battle or flees, the
    /// monster eats one edge part of the raft and swims off gorged. It comes back later, stronger for
    /// every part inside it; beating it gives the parts back plus extra loot.
    /// Module battles don't eat parts (Claude's default until Ed decides). Plain C# for EditMode tests.
    /// </summary>
    public static class AbsorbRules
    {
        /// <summary>A raft this small or smaller is never eaten.</summary>
        public const int MinParts = 5;
        /// <summary>Chance each new drift is a gorged monster coming back, while one is out there.</summary>
        public const double ReturnChance = 0.3;
        /// <summary>Each eaten part makes the monster 20% stronger, up to double.</summary>
        public const float PerPart = 0.2f;
        public const int MaxStackedParts = 5;

        public static float StatScale(int parts) => 1f + PerPart * Math.Min(Math.Max(0, parts), MaxStackedParts);

        /// <summary>The gorged monster's battle stats: HP, ATK and DEF grow, speed stays.</summary>
        public static Combatant Gorge(Combatant monster, int parts)
        {
            if (parts <= 0) return monster;
            float k = StatScale(parts);
            monster.Name = $"Gorged {monster.Name}";
            monster.MaxHp = monster.Hp = (int)Math.Round(monster.MaxHp * k);
            monster.Attack = (int)Math.Round(monster.Attack * k);
            monster.Defense = (int)Math.Round(monster.Defense * k);
            return monster;
        }

        /// <summary>Extra loot rolls for beating a gorged monster: one per part it ate.</summary>
        public static int BonusRolls(int parts) => Math.Max(0, parts);
    }

    /// <summary>Monsters that ate raft parts and are still out there (kept in the save).</summary>
    public class GorgedLog
    {
        public class Entry
        {
            public string MonsterId;
            public readonly List<ItemDefinition> Parts = new List<ItemDefinition>();
        }

        readonly List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
        public int Count => entries.Count;

        public void Add(string monsterId, ItemDefinition part)
        {
            if (string.IsNullOrEmpty(monsterId) || part == null) return;
            var e = Find(monsterId);
            if (e == null) entries.Add(e = new Entry { MonsterId = monsterId });
            e.Parts.Add(part);
        }

        public int PartsIn(string monsterId) => Find(monsterId)?.Parts.Count ?? 0;

        /// <summary>Beaten: the parts come back and the monster leaves the log.</summary>
        public List<ItemDefinition> Recover(string monsterId)
        {
            var e = Find(monsterId);
            if (e == null) return new List<ItemDefinition>();
            entries.Remove(e);
            return e.Parts.ToList();
        }

        /// <summary>The oldest gorged monster comes back <see cref="AbsorbRules.ReturnChance"/> of the time; null otherwise.</summary>
        public Entry PickReturning(Random rng) =>
            entries.Count > 0 && rng.NextDouble() < AbsorbRules.ReturnChance ? entries[0] : null;

        public void Clear() => entries.Clear();

        Entry Find(string monsterId) => entries.FirstOrDefault(e => e.MonsterId == monsterId);
    }
}
