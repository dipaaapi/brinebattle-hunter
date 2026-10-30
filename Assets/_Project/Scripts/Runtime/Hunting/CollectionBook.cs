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

        static bool open => WorldPause.Open == Menu.Book;
        CollectionPage page;
        Vector2 scroll;
        string selected;

        static readonly string[] Tabs = { "Monsters", "Items", "Pets" };

        void Awake()
        {
            Log = CollectionLog.Deserialize(PlayerPrefs.GetString(SaveKey, ""));
            // Floating loot (U-30) isn't a creature: keep it out of the monster pages.
            monsters = monsters.Where(m => m != null && !m.isFlotsam).ToList();
        }

        void OnEnable()
        {
            if (hook == null) return;
            hook.BookToggleRequested += Toggle;
            hook.MonsterAcquired += OnMonster;
            hook.MonsterTamed += OnTamed;
            hook.Inventory.Changed += OnInventory;
            Log.Unlocked += OnUnlocked;
        }

        void OnDisable()
        {
            if (hook == null) return;
            hook.BookToggleRequested -= Toggle;
            hook.MonsterAcquired -= OnMonster;
            hook.MonsterTamed -= OnTamed;
            hook.Inventory.Changed -= OnInventory;
            Log.Unlocked -= OnUnlocked;
        }

        void Toggle() => WorldPause.Toggle(Menu.Book);

        /// <summary>Replaces the book with a saved one (loading a save file).</summary>
        public void Restore(string data)
        {
            Log.Unlocked -= OnUnlocked;
            Log = CollectionLog.Deserialize(data);
            Log.Unlocked += OnUnlocked;
            PlayerPrefs.SetString(SaveKey, Log.Serialize());
            selected = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) Toggle();
        }

        void OnMonster(MonsterDefinition m) => Log.Record(CollectionPage.Monsters, m.id);

        void OnTamed(MonsterDefinition m) => Log.Record(CollectionPage.Pets, m.id);

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
            HuntStyles.Begin();
            if (!open) return;
            GUI.depth = -5;
            float left = HookController.HudRight + 10, w = Mathf.Min(640, HuntStyles.W - left - 12), h = Mathf.Min(520, HuntStyles.H - 120);
            GUILayout.BeginArea(new Rect(left, 20, w, h), "Collection Book", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Book); GUILayout.EndArea(); return; }

            var labels = new string[Tabs.Length + 1];
            for (int i = 0; i < Tabs.Length; i++)
            {
                var all = IdsOf((CollectionPage)i);
                labels[i] = $"{Tabs[i]} {all.Count(x => Log.Has((CollectionPage)i, x))}/{all.Count}";
            }
            var recipes = hook != null && hook.builder != null ? hook.builder.recipes.Where(r => r != null).ToList() : new List<RecipeDefinition>();
            labels[Tabs.Length] = $"Recipes {recipes.Count(r => hook.Knows(r))}/{recipes.Count}";
            int newTab = HuntStyles.Tabs(recipesTab ? Tabs.Length : (int)page, labels);
            if (newTab == Tabs.Length) { if (!recipesTab) scroll = Vector2.zero; recipesTab = true; }
            else if (recipesTab || (CollectionPage)newTab != page) { recipesTab = false; page = (CollectionPage)newTab; selected = null; scroll = Vector2.zero; }
            if (recipesTab) { DrawRecipes(recipes); GUILayout.EndArea(); return; }

            var ids = IdsOf(page);
            GUILayout.Label($"{Mathf.RoundToInt(Log.Completion(page, ids) * 100f)}% found", HuntStyles.Hint);
            HuntStyles.Bar(Log.Completion(page, ids), new Color(0.98f, 0.82f, 0.3f));

            GUILayout.BeginHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(250));
            DrawList();
            GUILayout.EndScrollView();
            GUILayout.BeginVertical(GUI.skin.box);
            DrawDetail();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<size=10>Reset book (testing)</size>", HuntStyles.RichButton, GUILayout.Width(140)))
            {
                Log.Unlocked -= OnUnlocked;
                Log = new CollectionLog();
                Log.Unlocked += OnUnlocked;
                PlayerPrefs.DeleteKey(SaveKey);
                selected = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        bool recipesTab;

        /// <summary>
        /// Idea Round 3 (U-14): every recipe the hunter's Alchemy Mastery knows, with its ingredients;
        /// the rest are "???" with the mastery level that reveals them.
        /// </summary>
        void DrawRecipes(List<RecipeDefinition> recipes)
        {
            var m = hook.Mastery;
            GUILayout.Label($"Alchemy Mastery Lv {m.Level}", HuntStyles.Heading);
            HuntStyles.Bar(m.Progress, new Color(0.75f, 0.5f, 1f));
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var r in recipes.OrderBy(AlchemyMastery.Needed).ThenBy(r => (int)CraftRules.CategoryOf(r)))
            {
                GUILayout.BeginVertical(HuntStyles.Card);
                if (hook.Knows(r))
                {
                    var main = r.outputs.FirstOrDefault(o => o.item != null);
                    string name = r.kind == RecipeKind.Dismantle ? $"Take apart: {CraftRules.Describe(r.inputs)}"
                        : main != null ? $"<color={BagPanel.RarityColor(main.item.rarity)}>{main.item.displayName}</color>" : r.displayName;
                    GUILayout.Label($"<b>{name}</b>  <size=11>{CraftRules.CategoryLabels[(int)CraftRules.CategoryOf(r)]}</size>", HuntStyles.Text);
                    GUILayout.Label(r.kind == RecipeKind.Dismantle ? $"Gives {CraftRules.Describe(r.outputs)}" : CraftRules.Describe(r.inputs), HuntStyles.Hint);
                }
                else GUILayout.Label($"<b>???</b>  <color={HuntStyles.BadHex}>{RaftBuilder.LockReason(hook, r)}</color>", HuntStyles.Text);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
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
            var swatch = GUILayoutUtility.GetRect(26, 26, GUILayout.Width(26));
            GUI.DrawTexture(swatch, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, known ? color : new Color(0.05f, 0.05f, 0.08f), 0, 6);
            var old = GUI.backgroundColor;
            if (selected == id) GUI.backgroundColor = HuntStyles.Go;
            if (GUILayout.Button(known ? name : "???", GUILayout.Height(26))) selected = id;
            GUI.backgroundColor = old;
            GUILayout.EndHorizontal();
        }

        void DrawDetail()
        {
            if (selected == null) { GUILayout.Label("", HuntStyles.Text); return; }
            bool known = Log.Has(page, selected);
            var big = GUILayoutUtility.GetRect(120, 90, GUILayout.ExpandWidth(true));

            if (page == CollectionPage.Items)
            {
                var i = items.FirstOrDefault(x => x.id == selected);
                if (i == null) return;
                Silhouette(big, known, Tint.Of(i.element));
                if (!known) { GUILayout.Label("???"); return; }
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
                    GUILayout.Label("???");
                    return;
                }
                GUILayout.Label($"<b><color={BagPanel.RarityColor(m.rarity)}>{m.displayName}</color></b>", Rich);
                GUILayout.Label($"{m.element} · {m.habitat} · size {m.sizeClass} · {m.rarity}");
                GUILayout.Label($"HP {m.stats.hp}  ATK {m.stats.attack}  DEF {m.stats.defense}  SPD {m.stats.speed}");
                if (m.loot != null)
                    GUILayout.Label("Drops: " + string.Join(", ", m.loot.entries.Where(e => e.item != null)
                        .Select(e => Log.Has(CollectionPage.Items, e.item.id) ? $"{e.item.displayName} ({e.chance:P0})" : "???")));
                GUILayout.Label(m.description, GUI.skin.textArea);
                if (page == CollectionPage.Pets)
                    foreach (var g in hook.Companions.Memorials.Where(x => x.Id == m.id))
                        GUILayout.Label($"In memory of {g.Name}, your {g.Role}: lived {g.Age} battles.");
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
