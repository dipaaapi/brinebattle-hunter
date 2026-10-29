using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;

namespace BrineBattle.Companions
{
    /// <summary>
    /// What Crew and Mystic companions do on their own between fights (owner idea U-23):
    /// the Crew builds the raft out from raft parts in the bag, the Mystic refines the hunter's gear
    /// with essence in the bag. Plain C# for EditMode tests; the scene calls these on a timer.
    /// </summary>
    public static class CompanionWork
    {
        /// <summary>Seconds between jobs.</summary>
        public const float JobSeconds = 20f;

        /// <summary>
        /// The raft part and open cell the Crew would build next: the highest-rarity part in the bag
        /// that fits the raft tier, on the open cell closest to the centre. False if there is nothing to do.
        /// Modules are left to the player (where they go matters).
        /// </summary>
        public static bool CrewPick(RaftGrid raft, Inventory bag, out Cell cell, out ItemDefinition part)
        {
            cell = default;
            part = bag.Items.Select(kv => kv.Key)
                .Where(i => i.type == ItemType.Raft && SalvageRules.IsCompatible(i.type, i.rarity, raft.Tier))
                .OrderByDescending(i => i.rarity).FirstOrDefault();
            if (part == null) return false;
            var p = part;
            var open = raft.OpenCells().Where(c => raft.CanPlace(c, p) == PlaceResult.Ok)
                .OrderBy(c => c.X * c.X + c.Z * c.Z).ThenBy(c => c.X).ThenBy(c => c.Z).ToList();
            if (open.Count == 0) { part = null; return false; }
            cell = open[0];
            return true;
        }

        /// <summary>Builds one part (takes it from the bag). False if there was nothing to do.</summary>
        public static bool CrewBuild(RaftGrid raft, Inventory bag, out ItemDefinition part)
        {
            if (!CrewPick(raft, bag, out var cell, out part)) return false;
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
