using System.Collections.Generic;
using System.Linq;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Looks only (owner's ask), rebuilt whenever the raft changes. Evolution (Ed's ask, 2026-10-31):
    /// - Raft Lv 1-50: a bamboo raft with a small sail that becomes a simple boat through the 22 ship pieces
    ///   (<see cref="ShipPieceRules"/>) the minions or the hunter install; each piece grows a little every level;
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
        /// <summary>Rebuilds every level, on every installed piece and on the ship pick, so the raft changes a little as it grows.</summary>
        int Stage => (raft.Grid.IsShip ? 1000 : 0) + raft.Grid.Level + 100000 * raft.Grid.Pieces.Count;

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
            byPiece.Clear();
            growing.Clear();
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
            AnimateInstall();
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
            // Ed's ask: the raft becomes a boat through real pieces that minions (or the hunter) install, little by
            // little. Each installed piece draws itself here and keeps growing a bit every level up to Lv 50
            // (ShipPieceRules.Growth); the next piece the raft needs shows as a pale scaffold. Rules kept from the
            // Lv 33 fix: nothing stands on the deck where people walk; walls and rails on the edges, the hull under
            // the deck, the stern and bow pieces past the ends, sails above head height.
            var g = raft.Grid;
            int lv = g.PieceLevel;
            float s = Raft.CellSize, deck = 0.34f, wl = raft.WaterLine;
            float grow = Mathf.Clamp01((lv - 1) / 49f);
            float x0 = b.X0, x1 = b.X1, length = b.Length, width = b.Width, midX = b.MidX, midZ = b.MidZ;
            float bowW = Mathf.Min(width, 3f), sternX = x0 - s * 0.55f;
            // The next piece shows as a scaffold from 2 levels before it opens (the minions are on it).
            var np = ShipPieceRules.Next(lv, g.Pieces);
            nextPiece = np != null && np.Level <= lv + 2 ? np.Id : null;
            float G(string id) => ShipPieceRules.Growth(ShipPieceRules.Find(id), lv);

            // The plain raft: a bamboo pole and a small sail at the front of the middle row (always there).
            int row = cells.Select(c => c.Z).OrderBy(z => Mathf.Abs(z)).First();
            int mastX = cells.Where(c => c.Z == row).Max(c => c.X);
            // On the outer part of the front edge cell, away from where anyone stands (cells are walked at their centre).
            var foot = new Vector3(mastX * s + s * 0.42f, 0.3f, row * s);
            bool wooden = g.HasPiece("piece_wooden_mast");
            float mastH = 1.4f + 0.4f * grow + (wooden ? 0.5f * G("piece_wooden_mast") + 0.3f : 0f);
            Begin(null);
            if (!wooden) Piece(PrimitiveType.Cylinder, foot + Vector3.up * mastH, new Vector3(0.08f, mastH, 0.08f), Bamboo);
            if (!g.HasPiece("piece_yard_sail"))
            {
                float small = 1.0f + 0.3f * grow;
                // The sail's bottom stays 1.5 m above the deck, over everyone's heads.
                sails.Add(Piece(PrimitiveType.Cube, new Vector3(foot.x, deck + 1.5f + small / 2f, foot.z), new Vector3(1.0f + 0.3f * grow, small, 0.03f), Cloth).transform);
            }
            // Every level: a little more cargo lashed to the OUTSIDE of the sides (never on a cell anyone walks on).
            int cargo = Mathf.Min(14, lv / 3);
            for (int i = 0; i < cargo; i++)
            {
                bool left = i % 2 == 0;
                float x = Mathf.Lerp(x0 + 0.35f, x1 - 0.6f, (i / 2) / 7f);
                float z = left ? b.Z0 - 0.32f : b.Z1 + 0.32f;
                if (i % 3 == 2) Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.18f, z), new Vector3(0.26f, 0.16f, 0.26f), new Color(0.6f, 0.42f, 0.24f));   // barrel
                else Piece(PrimitiveType.Cube, new Vector3(x, wl + 0.16f, z), Vector3.one * 0.28f, new Color(0.7f, 0.53f, 0.32f));   // crate
            }

            // Lashing Rope: flat rope bands across the deck, one more every few levels.
            if (Begin("piece_lashing_rope"))
            {
                int n = 2 + Mathf.RoundToInt(6 * G("piece_lashing_rope"));
                for (int i = 0; i < n; i++)
                    Piece(PrimitiveType.Cube, new Vector3(Mathf.Lerp(x0 + 0.15f, x1 - 0.15f, i / (float)(n - 1)), deck + 0.01f, midZ), new Vector3(0.05f, 0.03f, width), new Color(0.45f, 0.32f, 0.18f));
            }
            // Outrigger floats and their arms (katig), until the hull planking closes the hull.
            bool hulled = g.HasPiece("piece_hull_plank");
            float floatZ = 1.1f + 0.3f * G("piece_outrigger");
            if (!hulled && Begin("piece_outrigger"))
                foreach (int side in new[] { -1, 1 })
                {
                    float z = (side < 0 ? b.MinZ - floatZ : b.MaxZ + floatZ) * s;
                    var log = Piece(PrimitiveType.Capsule, new Vector3(midX, wl + 0.12f, z), new Vector3(0.24f + 0.06f * G("piece_outrigger"), length * (0.35f + 0.15f * G("piece_outrigger")), 0.24f), Bamboo);
                    log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            if (!hulled && Begin("piece_bamboo_arm"))
            {
                float armZ = (b.MaxZ - b.MinZ + 2f * floatZ) * s;
                foreach (float x in new[] { x0 + length * 0.2f, x1 - length * 0.2f })
                    Piece(PrimitiveType.Cylinder, new Vector3(x, wl + 0.08f, midZ), new Vector3(0.09f, armZ / 2f, 0.09f), Bamboo)   // under the deck planks
                        .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            // Deck Trim: a flat band around the rim.
            if (Begin("piece_deck_trim"))
            {
                foreach (float z in new[] { b.Z0, b.Z1 }) Piece(PrimitiveType.Cube, new Vector3(midX, deck + 0.02f, z), new Vector3(length, 0.06f, 0.12f), Trim);
                foreach (float x in new[] { x0, x1 }) Piece(PrimitiveType.Cube, new Vector3(x, deck + 0.02f, midZ), new Vector3(0.12f, 0.06f, width), Trim);
            }
            // Low Wall, then the Gunwale: side walls just outside the edges that rise a little every level.
            float wall = 0f;
            if (Begin("piece_low_wall"))
            {
                wall = 0.1f + 0.1f * G("piece_low_wall");
                foreach (float z in new[] { b.Z0 - 0.08f, b.Z1 + 0.08f })
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck + wall / 2f - 0.15f, z), new Vector3(length, wall + 0.3f, 0.12f), Wood);
            }
            if (Begin("piece_gunwale"))
            {
                float up = 0.08f + 0.17f * G("piece_gunwale");
                foreach (float z in new[] { b.Z0 - 0.08f, b.Z1 + 0.08f })
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck + wall + up / 2f, z), new Vector3(length, up, 0.14f), Wood);
                wall += up;
            }
            // Pointed Bow: a low diamond whose back corner sits at the front edge; it grows longer.
            if (Begin("piece_pointed_bow"))
            {
                float d = bowW * (0.55f + 0.45f * G("piece_pointed_bow")) / 1.414f;
                var bow = Piece(PrimitiveType.Cube, new Vector3(x1 + d * 0.707f, deck - 0.12f, midZ), new Vector3(d, 0.35f, d), Wood);
                bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
            // The hull, all UNDER the deck: ribs, then a keel, then planking; it deepens a little every level.
            float depthMax = Mathf.Min(1f, 0.35f + width * 0.08f), top = wl - 0.1f, half = width / 2f;
            float depth = depthMax * (0.35f + 0.65f * G("piece_hull_rib"));
            float angle = Mathf.Atan2(depth, half) * Mathf.Rad2Deg, slope = Mathf.Sqrt(half * half + depth * depth);
            if (Begin("piece_hull_rib"))
                for (float x = x0 + 0.3f; x < x1 - 0.1f; x += s)
                    foreach (int side in new[] { -1, 1 })
                        Piece(PrimitiveType.Cube, new Vector3(x, top - depth / 2f, midZ + side * half / 2f), new Vector3(0.1f, 0.08f, slope), Trim)
                            .transform.localRotation = Quaternion.Euler(-side * angle, 0f, 0f);   // outer end up at the deck, inner end down at the keel
            if (Begin("piece_keel"))
                Piece(PrimitiveType.Cube, new Vector3(midX, top - depth - 0.06f, midZ), new Vector3(length * (0.7f + 0.2f * G("piece_keel")), 0.12f + 0.06f * G("piece_keel"), 0.12f), Trim);
            if (Begin("piece_hull_plank"))
                foreach (int side in new[] { -1, 1 })
                    Piece(PrimitiveType.Cube, new Vector3(midX, top - depth / 2f, midZ + side * half / 2f), new Vector3(length, 0.07f, slope), Wood)
                        .transform.localRotation = Quaternion.Euler(-side * angle, 0f, 0f);
            // Railing on top of the walls.
            if (Begin("piece_railing"))
                foreach (float z in new[] { b.Z0 - 0.08f, b.Z1 + 0.08f })
                {
                    float railH = 0.25f + 0.1f * G("piece_railing");
                    for (float x = x0 + 0.2f; x <= x1 - 0.1f; x += s) Piece(PrimitiveType.Cube, new Vector3(x, deck + wall + railH / 2f, z), new Vector3(0.06f, railH, 0.06f), Trim);
                    Piece(PrimitiveType.Cube, new Vector3(midX, deck + wall + railH, z), new Vector3(length, 0.05f, 0.07f), Trim);
                }
            // Mast Step, the Wooden Mast, the Yard & Sail.
            if (Begin("piece_mast_step")) Piece(PrimitiveType.Cube, foot + Vector3.up * 0.08f, new Vector3(0.16f, 0.12f, 0.3f), Trim);
            if (Begin("piece_wooden_mast"))
            {
                float thick = 0.1f + 0.05f * G("piece_wooden_mast");
                Piece(PrimitiveType.Cylinder, foot + Vector3.up * mastH, new Vector3(thick, mastH, thick), Trim);
            }
            if (Begin("piece_yard_sail"))
            {
                float k = G("piece_yard_sail"), sailH = 1.3f + 0.6f * k, sailW = 1.4f + 0.7f * k;
                sails.Add(Piece(PrimitiveType.Cube, new Vector3(foot.x, deck + 1.55f + sailH / 2f, foot.z), new Vector3(sailW, sailH, 0.03f), Cloth).transform);
                Piece(PrimitiveType.Cylinder, new Vector3(foot.x, deck + 1.6f + sailH, foot.z), new Vector3(0.06f, sailW / 2f + 0.1f, 0.06f), Trim)
                    .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // the yard arm
            }
            // Second Mast on the back edge.
            if (Begin("piece_second_mast"))
            {
                var back = new Vector3(x0 - 0.15f, 0.3f, row * s);
                float h2 = mastH * (0.75f + 0.15f * G("piece_second_mast")), sail2 = 1.0f + 0.5f * G("piece_second_mast");
                Piece(PrimitiveType.Cylinder, back + Vector3.up * h2, new Vector3(0.08f, h2, 0.08f), Trim);
                sails.Add(Piece(PrimitiveType.Cube, new Vector3(back.x, deck + 1.5f + sail2 / 2f, back.z), new Vector3(0.9f + 0.4f * G("piece_second_mast"), sail2, 0.03f), Cloth).transform);
            }
            // Stern Deck past the back edge, a Rudder behind it and a Small Cabin on it (never over the deck).
            float sternW = Mathf.Min(width, 3.2f);
            if (Begin("piece_stern_deck"))
                Piece(PrimitiveType.Cube, new Vector3(sternX, deck - 0.05f, midZ), new Vector3(s * (0.7f + 0.3f * G("piece_stern_deck")), 0.25f, sternW), Wood);
            if (Begin("piece_rudder"))
                Piece(PrimitiveType.Cube, new Vector3(sternX - s * 0.55f, wl - 0.1f, midZ), new Vector3(0.08f, 0.7f + 0.2f * G("piece_rudder"), 0.5f), Trim);
            if (Begin("piece_small_cabin"))
            {
                float ch = 0.5f + 0.25f * G("piece_small_cabin");
                var cabin = new Vector3(sternX, deck + ch / 2f + 0.05f, midZ);
                Piece(PrimitiveType.Cube, cabin, new Vector3(s * 0.85f, ch, Mathf.Min(width - 0.4f, 1.8f)), Wood);
                Piece(PrimitiveType.Cube, cabin + Vector3.up * (ch / 2f + 0.05f), new Vector3(s * 0.95f, 0.1f, Mathf.Min(width - 0.2f, 2f)), Red);   // its roof, off the deck
            }
            // Bowsprit and Figurehead past the bow.
            float bowTip = x1 + bowW * 0.9f;
            if (Begin("piece_bowsprit"))
                Piece(PrimitiveType.Cylinder, new Vector3(bowTip + 0.2f, deck + 0.35f, midZ), new Vector3(0.06f, 0.6f + 0.3f * G("piece_bowsprit"), 0.06f), Trim)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
            if (Begin("piece_figurehead"))
                Piece(PrimitiveType.Sphere, new Vector3(bowTip, deck + 0.1f, midZ), Vector3.one * (0.25f + 0.1f * G("piece_figurehead")), Brass);
            // Lanterns on the corners, above the walls.
            if (Begin("piece_lanterns"))
                foreach (var corner in new[] { new Vector3(x0, 0, b.Z0 - 0.08f), new Vector3(x0, 0, b.Z1 + 0.08f), new Vector3(x1, 0, b.Z0 - 0.08f), new Vector3(x1, 0, b.Z1 + 0.08f) })
                    Piece(PrimitiveType.Sphere, corner + Vector3.up * (deck + wall + 0.45f), Vector3.one * 0.18f, new Color(1f, 0.85f, 0.4f));
            // Flag at the masthead.
            if (Begin("piece_flag"))
                Piece(PrimitiveType.Cube, foot + new Vector3(0f, mastH * 2f + 0.15f, 0.3f), new Vector3(0.02f, 0.3f, 0.5f + 0.2f * G("piece_flag")), Red);
            Begin(null);
        }

        // ---- Ship pieces: which piece is being drawn, its scaffold, and the build animation ----

        string current, nextPiece, seenInstalled;
        bool scaffold, seenFirst;
        readonly Dictionary<string, List<Transform>> byPiece = new Dictionary<string, List<Transform>>();
        readonly List<(Transform t, Vector3 to)> growing = new List<(Transform, Vector3)>();
        float growAt;
        const float GrowSeconds = 0.6f;
        static readonly Color Scaffold = new Color(0.92f, 0.88f, 0.75f);

        /// <summary>
        /// Starts drawing a piece: true when it's installed, or when it is the next one the raft needs (then it is
        /// drawn pale, as a scaffold the minions are working on). Null = plain raft decor.
        /// </summary>
        bool Begin(string id)
        {
            current = id;
            scaffold = id != null && !raft.Grid.HasPiece(id);
            return id == null || !scaffold || id == nextPiece;
        }

        /// <summary>After a rebuild: the piece just installed grows in from nothing, with sparks and its name.</summary>
        void AnimateInstall()
        {
            string id = raft.Grid.LastInstalled;
            if (!seenFirst) { seenFirst = true; seenInstalled = id; return; }
            if (id == null || id == seenInstalled) return;
            seenInstalled = id;
            if (!byPiece.TryGetValue(id, out var list) || list.Count == 0) return;
            growing.Clear();
            foreach (var t in list) { growing.Add((t, t.localScale)); t.localScale *= 0.05f; }
            growAt = Time.unscaledTime;
            var at = list[0].position;
            Juice.Pop($"+ {ShipPieceRules.Find(id)?.Name}", at + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.4f));
            for (int i = 0; i < 8; i++) StartCoroutine(Spark(at));
        }

        static System.Collections.IEnumerator Spark(Vector3 at)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Hammer spark";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.07f;
            Tint.Apply(go.GetComponent<Renderer>(), new Color(1f, 0.8f, 0.3f));
            var v = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1.5f, 3f), Random.Range(-1.5f, 1.5f));
            for (float t = 0f; t < 0.5f && go != null; t += Time.unscaledDeltaTime)
            {
                v.y -= 9f * Time.unscaledDeltaTime;
                go.transform.position += v * Time.unscaledDeltaTime;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        void Grow()
        {
            if (growing.Count == 0) return;
            float k = Mathf.Clamp01((Time.unscaledTime - growAt) / GrowSeconds);
            // A small overshoot, like a piece knocked into place.
            float pop = k < 1f ? Mathf.Sin(k * Mathf.PI * 0.5f) * (1f + 0.15f * Mathf.Sin(k * Mathf.PI)) : 1f;
            foreach (var (t, to) in growing) if (t != null) t.localScale = to * Mathf.Max(0.05f, pop);
            if (k >= 1f) growing.Clear();
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
            // A piece the raft doesn't have yet is a pale scaffold.
            Tint.Apply(go.GetComponent<Renderer>(), scaffold ? Color.Lerp(color, Scaffold, 0.7f) : color);
            parts.Add(go);
            if (current != null)
            {
                go.name = scaffold ? $"Scaffold {current}" : current;
                if (!byPiece.TryGetValue(current, out var list)) byPiece[current] = list = new List<Transform>();
                list.Add(go.transform);
            }
            return go;
        }

        void Update()
        {
            // A raft tree pick changes the look even when no part moved.
            if (Look != look || raft.Grid.VesselCount != built || Stage != stage) { built = raft.Grid.VesselCount; Rebuild(); }
            Grow();
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
