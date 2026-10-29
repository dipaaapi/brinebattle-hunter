using System;
using System.Collections.Generic;

namespace BrineBattle.Items
{
    /// <summary>
    /// Alchemy Mastery (Idea Round 3: owner idea U-14 + Claude idea C-1, with U-6). Harvesting, beating,
    /// taming and crafting give mastery points. Each level makes the drift hints more accurate (C-1),
    /// reveals more recipes (U-14), and from Lv 3 lets essence be combined into a higher rarity (U-6).
    /// Plain C# for EditMode tests.
    /// </summary>
    public class AlchemyMastery
    {
        public const int MaxLevel = 10;
        public const int HarvestPoints = 2, TamePoints = 3, CraftPoints = 1;
        /// <summary>Essence combining (U-6) unlocks at this level.</summary>
        public const int CombineLevel = 3;
        /// <summary>Essence of one rarity and element that makes one of the next rarity.</summary>
        public const int CombineCost = 5;

        public int Points { get; private set; }
        public int Level => LevelFor(Points);

        public event Action<int> LeveledUp;

        /// <summary>Points to go from <paramref name="level"/> to the next: 10, 20, 30 …</summary>
        public static int PointsToNext(int level) => 10 * level;

        /// <summary>Total points needed to reach <paramref name="level"/> (Lv 1 = 0).</summary>
        public static int PointsFor(int level)
        {
            int total = 0;
            for (int l = 1; l < level; l++) total += PointsToNext(l);
            return total;
        }

        public static int LevelFor(int points)
        {
            int level = 1;
            while (level < MaxLevel && points >= PointsFor(level + 1)) level++;
            return level;
        }

        /// <summary>Chance the Dead/Weakened/Alive hint is right (C-1): 60% at Lv 1, +4% per level, 96% at Lv 10.</summary>
        public static float HintAccuracy(int level) => Math.Min(0.96f, 0.6f + 0.04f * (Math.Max(1, level) - 1));

        /// <summary>Progress inside the current level, 0..1 (1 at the top level).</summary>
        public float Progress
        {
            get
            {
                if (Level >= MaxLevel) return 1f;
                return (Points - PointsFor(Level)) / (float)PointsToNext(Level);
            }
        }

        public void Add(int points)
        {
            if (points <= 0) return;
            int before = Level;
            Points += points;
            for (int l = before + 1; l <= Level; l++) LeveledUp?.Invoke(l);
        }

        public void Restore(int points) => Points = Math.Max(0, points);

        /// <summary>Mastery a recipe needs (U-14): by its output's rarity. Dismantling is always known.</summary>
        public static int Needed(RecipeDefinition r)
        {
            if (r == null || r.kind == RecipeKind.Dismantle) return 1;
            var item = r.outputs.Count > 0 ? r.outputs[0].item : null;
            return item == null ? 1 : Needed(item.rarity);
        }

        /// <summary>
        /// Common 1 … Mythic 6: one level per raft level, so mastery from beating the monsters a raft
        /// level-up asks for (5, 10, 15 … kills × 2 points) keeps up with the hammer and parts it needs.
        /// </summary>
        public static int Needed(Rarity rarity) => (int)rarity + 1;

        public bool Knows(RecipeDefinition r) => Needed(r) <= Level;

        /// <summary>
        /// Saves from before mastery: start at the level the raft has reached, so no recipe the player
        /// already needed gets hidden.
        /// </summary>
        public static int StartingPoints(Rarity raftTier) => PointsFor(Needed(raftTier));

        /// <summary>U-6: 5 essence of one rarity and element → 1 of the next rarity. False if not allowed or not enough.</summary>
        public bool Combine(Inventory inv, EssenceKey from)
        {
            if (Level < CombineLevel || from.Rarity >= Rarity.Mythic || !inv.RemoveEssence(from, CombineCost)) return false;
            inv.AddEssence(new EssenceKey(from.Rarity + 1, from.Element), 1);
            return true;
        }

        /// <summary>Recipes that become known at exactly <paramref name="level"/>.</summary>
        public static List<RecipeDefinition> NewAt(IEnumerable<RecipeDefinition> recipes, int level)
        {
            var list = new List<RecipeDefinition>();
            foreach (var r in recipes) if (r != null && Needed(r) == level) list.Add(r);
            return list;
        }
    }
}
