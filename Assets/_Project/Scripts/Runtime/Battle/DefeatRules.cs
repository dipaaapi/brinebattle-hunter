using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Elements;
using BrineBattle.Items;

namespace BrineBattle.Battle
{
    /// <summary>
    /// What a lost battle costs (Ed's call: "rest + a little lost"): the hunter rests for a while before
    /// hooking again, and the waves wash away a few Common materials. Gear, pets, raft parts, tools,
    /// potions and anything rarer are never lost. Also the "Danger!" check shown before a fight.
    /// </summary>
    public static class DefeatRules
    {
        /// <summary>Seconds the hunter rests after a loss before hooking again.</summary>
        public const float RestSeconds = 30f;

        /// <summary>How many Common materials the waves take, at most.</summary>
        public const int WashedAway = 3;

        public static bool CanWashAway(ItemDefinition item) => item != null && item.type == ItemType.Material && item.rarity == Rarity.Common;

        /// <summary>Takes up to <see cref="WashedAway"/> Common materials at random and returns what was lost.</summary>
        public static List<(ItemDefinition item, int count)> WashAway(Inventory inv, Random rng)
        {
            var lost = new Dictionary<ItemDefinition, int>();
            for (int n = 0; n < WashedAway; n++)
            {
                var pool = inv.Items.Where(kv => CanWashAway(kv.Key) && kv.Value > 0).Select(kv => kv.Key).ToList();
                if (pool.Count == 0) break;
                var item = pool[rng.Next(pool.Count)];
                inv.Remove(item, 1);
                lost[item] = lost.TryGetValue(item, out int c) ? c + 1 : 1;
            }
            return lost.Select(kv => (kv.Key, kv.Value)).ToList();
        }

        /// <summary>Average hit without the random spread.</summary>
        static float Hit(Combatant from, Combatant to) =>
            Math.Max(1f, (from.Attack - to.Defense * 0.5f) * ElementRules.Multiplier(from.Element, to.Element));

        /// <summary>
        /// True when the monster would likely knock the hunter out before the hunter (and the pet in battle,
        /// if any) beats it: compares how many hits each side needs.
        /// </summary>
        public static bool Danger(Combatant hunter, Combatant monster, Combatant pet = null)
        {
            float ours = Hit(hunter, monster) + (pet != null ? Hit(pet, monster) : 0f);
            int toBeatIt = (int)Math.Ceiling(monster.MaxHp / ours);
            int toBeatUs = (int)Math.Ceiling(hunter.MaxHp / Hit(monster, hunter));
            return toBeatUs < toBeatIt;
        }
    }
}
