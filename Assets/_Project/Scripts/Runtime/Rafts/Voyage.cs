using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// How many raft types must leave the sea to beat the game (Ed's ask): Easy 1, Normal 2, Hard 3.
    /// Harder also means stronger monsters and less loot.
    /// </summary>
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
    }

    /// <summary>Difficulty numbers (Ed's ask, tunable). Plain C# for EditMode tests.</summary>
    public static class DifficultyRules
    {
        public static readonly string[] Labels = { "Easy", "Normal", "Hard" };

        /// <summary>Raft types that must be finished and leave the sea to beat the game.</summary>
        public static int TypesToWin(Difficulty d) => (int)d + 1;

        /// <summary>Monster HP, ATK and DEF: ×1, ×1.25, ×1.5.</summary>
        public static float EnemyScale(Difficulty d) => 1f + 0.25f * (int)d;

        /// <summary>Loot amounts: ×1, ×0.8, ×0.6.</summary>
        public static float LootScale(Difficulty d) => 1f - 0.2f * (int)d;

        public static string Describe(Difficulty d)
        {
            string types = TypesToWin(d) == 1 ? "1 raft type" : $"{TypesToWin(d)} raft types";
            return $"Finish {types} to win. Monsters ×{EnemyScale(d):0.##}, loot ×{LootScale(d):0.##}.";
        }

        /// <summary>
        /// A loot amount after the difficulty: scaled, with the leftover fraction as a chance of one more.
        /// Can drop to 0 on Normal and Hard (loot is harder to get).
        /// </summary>
        public static int ScaleLoot(int quantity, Difficulty d, Random rng)
        {
            float q = quantity * LootScale(d);
            int whole = (int)Math.Floor(q);
            return whole + (rng.NextDouble() < q - whole ? 1 : 0);
        }
    }

    /// <summary>
    /// The vehicle each raft type builds toward (Ed's ask): five parts, one opening at each raft level
    /// from Lv 2 to Lv 6. With all five on, the Lv 6 raft and the last boss beaten, the vehicle can
    /// leave the sea: the plane takes off, the train rides its track out, the ship sets sail.
    /// </summary>
    public static class VesselRules
    {
        public const int Parts = 5;

        static readonly string[] Train = { "Sea Track", "Iron Wheels", "Boiler", "Smokestack", "Locomotive Cab" };
        static readonly string[] Plane = { "Wings", "Tail Fin", "Propeller", "Engine", "Cockpit" };
        static readonly string[] Ship = { "Hull Walls", "Cannons", "Bow & Bowsprit", "Masts & Sails", "Stern Castle" };

        /// <summary>The part's name; <paramref name="slot"/> is 1-5.</summary>
        public static string PartName(RaftKind kind, int slot)
        {
            var names = kind == RaftKind.Fortress ? Train : kind == RaftKind.Sky ? Plane : kind == RaftKind.Beast ? Ship : null;
            return names == null || slot < 1 || slot > Parts ? "" : names[slot - 1];
        }

        /// <summary>The raft level a part opens at: slot 1 at Lv 2 … slot 5 at Lv 6.</summary>
        public static int UnlockLevel(int slot) => slot + 1;

        /// <summary>The rarity of the part for a slot (its level's rarity).</summary>
        public static Rarity RarityOf(int slot) => (Rarity)slot;

        public static string Vehicle(RaftKind kind) =>
            kind == RaftKind.Fortress ? "sea train" : kind == RaftKind.Sky ? "plane" : kind == RaftKind.Beast ? "pirate ship" : "raft";

        /// <summary>What leaving looks like, for the button.</summary>
        public static string LeaveVerb(RaftKind kind) =>
            kind == RaftKind.Fortress ? "Ride the track out of this sea" : kind == RaftKind.Sky ? "Take off and fly out of this sea" : "Set sail out of this sea";
    }

    /// <summary>
    /// Bosses (Ed's ask): one per raft level. To level up, beat the boss of the raft's level; it comes
    /// once enough monsters are beaten. The Lv 6 boss guards the way out of the sea.
    /// </summary>
    public static class BossRules
    {
        /// <summary>The boss turns up once the level-up kill count is reached.</summary>
        public static bool Due(RaftGrid g) =>
            g.BossesNeeded && !g.BossBeaten(g.Tier) && g.Kills >= RaftRules.KillsToUpgrade(g.Tier);

        /// <summary>Seconds between boss sightings while one is due.</summary>
        public const float SpawnSeconds = 25f;
    }

    /// <summary>
    /// The whole voyage across seas (Ed's ask): the difficulty, which sea this is, and which raft types
    /// already left a sea. After leaving, a new sea starts back at Lv 1, harder, keeping pets, gear and
    /// the bag. A type that already left can't be picked again until the game is won.
    /// </summary>
    public class Voyage
    {
        public Difficulty Difficulty { get; set; } = Difficulty.Normal;
        public int Sea { get; private set; } = 1;
        readonly List<RaftKind> escaped = new List<RaftKind>();
        public IReadOnlyList<RaftKind> Escaped => escaped;

        public int TypesToWin => DifficultyRules.TypesToWin(Difficulty);
        public bool Won => escaped.Distinct().Count() >= TypesToWin;

        /// <summary>Each new sea makes monsters 35% stronger.</summary>
        public const float SeaStep = 0.35f;
        public float SeaScale => 1f + SeaStep * (Sea - 1);
        /// <summary>Monster HP/ATK/DEF multiplier: the sea times the difficulty.</summary>
        public float EnemyScale => SeaScale * DifficultyRules.EnemyScale(Difficulty);

        /// <summary>True when this type may be picked for the raft (not one that already left, unless the game is won).</summary>
        public bool CanPick(RaftKind kind) => kind != RaftKind.Plain && (Won || !escaped.Contains(kind));

        /// <summary>Records the departure and moves on to the next sea.</summary>
        public void Leave(RaftKind kind)
        {
            if (kind != RaftKind.Plain) escaped.Add(kind);
            Sea++;
        }

        public void Restore(Difficulty difficulty, int sea, IEnumerable<RaftKind> left)
        {
            Difficulty = difficulty;
            Sea = Math.Max(1, sea);
            escaped.Clear();
            if (left != null) escaped.AddRange(left);
        }

        /// <summary>Scales a monster's HP, ATK and DEF (not SPD) by <paramref name="k"/>.</summary>
        public static void Scale(Combatant c, float k)
        {
            if (Math.Abs(k - 1f) < 1e-4f) return;
            c.MaxHp = Math.Max(1, (int)Math.Round(c.MaxHp * k));
            c.Hp = Math.Max(1, (int)Math.Round(c.Hp * k));
            c.Attack = (int)Math.Round(c.Attack * k);
            c.Defense = (int)Math.Round(c.Defense * k);
        }
    }
}
