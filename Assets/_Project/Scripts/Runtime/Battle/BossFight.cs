using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Items;

namespace BrineBattle.Battle
{
    /// <summary>
    /// Grand boss fights (Ed's ask, numbers tunable): the boss enrages at half HP (harder hits) and then
    /// throws a tidal wave at the raft every few rounds; every minion helps with its own skill; the treasure
    /// is bigger with Looters aboard. Plain C# for EditMode tests.
    /// </summary>
    public static class BossFightRules
    {
        /// <summary>HP share at which the boss enrages.</summary>
        public const float EnrageAt = 0.5f;
        /// <summary>Enraged attack multiplier.</summary>
        public const float EnragedAttack = 1.3f;
        /// <summary>Rounds between tidal waves once enraged.</summary>
        public const int TidalEvery = 3;
        /// <summary>Share of the raft's max HP a tidal wave takes (Shell Guard blocks it).</summary>
        public const float TidalShare = 0.12f;

        public static bool ShouldEnrage(Combatant boss, bool already) =>
            !already && boss != null && boss.Alive && boss.Hp <= boss.MaxHp * EnrageAt;

        /// <summary>A tidal wave in this round? Every <see cref="TidalEvery"/> rounds after the enrage round.</summary>
        public static bool TidalRound(int round, int enragedRound) =>
            enragedRound > 0 && round > enragedRound && (round - enragedRound) % TidalEvery == 0;

        public static int TidalDamage(int raftMaxHp) => Math.Max(1, (int)Math.Round(raftMaxHp * TidalShare));

        /// <summary>
        /// Damage the other Fighter minions add each round (the battle minion fights on its own): a third
        /// of each one's ATK, at least 1 each.
        /// </summary>
        public static int AllyStrike(IEnumerable<Companion> minions, Companion active) =>
            minions.Where(c => c != active && c.Has(MinionSkill.Fighter) && !c.KnockedOut).Sum(c => Math.Max(1, c.Attack / 3));

        /// <summary>Extra rolls of the boss's loot table: one per Looter minion, at most 3 ("Lucky").</summary>
        public static int LuckyRolls(IEnumerable<Companion> minions) => Math.Min(3, minions.Count(c => c.Has(MinionSkill.Looter) && !c.KnockedOut));

        /// <summary>Essence of the boss's rarity and element in its treasure: 3 + 2 per rarity step, +1 per lucky roll.</summary>
        public static int TreasureEssence(Rarity bossLevel, int lucky) => 3 + 2 * (int)bossLevel + lucky;

        /// <summary>What each minion does in a boss fight, for the log and the minions' battle effects.</summary>
        public static List<string> Contributions(IEnumerable<Companion> minions, Companion active)
        {
            var lines = new List<string>();
            foreach (var c in minions.Where(c => !c.KnockedOut))
            {
                var jobs = new List<string>();
                if (c == active) jobs.Add("fights beside you");
                else if (c.Has(MinionSkill.Fighter)) jobs.Add("strikes the boss");
                if (c.Has(MinionSkill.Alchemist)) jobs.Add("heals");
                if (c.Has(MinionSkill.Crewmate)) jobs.Add("patches the raft");
                if (c.Has(MinionSkill.Looter)) jobs.Add("brings luck");
                if (jobs.Count > 0) lines.Add($"{c.Name}: {string.Join(", ", jobs)}");
            }
            return lines;
        }
    }
}
