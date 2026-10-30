using System;

namespace BrineBattle.Battle
{
    /// <summary>Who gets hurt by an ambush or an outburst.</summary>
    public enum HazardTarget { None, Hunter, Pet }

    /// <summary>One ambush or outburst: who it hits, how hard, and any ailment it gives.</summary>
    public struct Hazard
    {
        public HazardTarget Target;
        public int Damage;
        public StatusEffect Status;
    }

    /// <summary>
    /// Health outside battle (owner idea U-32, numbers tunable): pets don't age, so HP is what matters.
    /// - **Ambush:** a live monster being reeled in may lash out at the hunter or a pet before any battle.
    /// - **Outburst:** a monster that was just beaten may thrash in its last moment and hurt someone.
    /// - **Rest:** the hunter and pets heal slowly on the raft; habitats make pets heal faster (C-10).
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class HazardRules
    {
        /// <summary>Chance a live drift lashes out while reeled in: 15% calm … 50% aggressive.</summary>
        public static double AmbushChance(float aggression) => 0.15 + 0.35 * Clamp01(aggression);
        /// <summary>Chance a beaten monster thrashes: 10% calm … 30% aggressive, +20% if it was gorged.</summary>
        public static double OutburstChance(float aggression, bool gorged) => 0.1 + 0.2 * Clamp01(aggression) + (gorged ? 0.2 : 0);
        /// <summary>Share of the monster's attack an ambush or outburst deals (before defense).</summary>
        public const float AmbushPower = 0.5f, OutburstPower = 0.7f;
        /// <summary>Chance the hit lands on a pet instead of the hunter, when there is a pet to hit.</summary>
        public const double PetShare = 0.4;
        /// <summary>Chance an outburst leaves the one it hit dazed (Confusion); ambushes give the monster's own ailment.</summary>
        public const double OutburstAilment = 0.5;

        /// <summary>Seconds per HP the hunter heals on the raft outside battle.</summary>
        public const float HunterRegenSeconds = 3f;
        /// <summary>Seconds per HP a pet heals: 6 s, each habitat on the raft 50% faster (C-10).</summary>
        public static float PetRegenSeconds(int habitats) => 6f / (1f + 0.5f * Math.Max(0, habitats));

        public static Hazard Ambush(Combatant monster, Combatant hunter, Combatant pet, float aggression, Random rng)
        {
            if (rng.NextDouble() >= AmbushChance(aggression)) return default;
            var h = Strike(monster, hunter, pet, AmbushPower, rng);
            if (monster.Inflicts != StatusEffect.None && rng.NextDouble() < monster.InflictChance) h.Status = monster.Inflicts;
            return h;
        }

        public static Hazard Outburst(Combatant monster, Combatant hunter, Combatant pet, float aggression, bool gorged, Random rng)
        {
            if (rng.NextDouble() >= OutburstChance(aggression, gorged)) return default;
            var h = Strike(monster, hunter, pet, OutburstPower, rng);
            if (rng.NextDouble() < OutburstAilment) h.Status = StatusEffect.Confusion;
            return h;
        }

        static Hazard Strike(Combatant monster, Combatant hunter, Combatant pet, float power, Random rng)
        {
            bool petHit = pet != null && pet.Alive && rng.NextDouble() < PetShare;
            var target = petHit ? pet : hunter;
            int dmg = Math.Max(1, (int)Math.Round(monster.Attack * power - target.Defense * 0.5f));
            return new Hazard { Target = petHit ? HazardTarget.Pet : HazardTarget.Hunter, Damage = dmg };
        }

        /// <summary>
        /// Applies a hazard outside battle. The hunter is never knocked out by one (keeps 1 HP); a pet can be.
        /// Returns the HP actually lost.
        /// </summary>
        public static int Apply(Hazard h, Combatant target)
        {
            if (h.Target == HazardTarget.None || target == null) return 0;
            int floor = h.Target == HazardTarget.Hunter ? 1 : 0;
            int lost = Math.Min(h.Damage, Math.Max(0, target.Hp - floor));
            target.Hp -= lost;
            if (h.Status != StatusEffect.None) StatusRules.Inflict(target, h.Status);
            return lost;
        }

        /// <summary>
        /// Uses a potion outside battle (U-32/U-33): heals (Brine Tonic for the hunter, Kelp Biscuit for a pet)
        /// or cures the item's ailment. False if it would do nothing; the caller takes the item from the bag.
        /// </summary>
        public static bool Treat(Items.ItemDefinition item, Combatant who, bool isPet)
        {
            if (item == null || who == null) return false;
            switch (item.effect)
            {
                case Items.ConsumableEffect.HealHunter when !isPet:
                case Items.ConsumableEffect.HealCompanion when isPet:
                    if (who.Hp >= who.MaxHp || item.effectAmount <= 0) return false;
                    who.Hp = Math.Min(who.MaxHp, who.Hp + item.effectAmount);
                    return true;
                case Items.ConsumableEffect.CureStatus:
                    return StatusRules.Cure(who, item.cures);
                default:
                    return false;
            }
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
