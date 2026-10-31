using System;
using BrineBattle.Items;
using BrineBattle.Rafts;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The Shipyard's rules (Ed's ask, 2026-10-31): the view's turn and zoom limits, the full halt of the world
    /// while it is open, and what clicking a cell would do. Plain C# for EditMode tests.
    /// </summary>
    public static class ShipyardRules
    {
        public const float MinPitch = 8f, MaxPitch = 85f, MinZoom = 5f, MaxZoom = 28f;
        public const float StartYaw = 35f, StartPitch = 40f, StartZoom = 12f;
        /// <summary>Mouse movement (pixels) below which a press counts as a click, not a turn.</summary>
        public const float ClickSlop = 6f;

        public static float ClampPitch(float p) => Math.Max(MinPitch, Math.Min(MaxPitch, p));
        public static float ClampZoom(float z) => Math.Max(MinZoom, Math.Min(MaxZoom, z));
        /// <summary>Yaw kept in 0-360 so it never grows without end.</summary>
        public static float WrapYaw(float y) => ((y % 360f) + 360f) % 360f;
        public static bool IsClick(float movedPixels) => movedPixels < ClickSlop;

        /// <summary>How far the view may pan from the raft's middle, in metres (WASD / arrows, middle-drag).</summary>
        public const float PanLimit = 7f;
        public static float ClampPan(float v) => Math.Max(-PanLimit, Math.Min(PanLimit, v));

        /// <summary>
        /// A part dragged from the list drops on release only over the raft view and a cell; anywhere else
        /// (over the list or bars, or off the raft) the drag is cancelled.
        /// </summary>
        public static bool Drops(bool overUi, bool overCell) => !overUi && overCell;

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
