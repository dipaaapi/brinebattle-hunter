using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>Module shapes (Ed's ask, 2026-10-31). Values are saved on items, so never renumber.</summary>
    public enum ModuleShape { Single = 0, Bar2 = 1, Square = 2, L = 3, T = 4, Bar3 = 5 }

    /// <summary>
    /// Bigger module shapes (Ed's ask): a shaped module kit places several cells of its module at once, in a
    /// 1×2, 2×2, L, T or 1×3 footprint that can be turned in the Shipyard. Kits open with the raft's growth and
    /// are made by minions (the hunter is an ordinary person; minions do the alchemy). Taking a kit's cells off
    /// later gives back plain modules, one per cell, so nothing comes free (a kit costs a module per cell).
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class ShapeRules
    {
        static readonly Cell[][] Offsets =
        {
            new[] { new Cell(0, 0) },
            new[] { new Cell(0, 0), new Cell(1, 0) },
            new[] { new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1) },
            new[] { new Cell(0, 0), new Cell(0, 1), new Cell(1, 0) },
            new[] { new Cell(-1, 0), new Cell(0, 0), new Cell(1, 0), new Cell(0, 1) },
            new[] { new Cell(-1, 0), new Cell(0, 0), new Cell(1, 0) },
        };

        public static int Size(ModuleShape s) => Offsets[(int)s].Length;

        public static string Name(ModuleShape s)
        {
            switch (s)
            {
                case ModuleShape.Bar2: return "1×2";
                case ModuleShape.Square: return "2×2";
                case ModuleShape.L: return "L";
                case ModuleShape.T: return "T";
                case ModuleShape.Bar3: return "1×3";
                default: return "1×1";
            }
        }

        /// <summary>A quarter turn: (x, z) → (−z, x).</summary>
        public static Cell Turn(Cell c) => new Cell(-c.Z, c.X);

        /// <summary>The cells a shape covers at <paramref name="anchor"/>, turned <paramref name="rotation"/> quarter turns (0-3).</summary>
        public static List<Cell> Footprint(ModuleShape s, int rotation, Cell anchor)
        {
            int r = ((rotation % 4) + 4) % 4;
            return Offsets[(int)s].Select(o => { for (int i = 0; i < r; i++) o = Turn(o); return new Cell(anchor.X + o.X, anchor.Z + o.Z); }).ToList();
        }

        /// <summary>Raft level a shape opens at: 1×2 at 10, 2×2 at 20, L at 30, T at 40; 1×3 on a ship.</summary>
        public static int OpensAt(ModuleShape s)
        {
            switch (s)
            {
                case ModuleShape.Bar2: return 10;
                case ModuleShape.Square: return 20;
                case ModuleShape.L: return 30;
                case ModuleShape.T: return 40;
                case ModuleShape.Bar3: return EvolutionRules.RaftMax;
                default: return 1;
            }
        }

        public static bool Unlocked(RaftGrid g, ModuleShape s) =>
            s == ModuleShape.Single || (g != null && (g.IsShip || (s != ModuleShape.Bar3 && g.Level >= OpensAt(s))));

        public static string Lock(ModuleShape s) => s == ModuleShape.Bar3 ? "Opens on a ship" : $"Opens at raft Lv {OpensAt(s)}";

        /// <summary>A kit is a module item with a shape and the plain module it lays down.</summary>
        public static bool IsKit(ItemDefinition i) => i != null && i.type == ItemType.Module && i.shape != ModuleShape.Single && i.segment != null;

        /// <summary>
        /// Can the kit go down here? Every cell in bounds and empty, one of them touching the raft, and the
        /// module itself allowed there (tier, hammer, …). <paramref name="order"/> lists the cells so each one
        /// touches the raft when it's placed.
        /// </summary>
        public static PlaceResult CanPlace(RaftGrid g, ItemDefinition kit, int rotation, Cell anchor, out List<Cell> order)
        {
            order = new List<Cell>();
            if (!IsKit(kit)) return PlaceResult.NotARaftPart;
            var cells = Footprint(kit.shape, rotation, anchor);
            foreach (var c in cells)
            {
                if (Math.Abs(c.X) > RaftRules.MaxRadius || Math.Abs(c.Z) > RaftRules.MaxRadius) return PlaceResult.OutOfBounds;
                if (g.Has(c)) return PlaceResult.Occupied;
            }
            var touching = cells.Where(c => c.Neighbours().Any(g.Has)).ToList();
            if (g.Count > 0 && touching.Count == 0) return PlaceResult.NotAdjacent;
            if (touching.Count > 0)
            {
                var r = g.CanPlace(touching[0], kit.segment);
                if (r != PlaceResult.Ok) return r;
            }
            // Grow outward from the touching cells so each one is adjacent when placed.
            var left = new HashSet<Cell>(cells);
            var queue = new Queue<Cell>(touching.Count > 0 ? touching : new List<Cell> { cells[0] });
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (!left.Remove(c)) continue;
                order.Add(c);
                foreach (var n in c.Neighbours()) if (left.Contains(n)) queue.Enqueue(n);
            }
            return PlaceResult.Ok;
        }

        /// <summary>Places the kit's cells (the caller takes one kit from the bag). Returns how many cells went down.</summary>
        public static int Place(RaftGrid g, ItemDefinition kit, int rotation, Cell anchor)
        {
            if (CanPlace(g, kit, rotation, anchor, out var order) != PlaceResult.Ok) return 0;
            int placed = 0;
            foreach (var c in order) if (g.Place(c, kit.segment) == PlaceResult.Ok) placed++;
            return placed;
        }
    }
}
