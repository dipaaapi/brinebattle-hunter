using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Z;
        public Cell(int x, int z) { X = x; Z = z; }
        public bool Equals(Cell o) => X == o.X && Z == o.Z;
        public override bool Equals(object obj) => obj is Cell o && Equals(o);
        public override int GetHashCode() => (X * 397) ^ Z;
        public override string ToString() => $"({X},{Z})";
        public IEnumerable<Cell> Neighbours()
        {
            yield return new Cell(X + 1, Z);
            yield return new Cell(X - 1, Z);
            yield return new Cell(X, Z + 1);
            yield return new Cell(X, Z - 1);
        }
    }

    public enum PlaceResult
    {
        Ok = 0,
        Occupied = 1,
        NotAdjacent = 2,
        OutOfBounds = 3,
        NotARaftPart = 4,
        BelowTier = 5,
    }

    public enum RemoveResult
    {
        Ok = 0,
        Empty = 1,
        LastPart = 2,
        WouldSplit = 3,
    }

    /// <summary>Numbers for the raft builder (GDD §6). Tunable here.</summary>
    public static class RaftRules
    {
        public const int MaxRadius = 4;          // grid is -4..4 on both axes (9x9)
        public const int BasePartHp = 10;
        public const float TierHpBonus = 0.2f;   // +20% part HP per raft tier
        public const int StarterParts = 9;
        public const int PartsPerSize = 4;

        /// <summary>9 parts = size 1, then +1 size every 4 parts (13 = 2, 17 = 3 ...).</summary>
        public static int SizeFor(int parts) =>
            Math.Max(1, Math.Min(10, 1 + (parts - StarterParts) / PartsPerSize));

        /// <summary>Part HP grows with the part's rarity and the raft's tier.</summary>
        public static int PartHp(Rarity part, Rarity raftTier) =>
            (int)Math.Round(BasePartHp * ((int)part + 1) * (1f + TierHpBonus * (int)raftTier));

        /// <summary>Essence (rarity ≥ current tier) needed to raise the tier one step. 0 at max.</summary>
        public static int UpgradeCost(Rarity tier) => tier >= Rarity.Mythic ? 0 : 10 * ((int)tier + 1);
    }

    /// <summary>
    /// The raft as a grid of parts. Parts must touch the raft, fit the raft's tier
    /// (rarity ≥ tier, GDD §11) and stay connected. Plain C# for EditMode tests.
    /// </summary>
    public class RaftGrid
    {
        readonly Dictionary<Cell, ItemDefinition> parts = new Dictionary<Cell, ItemDefinition>();

        public Rarity Tier { get; private set; } = Rarity.Common;
        public event Action Changed;

        public IReadOnlyDictionary<Cell, ItemDefinition> Parts => parts;
        public int Count => parts.Count;
        public int Size => RaftRules.SizeFor(parts.Count);
        public int MaxHp => parts.Values.Sum(p => RaftRules.PartHp(p.rarity, Tier));
        public int Radius => parts.Count == 0 ? 0 : parts.Keys.Max(c => Math.Max(Math.Abs(c.X), Math.Abs(c.Z)));

        /// <summary>Square of starter parts around the centre, ignoring tier and adjacency.</summary>
        public void FillSquare(ItemDefinition part, int half)
        {
            for (int x = -half; x <= half; x++)
                for (int z = -half; z <= half; z++)
                    parts[new Cell(x, z)] = part;
            Changed?.Invoke();
        }

        public bool Has(Cell c) => parts.ContainsKey(c);

        public PlaceResult CanPlace(Cell c, ItemDefinition part)
        {
            if (part == null || part.type != ItemType.Raft) return PlaceResult.NotARaftPart;
            if (Math.Abs(c.X) > RaftRules.MaxRadius || Math.Abs(c.Z) > RaftRules.MaxRadius) return PlaceResult.OutOfBounds;
            if (parts.ContainsKey(c)) return PlaceResult.Occupied;
            if (parts.Count > 0 && !c.Neighbours().Any(parts.ContainsKey)) return PlaceResult.NotAdjacent;
            if (!SalvageRules.IsCompatible(part.type, part.rarity, Tier)) return PlaceResult.BelowTier;
            return PlaceResult.Ok;
        }

        public PlaceResult Place(Cell c, ItemDefinition part)
        {
            var r = CanPlace(c, part);
            if (r != PlaceResult.Ok) return r;
            parts[c] = part;
            Changed?.Invoke();
            return r;
        }

        public RemoveResult CanRemove(Cell c)
        {
            if (!parts.ContainsKey(c)) return RemoveResult.Empty;
            if (parts.Count == 1) return RemoveResult.LastPart;
            return StaysConnectedWithout(c) ? RemoveResult.Ok : RemoveResult.WouldSplit;
        }

        public RemoveResult Remove(Cell c, out ItemDefinition removed)
        {
            removed = null;
            var r = CanRemove(c);
            if (r != RemoveResult.Ok) return r;
            removed = parts[c];
            parts.Remove(c);
            Changed?.Invoke();
            return r;
        }

        /// <summary>Empty cells next to the raft where a part could go (ignores tier).</summary>
        public IEnumerable<Cell> OpenCells() =>
            parts.Keys.SelectMany(c => c.Neighbours())
                .Where(c => !parts.ContainsKey(c) && Math.Abs(c.X) <= RaftRules.MaxRadius && Math.Abs(c.Z) <= RaftRules.MaxRadius)
                .Distinct();

        /// <summary>Raise the tier one step if the inventory can pay. Lower-rarity parts already placed stay.</summary>
        public bool TryUpgrade(Inventory inv)
        {
            int cost = RaftRules.UpgradeCost(Tier);
            if (cost == 0 || !inv.SpendEssence(Tier, cost)) return false;
            Tier++;
            Changed?.Invoke();
            return true;
        }

        bool StaysConnectedWithout(Cell removed)
        {
            var rest = parts.Keys.Where(c => !c.Equals(removed)).ToList();
            var seen = new HashSet<Cell> { rest[0] };
            var queue = new Queue<Cell>(seen);
            while (queue.Count > 0)
                foreach (var n in queue.Dequeue().Neighbours())
                    if (!n.Equals(removed) && parts.ContainsKey(n) && seen.Add(n))
                        queue.Enqueue(n);
            return seen.Count == rest.Count;
        }
    }
}
