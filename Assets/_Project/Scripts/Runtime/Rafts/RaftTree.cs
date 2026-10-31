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
        /// <summary>The raft level at which the ship line is picked, once (Ed's ask, 2026-10-31).</summary>
        public const int PickLevel = EvolutionRules.RaftMax;

        /// <summary>STR/INT/DEX tag and look for each ship line (Ed's ask).</summary>
        public static string Look(RaftKind kind)
        {
            switch (kind)
            {
                case RaftKind.Sea: return "Sea-level ship · STR (Sloop → Galleon → Flagship)";
                case RaftKind.Aerial: return "Aerial ship · INT (Glider → Airship → Sky Citadel)";
                case RaftKind.Undersea: return "Undersea ship · DEX (Diving Bell → Submarine → Abyss Ark)";
                default: return "Raft";
            }
        }

        /// <summary>
        /// True when the line's branch is open at <paramref name="level"/> (an item's rarity + 1): only the picked
        /// ship line, up to its current tier. Before the pick no ship-line recipes are open.
        /// </summary>
        public static bool Unlocked(RaftGrid g, RaftKind kind, int level)
        {
            if (kind == RaftKind.Plain) return true;
            var chosen = g != null ? g.Chosen : RaftKind.Plain;
            return chosen != RaftKind.Plain && kind == chosen && level <= (int)g.Tier + 1;
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

        public static readonly RaftKind[] Kinds = { RaftKind.Sea, RaftKind.Aerial, RaftKind.Undersea };
    }
}
