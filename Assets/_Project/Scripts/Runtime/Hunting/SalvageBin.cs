using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The Salvage Bin (Ed's ask): its own box on the raft, apart from the storage box. Everything
    /// salvaged or dismantled, from the Bag (B) or the Build menu (R), goes here: little glowing essence
    /// orbs fly from the storage box into the bin, the lid pops and a label says what it got. Clicking
    /// the bin (or its button in the Bag and Build menus) opens it: the essence it holds and the
    /// dismantle recipes. Placeholder primitives; added at runtime by HookController.
    /// </summary>
    public class SalvageBin : MonoBehaviour
    {
        public static SalvageBin Instance { get; private set; }

        public Raft raft;
        public HookController hook;

        Transform root, lid;
        Rafts.Cell cell;
        bool placed, dirty = true;
        float pulse;
        readonly List<(string text, float age)> labels = new List<(string, float)>();
        GUIStyle labelStyle;
        Vector2 scroll;
        int page;

        static readonly Color Metal = new Color(0.32f, 0.4f, 0.38f), Rim = new Color(0.55f, 0.62f, 0.6f);
        static readonly Color Glow = new Color(0.8f, 0.45f, 1f);

        public bool Open => WorldPause.Open == Menu.Bin;
        public Rafts.Cell Cell => cell;
        public bool Placed => placed;
        /// <summary>Where the bin stands, in the raft's local space (pets keep clear of it).</summary>
        public Vector3 LocalPosition => new Vector3(cell.X * Raft.CellSize, 0.3f, cell.Z * Raft.CellSize);

        public static void Toggle() => WorldPause.Toggle(Menu.Bin);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (raft != null) raft.Grid.Changed -= MarkDirty;
            if (hook != null) hook.Inventory.Salvaged -= OnSalvaged;
        }

        void Start()
        {
            root = new GameObject("Salvage Bin").transform;
            root.SetParent(raft.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Salvage Bin body";
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            Tint.Apply(body.GetComponent<Renderer>(), Metal);
            // The collider stays so the bin can be clicked; it opens the bin.
            body.AddComponent<SalvageBinClick>();
            var rim = Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.6f, 0f), new Vector3(0.56f, 0.03f, 0.56f), Rim);
            rim.name = "Rim";
            var hinge = new GameObject("Lid hinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(0f, 0.63f, -0.27f);
            lid = hinge;
            Part(PrimitiveType.Cylinder, hinge, new Vector3(0f, 0f, 0.27f), new Vector3(0.54f, 0.03f, 0.54f), Rim);
            Part(PrimitiveType.Cube, hinge, new Vector3(0f, 0.05f, 0.27f), new Vector3(0.18f, 0.05f, 0.06f), Metal);   // handle
            // A recycle-ish band of glow round the middle: the essence inside.
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.32f, 0f), new Vector3(0.51f, 0.04f, 0.51f), Glow);
            raft.Grid.Changed += MarkDirty;
            hook.Inventory.Salvaged += OnSalvaged;
        }

        static GameObject Part(PrimitiveType type, Transform parent, Vector3 local, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Salvage Bin part";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            Tint.Apply(go.GetComponent<Renderer>(), color);
            return go;
        }

        void MarkDirty() => dirty = true;

        /// <summary>
        /// Its own plank tile next to the storage box: never the hunter's centre tile, the box's tile, a
        /// module or a habitat.
        /// </summary>
        void Place()
        {
            dirty = false;
            var box = StorageBox.Instance;
            var tiles = raft.Grid.Parts.Where(kv => kv.Value.type == ItemType.Raft && !(kv.Key.X == 0 && kv.Key.Z == 0)
                && (box == null || !kv.Key.Equals(box.Cell))).Select(kv => kv.Key).ToList();
            placed = tiles.Count > 0;
            root.gameObject.SetActive(placed);
            if (!placed) return;
            var near = box != null ? box.Cell : new Rafts.Cell(-1, 1);
            cell = tiles.OrderBy(c => Mathf.Abs(c.X - near.X) + Mathf.Abs(c.Z - near.Z))
                .ThenBy(c => Mathf.Max(Mathf.Abs(c.X), Mathf.Abs(c.Z))).ThenByDescending(c => c.Z).First();
            root.localPosition = new Vector3(cell.X * Raft.CellSize, 0.3f, cell.Z * Raft.CellSize);
        }

        void OnSalvaged(ItemDefinition item, int gained)
        {
            string what = item != null ? $"{item.displayName} → +{gained} {item.rarity} essence" : $"+{gained} essence";
            Took(what, Mathf.Clamp(gained, 1, 6), item != null && item.element != Elements.Element.None ? Tint.Of(item.element) : Glow);
        }

        /// <summary>Something was taken apart into the bin: orbs fly in from the storage box, the lid pops.</summary>
        public void Took(string what, int orbs, Color color)
        {
            if (root == null) return;
            pulse = 1.2f;
            labels.Add(($"<color=#d9a6ff>Bin:</color> {what}", 0f));
            if (labels.Count > 3) labels.RemoveAt(0);
            var from = StorageBox.Instance != null
                ? raft.transform.TransformPoint(StorageBox.Instance.LocalPosition) + Vector3.up * 0.5f
                : raft.transform.position + Vector3.up;
            for (int i = 0; i < orbs; i++) StartCoroutine(Orb(from, color, i * 0.08f));
        }

        IEnumerator Orb(Vector3 from, Color color, float delay)
        {
            for (float w = 0f; w < delay; w += Time.unscaledDeltaTime) yield return null;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Essence orb";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.16f;
            Tint.Apply(go.GetComponent<Renderer>(), color);
            var jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                if (root == null) break;
                float k = t / 0.8f;
                var to = root.position + Vector3.up * 0.7f;
                go.transform.position = Vector3.Lerp(from + jitter, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.08f, k);
                yield return null;
            }
            Destroy(go);
        }

        void Update()
        {
            if (root == null) return;
            if (dirty) Place();
            var kb = Keyboard.current;
            if (kb != null && Open && kb.escapeKey.wasPressedThisFrame) WorldPause.Close(Menu.Bin);
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime);
            float k = pulse > 0f ? Mathf.Sin((1.2f - pulse) * 14f) * pulse * 0.12f : 0f;
            root.localScale = new Vector3(1f - k, 1f + k, 1f - k);
            float openAngle = pulse > 0f ? -70f * Mathf.Clamp01(pulse) : Open ? -50f : 0f;
            lid.localRotation = Quaternion.Slerp(lid.localRotation, Quaternion.Euler(openAngle, 0f, 0f), 12f * Time.unscaledDeltaTime);
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var (text, age) = labels[i];
                age += Time.unscaledDeltaTime;
                if (age > 2.5f) labels.RemoveAt(i);
                else labels[i] = (text, age);
            }
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (root == null || !placed) return;
            DrawLabels();
            if (!Open || hook == null) return;
            GUI.depth = -5;
            float w = Mathf.Min(460, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 120);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Salvage Bin", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Bin); GUILayout.EndArea(); return; }
            page = HuntStyles.Tabs(page, new[] { "Essence inside", "Dismantle" });
            scroll = GUILayout.BeginScrollView(scroll);
            if (page == 0) DrawEssence();
            else DrawDismantle();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawEssence()
        {
            var inv = hook.Inventory;
            var list = inv.Essence.OrderByDescending(kv => kv.Key.Rarity).ToList();
            GUILayout.Label($"<b>{list.Sum(kv => kv.Value)}</b> essence inside", HuntStyles.Heading);
            if (list.Count == 0) { GUILayout.Label("Empty. Salvage gear in the Bag or dismantle things here.", HuntStyles.Text); return; }
            foreach (var kv in list)
            {
                GUILayout.BeginHorizontal(HuntStyles.Card);
                GUILayout.Label($"<b>{kv.Value}</b> × <color={BagPanel.RarityColor(kv.Key.Rarity)}>{kv.Key}</color>", HuntStyles.Text);
                GUILayout.EndHorizontal();
            }
        }

        void DrawDismantle()
        {
            var builder = hook.builder;
            if (builder == null) return;
            var inv = hook.Inventory;
            var list = CraftRules.ByReadiness(inv, builder.recipes.Where(r => r != null && r.kind == RecipeKind.Dismantle));
            var ready = list.Where(r => CraftRules.CanCraft(inv, r)).ToList();
            if (ready.Count == 0) GUILayout.Label("Nothing to take apart right now.", HuntStyles.Text);
            foreach (var r in ready) builder.RecipeCard(r);
            if (list.Count > ready.Count)
                GUILayout.Label($"<size=11>{list.Count - ready.Count} more you could take apart once you have them.</size>", HuntStyles.Hint);
        }

        void DrawLabels()
        {
            if (labels.Count == 0 || Camera.main == null || WorldPause.Open != Menu.None && !Open) return;
            labelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, richText = true };
            var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(root.position + Vector3.up * 1.2f));
            if (sp.z < 0f) return;
            for (int i = 0; i < labels.Count; i++)
            {
                var (text, age) = labels[i];
                GUI.Label(new Rect(sp.x - 120, HuntStyles.H - sp.y - 18 - 18 * (labels.Count - 1 - i) - age * 12f, 240, 20), text, labelStyle);
            }
        }
    }

    /// <summary>Marks the bin's body so a click on it opens the bin (HookController's raycast).</summary>
    public class SalvageBinClick : MonoBehaviour { }
}
