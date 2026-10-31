using System.Collections.Generic;
using System.Linq;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Looks only (owner's ask), rebuilt whenever the raft changes. Evolution (Ed's ask, 2026-10-31):
    /// - Raft Lv 1-50: katig (outrigger floats on bamboo arms) and a small mast with a sail, then a little more every
    ///   ten levels: lashings (10), railings (20), a second mast (30), a cabin (40), and at Lv 50 boat sides and a bow;
    /// - Sea-level ship (STR): a galleon: hull walls, a pointed bow with a bowsprit, masts with square sails, a flag;
    /// - Aerial ship (INT): a skyship: wings out to both sides, a propeller up front, a tail at the back;
    /// - Undersea ship (DEX): a submarine: a dark hull with a conning tower, ballast tanks, a screw, a periscope, a sonar dome.
    /// Each ship piece shows once its ship part is on (see <see cref="VesselRules"/>); every ship flies a pennant that
    /// grows with its level. The front is downstream (+X). Placeholder primitives until the art pass; no gameplay.
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
        /// <summary>The raft-to-boat stages use light wood (Ed: the dark wood looked black at night).</summary>
        static readonly Color Wood = new Color(0.72f, 0.55f, 0.36f), Trim = new Color(0.55f, 0.4f, 0.25f);

        /// <summary>The look follows the ship line once it is picked; before that it's the raft.</summary>
        RaftKind Look => raft.Grid.Chosen;
        int built = -1, stage = -1;
        /// <summary>Rebuilds every level (and on the ship pick), so the raft changes a little as it grows.</summary>
        int Stage => (raft.Grid.IsShip ? 1000 : 0) + raft.Grid.Level;

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
            // The vehicle builds up part by part (Ed's ask). Once the type is picked its frame shows right away
            // (Ed: "hindi pa rin siya nagmumukhang pirate ship"); each vehicle part then adds its full piece.
            stage = Stage;
            switch (look)
            {
                case RaftKind.Sea: PirateShip(b); break;
                case RaftKind.Aerial: Plane(b); break;
                case RaftKind.Undersea: Submarine(b); break;
                default: Plain(b, cells); break;
            }
            if (raft.Grid.IsShip) Pennant(b);
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
            // Ed: at Lv 39 it still looked like a flat raft. Each 10 levels is now a clearly new stage, and every
            // level adds a little (rope, crates, a bigger sail), until a finished simple boat at Lv 50.
            int lv = raft.Grid.Level;
            float s = Raft.CellSize, deck = 0.34f, wl = raft.WaterLine;
            float grow = Mathf.Clamp01((lv - 1) / 49f);
            float x0 = b.X0, x1 = b.X1, length = b.Length, width = b.Width, midX = b.MidX, midZ = b.MidZ;

            // Main mast and sail at the front of the middle row; both grow with every level.
            int row = cells.Select(c => c.Z).OrderBy(z => Mathf.Abs(z)).First();
            int mastX = cells.Where(c => c.Z == row).Max(c => c.X);
            // On the outer half of the front edge cell, away from where anyone stands (cells are walked at their centre).
            var foot = new Vector3(mastX * s + s * 0.42f, 0.3f, row * s);
            float mastH = 1.5f + 0.8f * grow;
            float sailH = 1.3f + 0.9f * grow;
            Piece(PrimitiveType.Cylinder, foot + Vector3.up * mastH, new Vector3(0.08f + 0.05f * grow, mastH, 0.08f + 0.05f * grow), lv >= 30 ? Trim : Bamboo);
            // The sail's bottom stays 1.5 m above the deck, over everyone's heads.
            sails.Add(Piece(PrimitiveType.Cube, new Vector3(foot.x, deck + 1.5f + sailH / 2f, foot.z), new Vector3(1.2f + 0.8f * grow, sailH, 0.03f), Cloth).transform);

            // Every level: rope lashings across the deck (one more pair every 2 levels) and cargo along the edges.
            int lashings = Mathf.Min(8, 1 + lv / 2 / 2);
            for (int i = 0; i < lashings; i++)
            {
                float x = Mathf.Lerp(x0 + 0.15f, x1 - 0.15f, lashings == 1 ? 0f : i / (float)(lashings - 1));
                Piece(PrimitiveType.Cube, new Vector3(x, deck + 0.01f, midZ), new Vector3(0.05f, 0.03f, width), new Color(0.45f, 0.32f, 0.18f));
            }
            // Cargo lashed to the OUTSIDE of the raft's sides (never on a cell anyone walks on).
            int cargo = Mathf.Min(14, lv / 3);
            for (int i = 0; i < cargo; i++)
            {
                bool left = i % 2 == 0;
                float x = Mathf.Lerp(x0 + 0.35f, x1 - 0.6f, (i / 2) / 7f);
                float z = left ? b.Z0 - 0.32f : b.Z1 + 0.32f;
                if (i % 3 == 2) Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.18f, z), new Vector3(0.26f, 0.16f, 0.26f), new Color(0.6f, 0.42f, 0.24f));   // barrel
                else Piece(PrimitiveType.Cube, new Vector3(x, wl + 0.16f, z), Vector3.one * 0.28f, new Color(0.7f, 0.53f, 0.32f));   // crate
            }

            // Lv 10-29: outriggers (katig) on bamboo arms. From Lv 30 a real hull replaces them.
            if (lv >= 10 && lv < 30)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float z = (side < 0 ? b.MinZ - 1.4f : b.MaxZ + 1.4f) * s;
                    var log = Piece(PrimitiveType.Capsule, new Vector3(midX, wl + 0.12f, z), new Vector3(0.28f, length / 2f, 0.28f), Bamboo);
                    log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
                float armZ = (b.MaxZ - b.MinZ + 2.8f) * s;
                foreach (float x in new[] { x0 + length * 0.2f, x1 - length * 0.2f })
                    Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.08f, midZ), new Vector3(0.09f, armZ / 2f, 0.09f), Bamboo)   // under the deck planks
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // Lv 15+: a dark trim band around the deck.
            if (lv >= 15)
            {
                foreach (float z in new[] { b.Z0, b.Z1 }) Piece(PrimitiveType.Cube, new Vector3(midX, deck + 0.02f, z), new Vector3(length, 0.06f, 0.12f), Trim);
                foreach (float x in new[] { x0, x1 }) Piece(PrimitiveType.Cube, new Vector3(x, deck + 0.02f, midZ), new Vector3(0.12f, 0.06f, width), Trim);
            }

            // Ed: at Lv 33 a dark wedge covered the whole deck. Rule from here on: nothing stands over the deck
            // where the hunter and minions walk. Walls and rails sit on the edges, the hull is under the deck,
            // the stern deck and cabin hang off the back edge, all in light wood.
            float bowW = Mathf.Min(width, 3f);

            // Lv 20+: raised side walls (gunwales) on the edges and a low pointed bow just past the front edge.
            if (lv >= 20)
            {
                float wall = 0.2f + 0.25f * grow;
                foreach (float z in new[] { b.Z0 - 0.08f, b.Z1 + 0.08f })
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck + wall / 2f - 0.15f, z), new Vector3(length, wall + 0.3f, 0.12f), Wood);
                float d = bowW / 1.414f;
                var bow = Piece(PrimitiveType.Cube, new Vector3(x1 + bowW / 2f, deck - 0.12f, midZ), new Vector3(d, 0.35f, d), Wood);   // its back corner at the front edge
                bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
            // Lv 25+: rails on top of the side walls.
            if (lv >= 25)
                foreach (float z in new[] { b.Z0 - 0.08f, b.Z1 + 0.08f })
                {
                    for (float x = x0 + 0.2f; x <= x1 - 0.1f; x += s) Piece(PrimitiveType.Cube, new Vector3(x, deck + 0.4f, z), new Vector3(0.06f, 0.35f, 0.06f), Trim);
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck + 0.58f, z), new Vector3(length, 0.05f, 0.07f), Trim);
                }

            // Lv 30+: a real hull UNDER the deck: two sloped planks from the deck's sides down to a keel, and a
            // second mast on the back edge.
            if (lv >= 30)
            {
                float top = wl - 0.1f, depth = Mathf.Min(1f, 0.35f + width * 0.08f);
                float half = width / 2f, slope = Mathf.Sqrt(half * half + depth * depth);
                float angle = Mathf.Atan2(depth, half) * Mathf.Rad2Deg;
                foreach (int side in new[] { -1, 1 })
                {
                    var plank = Piece(PrimitiveType.Cube, new Vector3(midX, top - depth / 2f, midZ + side * half / 2f), new Vector3(length, 0.08f, slope), Wood);
                    plank.transform.localRotation = Quaternion.Euler(-side * angle, 0f, 0f);   // outer edge up at the deck, inner edge down at the keel
                }
                Piece(PrimitiveType.Cube, new Vector3(midX, top - depth - 0.08f, midZ), new Vector3(length * 0.9f, 0.16f, 0.12f), Trim);   // keel
                var back = new Vector3(x0 - 0.15f, 0.3f, row * s);
                float h2 = mastH * 0.9f, sail2 = 1.0f + 0.7f * grow;
                Piece(PrimitiveType.Cylinder, back + Vector3.up * h2, new Vector3(0.08f, h2, 0.08f), Trim);
                sails.Add(Piece(PrimitiveType.Cube, new Vector3(back.x, deck + 1.5f + sail2 / 2f, back.z), new Vector3(0.9f + 0.6f * grow, sail2, 0.03f), Cloth).transform);
            }

            // Lv 35+: a low stern platform hanging off the back edge (not over the deck).
            float sternX = x0 - s * 0.55f;
            if (lv >= 35)
                Piece(PrimitiveType.Cube, new Vector3(sternX, deck - 0.05f, midZ), new Vector3(s, 0.25f, Mathf.Min(width, 3.2f)), Wood);

            // Lv 40+: a small cabin on that platform (one cell deep), a flag on the main mast and lanterns on the corners.
            if (lv >= 40)
            {
                var cabin = new Vector3(sternX, deck + 0.4f, midZ);
                Piece(PrimitiveType.Cube, cabin, new Vector3(s * 0.85f, 0.7f, Mathf.Min(width - 0.4f, 1.8f)), Wood);
                Piece(PrimitiveType.Cube, cabin + Vector3.up * 0.4f, new Vector3(s * 0.95f, 0.1f, Mathf.Min(width - 0.2f, 2f)), Red);   // its roof, off the deck
                Piece(PrimitiveType.Cube, foot + Vector3.up * (mastH * 2f + 0.15f), new Vector3(0.5f, 0.3f, 0.02f), Red);   // flag
                foreach (var corner in new[] { new Vector3(x0, 0, b.Z0 - 0.08f), new Vector3(x0, 0, b.Z1 + 0.08f), new Vector3(x1, 0, b.Z0 - 0.08f), new Vector3(x1, 0, b.Z1 + 0.08f) })
                    Piece(PrimitiveType.Sphere, corner + Vector3.up * (deck + 0.75f), Vector3.one * 0.18f, new Color(1f, 0.85f, 0.4f));
            }

            // Lv 45+: a crow's nest high on the main mast and a figurehead at the bow.
            if (lv >= 45)
            {
                Piece(PrimitiveType.Cylinder, foot + Vector3.up * (mastH * 1.6f), new Vector3(0.45f, 0.06f, 0.45f), Trim);
                Piece(PrimitiveType.Sphere, new Vector3(x1 + bowW * 0.95f, deck + 0.15f, midZ), Vector3.one * 0.3f, Brass);
            }

            // Lv 50: a finished simple boat: a low closed stern, a painted stripe and a bowsprit.
            if (lv >= EvolutionRules.RaftMax)
            {
                Piece(PrimitiveType.Cube, new Vector3(sternX - s * 0.5f, deck, midZ), new Vector3(0.12f, 0.5f, Mathf.Min(width, 3.2f)), Wood);
                foreach (float z in new[] { b.Z0 - 0.15f, b.Z1 + 0.15f })
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck - 0.05f, z), new Vector3(length, 0.07f, 0.02f), Red);
                var sprit = Piece(PrimitiveType.Cylinder, new Vector3(x1 + bowW * 1.2f, deck + 0.35f, midZ), new Vector3(0.06f, 0.8f, 0.06f), Trim);
                sprit.transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
            }
        }

        /// <summary>Every ship flies a pennant in its line's colour that grows longer as it levels up (Lv 1 → 100).</summary>
        void Pennant(Bounds2 b)
        {
            int lv = raft.Grid.Level;
            var col = raft.Grid.Chosen == RaftKind.Sea ? new Color(0.85f, 0.25f, 0.2f) : raft.Grid.Chosen == RaftKind.Aerial ? Sky : new Color(0.2f, 0.75f, 0.7f);
            var top = new Vector3(b.MidX, 2.9f, b.MidZ);
            Piece(PrimitiveType.Cylinder, top - Vector3.up * 0.6f, new Vector3(0.05f, 0.6f, 0.05f), DarkWood);
            float len = 0.4f + 1.6f * lv / (float)EvolutionRules.ShipMax;
            sails.Add(Piece(PrimitiveType.Cube, top + new Vector3(-len / 2f, 0f, 0f), new Vector3(len, 0.22f, 0.02f), col).transform);
            // A gold star for every 25 levels.
            for (int i = 0; i < lv / 25; i++)
                Piece(PrimitiveType.Sphere, top + new Vector3(0f, 0.2f + 0.18f * i, 0f), Vector3.one * 0.14f, Brass);
        }

        bool Has(int slot) => raft.Grid.HasVessel(slot);

        static readonly Color Deep = new Color(0.12f, 0.22f, 0.3f), Glass = new Color(0.55f, 0.9f, 1f);

        /// <summary>Undersea, DEX: a submarine. A dark hull under the raft and a conning tower; each part adds its piece.</summary>
        void Submarine(Bounds2 b)
        {
            float y = raft.WaterLine;
            // Frame: the hull along the raft and the conning tower in the middle.
            var hull = Piece(PrimitiveType.Capsule, new Vector3(b.MidX, y - 0.05f, b.MidZ), new Vector3(Mathf.Max(1.2f, b.Width * 0.7f), b.Length / 2f + 0.6f, 0.9f), Deep);
            hull.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Piece(PrimitiveType.Cube, new Vector3(b.MidX - 0.4f, 0.75f, b.Row), new Vector3(0.9f, 0.9f, 0.6f), Deep);   // conning tower
            if (Has(1))   // pressure hull plates
                foreach (int side in new[] { -1, 1 })
                    Piece(PrimitiveType.Cube, new Vector3(b.MidX, y + 0.2f, side < 0 ? b.Z0 - 0.1f : b.Z1 + 0.1f), new Vector3(b.Length, 0.35f, 0.08f), Iron);
            if (Has(2))   // ballast tanks
                foreach (int side in new[] { -1, 1 })
                {
                    var tank = Piece(PrimitiveType.Capsule, new Vector3(b.MidX, y - 0.1f, (side < 0 ? b.Z0 - 0.45f : b.Z1 + 0.45f)), new Vector3(0.45f, b.Length / 2.5f, 0.45f), Iron);
                    tank.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            if (Has(3))   // the screw at the back
            {
                var hub = Piece(PrimitiveType.Cylinder, new Vector3(b.X0 - 0.5f, y, b.MidZ), new Vector3(0.25f, 0.15f, 0.25f), Brass);
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                propeller = Piece(PrimitiveType.Cube, new Vector3(b.X0 - 0.6f, y, b.MidZ), new Vector3(0.06f, 0.9f, 0.18f), Brass).transform;
            }
            if (Has(4)) Piece(PrimitiveType.Cylinder, new Vector3(b.MidX - 0.3f, 1.55f, b.Row), new Vector3(0.07f, 0.45f, 0.07f), Iron);   // periscope
            if (Has(5)) Piece(PrimitiveType.Sphere, new Vector3(b.X1 + 0.5f, y + 0.1f, b.MidZ), Vector3.one * 0.8f, Glass);   // sonar dome
        }

        /// <summary>Aerial, INT: a skyship. Wings out to both sides, a propeller in front, a tail behind.</summary>
        void Plane(Bounds2 b)
        {
            float y = 0.32f;
            float span = Mathf.Max(2.5f, b.Width * 0.9f), chord = Mathf.Min(2.2f, b.Length * 0.45f);
            // Frame before the Wings are on: short stub wings.
            if (!Has(1))
                foreach (int side in new[] { -1, 1 })
                    Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f, y + 0.1f, (side < 0 ? b.Z0 : b.Z1) + side * 0.4f), new Vector3(chord * 0.7f, 0.06f, 0.8f), Wing);
            foreach (int side in Has(1) ? new[] { -1, 1 } : new int[0])
            {
                float edge = side < 0 ? b.Z0 : b.Z1;
                var wing = Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f, y + 0.1f, edge + side * span / 2f), new Vector3(chord, 0.08f, span), Wing);
                wing.transform.localRotation = Quaternion.Euler(side * -6f, 0f, 0f);   // a little dihedral
                Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f - chord / 2f + 0.15f, y + 0.12f + side * 0f, edge + side * span / 2f), new Vector3(0.3f, 0.1f, span * 0.9f), Sky)
                    .transform.localRotation = Quaternion.Euler(side * -6f, 0f, 0f);   // flap stripe
                Piece(PrimitiveType.Cube, new Vector3(b.MidX + 0.3f, y + 0.2f, edge + side * (span - 0.15f)), new Vector3(chord * 0.6f, 0.25f, 0.08f), Sky);   // wingtip
            }
            // Engines: a pod under each wing (or at the raft's sides before the wings are on).
            if (Has(4))
                foreach (int side in new[] { -1, 1 })
                {
                    float z = side < 0 ? b.Z0 - span * 0.35f : b.Z1 + span * 0.35f;
                    var pod = Piece(PrimitiveType.Capsule, new Vector3(b.MidX + 0.5f, y - 0.1f, z), new Vector3(0.35f, 0.55f, 0.35f), Iron);
                    pod.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Piece(PrimitiveType.Cylinder, new Vector3(b.MidX + 1.05f, y - 0.1f, z), new Vector3(0.3f, 0.03f, 0.3f), Brass)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // intake ring
                }
            // Cockpit: a glass canopy on the front of the deck.
            if (Has(5))
            {
                Piece(PrimitiveType.Sphere, new Vector3(b.X1 - 0.6f, y + 0.35f, b.Row), new Vector3(1.1f, 0.7f, 0.9f), new Color(0.55f, 0.85f, 1f));
                Piece(PrimitiveType.Cube, new Vector3(b.X1 - 0.6f, y + 0.08f, b.Row), new Vector3(1.2f, 0.16f, 1f), Sky);
            }
            if (!Has(3)) { Tail(b, y); return; }
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
            Tail(b, y);
        }

        /// <summary>Tail behind the back edge: a fin and two small stabilizers on a boom.</summary>
        void Tail(Bounds2 b, float y)
        {
            if (!Has(2)) return;
            float back = b.X0 - 0.2f;
            Piece(PrimitiveType.Cube, new Vector3(back - 0.5f, y + 0.25f, b.Row), new Vector3(1.2f, 0.18f, 0.25f), Wing);   // boom
            Piece(PrimitiveType.Cube, new Vector3(back - 0.9f, y + 0.85f, b.Row), new Vector3(0.6f, 1.1f, 0.08f), Sky);       // fin
            Piece(PrimitiveType.Cube, new Vector3(back - 0.9f, y + 0.3f, b.Row), new Vector3(0.5f, 0.06f, 1.6f), Wing);      // stabilizers
        }

        /// <summary>Sea level, STR: a galleon. Hull walls, a pointed bow, two masts with square sails, a skull flag.</summary>
        void PirateShip(Bounds2 b)
        {
            float wl = raft.WaterLine;
            // Hull walls along both sides, rising a little above the deck.
            foreach (int side in new[] { -1, 1 })
            {
                float z = side < 0 ? b.Z0 - 0.12f : b.Z1 + 0.12f;
                if (Has(1))
                {
                    Piece(PrimitiveType.Cube, new Vector3(b.MidX, wl + 0.35f, z), new Vector3(b.Length + 0.3f, 0.75f, 0.18f), Hull);
                    Piece(PrimitiveType.Cube, new Vector3(b.MidX, wl + 0.75f, z), new Vector3(b.Length + 0.3f, 0.08f, 0.24f), DarkWood);   // rail
                }
                else Piece(PrimitiveType.Cube, new Vector3(b.MidX, wl + 0.2f, z), new Vector3(b.Length + 0.3f, 0.4f, 0.14f), Hull);   // frame: low hull sides
                // Cannons poking out.
                if (Has(2))
                for (float x = b.X0 + 1f; x < b.X1 - 0.5f; x += 1.6f)
                    Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.45f, z + side * 0.2f), new Vector3(0.16f, 0.22f, 0.16f), Black)
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // Pointed bow past the front, and a bowsprit.
            float front = b.X1 + 0.1f, half = b.Width / 2f + 0.12f;
            foreach (int side in Has(3) ? new[] { -1, 1 } : new int[0])
            {
                float len = Mathf.Sqrt(half * half + 1.2f * 1.2f);
                var plank = Piece(PrimitiveType.Cube, new Vector3(front + 0.6f, wl + 0.35f, b.MidZ + side * half / 2f), new Vector3(len, 0.75f, 0.18f), Hull);
                plank.transform.localRotation = Quaternion.Euler(0f, side * Mathf.Atan2(half, 1.2f) * Mathf.Rad2Deg, 0f);
            }
            if (Has(3))
            {
                var sprit = Piece(PrimitiveType.Cylinder, new Vector3(front + 1.5f, wl + 0.9f, b.MidZ), new Vector3(0.09f, 0.8f, 0.09f), DarkWood);
                sprit.transform.localRotation = Quaternion.Euler(0f, 0f, -65f);
            }
            // Stern castle behind the back edge.
            if (Has(5))
            {
                Piece(PrimitiveType.Cube, new Vector3(b.X0 - 0.45f, wl + 0.55f, b.MidZ), new Vector3(0.8f, 1.1f, b.Width + 0.5f), Hull);
                Piece(PrimitiveType.Cube, new Vector3(b.X0 - 0.45f, wl + 1.15f, b.MidZ), new Vector3(0.9f, 0.1f, b.Width + 0.6f), DarkWood);
            }
            // Two masts with square sails, on the middle row, and a skull flag on the tall one.
            float[] masts = Has(4) ? new[] { b.X1 - b.Length * 0.25f, b.X0 + b.Length * 0.35f } : new float[0];
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
            if (Look != look || raft.Grid.VesselCount != built || Stage != stage) { built = raft.Grid.VesselCount; Rebuild(); }
            if (spawner == null) spawner = FindFirstObjectByType<DriftSpawner>();
            var wind = spawner != null && spawner.currentDirection.sqrMagnitude > 0.01f ? spawner.currentDirection : Vector3.right;
            float flutter = Mathf.Sin(Time.time * 3f) * 4f;
            var face = Quaternion.LookRotation(Vector3.ProjectOnPlane(wind, Vector3.up).normalized);
            // Plain: the sail turns to catch the current's wind and flutters. Pirate sails stay square to the ship.
            foreach (var sail in sails)
                if (sail != null)
                    sail.rotation = look == RaftKind.Sea
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
