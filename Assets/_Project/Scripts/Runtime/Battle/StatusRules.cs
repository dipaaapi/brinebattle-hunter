using System;

namespace BrineBattle.Battle
{
    /// <summary>Ailments a monster can give the hunter or a pet (owner idea U-33).</summary>
    public enum StatusEffect
    {
        None = 0,
        /// <summary>Loses a little HP every round. Cured by Toxic Relief.</summary>
        Poison = 1,
        /// <summary>Sometimes hits itself instead of the monster. Cured by Confusion Relief.</summary>
        Confusion = 2,
        /// <summary>Loses more HP every round, but wears off sooner. Cured by a Clotting Patch.</summary>
        Bleed = 3,
        /// <summary>Blinded and silenced: misses often and can't use gear skills. Cured by Eye Cleanser.</summary>
        Blind = 4,
    }

    /// <summary>
    /// Status numbers (U-33, tunable). A status lasts a number of rounds. Rounds pass in battle, and
    /// outside battle one round passes every <see cref="OutsideRoundSeconds"/>; outside battle a status
    /// never takes the last HP. Plain C# for EditMode tests.
    /// </summary>
    public static class StatusRules
    {
        public const float OutsideRoundSeconds = 5f;
        public const double ConfusionSelfHit = 0.35;
        public const double BlindMiss = 0.4;

        public static int Rounds(StatusEffect s)
        {
            switch (s)
            {
                case StatusEffect.Poison: return 5;
                case StatusEffect.Confusion: return 3;
                case StatusEffect.Bleed: return 3;
                case StatusEffect.Blind: return 3;
                default: return 0;
            }
        }

        /// <summary>HP lost at the end of a round: poison 5% of max HP, bleed 10% (at least 1 and 2).</summary>
        public static int TickDamage(StatusEffect s, int maxHp)
        {
            switch (s)
            {
                case StatusEffect.Poison: return Math.Max(1, (int)Math.Round(maxHp * 0.05f));
                case StatusEffect.Bleed: return Math.Max(2, (int)Math.Round(maxHp * 0.1f));
                default: return 0;
            }
        }

        public static string Name(StatusEffect s)
        {
            switch (s)
            {
                case StatusEffect.Poison: return "Poisoned";
                case StatusEffect.Confusion: return "Confused";
                case StatusEffect.Bleed: return "Bleeding";
                case StatusEffect.Blind: return "Blinded";
                default: return "";
            }
        }

        /// <summary>Short tag with a symbol, for labels over drifts and HP bars (idea C-11).</summary>
        public static string Tag(StatusEffect s)
        {
            switch (s)
            {
                case StatusEffect.Poison: return "☠ Poison";
                case StatusEffect.Confusion: return "? Confusion";
                case StatusEffect.Bleed: return "♦ Bleed";
                case StatusEffect.Blind: return "◉ Blind";
                default: return "";
            }
        }

        /// <summary>Gives a status. A new one replaces the old; the same one is refreshed to full length.</summary>
        public static void Inflict(Combatant c, StatusEffect s)
        {
            if (c == null || s == StatusEffect.None) return;
            c.Status = s;
            c.StatusRounds = Rounds(s);
        }

        /// <summary>Removes <paramref name="s"/> if the combatant has it. True if something was cured.</summary>
        public static bool Cure(Combatant c, StatusEffect s)
        {
            if (c == null || s == StatusEffect.None || c.Status != s) return false;
            c.Status = StatusEffect.None;
            c.StatusRounds = 0;
            return true;
        }

        /// <summary>
        /// One round passes: poison and bleed hurt, then the status counts down. <paramref name="floor"/>
        /// is the lowest HP the tick may leave (1 outside battle, 0 in battle). Returns the HP lost.
        /// </summary>
        public static int Tick(Combatant c, int floor = 0)
        {
            if (c == null) return 0;
            int hp = c.Hp, rounds = c.StatusRounds;
            var s = c.Status;
            int dmg = Tick(ref hp, c.MaxHp, ref s, ref rounds, floor);
            c.Hp = hp; c.Status = s; c.StatusRounds = rounds;
            return dmg;
        }

        /// <summary>The same tick on plain values (pets and the hunter between battles).</summary>
        public static int Tick(ref int hp, int maxHp, ref StatusEffect status, ref int rounds, int floor = 0)
        {
            if (status == StatusEffect.None) return 0;
            int dmg = Math.Min(TickDamage(status, maxHp), Math.Max(0, hp - floor));
            hp -= dmg;
            if (--rounds <= 0) { status = StatusEffect.None; rounds = 0; }
            return dmg;
        }
    }
}
