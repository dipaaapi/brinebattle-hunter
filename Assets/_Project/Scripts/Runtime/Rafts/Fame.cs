using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>One achievement: a goal and how much fame it gives.</summary>
    public class Achievement
    {
        public string Id, Title, Goal;
        public int Fame;
        public Func<FameBook, bool> Done;
    }

    /// <summary>
    /// Fame and achievements (Ed's ask, 2026-10-31): the game's goal is the player's fame, not leaving the sea.
    /// First version, local only (no online leaderboard yet): fame comes from monsters, bosses, tames, crafts and
    /// every new best raft or ship level, times the difficulty; achievements add a lump of fame once.
    /// Numbers are Claude's defaults until Ed confirms. Plain C# for EditMode tests.
    /// </summary>
    public class FameBook
    {
        public int Points { get; private set; }
        public int Kills, Bosses, Tames, Crafts;
        /// <summary>Best raft level reached, and best ship level on any line.</summary>
        public int BestRaft = 1, BestShip;
        readonly HashSet<string> unlocked = new HashSet<string>();
        public IEnumerable<string> Unlocked => unlocked;
        public bool Has(string id) => unlocked.Contains(id);

        /// <summary>Fame multiplier by difficulty: Easy ×1, Normal ×1.5, Hard ×2.</summary>
        public static float Multiplier(Difficulty d) => 1f + 0.5f * (int)d;

        public event Action<Achievement> Earned;

        int Add(int basePoints, Difficulty d)
        {
            int p = (int)Math.Round(basePoints * Multiplier(d));
            Points += p;
            return p;
        }

        /// <summary>A monster beaten: 1 + its rarity step (a boss: 25 × (1 + step)).</summary>
        public int Kill(Rarity r, bool boss, Difficulty d)
        {
            Kills++;
            if (boss) Bosses++;
            int p = Add(boss ? 25 * (1 + (int)r) : 1 + (int)r, d);
            Check(d);
            return p;
        }

        public int Tame(Rarity r, Difficulty d) { Tames++; int p = Add(3 + (int)r, d); Check(d); return p; }
        public int Craft(Difficulty d) { Crafts++; int p = Add(1, d); Check(d); return p; }

        /// <summary>
        /// Called whenever the raft changes: a new best raft level gives 5 fame a level, a new best ship level 10.
        /// Returns the fame gained (0 when nothing is new).
        /// </summary>
        public int Reached(RaftKind line, int level, Difficulty d)
        {
            int gained = 0;
            if (line == RaftKind.Plain && level > BestRaft) { gained += Add(5 * (level - BestRaft), d); BestRaft = level; }
            if (line != RaftKind.Plain)
            {
                BestRaft = Math.Max(BestRaft, EvolutionRules.RaftMax);
                if (level > BestShip) { gained += Add(10 * (level - BestShip), d); BestShip = level; }
            }
            Check(d);
            return gained;
        }

        /// <summary>Unlocks every achievement whose goal is met (each once), adding its fame.</summary>
        public List<Achievement> Check(Difficulty d)
        {
            var got = new List<Achievement>();
            foreach (var a in All)
                if (!unlocked.Contains(a.Id) && a.Done(this))
                {
                    unlocked.Add(a.Id);
                    Add(a.Fame, d);
                    got.Add(a);
                    Earned?.Invoke(a);
                }
            return got;
        }

        public void Restore(int points, int kills, int bosses, int tames, int crafts, int bestRaft, int bestShip, IEnumerable<string> ids)
        {
            Points = points; Kills = kills; Bosses = bosses; Tames = tames; Crafts = crafts;
            BestRaft = Math.Max(1, bestRaft); BestShip = bestShip;
            unlocked.Clear();
            if (ids != null) foreach (var id in ids) if (!string.IsNullOrEmpty(id)) unlocked.Add(id);
        }

        /// <summary>A title for the HUD from the fame so far.</summary>
        public string Rank => Points >= 20000 ? "Legend of the Brine" : Points >= 5000 ? "Famed Captain" : Points >= 1000 ? "Captain"
            : Points >= 250 ? "Sailor" : Points >= 50 ? "Deckhand" : "Castaway";

        static Achievement A(string id, string title, string goal, int fame, Func<FameBook, bool> done) =>
            new Achievement { Id = id, Title = title, Goal = goal, Fame = fame, Done = done };

        public static readonly Achievement[] All =
        {
            A("first_blood", "First Catch", "Beat a monster", 5, f => f.Kills >= 1),
            A("hunter_50", "Seasoned Hunter", "Beat 50 monsters", 50, f => f.Kills >= 50),
            A("hunter_500", "Terror of the Brine", "Beat 500 monsters", 300, f => f.Kills >= 500),
            A("boss_1", "Boss Breaker", "Beat a boss", 50, f => f.Bosses >= 1),
            A("boss_6", "Every Crown", "Beat 6 bosses", 400, f => f.Bosses >= 6),
            A("tamer_1", "New Friend", "Tame a minion", 10, f => f.Tames >= 1),
            A("tamer_10", "Pack Leader", "Tame 10 minions", 80, f => f.Tames >= 10),
            A("crafter_25", "Busy Bench", "Craft 25 things", 30, f => f.Crafts >= 25),
            A("raft_10", "Lashed Together", "Raft Lv 10", 25, f => f.BestRaft >= 10),
            A("raft_25", "Railed and Ready", "Raft Lv 25", 60, f => f.BestRaft >= 25),
            A("raft_50", "A Real Boat", "Raft Lv 50", 150, f => f.BestRaft >= 50),
            A("ship_1", "Shipwright", "Pick a ship line", 100, f => f.BestShip >= 1),
            A("ship_50", "Halfway to Legend", "Ship Lv 50", 300, f => f.BestShip >= 50),
            A("ship_100", "Fully Evolved", "Ship Lv 100", 1000, f => f.BestShip >= EvolutionRules.ShipMax),
        };
    }
}
