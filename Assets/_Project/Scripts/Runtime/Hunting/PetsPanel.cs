using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Minions menu (Ed's U-39; was Pets). Press P or the HUD button. Three pages: My minions (pick one on the
    /// left: level, EXP, skills, Feed, Fuse, look, release), Skill tree (the four branches for the picked minion)
    /// and Chores (one switch per auto job).
    /// </summary>
    public class PetsPanel : MonoBehaviour
    {
        public HookController hook;
        static bool open => WorldPause.Open == Menu.Pets;
        Vector2 scroll;

        public static void Toggle() => WorldPause.Toggle(Menu.Pets);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.pKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Pets)) Toggle();
        }

        static readonly string[] PageLabels = { "My minions", "Skill tree", "Chores" };
        int page;
        Companion picked;
        Vector2 listScroll;

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open || hook == null) return;
            GUI.depth = -5;
            float w = Mathf.Min(760, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(640, HuntStyles.H - 80);
            panelWidth = w;
            var roster = hook.Companions;
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), $"Minions ({roster.All.Count}/{roster.Capacity})", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Pets); GUILayout.EndArea(); return; }
            page = HuntStyles.Tabs(page, PageLabels);
            if (page == 2) { scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar); DrawChores(); GUILayout.EndScrollView(); GUILayout.EndArea(); return; }

            if (roster.All.Count == 0)
            {
                GUILayout.Label("No minions yet.", HuntStyles.Heading);
                GUILayout.EndArea();
                return;
            }
            if (picked == null || !roster.All.Contains(picked)) picked = roster.All[0];

            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(200));
            foreach (var c in roster.All)
            {
                var old = GUI.backgroundColor;
                if (c == picked) GUI.backgroundColor = HuntStyles.Go;
                string battle = roster.Active == c ? " ⚔" : "";
                string hp = c.KnockedOut ? $"<color={HuntStyles.BadHex}>KO</color>" : $"{c.Hp}/{c.MaxHp} HP";
                string ail = c.Status != StatusEffect.None ? $" <color=#c9a0ff>{StatusRules.Tag(c.Status)}</color>" : "";
                if (GUILayout.Button($"<color={BagPanel.RarityColor(c.Rarity)}><b>{c.Name}</b></color>{battle}\n<size=11>Lv {c.Level} · {c.Skills.Count} skills · {hp}{ail}</size>", HuntStyles.RichButton, GUILayout.Height(44)))
                { picked = c; feeding = fusing = null; releasing = null; }
                GUI.backgroundColor = old;
            }
            GUILayout.Label($"Room: {roster.All.Count}/{roster.Capacity}", HuntStyles.Hint);
            GUILayout.EndScrollView();
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            if (page == 1) Tree(picked); else Detail(picked);
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>Name, level and the EXP bar: the top of both minion pages.</summary>
        void Header(Companion c)
        {
            GUILayout.Label($"<color={BagPanel.RarityColor(c.Rarity)}>{c.Name}</color>  <size=12>{c.Rarity} · {c.Element} · {c.StageName}</size>", HuntStyles.Heading);
            int need = MinionRules.ExpToNext(c.Level);
            GUILayout.Label(need > 0 ? $"<b>Lv {c.Level}</b>  <size=11>{c.Exp}/{need} EXP</size>" : $"<b>Lv {c.Level}</b>  <size=11>max level</size>", HuntStyles.Text);
            if (c.Favourite >= 0)
                GUILayout.Label($"<size=11>Loves eating <b>{MinionRules.FoodName(c.Favourite)}</b> (double EXP). "
                    + (c.Sulking ? "<color=#9bb0c8>Sulking: hasn't had it in a while, works 20% slower.</color>" : "<color=#9fe89f>Happy.</color>") + "</size>", HuntStyles.Text);
            HuntStyles.Bar(need > 0 ? (float)c.Exp / need : 1f, new Color(0.55f, 0.8f, 1f));
            int max = MinionRules.MaxSkills(c.Rarity, c.Stage);
            int nextAt = MinionRules.NextLearnLevel(c);
            var next = MinionRules.NextSkill(c);
            GUILayout.Label($"Skills {HuntStyles.Count(c.Skills.Count, max)}" + (nextAt > 0 && next != null ? $" · next: <b>{next}</b> at Lv {nextAt}" : ""), HuntStyles.Hint);
        }

        void Detail(Companion c)
        {
            var roster = hook.Companions;
            bool active = roster.Active == c;
            Header(c);
            GUILayout.Label(c.Stats + (active ? "  · <b>in battle</b>" : ""), HuntStyles.Text);

            // Health (Ed's U-32): HP carries over between battles and heals on the raft.
            string ail = c.Status != StatusEffect.None ? $"  <color=#c9a0ff>{StatusRules.Tag(c.Status)} ({c.StatusRounds})</color>" : "";
            GUILayout.Label(c.KnockedOut ? $"<color={HuntStyles.BadHex}>Knocked out</color> · resting{ail}" : $"Health: {c.Hp}/{c.MaxHp}{ail}", HuntStyles.Text);
            HuntStyles.Bar((float)c.Hp / Mathf.Max(1, c.MaxHp), c.Hp <= c.MaxHp * 0.35f ? new Color(0.95f, 0.45f, 0.4f) : new Color(0.4f, 0.85f, 0.45f));
            var treats = hook.TreatsFor(c);
            if (treats.Count > 0)
            {
                GUILayout.BeginHorizontal();
                foreach (var t in treats.Take(3))
                    if (GUILayout.Button($"{t.displayName} ({hook.Inventory.Count(t)})", GUILayout.Height(26))) hook.Treat(t, c);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("<b>Skills</b>", HuntStyles.Text);
            foreach (var s in c.Skills)
                GUILayout.Label($"<color=#7be08a>✔</color> <b>{s}</b> <size=11>· {MinionRules.Describe(s)}</size>", HuntStyles.Text);
            GUILayout.Label($"Potions drunk: {c.Infusions}/{CompanionGrowth.InfusionCap(c.Stage)}{(c.Echo != null ? $" · {c.Echo}" : "")}", HuntStyles.Hint);

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(feeding == c ? "Close food" : "Feed", GUILayout.Height(30))) { feeding = feeding == c ? null : c; fusing = null; }
            GUI.enabled = roster.All.Count > 1;
            if (GUILayout.Button(fusing == c ? "Close fuse" : "Fuse", GUILayout.Height(30))) { fusing = fusing == c ? null : c; feeding = null; }
            GUI.enabled = true;
            if (GUILayout.Button($"Look: {CompanionGrowth.LookName(c.Look)}", GUILayout.Height(30))) c.Look = CompanionGrowth.NextLook(c.Look);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (c.Has(MinionSkill.Fighter))
            {
                GUI.enabled = !active;
                if (GUILayout.Button(active ? "In battle ⚔" : "Take into battle", GUILayout.Height(28))) roster.SetActive(c);
                GUI.enabled = true;
            }
            if (releasing == c)
            {
                if (HuntStyles.ColorButton("Yes, release", HuntStyles.Risky)) { roster.Release(c); releasing = null; hook.ApplyCompanionBonuses(); hook.Log($"Released {c.Name} back to the sea."); GUILayout.EndHorizontal(); return; }
                if (GUILayout.Button("Keep", GUILayout.Height(28))) releasing = null;
            }
            else if (GUILayout.Button("Release", GUILayout.Height(28))) releasing = c;
            GUILayout.EndHorizontal();
            GrowthRow(c);
        }

        // ---- Skill tree (Ed's ask: a visual tree, four branches; made livelier) ----

        static readonly Color Learned = new Color(0.3f, 0.8f, 0.45f), NextCol = new Color(1f, 0.78f, 0.25f),
            LaterCol = new Color(0.35f, 0.42f, 0.52f), LockedCol = new Color(0.55f, 0.25f, 0.25f);
        GUIStyle nodeText, center;

        static Color FamilyColor(MinionRules.Family f) =>
            f == MinionRules.Family.Battle ? new Color(0.9f, 0.4f, 0.35f) : f == MinionRules.Family.Crew ? new Color(0.95f, 0.7f, 0.3f)
            : f == MinionRules.Family.Alchemy ? new Color(0.7f, 0.5f, 1f) : new Color(0.35f, 0.8f, 0.85f);

        static string FamilyBlurb(MinionRules.Family f) =>
            f == MinionRules.Family.Battle ? "fights beside you" : f == MinionRules.Family.Crew ? "builds and repairs"
            : f == MinionRules.Family.Alchemy ? "crafts and upgrades" : "gathers and tidies";

        /// <summary>
        /// Four branches as glowing constellations: round nodes joined by lines that light up as skills are
        /// learned, the next one pulsing gold, locked ones dim with what they need. Hover a node for what it does.
        /// </summary>
        void Tree(Companion c)
        {
            Header(c);
            nodeText ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11, alignment = TextAnchor.UpperCenter };
            center ??= new GUIStyle(HuntStyles.Text) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(6);
            float width = Mathf.Max(360f, panelWidth - 250f);
            var area = GUILayoutUtility.GetRect(width, 330, GUILayout.Width(width));
            float colW = area.width / MinionRules.Families.Length;
            string tip = null;
            for (int fi = 0; fi < MinionRules.Families.Length; fi++)
            {
                var f = MinionRules.Families[fi];
                var fc = FamilyColor(f);
                float cx = area.x + colW * fi + colW / 2f;
                // Branch header: a coloured band with its name and what it's for.
                var head = new Rect(area.x + colW * fi + 4, area.y, colW - 8, 40);
                GUI.DrawTexture(head, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, fc * 0.45f, 0, 10);
                GUI.Label(new Rect(head.x, head.y + 3, head.width, 18), $"<b>{f}</b>", center);
                GUI.Label(new Rect(head.x, head.y + 20, head.width, 16), $"<size=10>{FamilyBlurb(f)}</size>", center);
                var skills = MinionRules.In(f).ToList();
                Vector2? prev = null;
                bool prevLearned = false;
                for (int si = 0; si < skills.Count; si++)
                {
                    var s = skills[si];
                    var state = MinionRules.StateOf(c, s, out int at);
                    var p = new Vector2(cx, area.y + 82 + si * 92);
                    bool learned = state == MinionRules.NodeState.Learned;
                    if (prev.HasValue)
                    {
                        var lineCol = learned && prevLearned ? fc : new Color(0.22f, 0.3f, 0.4f);
                        GUI.DrawTexture(new Rect(p.x - 2, prev.Value.y + 26, 4, p.y - prev.Value.y - 52), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, lineCol, 0, 2);
                    }
                    float r = 24;
                    var col = learned ? fc : state == MinionRules.NodeState.Next ? NextCol : state == MinionRules.NodeState.Locked ? LockedCol * 0.7f : LaterCol;
                    if (learned) GUI.DrawTexture(new Rect(p.x - r - 6, p.y - r - 6, 2 * r + 12, 2 * r + 12), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(fc.r, fc.g, fc.b, 0.25f), 0, r + 6);
                    if (state == MinionRules.NodeState.Next)
                    {
                        float pulse = 0.3f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
                        GUI.DrawTexture(new Rect(p.x - r - 6, p.y - r - 6, 2 * r + 12, 2 * r + 12), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.8f, 0.3f, pulse), 0, r + 6);
                    }
                    var disc = new Rect(p.x - r, p.y - r, 2 * r, 2 * r);
                    GUI.DrawTexture(disc, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, col, 0, r);
                    GUI.DrawTexture(new Rect(disc.x + 3, disc.y + 3, disc.width - 6, disc.height - 6), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.06f, 0.1f, 0.16f, learned ? 0.35f : 0.75f), 0, r - 3);
                    // The skill's initial in the middle, like a rune.
                    GUI.Label(disc, learned ? $"<size=18><b>{s.ToString()[0]}</b></size>" : $"<size=18><color=#9aa7b6><b>{s.ToString()[0]}</b></color></size>", center);
                    string mark = learned ? $"<color={HuntStyles.GoodHex}>learned</color>"
                        : state == MinionRules.NodeState.Next ? $"<color=#ffd866>at Lv {at}</color>"
                        : state == MinionRules.NodeState.Locked ? $"<color=#ff8a80>{MinionRules.Gate(s) ?? "no room"}</color>"
                        : $"<color=#8a96a6>{MinionRules.Gate(s) ?? "later"}</color>";
                    GUI.Label(new Rect(p.x - colW / 2f + 4, p.y + r + 2, colW - 8, 32), $"<b>{s}</b>\n{mark}", nodeText);
                    if (disc.Contains(Event.current.mousePosition)) tip = $"<b>{s}</b> · {MinionRules.Describe(s)}";
                    prev = p;
                    prevLearned = learned;
                }
            }
            GUILayout.Label(tip ?? "Hover a skill to see what it does. Lines light up as skills are learned; the gold one is next.", HuntStyles.Hint);
        }

        float panelWidth = 720f;

        // ---- Chores: one card and one switch per job (Ed: less reading) ----

        static readonly Dictionary<MinionSkill, string> ChoreLine = new Dictionary<MinionSkill, string>
        {
            { MinionSkill.Looter, "Fetches floating loot" },
            { MinionSkill.Crewmate, "Builds from the Bag" },
            { MinionSkill.Mender, "Repairs harpoon & modules" },
            { MinionSkill.Builder, "Grows the raft" },
            { MinionSkill.Alchemist, "Keeps potions stocked" },
            { MinionSkill.Smith, "Refines your gear" },
            { MinionSkill.Tinker, "Makes better gear" },
            { MinionSkill.Salvager, "Takes junk apart" },
            { MinionSkill.Glutton, "Eats the rest of the junk" },
        };

        /// <summary>A pill switch. Returns the new value.</summary>
        static bool Switch(bool on)
        {
            var r = GUILayoutUtility.GetRect(54, 26, GUILayout.Width(54), GUILayout.Height(26));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, on ? new Color(0.3f, 0.8f, 0.45f) : new Color(0.25f, 0.3f, 0.38f), 0, 13);
            var knob = new Rect(on ? r.xMax - 23 : r.x + 3, r.y + 3, 20, 20);
            GUI.DrawTexture(knob, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.white, 0, 10);
            return GUI.Button(r, GUIContent.none, GUIStyle.none) ? !on : on;
        }

        /// <summary>A round badge with a letter: the job's picture until real icons come.</summary>
        static void Badge(string letter, Color color)
        {
            var r = GUILayoutUtility.GetRect(38, 38, GUILayout.Width(38), GUILayout.Height(38));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 19);
            GUI.Label(r, $"<size=17><b>{letter}</b></size>", BadgeText);
        }

        static GUIStyle badgeText;
        static GUIStyle BadgeText => badgeText ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true };

        void DrawChores()
        {
            var roster = hook.Companions;
            float cardW = (panelWidth - 64) / 2f;
            var jobs = ChoreLine.Keys.Where(s => roster.Best(s) != null).ToList();
            GUILayout.Label("Tap a switch to turn a job on or off. Who does it and what they're doing now is under each name.", HuntStyles.Hint);
            if (jobs.Count == 0) GUILayout.Label("No minion has a job skill yet. They learn them as they level up.", HuntStyles.Text);
            for (int i = 0; i < jobs.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < Mathf.Min(i + 2, jobs.Count); j++) ChoreCard(jobs[j], roster.Best(jobs[j]), cardW);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(6);
            FeedCard(panelWidth - 52);
        }

        void ChoreCard(MinionSkill skill, Companion who, float width)
        {
            GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(width), GUILayout.MinHeight(70));
            GUILayout.BeginHorizontal();
            Badge(skill.ToString().Substring(0, 1), FamilyColor(MinionRules.FamilyOf(skill)));
            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{skill}</b>  <size=11><color=#9bb0c8>{ChoreLine[skill]}</color></size>", HuntStyles.Text);
            GUILayout.Label($"{who.Name} · {MinionActivity.Now(who)}", HuntStyles.Hint);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            switch (skill)
            {
                case MinionSkill.Looter: CompanionSettings.FamiliarLoots = Switch(CompanionSettings.FamiliarLoots); break;
                case MinionSkill.Crewmate: CompanionSettings.CrewBuilds = Switch(CompanionSettings.CrewBuilds); break;
                case MinionSkill.Mender: CompanionSettings.Mends = Switch(CompanionSettings.Mends); break;
                case MinionSkill.Builder: CompanionSettings.BuilderCrafts = Switch(CompanionSettings.BuilderCrafts); break;
                case MinionSkill.Alchemist: CompanionSettings.AlchemistCrafts = Switch(CompanionSettings.AlchemistCrafts); break;
                case MinionSkill.Smith: CompanionSettings.MysticRefines = Switch(CompanionSettings.MysticRefines); break;
                case MinionSkill.Tinker: CompanionSettings.TinkerCrafts = Switch(CompanionSettings.TinkerCrafts); break;
                case MinionSkill.Salvager: CompanionSettings.SalvagerSorts = Switch(CompanionSettings.SalvagerSorts); break;
                case MinionSkill.Glutton: CompanionSettings.GluttonEats = Switch(CompanionSettings.GluttonEats); break;
            }
            GUILayout.EndHorizontal();
            // The one extra setting a few jobs have, as a small stepper.
            if (skill == MinionSkill.Alchemist)
                CompanionSettings.MysticKeep = Stepper($"Keep {CompanionSettings.MysticKeep} of each", CompanionSettings.MysticKeep, 0, 5);
            if (skill == MinionSkill.Salvager)
                CompanionSettings.TakeApartUpTo = (Rarity)Stepper($"Up to <color={BagPanel.RarityColor(CompanionSettings.TakeApartUpTo)}>{CompanionSettings.TakeApartUpTo}</color>", (int)CompanionSettings.TakeApartUpTo, 0, (int)Rarity.Mythic);
            if (skill == MinionSkill.Builder)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Level the raft up when ready", HuntStyles.Hint);
                GUILayout.FlexibleSpace();
                CompanionSettings.BuilderLevelsUp = Switch(CompanionSettings.BuilderLevelsUp);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        static int Stepper(string label, int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, HuntStyles.Hint);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("–", GUILayout.Width(28), GUILayout.Height(22)) && value > min) value--;
            if (GUILayout.Button("+", GUILayout.Width(28), GUILayout.Height(22)) && value < max) value++;
            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>Ed's ask: in Auto, minions feed themselves; pick which rarities and how much of a stack.</summary>
        void FeedCard(float width)
        {
            GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(width));
            GUILayout.BeginHorizontal();
            Badge("F", new Color(1f, 0.55f, 0.7f));
            GUILayout.BeginVertical();
            GUILayout.Label("<b>Auto-feed</b>  <size=11><color=#9bb0c8>Minions snack from the Bag (Auto mode)</color></size>", HuntStyles.Text);
            GUILayout.Label($"Each minion eats once every {MinionRules.AutoFeedSeconds:0} s, its favourite first. Never hammers.", HuntStyles.Hint);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            CompanionSettings.AutoFeed = Switch(CompanionSettings.AutoFeed);
            GUILayout.EndHorizontal();
            if (Battle.BattleSettings.Mode != Battle.BattleMode.Auto)
                GUILayout.Label("<color=#ffc864>Works only while the battle mode is Auto (Settings → Game).</color>", HuntStyles.Hint);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rarities:", HuntStyles.Hint, GUILayout.Width(62));
            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
            {
                bool on = CompanionSettings.FeedTier(r);
                bool now = GUILayout.Toggle(on, $"<color={BagPanel.RarityColor(r)}>{r}</color>", RichToggle);
                if (now != on) CompanionSettings.SetFeedTier(r, now);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Eat up to {Mathf.RoundToInt(CompanionSettings.FeedShare * 100)}% of a stack", HuntStyles.Hint, GUILayout.Width(170));
            CompanionSettings.FeedShare = Mathf.Round(GUILayout.HorizontalSlider(CompanionSettings.FeedShare, 0f, 1f) * 20f) / 20f;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        static GUIStyle richToggle;
        static GUIStyle RichToggle => richToggle ??= new GUIStyle(GUI.skin.toggle) { richText = true, fontSize = 11 };

        Companion releasing, feeding, fusing;
        bool catalyst;

        /// <summary>
        /// Feed (loot gives EXP, potions infuse), Fuse in the Transmutation Circle, and change its look.
        /// One minion's feed or fuse list is open at a time. Cheapest food first.
        /// </summary>
        void GrowthRow(Companion c)
        {
            var inv = hook.Inventory;
            if (feeding == c)
            {
                GUILayout.Label("<b>Food</b>", HuntStyles.Text);
                var food = inv.Items.Where(kv => CompanionGrowth.CanFeed(kv.Key))
                    .OrderBy(kv => kv.Key.type == ItemType.Consumable ? 1 : 0).ThenBy(kv => kv.Key.rarity).ToList();
                if (food.Count == 0) GUILayout.Label("Nothing to feed.", HuntStyles.Text);
                foreach (var kv in food)
                {
                    var item = kv.Key;
                    string what = item.type == ItemType.Consumable ? "potion: +stat"
                        : $"+{CompanionGrowth.FeedPoints(item, c)} EXP{(item.element == c.Element && item.element != Elements.Element.None ? " (favourite!)" : "")}";
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    if (GUILayout.Button($"{kv.Value} × <color={BagPanel.RarityColor(item.rarity)}>{item.displayName}</color>  <size=10>{what}</size>", RichButton))
                    {
                        var learned = new List<MinionSkill>();
                        int before = c.Level;
                        var r = CompanionGrowth.Feed(c, item, learned);
                        if (r != FeedResult.CantEat && r != FeedResult.Full) inv.Remove(item, 1);
                        if (learned.Count > 0) hook.Companions.Refresh();
                        if (r != FeedResult.CantEat && r != FeedResult.Full) hook.GetComponent<RaftCompanions>()?.Yum(c, item);
                        hook.Log(r == FeedResult.Infused ? $"{c.Name} drank the {item.displayName}. {c.Stats}"
                            : r == FeedResult.Full ? $"{c.Name} is full: {(item.type == ItemType.Consumable ? "no more potions until it evolves" : "it is max level")}."
                            : r == FeedResult.CantEat ? $"{c.Name} won't eat that."
                            : RaftCompanions.FeedLine(c, item, r, before, learned));
                    }
                    GUILayout.EndHorizontal();
                }
            }

            if (fusing == c)
            {
                GUILayout.Label($"<b>Fuse into {c.Name}</b>", HuntStyles.Text);
                foreach (var other in hook.Companions.All.Where(o => o != c).ToList())
                {
                    var rarity = CompanionGrowth.ResultRarity(c, other);
                    int cost = CompanionGrowth.CatalystCost(c, other);
                    bool canCat = inv.EssenceAtLeast(rarity) >= cost;
                    float chance = CompanionGrowth.FuseChance(c, other, catalyst && canCat);
                    string kind = c.Id == other.Id ? $"same kind → {rarity}" : $"hybrid → {CompanionGrowth.HybridName(c.Name, other.Name)}";
                    var gift = CompanionGrowth.EchoSkill(c, other);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);
                    if (GUILayout.Button($"+ {other.Name} Lv {other.Level} <size=10>({kind}, {chance:P0}{(gift != null ? $", passes {gift}" : "")})</size>", RichButton))
                    {
                        bool useCat = catalyst && canCat;
                        if (useCat) inv.SpendEssence(rarity, cost);
                        string otherName = other.Name;
                        var r = CompanionGrowth.Fuse(c, other, useCat, Random.value);
                        hook.Companions.Release(other);
                        hook.Companions.Refresh();
                        hook.ApplyCompanionBonuses();
                        hook.Log(r == FuseResult.Failed ? $"The circle fizzled: {otherName} slipped away into the sea. {c.Name} is unchanged."
                            : r == FuseResult.Upgraded ? $"{otherName} fused into {c.Name}: now {c.Rarity}! {c.Stats} · {MinionRules.Skills(c)}"
                            : $"{otherName} fused into a hybrid: {c.Name} ({c.Rarity}). {c.Stats} · {MinionRules.Skills(c)}");
                        fusing = null;
                        GUILayout.EndHorizontal();
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(4);
                var anyCost = hook.Companions.All.Where(o => o != c).Select(o => (CompanionGrowth.ResultRarity(c, o), CompanionGrowth.CatalystCost(c, o))).FirstOrDefault();
                catalyst = GUILayout.Toggle(catalyst, $" Use essence as a catalyst (+15%; {anyCost.Item2} {anyCost.Item1}+ essence)");
                GUILayout.EndHorizontal();
            }
        }

        GUIStyle RichButton => HuntStyles.RichButton;
    }
}
