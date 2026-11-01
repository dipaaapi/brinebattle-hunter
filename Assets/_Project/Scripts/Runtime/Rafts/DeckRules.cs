using System;
using System.Collections.Generic;

namespace BrineBattle.Rafts
{
    /// <summary>A flat box in the raft's own frame (metres): the footprint of one piece of raft decor.</summary>
    public readonly struct DeckBox
    {
        public readonly float MinX, MaxX, MinZ, MaxZ;
        public DeckBox(float minX, float maxX, float minZ, float maxZ) { MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; }
        public bool Overlaps(float x, float z, float r) => x + r > MinX && x - r < MaxX && z + r > MinZ && z - r < MaxZ;
    }

    /// <summary>
    /// Ed's ask: no part of the raft or ship may overlap a body (hunter, minion or monster), and nobody may stand
    /// half off the planks looking like they float. Bodies on the raft stay fully on walkable planks; bodies in
    /// the water stay out of the decor's footprint. Plain C# for EditMode tests.
    /// </summary>
    public static class DeckRules
    {
        /// <summary>Gap kept between a body and the deck's edge or a piece of decor.</summary>
        public const float Margin = 0.05f;

        /// <summary>
        /// Keeps a round body of radius <paramref name="r"/> at (x, z) fully on <paramref name="walkable"/> cells:
        /// it can lean into a walkable neighbour, but never past an edge (where the walls and the water are).
        /// A point off the planks is brought back onto the nearest walkable cell.
        /// </summary>
        public static (float x, float z) Clamp(float x, float z, float r, float cellSize, ICollection<Cell> walkable)
        {
            if (walkable == null || walkable.Count == 0) return (x, z);
            var c = new Cell((int)Math.Round(x / cellSize), (int)Math.Round(z / cellSize));
            if (!walkable.Contains(c))
            {
                float best = float.MaxValue;
                foreach (var w in walkable)
                {
                    float dx = w.X * cellSize - x, dz = w.Z * cellSize - z, d = dx * dx + dz * dz;
                    if (d < best) { best = d; c = w; }
                }
            }
            float limit = Math.Max(0f, cellSize / 2f - r - Margin);
            float cx = c.X * cellSize, cz = c.Z * cellSize;
            // A walkable neighbour lets the body lean over the seam (full cell); an edge stops it.
            float maxX = walkable.Contains(new Cell(c.X + 1, c.Z)) ? cx + cellSize / 2f : cx + limit;
            float minX = walkable.Contains(new Cell(c.X - 1, c.Z)) ? cx - cellSize / 2f : cx - limit;
            float maxZ = walkable.Contains(new Cell(c.X, c.Z + 1)) ? cz + cellSize / 2f : cz + limit;
            float minZ = walkable.Contains(new Cell(c.X, c.Z - 1)) ? cz - cellSize / 2f : cz - limit;
            return (Math.Max(minX, Math.Min(maxX, x)), Math.Max(minZ, Math.Min(maxZ, z)));
        }

        /// <summary>Pushes a round body at (x, z) out of a decor box along the shorter way, or leaves it.</summary>
        public static (float x, float z) PushOut(DeckBox b, float x, float z, float r)
        {
            if (!b.Overlaps(x, z, r + Margin)) return (x, z);
            float left = x + r + Margin - b.MinX, right = b.MaxX - (x - r - Margin);
            float down = z + r + Margin - b.MinZ, up = b.MaxZ - (z - r - Margin);
            float m = Math.Min(Math.Min(left, right), Math.Min(down, up));
            if (m == left) return (x - left, z);
            if (m == right) return (x + right, z);
            if (m == down) return (x, z - down);
            return (x, z + up);
        }

        /// <summary>How far the boxes reach from the raft's centre along a direction (dx, dz): where a body coming from there must stop.</summary>
        public static float Reach(IEnumerable<DeckBox> boxes, float dx, float dz)
        {
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-5f) return 0f;
            dx /= len; dz /= len;
            float best = 0f;
            foreach (var b in boxes)
            {
                // The box's farthest corner along the direction, but only for boxes that lie on that side's line.
                float along = Math.Max(b.MinX * dx, b.MaxX * dx) + Math.Max(b.MinZ * dz, b.MaxZ * dz);
                float across = Math.Min(Math.Abs(Side(b.MinX, b.MinZ, dx, dz)), Math.Abs(Side(b.MaxX, b.MaxZ, dx, dz)));
                bool spans = Side(b.MinX, b.MinZ, dx, dz) * Side(b.MaxX, b.MaxZ, dx, dz) <= 0f
                    || Side(b.MinX, b.MaxZ, dx, dz) * Side(b.MaxX, b.MinZ, dx, dz) <= 0f;
                if (spans || across < 1.5f) best = Math.Max(best, along);
            }
            return best;
        }

        static float Side(float x, float z, float dx, float dz) => x * dz - z * dx;
    }
}
