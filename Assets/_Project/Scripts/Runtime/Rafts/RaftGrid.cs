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
        /// <summary>Raft parts wait until enough of the upstream edge holds auto-loot modules.</summary>
        NeedsModules = 6,
        /// <summary>No hammer of the raft's level or better in the bag (Ed's ask).</summary>
        NeedsHammer = 7,
        /// <summary>Replace: the new part is the same as the one already there.</summary>
        Same = 8,
        /// <summary>A typed part of a raft type that wasn't picked (Ed's ask: it's disabled once a type is picked).</summary>
        WrongType = 9,
    }

    public enum RemoveResult
    {
        Ok = 0,
        Empty = 1,
        LastPart = 2,
        WouldSplit = 3,
        NeedsHammer = 4,
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

        /// <summary>Monsters killed since the last tier upgrade needed for the next one (owner's call): 5, 10, 15 ...</summary>
        public static int KillsToUpgrade(Rarity tier) => 5 * ((int)tier + 1);

        /// <summary>Monster materials (loot, any rarity) spent on a tier upgrade: 5, 10, 15 ...</summary>
        public static int MaterialsToUpgrade(Rarity tier) => 5 * ((int)tier + 1);

        /// <summary>Share of the upstream edge rows that must end in an auto-loot module before the raft can grow or upgrade (owner's call).</summary>
        public const float ModuleShareToGrow = 0.5f;

        /// <summary>
        /// Building needs a hammer (Ed's ask: no building by hand in the middle of the ocean). A hammer
        /// works on a raft whose level is at most its rarity, so after a raft level-up the hammer needs upgrading too.
        /// </summary>
        public static bool HammerFits(Rarity hammer, Rarity raftTier) => hammer >= raftTier;

        /// <summary>The best hammer in the bag, or null.</summary>
        public static ItemDefinition BestHammer(Inventory inv) =>
            inv?.Items.Select(kv => kv.Key).Where(i => i.type == ItemType.Tool).OrderByDescending(i => i.rarity).FirstOrDefault();

        public static bool HasHammer(Inventory inv, Rarity raftTier)
        {
            var h = BestHammer(inv);
            return h != null && HammerFits(h.rarity, raftTier);
        }

        /// <summary>
        /// How high the deck stands above the water, in metres (owner's call: a higher raft level means
        /// thicker planks, so waves stop washing over it). Lv 1 0.30 … Lv 6 0.90, above the tallest wave.
        /// </summary>
        public static float Freeboard(Rarity tier) => 0.3f + 0.12f * (int)tier;
        /// <summary>Share of the full swell around the raft: Lv 1 rides half-height waves, Lv 6 the open sea.</summary>
        public static float SeaShare(Rarity tier) => Math.Min(1f, 0.5f + 0.1f * (int)tier);
    }

    /// <summary>
    /// The raft as a grid of parts. Parts must touch the raft, fit the raft's tier
    /// (rarity ≥ tier, GDD §11) and stay connected. Plain C# for EditMode tests.
    /// </summary>
    public class RaftGrid
    {
        readonly Dictionary<Cell, ItemDefinition> parts = new Dictionary<Cell, ItemDefinition>();

        /// <summary>The tier of the current level (<see cref="EvolutionRules.TierOf"/>): what parts, hammers, essence and the boss must be.</summary>
        public Rarity Tier { get; private set; } = Rarity.Common;

        /// <summary>Raft Lv 1-50, then (after the ship pick) ship Lv 1-100 (Ed's ask).</summary>
        public int Level { get; private set; } = 1;
        RaftKind line;
        /// <summary>The picked ship line, or Plain while it is still a raft.</summary>
        public RaftKind Chosen => line;
        public bool IsShip => line != RaftKind.Plain;
        public int MaxLevel => EvolutionRules.MaxLevel(line);
        public bool AtMax => Level >= MaxLevel;
        /// <summary>"Raft Lv 12" or "Aerial ship Lv 3".</summary>
        public string LevelName => EvolutionRules.Label(line, Level);
        public string FormName => EvolutionRules.FormName(line, Level);

        public int KillsNeeded => EvolutionRules.Kills(Tier);
        public int MaterialsNeeded => EvolutionRules.Materials(Tier);
        public int EssenceNeeded => EvolutionRules.Essence(Tier);
        /// <summary>True when this level's boss must be beaten before going on (and isn't yet).</summary>
        public bool BossPending => BossesNeeded && EvolutionRules.BossGate(line, Level) && !BossBeaten(Tier);
        /// <summary>The ship part (1-5) that must be on before the next level-up, or 0.</summary>
        public int PartPending
        {
            get { int p = EvolutionRules.PartNeeded(line, Level); return p > 0 && !HasVessel(p) ? p : 0; }
        }

        void SetLevel(RaftKind newLine, int level)
        {
            line = newLine;
            Level = Math.Max(1, Math.Min(EvolutionRules.MaxLevel(newLine), level));
            Tier = EvolutionRules.TierOf(line, Level);
        }

        /// <summary>Loads the line and level from a save.</summary>
        public void RestoreEvolution(RaftKind savedLine, int level)
        {
            SetLevel(savedLine, level);
            Changed?.Invoke();
        }

        /// <summary>Monsters killed since the last level-up; the scene counts them.</summary>
        public int Kills { get; set; }

        /// <summary>
        /// Share of the upstream edge that must hold modules before raft parts go on or the tier goes up.
        /// 0 (off) for a bare grid; the game sets <see cref="RaftRules.ModuleShareToGrow"/>.
        /// </summary>
        public float ModulesToGrow { get; set; }

        /// <summary>Drifts come from −X (the left), so that is the upstream edge.</summary>
        public const int UpstreamX = -1;
        public event Action Changed;

        /// <summary>
        /// The bag whose hammer allows building (Ed's ask). Null = no hammer needed (bare grids in tests);
        /// the game sets the hunter's bag.
        /// </summary>
        public Inventory Tools { get; set; }
        bool HammerReady => Tools == null || RaftRules.HasHammer(Tools, Tier);

        public IReadOnlyDictionary<Cell, ItemDefinition> Parts => parts;
        public int Count => parts.Count;
        public int Size => RaftRules.SizeFor(parts.Count);
        /// <summary>Battle HP: every part's HP, with the raft type's bonus (Phase 8).</summary>
        public int MaxHp => parts.Count == 0 ? 0 : RaftTypeRules.WithHpBonus(parts.Values.Sum(p => RaftRules.PartHp(p.rarity, Tier)), Type.Bonus);
        /// <summary>Fortress, Sky, Beast, a hybrid or Plain, from the raft parts (Phase 8).</summary>
        /// <summary>
        /// The type picked in the raft tree wins (Ed: a Sky raft must look and fight like Sky even with
        /// Fortress planks on it). Before a pick, the parts decide (Phase 8).
        /// </summary>
        public RaftType Type => Chosen != RaftKind.Plain ? new RaftType(Chosen, level: Level) : RaftTypeRules.Classify(parts.Values);
        /// <summary>Companion habitats on the raft; each adds room for one companion.</summary>
        public int HabitatCount => parts.Values.Count(p => p.type == ItemType.Habitat);
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

        /// <summary>Planks only (Ed's ask): the hunter and pets walk on raft parts, never on modules or habitats.</summary>
        public bool Walkable(Cell c) => parts.TryGetValue(c, out var p) && p.type == ItemType.Raft;

        /// <summary>Rows of the raft, and how many of them end upstream in an auto-loot module.</summary>
        public (int guarded, int rows) UpstreamModules()
        {
            var ends = parts.Keys.GroupBy(c => c.Z).Select(row => UpstreamX < 0 ? row.OrderBy(c => c.X).First() : row.OrderByDescending(c => c.X).First()).ToList();
            return (ends.Count(c => parts[c].type == ItemType.Module), ends.Count);
        }

        /// <summary>
        /// Where a module would guard a row that has none yet: the open cell just upstream of each such
        /// row's end (the build guide highlights these). Rows already at the grid's edge have no spot.
        /// </summary>
        public List<Cell> UpstreamGaps()
        {
            var gaps = new List<Cell>();
            foreach (var row in parts.Keys.GroupBy(c => c.Z).OrderBy(r => Math.Abs(r.Key)).ThenBy(r => r.Key))
            {
                var end = UpstreamX < 0 ? row.OrderBy(c => c.X).First() : row.OrderByDescending(c => c.X).First();
                if (parts[end].type == ItemType.Module) continue;
                var spot = new Cell(end.X + UpstreamX, end.Z);
                if (Math.Abs(spot.X) <= RaftRules.MaxRadius) gaps.Add(spot);
            }
            return gaps;
        }

        /// <summary>Modules needed on the upstream edge (rounded up), given the current rows.</summary>
        public int ModulesNeeded => (int)Math.Ceiling(UpstreamModules().rows * ModulesToGrow - 1e-4);

        /// <summary>True when enough of the upstream edge holds modules to add raft parts or upgrade.</summary>
        public bool ModulesReady => UpstreamModules().guarded >= ModulesNeeded;

        /// <summary>
        /// Replaces every part at once (loading a save); skips placement rules. <paramref name="tier"/> sets the
        /// level to the first one of that tier on the current line (older saves and tests).
        /// </summary>
        public void Restore(IEnumerable<KeyValuePair<Cell, ItemDefinition>> saved, Rarity tier)
        {
            parts.Clear();
            foreach (var kv in saved) if (kv.Value != null) parts[kv.Key] = kv.Value;
            SetLevel(line, EvolutionRules.FirstLevelOf(line, tier));
            Tier = tier;
            Changed?.Invoke();
        }

        public PlaceResult CanPlace(Cell c, ItemDefinition part)
        {
            if (part == null || (part.type != ItemType.Raft && part.type != ItemType.Module && part.type != ItemType.Habitat)) return PlaceResult.NotARaftPart;
            if (Math.Abs(c.X) > RaftRules.MaxRadius || Math.Abs(c.Z) > RaftRules.MaxRadius) return PlaceResult.OutOfBounds;
            if (parts.ContainsKey(c)) return PlaceResult.Occupied;
            if (parts.Count > 0 && !c.Neighbours().Any(parts.ContainsKey)) return PlaceResult.NotAdjacent;
            if (!SalvageRules.IsCompatible(part.type, part.rarity, Tier)) return PlaceResult.BelowTier;
            if (!FitsType(part)) return PlaceResult.WrongType;
            if (part.type == ItemType.Raft && !ModulesReady) return PlaceResult.NeedsModules;
            if (!HammerReady) return PlaceResult.NeedsHammer;
            return PlaceResult.Ok;
        }

        /// <summary>
        /// Swaps the part on an occupied cell for another (Ed's ask: replace instead of remove, then place).
        /// The raft keeps its shape, so only the type, tier and hammer rules apply.
        /// </summary>
        public PlaceResult CanReplace(Cell c, ItemDefinition part)
        {
            if (part == null || (part.type != ItemType.Raft && part.type != ItemType.Module && part.type != ItemType.Habitat)) return PlaceResult.NotARaftPart;
            if (!parts.TryGetValue(c, out var old)) return PlaceResult.NotAdjacent;
            if (old == part) return PlaceResult.Same;
            if (!SalvageRules.IsCompatible(part.type, part.rarity, Tier)) return PlaceResult.BelowTier;
            if (!FitsType(part)) return PlaceResult.WrongType;
            if (!HammerReady) return PlaceResult.NeedsHammer;
            return PlaceResult.Ok;
        }

        public PlaceResult Replace(Cell c, ItemDefinition part, out ItemDefinition old)
        {
            old = null;
            var r = CanReplace(c, part);
            if (r != PlaceResult.Ok) return r;
            old = parts[c];
            parts[c] = part;
            Changed?.Invoke();
            return r;
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
            if (!HammerReady) return RemoveResult.NeedsHammer;
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

        /// <summary>
        /// Why the next level-up can't happen yet, or null when it can: kills, materials, essence, modules on the
        /// left edge, the hammer, the band's boss and the ship part the level needs. At raft Lv 50 the next step
        /// is the ship pick instead; at ship Lv 100 the next evolution is coming soon.
        /// </summary>
        public string UpgradeBlocker(Inventory inv)
        {
            if (!IsShip && AtMax) return "The raft is a boat now: pick its ship line in the Raft tree.";
            if (IsShip && AtMax) return $"Lv {EvolutionRules.ShipMax}: the next evolution is {EvolutionRules.NextEvolution.ToLowerInvariant()}.";
            var missing = new List<string>();
            int kills = KillsNeeded, mats = MaterialsNeeded, cost = EssenceNeeded;
            if (Kills < kills) missing.Add($"kill {kills - Kills} more monster{(kills - Kills == 1 ? "" : "s")}");
            if (inv.MaterialCount() < mats) missing.Add($"{mats - inv.MaterialCount()} more monster materials");
            if (inv.EssenceAtLeast(Tier) < cost) missing.Add($"{cost - inv.EssenceAtLeast(Tier)} more {Tier}+ essence");
            var (guarded, _) = UpstreamModules();
            if (guarded < ModulesNeeded) missing.Add($"{ModulesNeeded - guarded} more auto-loot module{(ModulesNeeded - guarded == 1 ? "" : "s")} on the left edge");
            if (!HammerReady) missing.Add($"a {Tier} hammer or better");
            if (BossPending) missing.Add($"beat the {Tier} boss");
            if (PartPending > 0) missing.Add($"put on the {VesselRules.PartName(line, PartPending)}");
            return missing.Count == 0 ? null : "Needs " + string.Join(", ", missing) + ".";
        }

        public bool TryUpgrade(Inventory inv) => TryUpgrade(inv, line);

        /// <summary>One level up: spends the materials and essence; parts already placed stay. <paramref name="path"/> is ignored (older callers).</summary>
        public bool TryUpgrade(Inventory inv, RaftKind path)
        {
            if (UpgradeBlocker(inv) != null) return false;
            inv.SpendMaterials(MaterialsNeeded);
            inv.SpendEssence(Tier, EssenceNeeded);
            Kills = 0;
            SetLevel(line, Level + 1);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Why the ship line can't be picked yet, or null: the raft must be Lv 50 and the Rare boss beaten.</summary>
        public string ChooseBlocker()
        {
            if (IsShip) return $"Already a {EvolutionRules.LineName(line)}.";
            if (Level < EvolutionRules.RaftMax) return $"The ship line opens at raft Lv {EvolutionRules.RaftMax}.";
            if (BossPending) return $"Beat the {Tier} boss first.";
            return null;
        }

        /// <summary>True when the raft is a Lv 50 boat with its boss beaten and no ship line yet.</summary>
        public bool CanChoose => ChooseBlocker() == null;

        /// <summary>
        /// Picks the ship line (Ed's ask): Sea level (STR), Aerial (INT) or Undersea (DEX). The ship starts again
        /// at Lv 1 and evolves to Lv 100. Once only.
        /// </summary>
        public bool Choose(RaftKind kind)
        {
            if (!CanChoose || kind == RaftKind.Plain) return false;
            SetLevel(kind, 1);
            Kills = 0;
            vessel.Clear();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// False for a typed raft part of a type that wasn't picked (Ed's ask). Plain parts, modules,
        /// habitats and every part before the pick always fit.
        /// </summary>
        public bool FitsType(ItemDefinition part) =>
            part == null || part.type != ItemType.Raft || part.raftKind == RaftKind.Plain
            || Chosen == RaftKind.Plain || part.raftKind == Chosen;

        /// <summary>
        /// Once the type is picked (Ed's ask), every placed part of another type goes back to the bag and
        /// a basic plank takes its cell, so the raft keeps its shape. Returns the parts sent to the bag.
        /// </summary>
        public List<ItemDefinition> ResetOffType(ItemDefinition basic, Inventory bag)
        {
            var back = new List<ItemDefinition>();
            if (basic == null || Chosen == RaftKind.Plain) return back;
            foreach (var c in parts.Keys.ToList())
            {
                var p = parts[c];
                if (FitsType(p)) continue;
                parts[c] = basic;
                bag?.Add(p, 1);
                back.Add(p);
            }
            if (back.Count > 0) Changed?.Invoke();
            return back;
        }

        /// <summary>Share of the parts the sea takes when the raft is reset from Settings (Ed's ask).</summary>
        public const float WashAwayShare = 0.2f;

        /// <summary>
        /// Reset raft (Settings, Ed's ask): the raft goes back to the starter square of basic planks so it
        /// can be rebuilt. Level and type stay. Every other part comes off: about
        /// <see cref="WashAwayShare"/> of them float away, the rest go to the bag. Habitats are never lost
        /// (companions live in them): they go back on the edge of the square. Returns what the sea took.
        /// </summary>
        public List<ItemDefinition> ResetRaft(ItemDefinition basic, Inventory bag, Random rng) => Strip(basic, bag, rng, WashAwayShare);

        /// <summary>
        /// Back to the starter square: every other part comes off, <paramref name="share"/> of them are lost,
        /// the rest go to the bag; habitats go back on the square's edge.
        /// </summary>
        List<ItemDefinition> Strip(ItemDefinition basic, Inventory bag, Random rng, float share)
        {
            var lost = new List<ItemDefinition>();
            if (basic == null) return lost;
            var square = new List<Cell>();
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++) square.Add(new Cell(x, z));
            var off = new List<ItemDefinition>();
            var habitats = new List<ItemDefinition>();
            foreach (var kv in parts)
            {
                // Basic planks already in the square just stay.
                if (kv.Value == basic && square.Contains(kv.Key)) continue;
                if (kv.Value.type == ItemType.Habitat) habitats.Add(kv.Value);
                else off.Add(kv.Value);
            }
            int wash = (int)Math.Round(off.Count * share);
            for (int i = 0; i < wash && off.Count > 0; i++)
            {
                int k = rng.Next(off.Count);
                lost.Add(off[k]);
                off.RemoveAt(k);
            }
            foreach (var p in off) bag?.Add(p, 1);
            parts.Clear();
            foreach (var c in square) parts[c] = basic;
            var ring = new List<Cell>();
            for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                    if (Math.Max(Math.Abs(x), Math.Abs(z)) == 2 && Math.Abs(x) + Math.Abs(z) < 4) ring.Add(new Cell(x, z));
            ring = ring.OrderByDescending(c => c.X).ThenBy(c => Math.Abs(c.Z)).ToList();
            for (int i = 0; i < habitats.Count; i++)
            {
                if (i < ring.Count) parts[ring[i]] = habitats[i];
                else bag?.Add(habitats[i], 1);
            }
            Changed?.Invoke();
            return lost;
        }

        // ---- Vehicle and bosses (Ed's ask) ----

        /// <summary>Bosses gate the level-ups. Off for bare grids in tests; the game turns it on.</summary>
        public bool BossesNeeded { get; set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Developer mode only: sets the line and level directly (raft 1-50, ship 1-100).</summary>
        public void DevSetLevel(RaftKind devLine, int level)
        {
            if (devLine != line) vessel.Clear();
            SetLevel(devLine, level);
            Kills = 0;
            Changed?.Invoke();
        }
#endif
        readonly HashSet<Rarity> bosses = new HashSet<Rarity>();
        /// <summary>Raft levels whose boss is beaten (by the level's rarity).</summary>
        public IEnumerable<Rarity> BossesBeaten => bosses;
        public bool BossBeaten(Rarity level) => bosses.Contains(level);
        public void MarkBoss(Rarity level)
        {
            if (bosses.Add(level)) Changed?.Invoke();
        }

        readonly HashSet<int> vessel = new HashSet<int>();
        /// <summary>The vehicle parts on the raft (slots 1-5) for the picked type.</summary>
        public IEnumerable<int> Vessel => vessel;
        public bool HasVessel(int slot) => vessel.Contains(slot);
        public int VesselCount => vessel.Count;
        public bool VesselComplete => vessel.Count >= VesselRules.Parts;

        /// <summary>Why this vehicle part can't go on yet, or null when it can.</summary>
        public string VesselBlocker(ItemDefinition part, Inventory bag)
        {
            if (part == null || part.type != ItemType.Vessel || part.vesselSlot < 1) return "That isn't a vehicle part.";
            if (Chosen == RaftKind.Plain) return "Pick the raft's type first.";
            if (part.raftKind != Chosen) return $"That part is for a {VesselRules.Vehicle(part.raftKind)}.";
            if (vessel.Contains(part.vesselSlot)) return "It's already on.";
            int need = EvolutionRules.PartOpensAt(part.vesselSlot);
            if (Level < need) return $"Opens at ship Lv {need}.";
            if (bag != null && bag.Count(part) <= 0) return "Make it at the Workbench first.";
            if (!HammerReady) return $"Needs a {Tier} hammer or better.";
            return null;
        }

        public bool InstallVessel(ItemDefinition part, Inventory bag)
        {
            if (VesselBlocker(part, bag) != null) return false;
            bag?.Remove(part, 1);
            vessel.Add(part.vesselSlot);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Why the vehicle can't leave the sea yet, or null when it can.</summary>
        public string LeaveBlocker()
        {
            if (Chosen == RaftKind.Plain) return "Pick the raft's type first.";
            var missing = new List<string>();
            if (Tier < Rarity.Mythic) missing.Add("reach raft Lv 6");
            if (!VesselComplete) missing.Add($"put on {VesselRules.Parts - vessel.Count} more vehicle part{(VesselRules.Parts - vessel.Count == 1 ? "" : "s")}");
            if (!BossBeaten(Rarity.Mythic)) missing.Add("beat the Lv 6 boss");
            return missing.Count == 0 ? null : "Needs " + string.Join(", ", missing) + ".";
        }

        public bool CanLeave => LeaveBlocker() == null;

        /// <summary>
        /// A new sea (Ed's ask): the raft goes back to the starter square at Lv 1 with no type, no vehicle
        /// and no bosses beaten. Every part that comes off goes to the bag; habitats stay on.
        /// </summary>
        public void NewSea(ItemDefinition basic, Inventory bag)
        {
            Strip(basic, bag, new Random(0), 0f);
            SetLevel(RaftKind.Plain, 1);
            Kills = 0;
            paths.Clear();
            vessel.Clear();
            bosses.Clear();
            Changed?.Invoke();
        }

        /// <summary>Loads the vehicle and bosses from a save.</summary>
        public void RestoreVoyage(IEnumerable<int> vesselSlots, IEnumerable<Rarity> beaten)
        {
            vessel.Clear();
            bosses.Clear();
            if (vesselSlots != null) foreach (var v in vesselSlots) vessel.Add(v);
            if (beaten != null) foreach (var b in beaten) bosses.Add(b);
            Changed?.Invoke();
        }

        readonly List<RaftKind> paths = new List<RaftKind>();
        /// <summary>The type picked at each level-up: [0] for Lv 2, [1] for Lv 3 … (U-31).</summary>
        public IReadOnlyList<RaftKind> Paths => paths;

        /// <summary>Older saves only (the per-level type picks before ship lines); kept so they still load.</summary>
        public void RestorePaths(IEnumerable<RaftKind> saved)
        {
            paths.Clear();
            if (saved != null) paths.AddRange(saved);
        }

        /// <summary>
        /// Raft parts a monster could eat (Phase 8): plain raft parts (not modules or habitats) on the
        /// edge, whose loss doesn't split the raft. None while the raft has <see cref="AbsorbRules.MinParts"/> parts or fewer.
        /// </summary>
        public List<Cell> EdibleParts()
        {
            if (parts.Count <= AbsorbRules.MinParts) return new List<Cell>();
            return parts.Where(kv => kv.Value.type == ItemType.Raft && kv.Key.Neighbours().Any(n => !parts.ContainsKey(n)))
                .Select(kv => kv.Key)
                .Where(StaysConnectedWithout)
                .OrderBy(c => c.X).ThenBy(c => c.Z)
                .ToList();
        }

        /// <summary>A monster eats one edge part (no hammer needed). Null if nothing can be eaten.</summary>
        public ItemDefinition Absorb(Random rng, out Cell eaten)
        {
            eaten = default;
            var options = EdibleParts();
            if (options.Count == 0) return null;
            eaten = options[rng.Next(options.Count)];
            var part = parts[eaten];
            parts.Remove(eaten);
            Changed?.Invoke();
            return part;
        }

        /// <summary>
        /// A fire left burning takes this part (Idea Round 4, U-15). Only an edge part that can go without
        /// splitting the raft; returns the part lost, or null if it can't burn away now.
        /// </summary>
        public ItemDefinition Burn(Cell c)
        {
            if (!EdibleParts().Contains(c)) return null;
            var part = parts[c];
            parts.Remove(c);
            Changed?.Invoke();
            return part;
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
