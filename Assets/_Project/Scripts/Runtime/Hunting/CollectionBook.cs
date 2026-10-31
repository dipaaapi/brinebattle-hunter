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
    /// The Journal (was the Collection Book, idea U-11): monsters, items and pets, shown as silhouettes until
    /// acquired, recipes, and the soundtrack to play piece by piece (Ed's ask).
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

        static readonly string[] Tabs = { "Monsters", "Items", "Minions" };

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
            // Wide and centred (Ed: the book looked outdated): a tile grid on the left, the picked entry on the right.
            float w = Mathf.Min(860, HuntStyles.W - HookController.HudRight - 24), h = Mathf.Min(600, HuntStyles.H - 80);
            float left = Mathf.Max(HookController.HudRight + 10, (HuntStyles.W - w) / 2f);
            GUILayout.BeginArea(new Rect(left, Mathf.Max(20, (HuntStyles.H - h) / 2f), w, h), "Journal", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Book); GUILayout.EndArea(); return; }

            var labels = new string[Tabs.Length + 2];
            for (int i = 0; i < Tabs.Length; i++)
            {
                var all = IdsOf((CollectionPage)i);
                labels[i] = $"{Tabs[i]} {all.Count(x => Log.Has((CollectionPage)i, x))}/{all.Count}";
            }
            var recipes = hook != null && hook.builder != null ? hook.builder.recipes.Where(r => r != null).ToList() : new List<RecipeDefinition>();
            labels[Tabs.Length] = $"Recipes {recipes.Count(r => hook.Knows(r))}/{recipes.Count}";
            labels[Tabs.Length + 1] = "Music";
            int newTab = HuntStyles.Tabs(musicTab ? Tabs.Length + 1 : recipesTab ? Tabs.Length : (int)page, labels);
            if (newTab == Tabs.Length + 1) { musicTab = true; recipesTab = false; }
            else if (newTab == Tabs.Length) { if (!recipesTab) scroll = Vector2.zero; recipesTab = true; musicTab = false; }
            else if (recipesTab || musicTab || (CollectionPage)newTab != page) { recipesTab = musicTab = false; page = (CollectionPage)newTab; selected = null; scroll = Vector2.zero; }
            if (!musicTab) MusicPlayer.Instance?.StopPreview();
            if (musicTab) { DrawMusic(w); GUILayout.EndArea(); return; }
            if (recipesTab) { DrawRecipes(recipes, w); GUILayout.EndArea(); return; }

            var ids = IdsOf(page);
            float done = Log.Completion(page, ids);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{Mathf.RoundToInt(done * 100f)}%</b> found", HuntStyles.Text, GUILayout.Width(110));
            GUILayout.BeginVertical();
            GUILayout.Space(6);
            HuntStyles.Bar(done, new Color(0.98f, 0.82f, 0.3f));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            if (page == CollectionPage.Items) itemFilter = GUILayout.Toolbar(itemFilter, ItemFilters, GUILayout.Height(26));

            const float detailW = 300f;
            float gridW = w - detailW - 44;
            GUILayout.BeginHorizontal();
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(gridW + 18));
            DrawGrid(gridW);
            GUILayout.EndScrollView();
            GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(detailW));
            detailScroll = GUILayout.BeginScrollView(detailScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            DrawDetail(detailW - 30);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("<size=10>Reset book (testing)</size>", HuntStyles.RichButton, GUILayout.Width(140)))
            {
                Log.Unlocked -= OnUnlocked;
                Log = new CollectionLog();
                Log.Unlocked += OnUnlocked;
                PlayerPrefs.DeleteKey(SaveKey);
                selected = null;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        bool recipesTab, musicTab;
        Vector2 detailScroll;

        // ---- Music (Ed's ask: the soundtrack is part of the collection; play each piece on its own) ----

        static readonly (string name, string what)[] Layers =
        {
            ("Open Sea", "The bed under everything: soft chords and the swell of the waves."),
            ("Morning Shanty", "The day tune, a marimba melody over the chords."),
            ("Moonlit Bells", "Glassy bells that take over at night."),
            ("Something Stirs", "Low drums when a drift near the raft looks alive."),
            ("Battle Stations", "Driving bass and drums for fights."),
            ("Leviathan Rising", "Brass and timpani on top of the battle for bosses."),
            ("Squall", "Rain and rolling thunder in rough seas and storms."),
        };

        static readonly (string name, string what, Audio.MusicMoment mix)[] Mixes =
        {
            ("Calm day", "What a quiet day at sea sounds like.", new Audio.MusicMoment { Daylight = 1f }),
            ("Night watch", "The sea after dark.", new Audio.MusicMoment { Daylight = 0f }),
            ("Danger near", "Something alive is close.", new Audio.MusicMoment { Daylight = 1f, Danger = true }),
            ("Storm", "Rough seas and lightning.", new Audio.MusicMoment { Daylight = 0.4f, Storm = true }),
            ("Battle", "A fight on the raft.", new Audio.MusicMoment { Daylight = 1f, InBattle = true }),
            ("Boss fight", "A raft-level boss.", new Audio.MusicMoment { Daylight = 1f, InBattle = true, BossBattle = true }),
        };

        int playingMix = -1;

        void DrawMusic(float w)
        {
            var player = MusicPlayer.Instance;
            GUILayout.Label("Soundtrack", HuntStyles.Heading);
            GUILayout.Label("The music is made of layers that mix by what's happening. Play each one on its own, or a whole scene. Sound effects come here next.", HuntStyles.Hint);
            if (player == null || !player.Ready) { GUILayout.Label("The music is still tuning up…", HuntStyles.Text); return; }
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            float half = (w - 70) / 2f;
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(half));
            GUILayout.Label("<b>Pieces</b>", HuntStyles.Text);
            for (int i = 0; i < Layers.Length; i++)
            {
                bool on = player.Solo == i;
                if (Track(Layers[i].name, Layers[i].what, on, half)) { if (on) player.StopPreview(); else { player.StopPreview(); player.Solo = i; playingMix = -1; } }
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical(GUILayout.Width(half));
            GUILayout.Label("<b>Scenes</b>", HuntStyles.Text);
            for (int i = 0; i < Mixes.Length; i++)
            {
                bool on = player.PreviewMix.HasValue && playingMix == i;
                if (Track(Mixes[i].name, Mixes[i].what, on, half)) { player.StopPreview(); if (!on) { player.PreviewMix = Mixes[i].mix; playingMix = i; } else playingMix = -1; }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Volume {Mathf.RoundToInt(MusicSettings.Volume * 100)}%", HuntStyles.Text, GUILayout.Width(110));
            MusicSettings.Volume = GUILayout.HorizontalSlider(MusicSettings.Volume, 0f, 1f, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            if (player.Previewing && GUILayout.Button("Stop", GUILayout.Width(80))) { player.StopPreview(); playingMix = -1; }
            GUILayout.EndHorizontal();
        }

        /// <summary>One track card with a play / stop button. True when the button was pressed.</summary>
        static bool Track(string name, string what, bool playing, float width)
        {
            GUILayout.BeginHorizontal(HuntStyles.Card, GUILayout.Width(width));
            bool hit = HuntStyles.ColorButton(playing ? "Stop" : "Play", playing ? HuntStyles.Risky : HuntStyles.Go, GUILayout.Width(64), GUILayout.Height(40));
            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{name}</b>{(playing ? "  <color=#7CFC9A>playing</color>" : "")}", HuntStyles.Text);
            GUILayout.Label(what, HuntStyles.Hint);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return hit;
        }
        int itemFilter;
        static readonly string[] ItemFilters = { "All", "Materials", "Raft", "Gear", "Potions", "Minion", "Tools" };

        static bool InItemFilter(ItemType t, int f)
        {
            switch (f)
            {
                case 1: return t == ItemType.Material || t == ItemType.Essence;
                case 2: return t == ItemType.Raft || t == ItemType.Module || t == ItemType.Vessel;
                case 3: return GearRules.IsGear(t);
                case 4: return t == ItemType.Consumable;
                case 5: return t == ItemType.Companion || t == ItemType.Habitat;
                case 6: return t == ItemType.Tool;
                default: return true;
            }
        }

        RecipeCategory recipeCat;
        readonly Dictionary<RecipeCategory, RecipeSub> recipeSub = new Dictionary<RecipeCategory, RecipeSub>();

        /// <summary>
        /// Idea Round 3 (U-14): every recipe the hunter's Alchemy Mastery knows, with its ingredients;
        /// the rest are "???" with the mastery level that reveals them. Sorted into the Workbench's tabs and
        /// sub-tabs (Ed: one long list was hard to read).
        /// </summary>
        void DrawRecipes(List<RecipeDefinition> recipes, float w)
        {
            var m = hook.Mastery;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Alchemy Mastery Lv {m.Level}</b>", HuntStyles.Text, GUILayout.Width(190));
            GUILayout.BeginVertical();
            GUILayout.Space(6);
            HuntStyles.Bar(m.Progress, new Color(0.75f, 0.5f, 1f));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            var cats = (RecipeCategory[])System.Enum.GetValues(typeof(RecipeCategory));
            var catLabels = cats.Select(c =>
            {
                var inCat = recipes.Where(r => CraftRules.CategoryOf(r) == c).ToList();
                return $"{CraftRules.CategoryLabels[(int)c]} {inCat.Count(hook.Knows)}/{inCat.Count}";
            }).ToArray();
            int ci = GUILayout.Toolbar(System.Array.IndexOf(cats, recipeCat), catLabels, GUILayout.Height(28));
            if (cats[ci] != recipeCat) { recipeCat = cats[ci]; scroll = Vector2.zero; }
            var subs = CraftRules.SubsOf(recipeCat);
            if (!recipeSub.TryGetValue(recipeCat, out var sub) || System.Array.IndexOf(subs, sub) < 0) sub = subs[0];
            if (subs.Length > 1)
            {
                var subLabels = subs.Select(x => { var inSub = recipes.Where(r => CraftRules.SubOf(r) == x).ToList(); return $"{CraftRules.Label(x)} {inSub.Count(hook.Knows)}/{inSub.Count}"; }).ToArray();
                int si = GUILayout.Toolbar(System.Array.IndexOf(subs, sub), subLabels, GUILayout.Height(24));
                if (subs[si] != sub) scroll = Vector2.zero;
                sub = subs[si];
            }
            recipeSub[recipeCat] = sub;

            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            var list = recipes.Where(r => CraftRules.SubOf(r) == sub).OrderBy(r => hook.Knows(r) ? 0 : 1).ThenBy(AlchemyMastery.Needed).ToList();
            if (list.Count == 0) GUILayout.Label("No recipes here.", HuntStyles.Hint);
            // Two cards a row, so more fits on screen.
            float cardW = (w - 70) / 2f;
            for (int i = 0; i < list.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + 2, list.Count); j++) RecipeCard(list[j], cardW);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        void RecipeCard(RecipeDefinition r, float width)
        {
            GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(width), GUILayout.MinHeight(62));
            if (hook.Knows(r))
            {
                var main = r.outputs.FirstOrDefault(o => o.item != null);
                string name = r.kind == RecipeKind.Dismantle ? $"Take apart: {CraftRules.Describe(r.inputs)}"
                    : main != null ? $"<color={BagPanel.RarityColor(main.item.rarity)}>{main.item.displayName}</color>" : r.displayName;
                string kind = r.kind == RecipeKind.Upgrade ? "  <size=10>upgrade</size>" : "";
                GUILayout.Label($"<b>{name}</b>{kind}", HuntStyles.Text);
                GUILayout.Label(r.kind == RecipeKind.Dismantle ? $"Gives {CraftRules.Describe(r.outputs)}"
                    : string.Join("  ·  ", r.inputs.Where(x => x.item != null).Select(x => $"{HuntStyles.Count(hook.Inventory.Count(x.item), x.count)} {x.item.displayName}")), HuntStyles.Hint);
            }
            else
            {
                GUILayout.Label("<b>???</b>", HuntStyles.Text);
                GUILayout.Label($"<color={HuntStyles.BadHex}>{RaftBuilder.LockReason(hook, r)}</color>", HuntStyles.Hint);
            }
            GUILayout.EndVertical();
        }

        List<string> IdsOf(CollectionPage p) =>
            p == CollectionPage.Items ? items.Select(i => i.id).ToList() : monsters.Select(m => m.id).ToList();

        readonly List<HuntStyles.GridTile> tiles = new List<HuntStyles.GridTile>();
        readonly List<string> tileIds = new List<string>();

        void DrawGrid(float width)
        {
            tiles.Clear();
            tileIds.Clear();
            if (page == CollectionPage.Items)
            {
                foreach (var i in items.Where(i => InItemFilter(i.type, itemFilter)).OrderBy(i => i.type).ThenBy(i => i.rarity))
                {
                    bool known = Log.Has(page, i.id);
                    int have = known ? hook.Inventory.Count(i) : 0;
                    tiles.Add(new HuntStyles.GridTile { Title = i.displayName, Swatch = ItemTint(i), Rim = HuntStyles.RarityTint(i.rarity), Locked = !known, Badge = have > 0 ? $"×{have}" : null });
                    tileIds.Add(i.id);
                }
            }
            else
            {
                foreach (var m in monsters.OrderBy(m => m.sizeClass).ThenBy(m => m.displayName))
                {
                    tiles.Add(new HuntStyles.GridTile { Title = m.displayName, Swatch = Tint.Of(m.element), Rim = HuntStyles.RarityTint(m.rarity), Locked = !Log.Has(page, m.id), Badge = m.isBoss ? "BOSS" : null });
                    tileIds.Add(m.id);
                }
            }
            if (tiles.Count == 0) { GUILayout.Label("Nothing of this kind.", HuntStyles.Hint); return; }
            int clicked = HuntStyles.TileGrid(tiles, width, tileIds.IndexOf(selected));
            if (clicked >= 0) { selected = tileIds[clicked]; detailScroll = Vector2.zero; }
        }

        /// <summary>Items without an element get a colour by kind, so the grid isn't all grey.</summary>
        static Color ItemTint(ItemDefinition i)
        {
            if (i.element != Elements.Element.None) return Tint.Of(i.element);
            switch (i.type)
            {
                case ItemType.Raft: return Raft.PartColor(i);
                case ItemType.Consumable: return new Color(0.85f, 0.45f, 0.75f);
                case ItemType.Tool: return new Color(0.75f, 0.6f, 0.4f);
                case ItemType.Module: return new Color(0.3f, 0.65f, 0.55f);
                case ItemType.Vessel: return new Color(0.55f, 0.5f, 0.75f);
                case ItemType.Essence: return new Color(0.6f, 0.9f, 1f);
                default: return HuntStyles.RarityTint(i.rarity);
            }
        }

        void DrawDetail(float width)
        {
            if (selected == null)
            {
                GUILayout.Label("Pick an entry to read about it.", HuntStyles.Hint);
                return;
            }
            bool known = Log.Has(page, selected);
            var big = GUILayoutUtility.GetRect(width, 96, GUILayout.Width(width));

            if (page == CollectionPage.Items)
            {
                var i = items.FirstOrDefault(x => x.id == selected);
                if (i == null) return;
                Silhouette(big, known, ItemTint(i));
                if (!known) { GUILayout.Label("Not found yet. Get one in your Bag to fill this page.", HuntStyles.Hint); return; }
                GUILayout.Label($"<color={BagPanel.RarityColor(i.rarity)}>{i.displayName}</color>", HuntStyles.Heading);
                GUILayout.BeginHorizontal();
                HuntStyles.Chip(TypeName(i.type), HuntStyles.TealDeep);
                HuntStyles.Chip(i.rarity.ToString(), HuntStyles.RarityTint(i.rarity) * 0.55f);
                if (i.element != Elements.Element.None) HuntStyles.Chip(i.element.ToString(), Tint.Of(i.element) * 0.55f);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                int have = hook.Inventory.Count(i);
                GUILayout.Label(have > 0 ? $"In your bag: <b>{have}</b>" : "None in your bag now.", HuntStyles.Hint);
                GUILayout.Space(4);
                if (!string.IsNullOrEmpty(i.description)) GUILayout.Label(i.description, HuntStyles.Text);
            }
            else
            {
                var m = monsters.FirstOrDefault(x => x.id == selected);
                if (m == null) return;
                Silhouette(big, known, Tint.Of(m.element));
                if (!known)
                {
                    GUILayout.Label(page == CollectionPage.Pets ? "Not tamed yet." : "Not met yet. Harvest, finish or beat one to fill this page.", HuntStyles.Hint);
                    return;
                }
                GUILayout.Label($"<color={BagPanel.RarityColor(m.rarity)}>{m.displayName}</color>", HuntStyles.Heading);
                GUILayout.BeginHorizontal();
                HuntStyles.Chip(m.element.ToString(), Tint.Of(m.element) * 0.55f);
                HuntStyles.Chip(m.habitat.ToString(), HuntStyles.TealDeep);
                HuntStyles.Chip($"Size {m.sizeClass}", HuntStyles.TealDeep);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                HuntStyles.Chip($"HP {m.stats.hp}", new Color(0.45f, 0.2f, 0.2f));
                HuntStyles.Chip($"ATK {m.stats.attack}", new Color(0.45f, 0.3f, 0.15f));
                HuntStyles.Chip($"DEF {m.stats.defense}", new Color(0.2f, 0.3f, 0.45f));
                HuntStyles.Chip($"SPD {m.stats.speed}", new Color(0.2f, 0.4f, 0.3f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(m.description)) { GUILayout.Space(4); GUILayout.Label($"<i>{m.description}</i>", HuntStyles.Text); }
                if (m.loot != null)
                {
                    GUILayout.Space(4);
                    GUILayout.Label("<b>Drops</b>", HuntStyles.Text);
                    foreach (var e in m.loot.entries.Where(e => e.item != null))
                    {
                        bool seen = Log.Has(CollectionPage.Items, e.item.id);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(seen ? $"<color={BagPanel.RarityColor(e.item.rarity)}>{e.item.displayName}</color>" : "???", HuntStyles.Text);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{e.chance:P0}", HuntStyles.Hint, GUILayout.Width(44));
                        GUILayout.EndHorizontal();
                    }
                }
                if (page == CollectionPage.Pets)
                    foreach (var g in hook.Companions.Memorials.Where(x => x.Id == m.id))
                        GUILayout.Label($"In memory of {g.Name}: lived {g.Age} battles.", HuntStyles.Hint);
            }
        }

        static string TypeName(ItemType t) =>
            t == ItemType.Consumable ? "Potion / oil" : t == ItemType.Raft ? "Raft part" : t == ItemType.Vessel ? "Vehicle part" : t == ItemType.Tool ? "Hammer" : t.ToString();

        static void Silhouette(Rect r, bool known, Color color)
        {
            var c = known ? color : new Color(0.05f, 0.05f, 0.08f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.09f, 0.14f), 0, 10);
            var body = new Rect(r.center.x - 55, r.y + 18, 110, 60);
            GUI.DrawTexture(body, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, c, 0, 30);
            if (known) GUI.DrawTexture(new Rect(body.x + 22, body.y + 10, 34, 12), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 1f, 1f, 0.3f), 0, 6);
            else GUI.Label(new Rect(body.x, body.y + 20, body.width, 20), "<b>?</b>", Centered);
        }

        static GUIStyle rich, centered;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        static GUIStyle Centered => centered ??= new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter, fontSize = 18 };
    }
}
