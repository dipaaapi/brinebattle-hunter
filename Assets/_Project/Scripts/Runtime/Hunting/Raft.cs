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
            // Katig and sail, looks only (owner's ask); added here so older Hook scenes get them too.
            if (!TryGetComponent<RaftDecor>(out _)) gameObject.AddComponent<RaftDecor>().raft = this;
        }

        void OnDestroy() => Grid.Changed -= Rebuild;

        [Tooltip("How much of the water's lean the raft follows (0 = stays level).")]
        public float rock = 0.6f;
        float restY = float.NaN;

        /// <summary>Rides the swell: rises and falls with the water under its centre and leans with the slope (owner's ask).</summary>
        void LateUpdate()
        {
            if (float.IsNaN(restY)) restY = transform.position.y;
            var p = transform.position;
            float span = Mathf.Max(1.5f, Grid.Radius * CellSize);
            p.y = restY + Waves.Height(p.x, p.z);   // always on the water, never under it
            transform.position = p;
            var up = Vector3.Slerp(Vector3.up, Waves.Normal(p.x, p.z, span), rock);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
        }

        /// <summary>Where a cell's tile sits now, following the raft as it rocks.</summary>
        public Vector3 CellToWorld(Cell c) => transform.TransformPoint(Local(c));

        static Vector3 Local(Cell c) => new Vector3(c.X * CellSize, 0.15f, c.Z * CellSize);

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
                    tile.transform.localScale = new Vector3(1f, 0.3f, 1f);
                    Destroy(tile.GetComponent<Collider>());
                    tiles[kv.Key] = tile;
                }
                var type = kv.Value.type;
                bool module = type == ItemType.Module, habitat = type == ItemType.Habitat;
                // Habitats: a sky perch stands tall, a tide pool sits low and watery.
                bool perch = habitat && kv.Value.element == Elements.Element.Wind;
                tile.transform.localScale = module ? new Vector3(0.8f, 0.7f, 0.8f) : perch ? new Vector3(0.35f, 1.3f, 0.35f) : habitat ? new Vector3(0.95f, 0.45f, 0.95f) : new Vector3(1f, 0.3f, 1f);
                tile.transform.localPosition = Local(kv.Key) + Vector3.up * (module ? 0.2f : perch ? 0.5f : habitat ? 0.08f : 0f);
                Tint.Apply(tile.GetComponent<Renderer>(), module ? new Color(0.25f, 0.55f, 0.45f)
                    : perch ? new Color(0.8f, 0.65f, 0.4f) : habitat ? new Color(0.3f, 0.75f, 0.9f) : PartColor(kv.Value.rarity));
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
