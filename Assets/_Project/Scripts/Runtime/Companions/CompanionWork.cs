using System.Collections.Generic;
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

        /// <summary>Seconds the Crew hammers to install one part or module (Ed's ask: never instant).</summary>
        public const float InstallSeconds = 4f;

        /// <summary>
        /// Finishes an install the Crew started: the part must still be in the bag and the cell still fit
        /// (the player may have built there meanwhile). Takes it from the bag and places it.
        /// </summary>
        public static bool CrewInstall(RaftGrid raft, Inventory bag, Cell cell, ItemDefinition part)
        {
            if (part == null || bag.Count(part) <= 0 || raft.CanPlace(cell, part) != PlaceResult.Ok) return false;
            if (!bag.Remove(part, 1)) return false;
            return raft.Place(cell, part) == PlaceResult.Ok;
        }

        /// <summary>Builds or installs one piece (takes it from the bag). False if there was nothing to do.</summary>
        public static bool CrewBuild(RaftGrid raft, Inventory bag, out ItemDefinition part, int upstreamX = -1)
        {
            if (!CrewPick(raft, bag, out var cell, out part, upstreamX)) return false;
            if (!bag.Remove(part, 1)) return false;
            raft.Place(cell, part);
            return true;
        }

        /// <summary>Crew repairs a module once its durability is at or below <paramref name="threshold"/> (0-1, set per module kind in Build).</summary>
        public static bool CrewShouldRepair(Durability d, float threshold) =>
            d != null && !d.Full && d.Current <= d.Max * threshold;

        /// <summary>
        /// The hammer the Mystic would make next (Ed's ask: it keeps the hammer up with the raft level):
        /// when the best hammer in the bag is below <paramref name="raftTier"/> (or there is none), the recipe
        /// for the next hammer up, if the bag can pay for it. Null when the hammer already fits or nothing is affordable.
        /// </summary>
        public static RecipeDefinition MysticHammerPick(IEnumerable<RecipeDefinition> recipes, Inventory bag, Rarity raftTier)
        {
            if (RaftRules.HasHammer(bag, raftTier)) return null;
            var best = RaftRules.BestHammer(bag);
            var next = best == null ? Rarity.Common : best.rarity + 1;
            return recipes.FirstOrDefault(r => r != null && r.kind != RecipeKind.Dismantle && r.outputs.Count > 0
                && r.outputs[0].item != null && r.outputs[0].item.type == ItemType.Tool && r.outputs[0].item.rarity == next
                && CraftRules.CanCraft(bag, r));
        }

        /// <summary>
        /// The consumable the Mystic would craft to keep the bag stocked: a potion or oil recipe the bag
        /// can pay for whose output is below <paramref name="keep"/>. Potions before oils, emptiest first. Null if none.
        /// </summary>
        public static RecipeDefinition MysticCraftPick(IEnumerable<RecipeDefinition> recipes, Inventory bag, int keep)
        {
            return recipes
                .Where(r => r != null && r.kind == RecipeKind.Craft && r.outputs.Count > 0 && r.outputs[0].item != null)
                .Where(r => { var s = CraftRules.SubOf(r); return s == RecipeSub.Potions || s == RecipeSub.Oils || s == RecipeSub.Treats; })
                .Where(r => bag.Count(r.outputs[0].item) < keep && CraftRules.CanCraft(bag, r))
                .OrderBy(r => CraftRules.SubOf(r) == RecipeSub.Oils ? 1 : 0)
                .ThenBy(r => bag.Count(r.outputs[0].item))
                .FirstOrDefault();
        }

        /// <summary>The equipped slot the Mystic would refine next: the lower + that the bag can pay for. Null if none.</summary>
        public static GearSlot? MysticPick(Equipment gear, Inventory bag)
        {
            GearSlot? best = null;
            foreach (var slot in new[] { GearSlot.Weapon, GearSlot.Armor, GearSlot.Accessory })
            {
                if (!gear.CanRefine(slot, bag)) continue;
                if (best == null || gear.RefineOf(gear.Get(slot)) < gear.RefineOf(gear.Get(best.Value))) best = slot;
            }
            return best;
        }
    }
}
