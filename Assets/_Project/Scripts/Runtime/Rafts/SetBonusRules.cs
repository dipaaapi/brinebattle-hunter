using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Raft layout bonuses (Claude's C-18), so where you build matters, not just what:
    /// - <b>Module line</b>: 3 or more modules side by side in a row or column each reach 1 m further.
    /// - <b>Cozy habitat</b>: a habitat with raft parts on all four sides counts double for minion healing.
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class SetBonusRules
    {
        public const int LineLength = 3;
        public const float LineReach = 1f;

        static bool IsModule(IReadOnlyDictionary<Cell, ItemDefinition> parts, Cell c) =>
            parts.TryGetValue(c, out var p) && p != null && p.type == ItemType.Module;

        /// <summary>Length of the longest straight run of modules (row or column) through <paramref name="c"/>.</summary>
        public static int LineThrough(IReadOnlyDictionary<Cell, ItemDefinition> parts, Cell c)
        {
            if (!IsModule(parts, c)) return 0;
            int Run(int dx, int dz)
            {
                int n = 0;
                for (var x = new Cell(c.X + dx, c.Z + dz); IsModule(parts, x); x = new Cell(x.X + dx, x.Z + dz)) n++;
                return n;
            }
            return 1 + System.Math.Max(Run(1, 0) + Run(-1, 0), Run(0, 1) + Run(0, -1));
        }

        /// <summary>Extra reach for the module at <paramref name="c"/>.</summary>
        public static float ModuleReach(IReadOnlyDictionary<Cell, ItemDefinition> parts, Cell c) =>
            LineThrough(parts, c) >= LineLength ? LineReach : 0f;

        /// <summary>True when the habitat at <paramref name="c"/> has raft parts (planks) on all four sides.</summary>
        public static bool Cozy(IReadOnlyDictionary<Cell, ItemDefinition> parts, Cell c)
        {
            if (!parts.TryGetValue(c, out var h) || h == null || h.type != ItemType.Habitat) return false;
            return c.Neighbours().All(n => parts.TryGetValue(n, out var p) && p != null && p.type == ItemType.Raft);
        }

        /// <summary>Habitats for minion healing: each cozy one counts twice.</summary>
        public static int HealingHabitats(IReadOnlyDictionary<Cell, ItemDefinition> parts) =>
            parts.Where(kv => kv.Value != null && kv.Value.type == ItemType.Habitat).Sum(kv => Cozy(parts, kv.Key) ? 2 : 1);
    }
}
