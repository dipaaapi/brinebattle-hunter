using System.Collections.Generic;
using BrineBattle.Items;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The player's raft: a <see cref="RaftGrid"/> of parts drawn as placeholder tiles (GDD §6).
    /// Size, battle HP and tier all come from the grid.
    /// </summary>
    public class Raft : MonoBehaviour
    {
        public const float CellSize = 1.05f;

        [Tooltip("Part used for the starting 3x3 raft.")]
        public ItemDefinition starterPart;
        [Tooltip("How far the hook reaches beyond the raft's edge.")]
        public float hookRange = 12f;

        public RaftGrid Grid { get; } = new RaftGrid { ModulesToGrow = RaftRules.ModuleShareToGrow };
        public int size => Grid.Size;
        public int MaxHp => Grid.MaxHp;
        public Rarity Tier => Grid.Tier;
        /// <summary>Distance from the centre to just past the outer parts.</summary>
        public float EdgeDistance => (Grid.Radius + 1.2f) * CellSize;
        public float HookReach => hookRange + Grid.Radius * CellSize + Grid.Type.Bonus.Reach - World.WorldRules.ReachPenalty(WorldClock.Weather);

        readonly Dictionary<Cell, GameObject> tiles = new Dictionary<Cell, GameObject>();

        void Awake()
        {
            // The scene builder may leave editor-time tiles; the grid owns the visuals at runtime.
            foreach (Transform child in transform)
                if (child.name.StartsWith("Part")) Destroy(child.gameObject);
            Grid.Changed += Rebuild;
            if (starterPart != null) Grid.FillSquare(starterPart, 1);
            // Katig and sail, looks only (owner's ask); added here so older Hook scenes get them too.
            if (!TryGetComponent<RaftDecor>(out _)) gameObject.AddComponent<RaftDecor>().raft = this;
        }

        void OnDestroy() => Grid.Changed -= Rebuild;

        /// <summary>How much of the water's lean the raft follows (owner's ask: it should rock with the waves).</summary>
        const float Rock = 1f;
        /// <summary>Deck top in raft space; everything stands on it.</summary>
        const float DeckTop = 0.3f;
        float restY = float.NaN;

        /// <summary>Where the water meets the raft, in raft space: thicker planks at higher levels lift the deck.</summary>
        public float WaterLine => DeckTop - RaftRules.Freeboard(Tier);

        /// <summary>Height above the water while the plane takes off (Ed's ask: leaving the sea).</summary>
        public float Lift { get; set; }

        /// <summary>Rides the swell: rises and falls with the water under its centre and leans with the slope (owner's ask).</summary>
        void LateUpdate()
        {
            if (float.IsNaN(restY)) restY = transform.position.y;
            var p = transform.position;
            // A low-level raft rides gentler water; each level up gives thicker planks and the open sea.
            Waves.CalmCenter = p;
            Waves.CalmRadius = (Grid.Radius + 2f) * CellSize;
            Waves.CalmShare = RaftRules.SeaShare(Tier);
            float span = Mathf.Max(1.5f, Grid.Radius * CellSize);
            p.y = restY + Waves.Height(p.x, p.z) - WaterLine + Lift;   // the planks' water line sits on the water
            transform.position = p;
            var up = Vector3.Slerp(Vector3.up, Waves.Normal(p.x, p.z, span), Rock);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
        }

        /// <summary>Where a cell's tile sits now, following the raft as it rocks.</summary>
        public Vector3 CellToWorld(Cell c) => transform.TransformPoint(Local(c));

        static Vector3 Local(Cell c) => new Vector3(c.X * CellSize, 0.15f, c.Z * CellSize);

        /// <summary>The cell under a point in the raft's local space.</summary>
        public static Cell LocalToCell(Vector3 local) => new Cell(Mathf.RoundToInt(local.x / CellSize), Mathf.RoundToInt(local.z / CellSize));

        /// <summary>True when a local point is over a plank the hunter and pets may stand on (not a module or habitat).</summary>
        public bool WalkableAt(Vector3 local) => Grid.Walkable(LocalToCell(local));

        public Cell WorldToCell(Vector3 p)
        {
            var local = transform.InverseTransformPoint(p);
            return new Cell(Mathf.RoundToInt(local.x / CellSize), Mathf.RoundToInt(local.z / CellSize));
        }

        void Rebuild()
        {
            foreach (var kv in new List<KeyValuePair<Cell, GameObject>>(tiles))
                if (!Grid.Has(kv.Key)) { Destroy(kv.Value); tiles.Remove(kv.Key); }

            foreach (var kv in Grid.Parts)
            {
                if (!tiles.TryGetValue(kv.Key, out var tile))
                {
                    tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Part {kv.Key.X},{kv.Key.Z}";
                    tile.transform.SetParent(transform, false);
                    Destroy(tile.GetComponent<Collider>());
                    tiles[kv.Key] = tile;
                }
                var type = kv.Value.type;
                bool module = type == ItemType.Module, habitat = type == ItemType.Habitat;
                // Habitats: a sky perch stands tall, a tide pool sits low and watery.
                bool perch = habitat && kv.Value.element == Elements.Element.Wind;
                // Every piece reaches from its top down past the water line, so higher raft levels
                // (thicker planks) never leave a gap under a module or habitat (owner's ask).
                float width = module ? 0.8f : perch ? 0.35f : habitat ? 0.95f : 1f;
                float top = module ? 0.7f : perch ? 1.3f : habitat ? 0.455f : DeckTop;
                float bottom = WaterLine - 0.12f;
                tile.transform.localScale = new Vector3(width, top - bottom, width);
                tile.transform.localPosition = new Vector3(kv.Key.X * CellSize, (top + bottom) / 2f, kv.Key.Z * CellSize);
                Tint.Apply(tile.GetComponent<Renderer>(), module ? new Color(0.25f, 0.55f, 0.45f)
                    : perch ? new Color(0.8f, 0.65f, 0.4f) : habitat ? new Color(0.3f, 0.75f, 0.9f) : PartColor(kv.Value));
            }
        }

        /// <summary>A raft part's colour: its rarity, mixed with its raft kind (Phase 8) so the type shows.</summary>
        public static Color PartColor(ItemDefinition part)
        {
            var c = PartColor(part.rarity);
            switch (part.raftKind)
            {
                case RaftKind.Fortress: return Color.Lerp(c, new Color(0.45f, 0.4f, 0.38f), 0.55f);
                case RaftKind.Sky: return Color.Lerp(c, new Color(0.75f, 0.9f, 1f), 0.55f);
                case RaftKind.Beast: return Color.Lerp(c, new Color(0.75f, 0.35f, 0.3f), 0.55f);
                default: return c;
            }
        }

        public static Color PartColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.7f, 0.85f, 0.65f);
                case Rarity.Rare: return new Color(0.6f, 0.72f, 0.95f);
                case Rarity.Epic: return new Color(0.78f, 0.62f, 0.95f);
                case Rarity.Legendary: return new Color(1f, 0.78f, 0.45f);
                case Rarity.Mythic: return new Color(1f, 0.55f, 0.65f);
                default: return new Color(0.85f, 0.8f, 0.68f);
            }
        }
    }
}
