using System;

namespace BrineBattle.Encounters
{
    /// <summary>State roll, unreliable hints and the raft-size gate. Plain C# for EditMode tests.</summary>
    public static class EncounterRules
    {
        /// <summary>Weighted roll. Negative weights count as 0; all zero falls back to Dead.</summary>
        public static DriftState RollState(float dead, float weakened, float alive, Random rng)
        {
            dead = Math.Max(0f, dead);
            weakened = Math.Max(0f, weakened);
            alive = Math.Max(0f, alive);
            float total = dead + weakened + alive;
            if (total <= 0f) return DriftState.Dead;
            double r = rng.NextDouble() * total;
            if (r < dead) return DriftState.Dead;
            if (r < dead + weakened) return DriftState.Weakened;
            return DriftState.Alive;
        }

        /// <summary>
        /// The hint shown before hooking. With probability <paramref name="accuracy"/> it is the true
        /// state, otherwise one of the two wrong states at random.
        /// </summary>
        public static DriftState Hint(DriftState actual, float accuracy, Random rng)
        {
            if (rng.NextDouble() < accuracy) return actual;
            int offset = rng.Next(1, 3);
            return (DriftState)(((int)actual + offset) % 3);
        }

        /// <summary>Only Alive creatures fight, so only they are gated by raft size.</summary>
        public static bool CanFight(int raftSize, int monsterSizeClass) => raftSize >= monsterSizeClass;

        /// <summary>Aggression at or above this: a weakened monster is killed without asking (idea U-17).</summary>
        public const float AggressiveAt = 0.5f;
        public const float AggressionSpread = 0.15f;

        public static bool IsAggressive(float aggression) => aggression >= AggressiveAt;

        /// <summary>One drift's temperament: the species' value ± spread, clamped 0–1.</summary>
        public static float RollAggression(float species, Random rng) =>
            Math.Max(0f, Math.Min(1f, species + ((float)rng.NextDouble() * 2f - 1f) * AggressionSpread));

        public static bool IsGated(DriftState state, int raftSize, int monsterSizeClass) =>
            state == DriftState.Alive && !CanFight(raftSize, monsterSizeClass);
    }
}
