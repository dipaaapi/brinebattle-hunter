using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;

namespace BrineBattle.Companions
{
    /// <summary>
    /// What Crew and Mystic companions do on their own between fights (owner idea U-23):
    /// the Crew installs auto-loot modules upstream and grows the raft downstream from the bag, the Mystic refines the hunter's gear
    /// with essence in the bag. Plain C# for EditMode tests; the scene calls these on a timer.
    /// </summary>
    public static class CompanionWork
    {
        /// <summary>Seconds between jobs.</summary>
        public const float JobSeconds = 20f;

        /// <summary>
        /// What the Crew would do next, and where. Drifts come from the upstream side (−X, the left,
        /// with the default current), so auto-loot modules from the bag go on the upstream edge, nearest
        /// the middle row; raft parts grow the raft on the downstream side (+X, the right). Modules come
        /// first, then the rarest raft part that fits the tier. False if there is nothing to do.
        /// </summary>
        public static bool CrewPick(RaftGrid raft, Inventory bag, out Cell cell, out ItemDefinition part, int upstreamX = -1)
        {
            cell = default;
            var candidates = bag.Items.Select(kv => kv.Key)
                .Where(i => (i.type == ItemType.Module || i.type == ItemType.Raft) && SalvageRules.IsCompatible(i.type, i.rarity, raft.Tier))
                .OrderBy(i => i.type == ItemType.Module ? 0 : 1).ThenByDescending(i => i.rarity).ThenByDescending(i => i.moduleLevel);
            foreach (var p in candidates)
            {
                var item = p;
                var open = raft.OpenCells().Where(c => raft.CanPlace(c, item) == PlaceResult.Ok);
                // Modules: as far upstream as possible. Parts: as far downstream as possible. Middle rows first.
                int sign = item.type == ItemType.Module ? upstreamX : -upstreamX;
                var best = open.OrderByDescending(c => c.X * sign).ThenBy(c => System.Math.Abs(c.Z)).ThenBy(c => c.Z).ToList();
                if (best.Count == 0) continue;
                cell = best[0];
                part = item;
                return true;
            }
            part = null;
            return false;
        }

        /// <summary>Builds or installs one piece (takes it from the bag). False if there was nothing to do.</summary>
        public static bool CrewBuild(RaftGrid raft, Inventory bag, out ItemDefinition part, int upstreamX = -1)
        {
            if (!CrewPick(raft, bag, out var cell, out part, upstreamX)) return false;
            if (!bag.Remove(part, 1)) return false;
            raft.Place(cell, part);
            return true;
        }

        /// <summary>The equipped slot the Mystic would refine next: the lower + that the bag can pay for. Null if none.</summary>
        public static GearSlot? MysticPick(Equipment gear, Inventory bag)
        {
            GearSlot? best = null;
            foreach (var slot in new[] { GearSlot.Weapon, GearSlot.Armor })
            {
                if (!gear.CanRefine(slot, bag)) continue;
                if (best == null || gear.RefineOf(gear.Get(slot)) < gear.RefineOf(gear.Get(best.Value))) best = slot;
            }
            return best;
        }
    }
}
