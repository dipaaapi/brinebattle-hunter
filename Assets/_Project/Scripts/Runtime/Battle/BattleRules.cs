using System;
using BrineBattle.Elements;

namespace BrineBattle.Battle
{
    /// <summary>
    /// Numbers and formulas for the Phase 3 battle. The action resource is stamina for now
    /// (open question U-7 / C-4); change it here when decided.
    /// </summary>
    public static class BattleRules
    {
        public const int MaxStamina = 10;
        public const int StartStamina = 4;
        public const int StaminaPerRound = 2;
        public const int BraceStaminaBonus = 3;
        public const int HarpoonCost = 4;
        public const float HarpoonPower = 1.8f;
        /// <summary>Harpoon durability (idea C-4); each throw uses 1.</summary>
        public const int HarpoonDurability = 20;
        public const int HarpoonWear = 1;
        /// <summary>Chance a sea monster hits the raft instead of the hunter; sky monsters use 1 minus this.</summary>
        public const float SeaHitsRaftChance = 0.7f;

        /// <summary>
        /// (attack * power - defense / 2) * element multiplier * random 0.9-1.1, halved if bracing, at least 1.
        /// </summary>
        public static int Damage(int attack, int defense, float power, Element attacker, Element defender, bool bracing, Random rng)
        {
            float raw = attack * power - defense * 0.5f;
            raw *= ElementRules.Multiplier(attacker, defender);
            raw *= 0.9f + (float)rng.NextDouble() * 0.2f;
            if (bracing) raw *= 0.5f;
            return Math.Max(1, (int)Math.Round(raw));
        }

        /// <summary>Faster side acts first; ties go to the hunter.</summary>
        public static bool HunterFirst(Combatant hunter, Combatant monster) => hunter.Speed >= monster.Speed;
    }
}
