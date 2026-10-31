using System;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Raft and ship evolution (Ed's ask, 2026-10-31; numbers tunable, Claude's defaults until Ed confirms).
    /// The raft levels 1 → 50, changing a little every level, and is a simple boat at 50. Then the player picks a
    /// ship line (Sea level = STR, Aerial = INT, Undersea = DEX); the ship starts again at Lv 1 and evolves up to
    /// Lv 100. After that the next evolution is "Coming soon". There is no leaving the sea: the goal is fame.
    /// The six rarity tiers (parts, hammers, essence, bosses) spread over the whole climb: raft Lv 1-16 Common,
    /// 17-33 Uncommon, 34-50 Rare; ship Lv 1-33 Epic, 34-66 Legendary, 67-100 Mythic. Plain C# for EditMode tests.
    /// </summary>
    public static class EvolutionRules
    {
        public const int RaftMax = 50;
        public const int ShipMax = 100;
        public const string NextEvolution = "Coming soon";

        public static int MaxLevel(RaftKind line) => line == RaftKind.Plain ? RaftMax : ShipMax;

        /// <summary>The rarity tier at a level: what parts, hammers, essence and the boss must be.</summary>
        public static Rarity TierOf(RaftKind line, int level)
        {
            if (line == RaftKind.Plain) return level <= 16 ? Rarity.Common : level <= 33 ? Rarity.Uncommon : Rarity.Rare;
            return level <= 33 ? Rarity.Epic : level <= 66 ? Rarity.Legendary : Rarity.Mythic;
        }

        /// <summary>The first level of a tier's band (loading older saves).</summary>
        public static int FirstLevelOf(RaftKind line, Rarity tier)
        {
            if (line == RaftKind.Plain) return tier <= Rarity.Common ? 1 : tier == Rarity.Uncommon ? 17 : 34;
            return tier <= Rarity.Epic ? 1 : tier == Rarity.Legendary ? 34 : 67;
        }

        /// <summary>
        /// Levels whose boss must be beaten before the next level-up: the end of each tier band (raft 16, 33,
        /// 50 = the ship pick; ship 33, 66, 99 = before the last level).
        /// </summary>
        public static bool BossGate(RaftKind line, int level) => line == RaftKind.Plain
            ? level == 16 || level == 33 || level >= RaftMax
            : level == 33 || level == 66 || level == ShipMax - 1;

        // Per level-up (small, because there are 150 of them).
        public static int Kills(Rarity tier) => 2 + (int)tier;
        public static int Materials(Rarity tier) => 2 + 2 * (int)tier;
        public static int Essence(Rarity tier) => 1 + 2 * (int)tier;

        /// <summary>
        /// Ship parts (Ed: ships have more parts to build): part n opens at ship Lv 20 × n (the fifth at Lv 99),
        /// and the ship can't level past that level until the part is on. Returns the part (1-5) needed to go
        /// from <paramref name="level"/> to the next, or 0.
        /// </summary>
        public static int PartNeeded(RaftKind line, int level)
        {
            if (line == RaftKind.Plain) return 0;
            if (level == ShipMax - 1) return VesselRules.Parts;
            return level % 20 == 0 && level / 20 < VesselRules.Parts ? level / 20 : 0;
        }

        public static int PartOpensAt(int slot) => slot >= VesselRules.Parts ? ShipMax - 1 : 20 * slot;

        public static string Stat(RaftKind line)
        {
            switch (line)
            {
                case RaftKind.Sea: return "STR";
                case RaftKind.Aerial: return "INT";
                case RaftKind.Undersea: return "DEX";
                default: return "";
            }
        }

        public static string LineName(RaftKind line)
        {
            switch (line)
            {
                case RaftKind.Sea: return "Sea-level ship";
                case RaftKind.Aerial: return "Aerial ship";
                case RaftKind.Undersea: return "Undersea ship";
                default: return "Raft";
            }
        }

        /// <summary>"Raft Lv 12" or "Aerial ship Lv 3".</summary>
        public static string Label(RaftKind line, int level) => $"{LineName(line)} Lv {level}";

        /// <summary>What it looks like at this level, for the HUD and the Raft tree road.</summary>
        public static string FormName(RaftKind line, int level)
        {
            string[] names;
            switch (line)
            {
                case RaftKind.Sea: names = new[] { "Sloop", "Brigantine", "Frigate", "Galleon", "Flagship" }; break;
                case RaftKind.Aerial: names = new[] { "Glider", "Balloon Skiff", "Airship", "Sky Cruiser", "Sky Citadel" }; break;
                case RaftKind.Undersea: names = new[] { "Diving Bell", "Mini-sub", "Submarine", "Deep Cruiser", "Abyss Ark" }; break;
                default: names = new[] { "Plank Raft", "Lashed Raft", "Railed Raft", "Sailing Raft", "Cabin Raft", "Simple Boat" }; break;
            }
            if (line == RaftKind.Plain) return level >= RaftMax ? names[5] : names[Math.Min(4, (level - 1) / 10)];
            return level >= ShipMax ? names[4] : names[Math.Min(3, (level - 1) / 25)];
        }

        /// <summary>
        /// The ship line's bonus grows as it evolves: the line's base bonus × (1 + level / 50), so a Lv 100 ship
        /// has three times its Lv 1 bonus. STR (Sea) is tough and strong, INT (Aerial) reaches far, DEX (Undersea) is fast.
        /// </summary>
        public static RaftBonus Bonus(RaftKind line, int level)
        {
            var b = RaftTypeRules.BonusOf(line);
            float k = 1f + Math.Max(0, level) / 50f;
            return new RaftBonus
            {
                RaftHpPercent = (int)Math.Round(b.RaftHpPercent * k),
                Attack = (int)Math.Round(b.Attack * k),
                Defense = (int)Math.Round(b.Defense * k),
                Speed = (int)Math.Round(b.Speed * k),
                Reach = b.Reach * k,
            };
        }
    }
}
