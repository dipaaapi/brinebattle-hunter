using System;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Material tree by raft type (Ed's U-31): at every raft level-up the player picks the type they're
    /// building toward (Fortress, Sky or Beast). That opens the kind's branch up to the new level: its
    /// refined material and its typed part of that level. Picking a kind later opens its whole branch up
    /// to that level, so switching is never a dead end. Lv 1 of every kind is always open.
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class RaftTree
    {
        /// <summary>True when the kind's branch is open at <paramref name="level"/> (1 = Common … 6 = Mythic).</summary>
        public static bool Unlocked(RaftGrid g, RaftKind kind, int level) =>
            kind == RaftKind.Plain || level <= 1 || (g != null && g.PathLevel(kind) >= level);

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
