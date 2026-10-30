using System;
using System.Collections.Generic;
using System.Linq;

namespace BrineBattle.Items
{
    /// <summary>Always-on gear effects in battle (GDD §9).</summary>
    public enum GearPassive
    {
        None = 0,
        /// <summary>Heal for part of the damage you deal.</summary>
        Leech = 1,
        /// <summary>Hits on the hunter hurt the monster back.</summary>
        Thorns = 2,
        /// <summary>Extra stamina every round.</summary>
        SecondWind = 3,
        /// <summary>The raft takes less damage.</summary>
        Bulwark = 4,
        /// <summary>Faster in battle (accessories, Ed's ask).</summary>
        Swift = 5,
    }

    /// <summary>Gear skills the player triggers in battle; each costs stamina and takes the turn (GDD §9).</summary>
    public enum GearActive
    {
        None = 0,
        /// <summary>A heavy hit.</summary>
        TidalStrike = 1,
        /// <summary>The raft takes no damage this round.</summary>
        ShellGuard = 2,
    }

    /// <summary>Which skills a piece of gear may use, by rarity, and their numbers (GDD §9). Plain C# for EditMode tests.</summary>
    public static class GearSkillRules
    {
        public const float LeechShare = 0.2f;
        public const float ThornsShare = 0.25f;
        public const int SecondWindStamina = 1;
        public const float BulwarkFactor = 0.8f;
        public const int SwiftSpeed = 2;
        public const int TidalStrikeCost = 6;
        public const float TidalStrikePower = 2.2f;
        public const int ShellGuardCost = 4;

        /// <summary>Common/Uncommon 0 (element only), Rare 1, Epic 1, Legendary 2, Mythic 3.</summary>
        public static int PassiveSlots(Rarity r) =>
            r >= Rarity.Mythic ? 3 : r >= Rarity.Legendary ? 2 : r >= Rarity.Rare ? 1 : 0;

        /// <summary>Epic 1, Legendary/Mythic 2, lower 0.</summary>
        public static int ActiveSlots(Rarity r) =>
            r >= Rarity.Legendary ? 2 : r >= Rarity.Epic ? 1 : 0;

        /// <summary>The passives an item really gets: listed ones, no None, no repeats, capped by its rarity's slots.</summary>
        public static IEnumerable<GearPassive> Passives(ItemDefinition item) =>
            item == null || item.passives == null ? Enumerable.Empty<GearPassive>()
            : item.passives.Where(p => p != GearPassive.None).Distinct().Take(PassiveSlots(item.rarity));

        public static IEnumerable<GearActive> Actives(ItemDefinition item) =>
            item == null || item.actives == null ? Enumerable.Empty<GearActive>()
            : item.actives.Where(a => a != GearActive.None).Distinct().Take(ActiveSlots(item.rarity));

        public static int Cost(GearActive a) =>
            a == GearActive.TidalStrike ? TidalStrikeCost : a == GearActive.ShellGuard ? ShellGuardCost : 0;

        public static string Label(GearPassive p)
        {
            switch (p)
            {
                case GearPassive.Leech: return $"Leech (heal {LeechShare:P0} of damage dealt)";
                case GearPassive.Thorns: return $"Thorns (return {ThornsShare:P0} of damage taken)";
                case GearPassive.SecondWind: return $"Second Wind (+{SecondWindStamina} stamina/round)";
                case GearPassive.Bulwark: return $"Bulwark (raft takes {1f - BulwarkFactor:P0} less)";
                case GearPassive.Swift: return $"Swift (+{SwiftSpeed} SPD)";
                default: return "";
            }
        }

        public static string Label(GearActive a)
        {
            switch (a)
            {
                case GearActive.TidalStrike: return "Tidal Strike";
                case GearActive.ShellGuard: return "Shell Guard";
                default: return "";
            }
        }

        public static string Describe(GearActive a)
        {
            switch (a)
            {
                case GearActive.TidalStrike: return $"×{TidalStrikePower} hit, {TidalStrikeCost} st";
                case GearActive.ShellGuard: return $"raft safe this round, {ShellGuardCost} st";
                default: return "";
            }
        }
    }
}
