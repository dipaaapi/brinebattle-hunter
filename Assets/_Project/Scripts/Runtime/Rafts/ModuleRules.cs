using System;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Auto-loot raft modules (idea U-13). Numbers follow the U-13 scaling table; only Lv 1
    /// (basic net: touch only, no hook) is built so far.
    /// </summary>
    public static class ModuleRules
    {
        public const int MaxLevel = 5;
        /// <summary>Durability lost when an alive drift bites the module.</summary>
        public const int AliveBite = 5;
        /// <summary>Durability restored per repair.</summary>
        public const int RepairChunk = 10;

        static readonly int[] hooks = { 0, 0, 1, 2, 2, 3 };
        static readonly float[] range = { 0, 1.5f, 4f, 6f, 8f, 10f };
        static readonly float[] interval = { 0, 20f, 12f, 10f, 8f, 6f };
        static readonly int[] baseDurability = { 0, 30, 40, 50, 60, 80 };

        static int L(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        public static int Hooks(int level) => hooks[L(level)];
        public static float Range(int level) => range[L(level)];
        public static float Interval(int level) => interval[L(level)];

        /// <summary>Base durability × rarity: Common ×1, +20% per step (Mythic ×2).</summary>
        public static int MaxDurability(int level, Rarity rarity) =>
            (int)Math.Round(baseDurability[L(level)] * (1f + 0.2f * (int)rarity));

        /// <summary>Cost per <see cref="RepairChunk"/> durability: level × (rarity + 1).</summary>
        public static int RepairCost(int level, Rarity rarity) => L(level) * ((int)rarity + 1);

        /// <summary>Lv 1–3 repair with materials; Lv 3–5 with essence (Lv 3 needs both).</summary>
        public static bool RepairUsesMaterials(int level) => L(level) <= 3;
        public static bool RepairUsesEssence(int level) => L(level) >= 3;
    }
}
