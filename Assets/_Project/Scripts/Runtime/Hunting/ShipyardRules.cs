using System;
using BrineBattle.Items;
using BrineBattle.Rafts;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The Shipyard's rules (Ed's ask, 2026-10-31): the view's turn and zoom limits, the full halt of the world
    /// while it is open, and what clicking a cell would do. Plain C# for EditMode tests.
    /// </summary>
    /// <summary>The Shipyard's part sub-tabs (Ed: the one long list was too long).</summary>
    public enum PartTab { Planks, AutoLoot, Kits, Habitats, ShipPieces, ShipParts }

    /// <summary>
    /// The raft's four edges for the Shipyard's direction markers. The bow is downstream (+X); the stern is the
    /// upstream "left edge" (−X) where the auto-loot modules go; port is +Z and starboard −Z.
    /// </summary>
    public enum RaftEdge { Bow, Stern, Port, Starboard }

    public static class ShipyardRules
    {
        public static readonly RaftEdge[] Edges = { RaftEdge.Bow, RaftEdge.Stern, RaftEdge.Port, RaftEdge.Starboard };
        static readonly RaftEdge[] NoEdges = new RaftEdge[0];

        /// <summary>The edge's direction in the raft's own frame (x, z).</summary>
        public static (int x, int z) Direction(RaftEdge e) => e switch
        {
            RaftEdge.Bow => (1, 0),
            RaftEdge.Stern => (-1, 0),
            RaftEdge.Port => (0, 1),
            _ => (0, -1),
        };

        public static string EdgeName(RaftEdge e) => e switch
        {
            // In the player's words (Ed: "kulang ng module sa left"): left/right as seen in the game's normal view.
            RaftEdge.Bow => "RIGHT · bow",
            RaftEdge.Stern => "LEFT · stern",
            RaftEdge.Port => "TOP side",
            _ => "BOTTOM side",
        };

        /// <summary>Where a piece goes, as edges to point at (the hull and the sides: both sides; the mast stands at the bow edge).</summary>
        public static RaftEdge[] EdgesOf(PieceSlot slot) => slot switch
        {
            PieceSlot.Bow => new[] { RaftEdge.Bow },
            PieceSlot.Mast => new[] { RaftEdge.Bow },
            PieceSlot.Stern => new[] { RaftEdge.Stern },
            PieceSlot.Sides => new[] { RaftEdge.Port, RaftEdge.Starboard },
            PieceSlot.Hull => new[] { RaftEdge.Port, RaftEdge.Starboard },
            _ => Edges,
        };

        /// <summary>The edges a quest points at: its ship piece's slot, or the left edge for auto-loot modules; none otherwise.</summary>
        public static RaftEdge[] EdgesFor(Quest q) =>
            q == null ? NoEdges : q.Piece != null ? EdgesOf(q.Piece.Slot) : q.Id == "modules" ? new[] { RaftEdge.Stern } : NoEdges;

        /// <summary>The quests the Shipyard shows: only the raft and ship ones (Ed's ask).</summary>
        public static bool IsShipyardQuest(Quest q) =>
            q != null && (q.Id == "level" || q.Id == "piece" || q.Id == "modules" || q.Id == "vessel" || q.Id == "type" || q.Id == "hammer" || q.Id == "blueprint");

        /// <summary>
        /// From the raft's centre on screen (cx, cy), along a direction (dx, dy), to the border of the view rect:
        /// where an edge marker sits, at the outer edge of the view and never over the raft.
        /// </summary>
        public static (float x, float y) ToBorder(float cx, float cy, float dx, float dy, float minX, float minY, float maxX, float maxY)
        {
            float t = float.MaxValue;
            if (dx > 1e-5f) t = Math.Min(t, (maxX - cx) / dx);
            if (dx < -1e-5f) t = Math.Min(t, (minX - cx) / dx);
            if (dy > 1e-5f) t = Math.Min(t, (maxY - cy) / dy);
            if (dy < -1e-5f) t = Math.Min(t, (minY - cy) / dy);
            if (t == float.MaxValue || t < 0f) t = 0f;
            return (Math.Max(minX, Math.Min(maxX, cx + dx * t)), Math.Max(minY, Math.Min(maxY, cy + dy * t)));
        }

        /// <summary>The sub-tab an item belongs in, or null when it isn't something the Shipyard builds with.</summary>
        public static PartTab? TabOf(ItemDefinition item)
        {
            if (item == null) return null;
            switch (item.type)
            {
                case ItemType.Raft: return PartTab.Planks;
                case ItemType.Module: return ShapeRules.IsKit(item) ? PartTab.Kits : PartTab.AutoLoot;
                case ItemType.Habitat: return PartTab.Habitats;
                case ItemType.ShipPiece: return PartTab.ShipPieces;
                case ItemType.Vessel: return PartTab.ShipParts;
                default: return null;
            }
        }

        public static string TabName(PartTab t) => t switch
        {
            PartTab.Planks => "Planks",
            PartTab.AutoLoot => "Auto-loot",
            PartTab.Kits => "Kits",
            PartTab.Habitats => "Habitats",
            PartTab.ShipPieces => "Pieces",
            _ => "Ship parts",
        };

        /// <summary>Tabs whose items go on a cell (the rest are installed on their slot with one click).</summary>
        public static bool OnCells(PartTab t) => t <= PartTab.Habitats;

        public const float MinPitch = 8f, MaxPitch = 85f, MinZoom = 5f, MaxZoom = 28f;
        public const float StartYaw = 35f, StartPitch = 40f, StartZoom = 12f;
        /// <summary>Mouse movement (pixels) below which a press counts as a click, not a turn.</summary>
        public const float ClickSlop = 6f;

        public static float ClampPitch(float p) => Math.Max(MinPitch, Math.Min(MaxPitch, p));
        public static float ClampZoom(float z) => Math.Max(MinZoom, Math.Min(MaxZoom, z));
        /// <summary>Yaw kept in 0-360 so it never grows without end.</summary>
        public static float WrapYaw(float y) => ((y % 360f) + 360f) % 360f;
        public static bool IsClick(float movedPixels) => movedPixels < ClickSlop;

        /// <summary>
        /// The cell under the mouse (Ed: clicks over the raft must hit the right cell at any turn and zoom). The
        /// mouse ray meets the raft's deck plane at <paramref name="localX"/>, <paramref name="localZ"/> in the
        /// raft model's own frame, which is centred on the raft's middle cell (<paramref name="centerX"/>,
        /// <paramref name="centerZ"/>). Pure maths, no physics, so it works while the game is halted.
        /// </summary>
        public static Cell CellAt(float localX, float localZ, float centerX, float centerZ, float cellSize) =>
            new Cell((int)Math.Round(localX / cellSize + centerX, MidpointRounding.AwayFromZero), (int)Math.Round(localZ / cellSize + centerZ, MidpointRounding.AwayFromZero));

        /// <summary>True when the cell is on the 9×9 building grid.</summary>
        public static bool OnGrid(Cell c) => Math.Abs(c.X) <= RaftRules.MaxRadius && Math.Abs(c.Z) <= RaftRules.MaxRadius;

        /// <summary>
        /// Ed's ask: the whole game is halted while the Shipyard is open (no battles, looting, waves, drifts,
        /// spawns, clocks, minion jobs or auto-save). The world's time scale is 0; on close the old one comes back.
        /// </summary>
        public static float WorldTimeScale(bool shipyardOpen, float normal) => shipyardOpen ? 0f : normal;

        /// <summary>Minions don't help in the Shipyard (Ed's ask): no jobs while it is open.</summary>
        public static bool MinionsMayWork(bool shipyardOpen) => !shipyardOpen;

        /// <summary>Real-time timers (auto-save, falling asleep) wait while the Shipyard is open.</summary>
        public static bool TimersRun(bool shipyardOpen) => !shipyardOpen;

        public enum Action { None, Place, Replace, Remove }

        /// <summary>
        /// What a click on <paramref name="cell"/> would do, and why not (the same rules as building at sea):
        /// remove a part, place <paramref name="selected"/> on an empty cell, or swap a built cell's part for it.
        /// </summary>
        public static Action Preview(RaftGrid g, Cell cell, ItemDefinition selected, bool remove, out string why)
        {
            why = null;
            if (remove)
            {
                var r = g.CanRemove(cell);
                if (r == RemoveResult.Ok) return Action.Remove;
                why = RemoveText(r);
                return Action.None;
            }
            if (selected == null) { why = "Pick a part on the left."; return Action.None; }
            if (g.Has(cell))
            {
                if (g.Parts[cell] == selected) { why = "That part is already there."; return Action.None; }
                var rr = g.CanReplace(cell, selected);
                if (rr == PlaceResult.Ok) return Action.Replace;
                why = PlaceText(rr);
                return Action.None;
            }
            var pr = g.CanPlace(cell, selected);
            if (pr == PlaceResult.Ok) return Action.Place;
            why = PlaceText(pr);
            return Action.None;
        }

        public const string HammerText = "Your hammer is too weak for this raft level.";

        public static string PlaceText(PlaceResult r)
        {
            switch (r)
            {
                case PlaceResult.Ok: return "Fits.";
                case PlaceResult.Occupied: return "There's already a part there.";
                case PlaceResult.NotAdjacent: return "Parts must touch the raft.";
                case PlaceResult.OutOfBounds: return "Too far out (max 9x9).";
                case PlaceResult.BelowTier: return "That part is too weak for this raft level.";
                case PlaceResult.NeedsModules: return "Not enough modules on the left edge yet.";
                case PlaceResult.NeedsHammer: return HammerText;
                case PlaceResult.WrongType: return "That part is for another ship line, so it stays off.";
                case PlaceResult.Same: return "That part is already there.";
                default: return "That isn't a raft part.";
            }
        }

        public static string RemoveText(RemoveResult r)
        {
            switch (r)
            {
                case RemoveResult.Ok: return "Comes off.";
                case RemoveResult.WouldSplit: return "That part holds the raft together.";
                case RemoveResult.LastPart: return "You can't remove the last part.";
                case RemoveResult.NeedsHammer: return HammerText;
                default: return "No part there.";
            }
        }
    }
}
