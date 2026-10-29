using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The raft's storage box, which is also the alchemy and crafting table (owner's ask: one object,
    /// not a separate workbench). It sits on its own tile, away from the hunter, the pets and the edge
    /// modules. When loot is stored or something is crafted (by the hunter or a pet) the lid opens,
    /// the box bounces, a little cube flies in or pops out, and a label says what happened.
    /// Placeholder primitives; added at runtime by HookController.
    /// </summary>
    public class StorageBox : MonoBehaviour
    {
        public static StorageBox Instance { get; private set; }

        public Raft raft;

        Transform body, lid;
        Rafts.Cell cell;
        float pulse;   // seconds of animation left
        readonly List<(string text, float age)> labels = new List<(string, float)>();
        GUIStyle labelStyle;

        static readonly Color Wood = new Color(0.55f, 0.36f, 0.2f);
        static readonly Color Trim = new Color(0.85f, 0.7f, 0.35f);

        /// <summary>Where the box stands, in the raft's local space (pets use this to work beside it).</summary>
        public Vector3 LocalPosition => new Vector3(cell.X * Raft.CellSize, 0.3f, cell.Z * Raft.CellSize);
        public Rafts.Cell Cell => cell;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (raft != null) raft.Grid.Changed -= Place;
        }

        void Start()
        {
            var root = new GameObject("Storage Box").transform;
            root.SetParent(raft.transform, false);
            body = Cube(root, new Vector3(0f, 0.22f, 0f), new Vector3(0.62f, 0.44f, 0.46f), Wood);
            Cube(body, new Vector3(0f, 0f, 0f), new Vector3(1.04f, 0.18f, 1.04f), Trim);   // band
            var hinge = new GameObject("Lid hinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(0f, 0.45f, -0.23f);
            lid = hinge;
            Cube(hinge, new Vector3(0f, 0.05f, 0.23f), new Vector3(0.66f, 0.1f, 0.5f), Wood);
            raft.Grid.Changed += Place;
            Place();
        }

        static Transform Cube(Transform parent, Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Storage Box part";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            Tint.Apply(go.GetComponent<Renderer>(), color);
            return go.transform;
        }

        /// <summary>
        /// Its own tile: a raft part (not a module or habitat) next to the centre, never the hunter's
        /// centre tile, preferring the back-left so the right side stays free for the sail.
        /// </summary>
        void Place()
        {
            var parts = raft.Grid.Parts.Where(kv => kv.Value.type == Items.ItemType.Raft && !(kv.Key.X == 0 && kv.Key.Z == 0)).Select(kv => kv.Key).ToList();
            if (parts.Count == 0) return;
            cell = parts.OrderBy(c => Mathf.Max(Mathf.Abs(c.X), Mathf.Abs(c.Z))).ThenBy(c => c.X).ThenByDescending(c => c.Z).First();
            body.parent.localPosition = new Vector3(cell.X * Raft.CellSize, 0.3f, cell.Z * Raft.CellSize);
        }

        /// <summary>Loot went into the box: a cube flies in from <paramref name="from"/> (world), the lid opens.</summary>
        public void Store(string what, Vector3 from)
        {
            Pulse($"<color=#9fe8ff>Stored:</color> {what}");
            StartCoroutine(Fly(from, body.position + Vector3.up * 0.3f, new Color(0.6f, 0.9f, 1f)));
        }

        /// <summary>Something was made at the box (by the hunter or a pet): it pops out of the lid.</summary>
        public void Crafted(string what)
        {
            Pulse($"<color=#ffd866>Crafted:</color> {what}");
            StartCoroutine(Fly(body.position + Vector3.up * 0.3f, body.position + Vector3.up * 1.4f, new Color(1f, 0.85f, 0.35f)));
        }

        void Pulse(string text)
        {
            pulse = 1.2f;
            labels.Add((text, 0f));
            if (labels.Count > 3) labels.RemoveAt(0);
        }

        System.Collections.IEnumerator Fly(Vector3 from, Vector3 to, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Box item";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.22f;
            Tint.Apply(go.GetComponent<Renderer>(), color);
            for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.7f;
                go.transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                go.transform.Rotate(0f, 360f * Time.unscaledDeltaTime, 0f);
                yield return null;
            }
            Destroy(go);
        }

        /// <summary>Pets are hiding inside after a lost battle (Ed's ask): the box shivers, for laughs.</summary>
        public bool Scared { get; set; }
        float eep;
        static readonly string[] Squeaks = { "eep!", "*shiver*", "is it gone?", "eek!", "*teeth chatter*" };

        void Update()
        {
            if (body == null) return;
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime);
            float k = pulse > 0f ? Mathf.Sin((1.2f - pulse) * 14f) * pulse * 0.12f : 0f;
            body.parent.localScale = new Vector3(1f - k, 1f + k, 1f - k);
            float peek = 0f;
            if (Scared)
            {
                // Rattle in place, and now and then the lid peeks open with a squeak.
                float t = Time.time;
                body.parent.localRotation = Quaternion.Euler(Mathf.Sin(t * 47f) * 4f, Mathf.Sin(t * 31f) * 3f, Mathf.Sin(t * 53f) * 5f);
                eep -= Time.deltaTime;
                if (eep <= 0f)
                {
                    eep = Random.Range(1.2f, 2.4f);
                    labels.Add((Squeaks[Random.Range(0, Squeaks.Length)], 0f));
                    if (labels.Count > 3) labels.RemoveAt(0);
                }
                peek = eep > 0.9f ? -18f : 0f;
            }
            else body.parent.localRotation = Quaternion.identity;
            lid.localRotation = Quaternion.Euler(pulse > 0f ? -70f * Mathf.Clamp01(pulse * 2f) : peek, 0f, 0f);
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var (text, age) = labels[i];
                age += Time.unscaledDeltaTime;
                if (age > 2f) labels.RemoveAt(i); else labels[i] = (text, age);
            }
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (labels.Count == 0 || body == null || BattleController.InBattle || Camera.main == null) return;
            labelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 13, fontStyle = FontStyle.Bold };
            var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(body.position + Vector3.up * 0.9f));
            if (sp.z < 0f) return;
            for (int i = 0; i < labels.Count; i++)
            {
                var (text, age) = labels[i];
                float y = HuntStyles.H - sp.y - 20 - age * 18f - (labels.Count - 1 - i) * 18f;
                GUI.Label(new Rect(sp.x - 120, y, 240, 22), text, labelStyle);
            }
        }
    }
}
