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
        public float HookReach => hookRange + Grid.Radius * CellSize;

        readonly Dictionary<Cell, GameObject> tiles = new Dictionary<Cell, GameObject>();

        void Awake()
        {
            // The scene builder may leave editor-time tiles; the grid owns the visuals at runtime.
            foreach (Transform child in transform)
                if (child.name.StartsWith("Part")) Destroy(child.gameObject);
            Grid.Changed += Rebuild;
            if (starterPart != null) Grid.FillSquare(starterPart, 1);
        }

        void OnDestroy() => Grid.Changed -= Rebuild;

        public Vector3 CellToWorld(Cell c) =>
            transform.position + new Vector3(c.X * CellSize, 0.15f, c.Z * CellSize);

        public Cell WorldToCell(Vector3 p)
        {
            var local = p - transform.position;
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
                    tile.transform.localScale = new Vector3(1f, 0.3f, 1f);
                    Destroy(tile.GetComponent<Collider>());
                    tiles[kv.Key] = tile;
                }
                bool module = kv.Value.type == ItemType.Module;
                tile.transform.localScale = new Vector3(module ? 0.8f : 1f, module ? 0.7f : 0.3f, module ? 0.8f : 1f);
                tile.transform.position = CellToWorld(kv.Key) + (module ? Vector3.up * 0.2f : Vector3.zero);
                Tint.Apply(tile.GetComponent<Renderer>(), module ? new Color(0.25f, 0.55f, 0.45f) : PartColor(kv.Value.rarity));
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
