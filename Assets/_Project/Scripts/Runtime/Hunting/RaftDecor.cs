using System.Collections.Generic;
using System.Linq;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Looks only (owner's ask), rebuilt whenever the raft changes. The raft's type sets its look (Ed's ask):
    /// - Plain: katig (outrigger floats on bamboo arms) and a small mast with a sail;
    /// - Fortress (STR, tank): a sea train: a locomotive with a smoking stack up front, rails and wheels along the sides;
    /// - Sky (INT): a winged plane: wings out to both sides, a propeller up front, a tail at the back;
    /// - Beast (DEX): a pirate ship: hull walls, a pointed bow with a bowsprit, two masts with square sails, a skull flag.
    /// The front is downstream (+X). Placeholder primitives until the art pass; no gameplay.
    /// </summary>
    public class RaftDecor : MonoBehaviour
    {
        public Raft raft;

        readonly List<GameObject> parts = new List<GameObject>();
        readonly List<Transform> sails = new List<Transform>();
        readonly List<Transform> wheels = new List<Transform>();
        Transform propeller, stack;
        float smokeTimer;
        DriftSpawner spawner;
        RaftKind look = (RaftKind)(-1);

        static readonly Color Bamboo = new Color(0.78f, 0.68f, 0.42f);
        static readonly Color Cloth = new Color(0.93f, 0.9f, 0.82f);
        static readonly Color Iron = new Color(0.22f, 0.24f, 0.27f), Brass = new Color(0.85f, 0.65f, 0.25f), Red = new Color(0.7f, 0.15f, 0.12f);
        static readonly Color Wing = new Color(0.9f, 0.93f, 0.97f), Sky = new Color(0.35f, 0.6f, 0.95f);
        static readonly Color Hull = new Color(0.42f, 0.27f, 0.15f), DarkWood = new Color(0.28f, 0.18f, 0.1f), Black = new Color(0.08f, 0.08f, 0.09f);

        /// <summary>The look follows the raft's type: the one picked in the raft tree, or before a pick, the parts'.</summary>
        RaftKind Look => raft.Grid.Type.Main;

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
            sails.Clear();
            wheels.Clear();
            propeller = stack = null;
            var cells = raft.Grid.Parts.Keys.ToList();
            if (cells.Count == 0) return;
            float s = Raft.CellSize;
            var b = new Bounds2
            {
                MinX = cells.Min(c => c.X), MaxX = cells.Max(c => c.X), MinZ = cells.Min(c => c.Z), MaxZ = cells.Max(c => c.Z),
            };
            b.X0 = (b.MinX - 0.5f) * s; b.X1 = (b.MaxX + 0.5f) * s; b.Z0 = (b.MinZ - 0.5f) * s; b.Z1 = (b.MaxZ + 0.5f) * s;
            int row = cells.Select(c => c.Z).OrderBy(z => Mathf.Abs(z)).First();
            b.Row = row * s;
            look = Look;
            switch (look)
            {
                case RaftKind.Fortress: SeaTrain(b); break;
                case RaftKind.Sky: Plane(b); break;
                case RaftKind.Beast: PirateShip(b); break;
                default: Plain(b, cells); break;
            }
        }

        struct Bounds2
        {
            public int MinX, MaxX, MinZ, MaxZ;
            public float X0, X1, Z0, Z1, Row;
            public float Length => X1 - X0;
            public float Width => Z1 - Z0;
            public float MidX => (X0 + X1) / 2f;
            public float MidZ => (Z0 + Z1) / 2f;
        }

        void Plain(Bounds2 b, List<Cell> cells)
        {
            float s = Raft.CellSize;
            float x0 = b.X0 + 0.2f * s, x1 = b.X1 - 0.2f * s, length = x1 - x0, midX = (x0 + x1) / 2f;
            // Katig: a float on each side, held by bamboo arms across the raft.
            foreach (int side in new[] { -1, 1 })
            {
                float z = (side < 0 ? b.MinZ - 1.4f : b.MaxZ + 1.4f) * s;
                var log = Piece(PrimitiveType.Capsule, new Vector3(midX, raft.WaterLine + 0.12f, z), new Vector3(0.28f, length / 2f, 0.28f), Bamboo);
                log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            float armZ = (b.MaxZ - b.MinZ + 2.8f) * s, armMid = (b.MinZ + b.MaxZ) / 2f * s;
            foreach (float x in new[] { x0 + length * 0.2f, x1 - length * 0.2f })
                Piece(PrimitiveType.Cylinder, new Vector3(x, 0.38f, armMid), new Vector3(0.09f, armZ / 2f, 0.09f), Bamboo)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Mast and sail at the downstream (right) edge of the middle row, out of the crew's way.
            int row = cells.Select(c => c.Z).OrderBy(z => Mathf.Abs(z)).First();
            int mastX = cells.Where(c => c.Z == row).Max(c => c.X);
            var foot = new Vector3(mastX * s, 0.3f, row * s);
            Piece(PrimitiveType.Cylinder, foot + Vector3.up * 1.3f, new Vector3(0.08f, 1.3f, 0.08f), Bamboo);
            sails.Add(Piece(PrimitiveType.Cube, foot + Vector3.up * 1.55f, new Vector3(1.4f, 1.6f, 0.03f), Cloth).transform);
        }

        /// <summary>Fortress, STR: a sea train. Locomotive in front, rails and wheels down both sides.</summary>
        void SeaTrain(Bounds2 b)
        {
            float y = 0.3f;
            // Locomotive just past the front edge, on the middle row.
            float front = b.X1 + 0.2f;
            var boiler = Piece(PrimitiveType.Cylinder, new Vector3(front + 0.6f, y + 0.45f, b.Row), new Vector3(0.9f, 0.9f, 0.9f), Iron);
            boiler.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Piece(PrimitiveType.Cylinder, new Vector3(front + 0.6f, y + 0.45f, b.Row), new Vector3(0.95f, 0.08f, 0.95f), Brass)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // brass band
            Piece(PrimitiveType.Cube, new Vector3(front - 0.35f, y + 0.6f, b.Row), new Vector3(0.7f, 1.2f, 1.1f), Red);   // cab
            Piece(PrimitiveType.Cube, new Vector3(front - 0.35f, y + 1.25f, b.Row), new Vector3(0.85f, 0.1f, 1.25f), Iron);   // cab roof
            stack = Piece(PrimitiveType.Cylinder, new Vector3(front + 1.0f, y + 1.15f, b.Row), new Vector3(0.28f, 0.35f, 0.28f), Iron).transform;
            Piece(PrimitiveType.Sphere, new Vector3(front + 1.3f, y + 0.55f, b.Row), Vector3.one * 0.25f, new Color(1f, 0.95f, 0.6f));   // headlamp
            var catcher = Piece(PrimitiveType.Cube, new Vector3(front + 1.25f, y + 0.12f, b.Row), new Vector3(0.6f, 0.25f, 0.9f), Red);
            catcher.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);   // cowcatcher wedge

            // Rails along both long sides at the waterline, with wheels that turn.
            foreach (int side in new[] { -1, 1 })
            {
                float z = side < 0 ? b.Z0 - 0.25f : b.Z1 + 0.25f;
                Piece(PrimitiveType.Cube, new Vector3((b.X0 + front + 1.3f) / 2f, raft.WaterLine + 0.15f, z), new Vector3(front + 1.3f - b.X0, 0.08f, 0.12f), Iron);
                for (float x = b.X0 + 0.5f; x < front + 1f; x += 1.1f)
                {
                    var w = Piece(PrimitiveType.Cylinder, new Vector3(x, raft.WaterLine + 0.3f, z), new Vector3(0.55f, 0.06f, 0.55f), Iron).transform;
                    w.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    wheels.Add(w);
                }
            }
            // Couplings between "carriages": iron bars across the raft every two cells.
            for (float x = b.X0 + 2f * Raft.CellSize; x < b.X1 - 0.5f; x += 2f * Raft.CellSize)
                Piece(PrimitiveType.Cube, new Vector3(x, raft.WaterLine + 0.2f, b.MidZ), new Vector3(0.1f, 0.1f, b.Width + 0.5f), Iron);
        }

        /// <summary>Sky, INT: a winged plane. Wings out to both sides, a propeller in front, a tail behind.</summary>
        void Plane(Bounds2 b)
        {
            float y = 0.32f;
            float span = Mathf.Max(2.5f, b.Width * 0.9f), chord = Mathf.Min(2.2f, b.Length * 0.45f);
            foreach (int side in new[] { -1, 1 })
            {
                float edge = side < 0 ? b.Z0 : b.Z1;
                var wing = Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f, y + 0.1f, edge + side * span / 2f), new Vector3(chord, 0.08f, span), Wing);
                wing.transform.localRotation = Quaternion.Euler(side * -6f, 0f, 0f);   // a little dihedral
                Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f - chord / 2f + 0.15f, y + 0.12f + side * 0f, edge + side * span / 2f), new Vector3(0.3f, 0.1f, span * 0.9f), Sky)
                    .transform.localRotation = Quaternion.Euler(side * -6f, 0f, 0f);   // flap stripe
                Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f, y + 0.2f, edge + side * (span - 0.15f)), new Vector3(chord * 0.6f, 0.25f, 0.08f), Sky);   // wingtip
            }
            // Nose and propeller just past the front.
            float front = b.X1 + 0.15f;
            var nose = Piece(PrimitiveType.Capsule, new Vector3(front + 0.35f, y + 0.35f, b.Row), new Vector3(0.6f, 0.45f, 0.6f), Sky);
            nose.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            propeller = new GameObject("Propeller").transform;
            propeller.SetParent(raft.transform, false);
            propeller.localPosition = new Vector3(front + 0.85f, y + 0.35f, b.Row);
            parts.Add(propeller.gameObject);
            var hub = Piece(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.25f, Iron);
            hub.transform.SetParent(propeller, false);
            foreach (float a in new[] { 0f, 90f })
            {
                var blade = Piece(PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 1.6f, 0.16f), new Color(0.3f, 0.25f, 0.2f));
                blade.transform.SetParent(propeller, false);
                blade.transform.localPosition = Vector3.zero;
                blade.transform.localRotation = Quaternion.Euler(a, 0f, 0f);
            }
            // Tail behind the back edge: a fin and two small stabilizers on a boom.
            float back = b.X0 - 0.2f;
            Piece(PrimitiveType.Cube, new Vector3(back - 0.5f, y + 0.25f, b.Row), new Vector3(1.2f, 0.18f, 0.25f), Wing);   // boom
            Piece(PrimitiveType.Cube, new Vector3(back - 0.9f, y + 0.85f, b.Row), new Vector3(0.6f, 1.1f, 0.08f), Sky);       // fin
            Piece(PrimitiveType.Cube, new Vector3(back - 0.9f, y + 0.3f, b.Row), new Vector3(0.5f, 0.06f, 1.6f), Wing);      // stabilizers
        }

        /// <summary>Beast, DEX: a pirate ship. Hull walls, a pointed bow, two masts with square sails, a skull flag.</summary>
        void PirateShip(Bounds2 b)
        {
            float wl = raft.WaterLine;
            // Hull walls along both sides, rising a little above the deck.
            foreach (int side in new[] { -1, 1 })
            {
                float z = side < 0 ? b.Z0 - 0.12f : b.Z1 + 0.12f;
                Piece(PrimitiveType.Cube, new Vector3(b.MidX, wl + 0.35f, z), new Vector3(b.Length + 0.3f, 0.75f, 0.18f), Hull);
                Piece(PrimitiveType.Cube, new Vector3(b.MidX, wl + 0.75f, z), new Vector3(b.Length + 0.3f, 0.08f, 0.24f), DarkWood);   // rail
                // Cannons poking out.
                for (float x = b.X0 + 1f; x < b.X1 - 0.5f; x += 1.6f)
                    Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.45f, z + side * 0.2f), new Vector3(0.16f, 0.22f, 0.16f), Black)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // Pointed bow past the front, and a bowsprit.
            float front = b.X1 + 0.1f, half = b.Width / 2f + 0.12f;
            foreach (int side in new[] { -1, 1 })
            {
                float len = Mathf.Sqrt(half * half + 1.2f * 1.2f);
                var plank = Piece(PrimitiveType.Cube, new Vector3(front + 0.6f, wl + 0.35f, b.MidZ + side * half / 2f), new Vector3(len, 0.75f, 0.18f), Hull);
                plank.transform.localRotation = Quaternion.Euler(0f, side * Mathf.Atan2(half, 1.2f) * Mathf.Rad2Deg, 0f);
            }
            var sprit = Piece(PrimitiveType.Cylinder, new Vector3(front + 1.5f, wl + 0.9f, b.MidZ), new Vector3(0.09f, 0.8f, 0.09f), DarkWood);
            sprit.transform.localRotation = Quaternion.Euler(0f, 0f, -65f);
            // Stern castle behind the back edge.
            Piece(PrimitiveType.Cube, new Vector3(b.X0 - 0.45f, wl + 0.55f, b.MidZ), new Vector3(0.8f, 1.1f, b.Width + 0.5f), Hull);
            Piece(PrimitiveType.Cube, new Vector3(b.X0 - 0.45f, wl + 1.15f, b.MidZ), new Vector3(0.9f, 0.1f, b.Width + 0.6f), DarkWood);
            // Two masts with square sails, on the middle row, and a skull flag on the tall one.
            float[] masts = { b.X1 - b.Length * 0.25f, b.X0 + b.Length * 0.35f };
            for (int i = 0; i < masts.Length; i++)
            {
                float h = i == 0 ? 2.6f : 2.2f;
                var foot = new Vector3(masts[i], 0.3f, b.Row);
                Piece(PrimitiveType.Cylinder, foot + Vector3.up * h / 2f, new Vector3(0.12f, h / 2f, 0.12f), DarkWood);
                Piece(PrimitiveType.Cylinder, foot + Vector3.up * (h * 0.8f), new Vector3(0.06f, 0.9f, 0.06f), DarkWood)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // yard
                sails.Add(Piece(PrimitiveType.Cube, foot + Vector3.up * (h * 0.55f), new Vector3(0.04f, h * 0.5f, 1.7f), Cloth).transform);
                Piece(PrimitiveType.Cylinder, foot + Vector3.up * (h - 0.1f), new Vector3(0.35f, 0.08f, 0.35f), DarkWood);   // crow's nest
                if (i == 0)
                {
                    var flagAt = foot + Vector3.up * (h + 0.3f) + new Vector3(-0.3f, 0f, 0f);
                    Piece(PrimitiveType.Cube, flagAt, new Vector3(0.6f, 0.4f, 0.03f), Black);
                    Piece(PrimitiveType.Sphere, flagAt + new Vector3(0.02f, 0.04f, 0.03f), new Vector3(0.16f, 0.16f, 0.02f), Color.white);   // skull
                    Piece(PrimitiveType.Cube, flagAt + new Vector3(0.02f, -0.1f, 0.03f), new Vector3(0.26f, 0.04f, 0.02f), Color.white)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 35f);   // crossed bones
                    Piece(PrimitiveType.Cube, flagAt + new Vector3(0.02f, -0.1f, 0.03f), new Vector3(0.26f, 0.04f, 0.02f), Color.white)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
                }
            }
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
            // A raft tree pick changes the look even when no part moved.
            if (Look != look) Rebuild();
            if (spawner == null) spawner = FindFirstObjectByType<DriftSpawner>();
            var wind = spawner != null && spawner.currentDirection.sqrMagnitude > 0.01f ? spawner.currentDirection : Vector3.right;
            float flutter = Mathf.Sin(Time.time * 3f) * 4f;
            var face = Quaternion.LookRotation(Vector3.ProjectOnPlane(wind, Vector3.up).normalized);
            // Plain: the sail turns to catch the current's wind and flutters. Pirate sails stay square to the ship.
            foreach (var sail in sails)
                if (sail != null)
                    sail.rotation = look == RaftKind.Beast
                        ? raft.transform.rotation * Quaternion.Euler(0f, flutter * 0.5f, 0f)
                        : raft.transform.rotation * face * Quaternion.Euler(0f, flutter, 0f);
            if (WorldPause.Active) return;
            if (propeller != null) propeller.localRotation *= Quaternion.Euler(900f * Time.deltaTime, 0f, 0f);
            foreach (var w in wheels) if (w != null) w.localRotation *= Quaternion.Euler(0f, 180f * Time.deltaTime, 0f);
            if (stack != null) Smoke();
        }

        /// <summary>The locomotive puffs smoke now and then.</summary>
        void Smoke()
        {
            smokeTimer -= Time.deltaTime;
            if (smokeTimer > 0f) return;
            smokeTimer = 0.45f;
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Smoke";
            Destroy(puff.GetComponent<Collider>());
            puff.transform.position = stack.position + Vector3.up * 0.4f;
            Tint.Apply(puff.GetComponent<Renderer>(), new Color(0.75f, 0.75f, 0.78f));
            StartCoroutine(Rise(puff.transform));
        }

        static System.Collections.IEnumerator Rise(Transform puff)
        {
            for (float t = 0f; t < 1.6f && puff != null; t += Time.deltaTime)
            {
                puff.position += new Vector3(-0.6f, 1.1f, 0f) * Time.deltaTime;
                puff.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.7f, t / 1.6f) * (1f - Mathf.Clamp01((t - 1.2f) / 0.4f));
                yield return null;
            }
            if (puff != null) Destroy(puff.gameObject);
        }
    }
}
