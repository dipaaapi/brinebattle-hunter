using System.Collections.Generic;
using System.Linq;
using BrineBattle.Collection;
using BrineBattle.Items;
using BrineBattle.Monsters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Collection book (idea U-11): monsters, items and pets, shown as silhouettes until acquired.
    /// A monster counts once harvested, finished or defeated; an item once it is in the bag; a pet
    /// once tamed (Phase 7). Saved in PlayerPrefs. Placeholder IMGUI; press C or the HUD button.
    /// </summary>
    public class CollectionBook : MonoBehaviour
    {
        const string SaveKey = "brinebattle.collection";

        public HookController hook;
        public List<MonsterDefinition> monsters = new List<MonsterDefinition>();
        public List<ItemDefinition> items = new List<ItemDefinition>();

        public CollectionLog Log { get; private set; }

        bool open;
        CollectionPage page;
        Vector2 scroll;
        string selected;

        static readonly string[] Tabs = { "Monsters", "Items", "Pets" };

        void Awake() => Log = CollectionLog.Deserialize(PlayerPrefs.GetString(SaveKey, ""));

        void OnEnable()
        {
            if (hook == null) return;
            hook.BookToggleRequested += Toggle;
            hook.MonsterAcquired += OnMonster;
            hook.Inventory.Changed += OnInventory;
            Log.Unlocked += OnUnlocked;
        }

        void OnDisable()
        {
            if (hook == null) return;
            hook.BookToggleRequested -= Toggle;
            hook.MonsterAcquired -= OnMonster;
            hook.Inventory.Changed -= OnInventory;
            Log.Unlocked -= OnUnlocked;
        }

        void Toggle() => open = !open;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) Toggle();
        }

        void OnMonster(MonsterDefinition m) => Log.Record(CollectionPage.Monsters, m.id);

        void OnInventory()
        {
            foreach (var kv in hook.Inventory.Items) Log.Record(CollectionPage.Items, kv.Key.id);
        }

        void OnUnlocked(CollectionPage p, string id)
        {
            PlayerPrefs.SetString(SaveKey, Log.Serialize());
            PlayerPrefs.Save();
            hook.Log($"New {p.ToString().TrimEnd('s').ToLower()} entry: {NameOf(p, id)}!");
        }

        string NameOf(CollectionPage p, string id)
        {
            if (p == CollectionPage.Items) return items.FirstOrDefault(i => i.id == id)?.displayName ?? id;
            return monsters.FirstOrDefault(m => m.id == id)?.displayName ?? id;
        }

        void OnGUI()
        {
            if (!open || (hook != null && hook.builder != null && hook.builder.Active)) return;
            float w = 640, h = Mathf.Min(520, Screen.height - 40);
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, 20, w, h), "Collection Book", HuntStyles.Window);

            var newPage = (CollectionPage)GUILayout.Toolbar((int)page, Tabs);
            if (newPage != page) { page = newPage; selected = null; scroll = Vector2.zero; }

            var ids = IdsOf(page);
            GUILayout.Label($"{Mathf.RoundToInt(Log.Completion(page, ids) * 100f)}% complete  ({ids.Count(i => Log.Has(page, i))}/{ids.Count})");
            if (page == CollectionPage.Pets) GUILayout.Label("<i>Pets unlock when taming arrives (Phase 7).</i>", Rich);

            GUILayout.BeginHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(250));
            DrawList();
            GUILayout.EndScrollView();
            GUILayout.BeginVertical(GUI.skin.box);
            DrawDetail();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset book (testing)", GUILayout.Width(170)))
            {
                Log.Unlocked -= OnUnlocked;
                Log = new CollectionLog();
                Log.Unlocked += OnUnlocked;
                PlayerPrefs.DeleteKey(SaveKey);
                selected = null;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close (C)", GUILayout.Width(120))) open = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        List<string> IdsOf(CollectionPage p) =>
            p == CollectionPage.Items ? items.Select(i => i.id).ToList() : monsters.Select(m => m.id).ToList();

        void DrawList()
        {
            if (page == CollectionPage.Items)
            {
                foreach (var i in items.OrderBy(i => i.type).ThenBy(i => i.rarity))
                    Entry(i.id, i.displayName, Tint.Of(i.element));
            }
            else
            {
                foreach (var m in monsters.OrderBy(m => m.sizeClass).ThenBy(m => m.displayName))
                    Entry(m.id, m.displayName, Tint.Of(m.element));
            }
        }

        void Entry(string id, string name, Color color)
        {
            bool known = Log.Has(page, id);
            GUILayout.BeginHorizontal();
            var swatch = GUILayoutUtility.GetRect(22, 22, GUILayout.Width(22));
            GUI.DrawTexture(swatch, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, known ? color : new Color(0.05f, 0.05f, 0.08f), 0, 6);
            if (GUILayout.Button(known ? name : "???", GUILayout.Height(22))) selected = id;
            GUILayout.EndHorizontal();
        }

        void DrawDetail()
        {
            if (selected == null) { GUILayout.Label("Select an entry."); return; }
            bool known = Log.Has(page, selected);
            var big = GUILayoutUtility.GetRect(120, 90, GUILayout.ExpandWidth(true));

            if (page == CollectionPage.Items)
            {
                var i = items.FirstOrDefault(x => x.id == selected);
                if (i == null) return;
                Silhouette(big, known, Tint.Of(i.element));
                if (!known) { GUILayout.Label("??? — find it to fill in this page."); return; }
                GUILayout.Label($"<b><color={BagPanel.RarityColor(i.rarity)}>{i.displayName}</color></b>", Rich);
                GUILayout.Label($"{i.type} · {i.rarity} · {i.element}");
                GUILayout.Label(i.description, GUI.skin.textArea);
            }
            else
            {
                var m = monsters.FirstOrDefault(x => x.id == selected);
                if (m == null) return;
                Silhouette(big, known, Tint.Of(m.element));
                if (!known)
                {
                    GUILayout.Label(page == CollectionPage.Pets ? "??? — tame one to fill in this page." : "??? — hunt one to fill in this page.");
                    return;
                }
                GUILayout.Label($"<b><color={BagPanel.RarityColor(m.rarity)}>{m.displayName}</color></b>", Rich);
                GUILayout.Label($"{m.element} · {m.habitat} · size {m.sizeClass} · {m.rarity}");
                GUILayout.Label($"HP {m.stats.hp}  ATK {m.stats.attack}  DEF {m.stats.defense}  SPD {m.stats.speed}");
                if (m.loot != null)
                    GUILayout.Label("Drops: " + string.Join(", ", m.loot.entries.Where(e => e.item != null)
                        .Select(e => Log.Has(CollectionPage.Items, e.item.id) ? $"{e.item.displayName} ({e.chance:P0})" : "???")));
                GUILayout.Label(m.description, GUI.skin.textArea);
            }
        }

        static void Silhouette(Rect r, bool known, Color color)
        {
            var c = known ? color : new Color(0.05f, 0.05f, 0.08f);
            var body = new Rect(r.center.x - 60, r.y + 15, 120, 60);
            GUI.DrawTexture(body, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, c, 0, 30);
            if (!known) GUI.Label(new Rect(body.x, body.y + 20, body.width, 20), "<b>?</b>", Centered);
        }

        static GUIStyle rich, centered;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        static GUIStyle Centered => centered ??= new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter, fontSize = 18 };
    }
}
