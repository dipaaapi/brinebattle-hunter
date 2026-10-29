using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Looks only (owner's ask): katig (outrigger floats on bamboo arms) along both long sides and a
    /// small mast with a sail, so the tiles read as a raft. Resized whenever the raft grows; no gameplay.
    /// </summary>
    public class RaftDecor : MonoBehaviour
    {
        public Raft raft;

        readonly List<GameObject> parts = new List<GameObject>();
        Transform sail;
        DriftSpawner spawner;
        static readonly Color Bamboo = new Color(0.78f, 0.68f, 0.42f);
        static readonly Color Cloth = new Color(0.93f, 0.9f, 0.82f);

        void Start()
        {
            raft.Grid.Changed += Rebuild;
            Rebuild();
        }

        void OnDestroy()
        {
            if (raft != null) raft.Grid.Changed -= Rebuild;
        }

        void Rebuild()
        {
            foreach (var p in parts) Destroy(p);
            parts.Clear();
            var cells = raft.Grid.Parts.Keys.ToList();
            if (cells.Count == 0) return;
            float s = Raft.CellSize;
            int minX = cells.Min(c => c.X), maxX = cells.Max(c => c.X), minZ = cells.Min(c => c.Z), maxZ = cells.Max(c => c.Z);
            float x0 = (minX - 0.3f) * s, x1 = (maxX + 0.3f) * s, length = x1 - x0, midX = (x0 + x1) / 2f;

            // Katig: a float on each side, held by bamboo arms across the raft.
            foreach (int side in new[] { -1, 1 })
            {
                float z = (side < 0 ? minZ - 1.4f : maxZ + 1.4f) * s;
                var log = Piece(PrimitiveType.Capsule, new Vector3(midX, 0.12f, z), new Vector3(0.28f, length / 2f, 0.28f), Bamboo);
                log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            float armZ = (maxZ - minZ + 2.8f) * s, armMid = (minZ + maxZ) / 2f * s;
            foreach (float x in new[] { x0 + length * 0.2f, x1 - length * 0.2f })
                Piece(PrimitiveType.Cylinder, new Vector3(x, 0.38f, armMid), new Vector3(0.09f, armZ / 2f, 0.09f), Bamboo)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Mast and sail at the downstream (right) edge of the middle row, out of the crew's way.
            int row = cells.Select(c => c.Z).OrderBy(z => Mathf.Abs(z)).First();
            int mastX = cells.Where(c => c.Z == row).Max(c => c.X);
            var foot = new Vector3(mastX * s, 0.3f, row * s);
            Piece(PrimitiveType.Cylinder, foot + Vector3.up * 1.3f, new Vector3(0.08f, 1.3f, 0.08f), Bamboo);
            sail = Piece(PrimitiveType.Cube, foot + Vector3.up * 1.55f, new Vector3(1.4f, 1.6f, 0.03f), Cloth).transform;
        }

        GameObject Piece(PrimitiveType type, Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Raft decor";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(raft.transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            Tint.Apply(go.GetComponent<Renderer>(), color);
            parts.Add(go);
            return go;
        }

        void Update()
        {
            // The sail turns to catch the current's wind and flutters a little.
            if (sail == null) return;
            if (spawner == null) spawner = FindFirstObjectByType<DriftSpawner>();
            var wind = spawner != null && spawner.currentDirection.sqrMagnitude > 0.01f ? spawner.currentDirection : Vector3.right;
            float flutter = Mathf.Sin(Time.time * 3f) * 4f;
            var face = Quaternion.LookRotation(Vector3.ProjectOnPlane(wind, Vector3.up).normalized);
            sail.rotation = raft.transform.rotation * face * Quaternion.Euler(0f, flutter, 0f);
        }
    }
}
