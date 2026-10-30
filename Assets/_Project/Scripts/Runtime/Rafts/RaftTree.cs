using System;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Material tree by raft type (Ed's U-31, reworked on his ask): the raft type is picked once, at
    /// <see cref="PickLevel"/>. Before that, Lv 1 of every type is open to try. After it, only the
    /// picked type's branch grows, one level per raft level-up (its refined material and typed part);
    /// the other types stay locked and never branch out. Plain C# for EditMode tests.
    /// </summary>
    public static class RaftTree
    {
        /// <summary>The raft level at which the type is picked (Ed asked for Lv 10 once the raft has that many levels).</summary>
        public const int PickLevel = 2;

        /// <summary>STR/INT/DEX tag and look for each type (Ed's ask).</summary>
        public static string Look(RaftKind kind)
        {
            switch (kind)
            {
                case RaftKind.Fortress: return "Sea Train · STR, tank";
                case RaftKind.Sky: return "Winged Plane · INT";
                case RaftKind.Beast: return "Pirate Ship · DEX";
                default: return "Raft";
            }
        }

        /// <summary>True when the kind's branch is open at <paramref name="level"/> (1 = Common … 6 = Mythic).</summary>
        public static bool Unlocked(RaftGrid g, RaftKind kind, int level)
        {
            if (kind == RaftKind.Plain) return true;
            var chosen = g != null ? g.Chosen : RaftKind.Plain;
            if (chosen == RaftKind.Plain) return level <= 1;
            return kind == chosen && (level <= 1 || g.PathLevel(kind) >= level);
        }

        /// <summary>The raft level a typed item belongs to (its rarity + 1).</summary>
        public static int LevelOf(ItemDefinition item) => item == null ? 1 : (int)item.rarity + 1;

        /// <summary>A recipe whose result is a typed part or material needs its branch open; everything else is free.</summary>
        public static bool Knows(RaftGrid g, RecipeDefinition r)
        {
            if (r == null || r.kind == RecipeKind.Dismantle || r.outputs.Count == 0) return true;
            var item = r.outputs[0].item;
            if (item == null || item.raftKind == RaftKind.Plain) return true;
            return Unlocked(g, item.raftKind, LevelOf(item));
        }

        /// <summary>The kind a locked recipe waits for, or Plain.</summary>
        public static RaftKind KindOf(RecipeDefinition r) =>
            r != null && r.outputs.Count > 0 && r.outputs[0].item != null ? r.outputs[0].item.raftKind : RaftKind.Plain;

        public static readonly RaftKind[] Kinds = { RaftKind.Fortress, RaftKind.Sky, RaftKind.Beast };
    }
}
