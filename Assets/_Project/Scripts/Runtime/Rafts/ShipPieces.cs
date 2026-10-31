using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// Where a ship piece goes. Never on a deck cell where people walk: flat on the deck's rim, on the sides,
    /// past the bow or the stern, under the deck (hull) or up on the mast line.
    /// </summary>
    public enum PieceSlot { Rim, Sides, Bow, Stern, Hull, Mast }

    /// <summary>One piece that turns the raft into a boat (Ed's ask): it opens at a raft level, a minion or the hunter installs it.</summary>
    public sealed class ShipPiece
    {
        /// <summary>Also the item id of the piece in the bag.</summary>
        public readonly string Id;
        public readonly string Name;
        /// <summary>Raft level it opens at; the raft can't level past it until the piece is on.</summary>
        public readonly int Level;
        public readonly PieceSlot Slot;
        public readonly RaftBonus Bonus;
        public readonly string Look;

        public ShipPiece(string id, string name, int level, PieceSlot slot, RaftBonus bonus, string look)
        {
            Id = id; Name = name; Level = level; Slot = slot; Bonus = bonus; Look = look;
        }

        public Rarity Rarity => ShipPieceRules.RarityAt(Level);
        public override string ToString() => Name;
    }

    /// <summary>
    /// The raft-to-boat pieces (Ed's ask, 2026-10-31): 22 of them open one every two or three raft levels from
    /// Lv 2 to 49. Minions (Builder/Crewmate) craft and install them, or the hunter does in the Shipyard. Each
    /// installed piece shows on the raft and gives a small stat, and the raft can't level past a piece's level
    /// until it is on, so by Lv 50 the whole set reads as a simple boat.
    /// </summary>
    public static class ShipPieceRules
    {
        static RaftBonus Hp(int p) => new RaftBonus { RaftHpPercent = p };
        static RaftBonus Def(int d) => new RaftBonus { Defense = d };
        static RaftBonus Spd(int s) => new RaftBonus { Speed = s };
        static RaftBonus Atk(int a) => new RaftBonus { Attack = a };
        static RaftBonus Reach(float r) => new RaftBonus { Reach = r };

        public static readonly ShipPiece[] All =
        {
            new ShipPiece("piece_lashing_rope", "Lashing Rope", 2, PieceSlot.Rim, Hp(2), "rope bands tie the planks together"),
            new ShipPiece("piece_outrigger", "Outrigger", 4, PieceSlot.Sides, Def(1), "a bamboo float (katig) on each side"),
            new ShipPiece("piece_bamboo_arm", "Bamboo Arms", 6, PieceSlot.Sides, Hp(3), "arms under the deck hold the floats"),
            new ShipPiece("piece_deck_trim", "Deck Trim", 8, PieceSlot.Rim, Def(1), "a flat trim band around the deck"),
            new ShipPiece("piece_low_wall", "Low Wall", 10, PieceSlot.Sides, Def(2), "low side walls along the edges"),
            new ShipPiece("piece_gunwale", "Gunwale", 13, PieceSlot.Sides, Def(2), "the side walls rise into gunwales"),
            new ShipPiece("piece_pointed_bow", "Pointed Bow", 15, PieceSlot.Bow, Spd(1), "a pointed bow past the front edge"),
            new ShipPiece("piece_hull_rib", "Hull Ribs", 18, PieceSlot.Hull, Hp(5), "ribs curve down under the deck"),
            new ShipPiece("piece_keel", "Keel", 20, PieceSlot.Hull, Spd(1), "a keel along the bottom"),
            new ShipPiece("piece_hull_plank", "Hull Planking", 23, PieceSlot.Hull, Hp(6), "planks close the hull under the deck"),
            new ShipPiece("piece_railing", "Railing", 25, PieceSlot.Sides, Def(2), "rails on top of the gunwales"),
            new ShipPiece("piece_mast_step", "Mast Step", 27, PieceSlot.Mast, Hp(3), "a block that holds a real mast"),
            new ShipPiece("piece_wooden_mast", "Wooden Mast", 29, PieceSlot.Mast, Spd(1), "the bamboo pole becomes a wooden mast"),
            new ShipPiece("piece_yard_sail", "Yard & Sail", 31, PieceSlot.Mast, Spd(2), "a yard arm and a big sail"),
            new ShipPiece("piece_second_mast", "Second Mast", 34, PieceSlot.Mast, Spd(1), "a second mast at the back"),
            new ShipPiece("piece_stern_deck", "Stern Deck", 36, PieceSlot.Stern, Hp(5), "a stern platform past the back edge"),
            new ShipPiece("piece_rudder", "Rudder", 38, PieceSlot.Stern, Spd(1), "a rudder at the stern"),
            new ShipPiece("piece_small_cabin", "Small Cabin", 40, PieceSlot.Stern, Def(3), "a small cabin on the stern deck"),
            new ShipPiece("piece_bowsprit", "Bowsprit", 43, PieceSlot.Bow, Reach(1f), "a bowsprit out past the bow"),
            new ShipPiece("piece_figurehead", "Figurehead", 45, PieceSlot.Bow, Atk(2), "a figurehead under the bowsprit"),
            new ShipPiece("piece_lanterns", "Lanterns", 47, PieceSlot.Sides, Reach(0.5f), "lanterns on the corners"),
            new ShipPiece("piece_flag", "Flag", 49, PieceSlot.Mast, Atk(1), "a flag at the masthead"),
        };

        public static ShipPiece Find(string id) => All.FirstOrDefault(p => p.Id == id);
        public static bool IsPiece(ItemDefinition item) => item != null && item.type == ItemType.ShipPiece && Find(item.id) != null;

        /// <summary>Rarity (and cost) by band: Lv 1-10 Common, 11-20 Uncommon, 21-30 Rare, 31-40 Epic, 41-50 Legendary.</summary>
        public static Rarity RarityAt(int level) => (Rarity)Math.Min((int)Rarity.Legendary, Math.Max(0, (level - 1) / 10));

        /// <summary>The raft level that counts for pieces: a ship has passed raft Lv 50, so every piece is open.</summary>
        public static int RaftLevel(bool isShip, int level) => isShip ? EvolutionRules.RaftMax : level;

        /// <summary>Pieces open at this raft level, in order.</summary>
        public static IEnumerable<ShipPiece> Opened(int raftLevel) => All.Where(p => p.Level <= raftLevel);

        /// <summary>Open pieces not on yet: the next level-up waits for all of them.</summary>
        public static List<ShipPiece> Missing(int raftLevel, IEnumerable<string> installed) =>
            Opened(raftLevel).Where(p => !installed.Contains(p.Id)).ToList();

        /// <summary>The next piece to put on (the lowest open one missing), or the next to open, or null when all are on.</summary>
        public static ShipPiece Next(int raftLevel, IEnumerable<string> installed) =>
            All.FirstOrDefault(p => !installed.Contains(p.Id));

        /// <summary>Why this piece can't go on now, or null when it can (the bag and hammer are checked by the raft).</summary>
        public static string InstallBlocker(ShipPiece piece, int raftLevel, IEnumerable<string> installed)
        {
            if (piece == null) return "That isn't a ship piece.";
            if (installed.Contains(piece.Id)) return "It's already on.";
            if (raftLevel < piece.Level) return $"Opens at raft Lv {piece.Level}.";
            return null;
        }

        /// <summary>All the stats the installed pieces give.</summary>
        public static RaftBonus Bonus(IEnumerable<string> installed)
        {
            var b = default(RaftBonus);
            foreach (var id in installed) { var p = Find(id); if (p != null) b += p.Bonus; }
            return b;
        }

        /// <summary>Older saves (before pieces): every piece their level had already passed, for free, so they look right.</summary>
        public static IEnumerable<string> FreeFor(int raftLevel) => Opened(raftLevel).Select(p => p.Id);

        /// <summary>0 → 1: how far a piece's look has grown, from its install level to Lv 50 (gradual change every level).</summary>
        public static float Growth(ShipPiece piece, int raftLevel) =>
            piece == null ? 0f : Math.Max(0f, Math.Min(1f, (raftLevel - piece.Level + 1) / (float)Math.Max(1, EvolutionRules.RaftMax - piece.Level + 1)));

        public static string SlotText(PieceSlot s) => s switch
        {
            PieceSlot.Rim => "deck rim (flat)",
            PieceSlot.Sides => "both sides",
            PieceSlot.Bow => "bow (front)",
            PieceSlot.Stern => "stern (back)",
            PieceSlot.Hull => "hull (under the deck)",
            _ => "mast",
        };

        public static string Describe(ShipPiece p) =>
            $"Ship piece for the {SlotText(p.Slot)}: {p.Look}. Opens at raft Lv {p.Level}; the raft can't level past {p.Level} without it. Gives {p.Bonus}. A Builder or Crewmate minion makes and installs it, or install it yourself in the Shipyard (Y).";
    }
}
