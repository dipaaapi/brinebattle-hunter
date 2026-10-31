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
            float w = Mathf.Min(720, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(600, HuntStyles.H - 120);
            var roster = hook.Companions;
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), $"Minions ({roster.All.Count}/{roster.Capacity})", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Pets); GUILayout.EndArea(); return; }
            page = HuntStyles.Tabs(page, PageLabels);
            if (page == 2) { scroll = GUILayout.BeginScrollView(scroll); DrawChores(); GUILayout.EndScrollView(); GUILayout.EndArea(); return; }

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
            scroll = GUILayout.BeginScrollView(scroll);
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

        // ---- Skill tree (Ed's ask: a visual tree, four branches) ----

        static readonly Color Learned = new Color(0.3f, 0.75f, 0.4f), NextCol = new Color(1f, 0.78f, 0.25f),
            LaterCol = new Color(0.45f, 0.5f, 0.6f), LockedCol = new Color(0.55f, 0.25f, 0.25f);
        GUIStyle node, center, link;

        void Tree(Companion c)
        {
            Header(c);
            node ??= new GUIStyle(GUI.skin.box) { richText = true, wordWrap = true, fontSize = 12, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(4, 4, 6, 6) };
            center ??= new GUIStyle(HuntStyles.Text) { alignment = TextAnchor.MiddleCenter };
            link ??= new GUIStyle(HuntStyles.Hint) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            foreach (var f in MinionRules.Families)
            {
                GUILayout.BeginVertical(GUILayout.Width(118));
                GUILayout.Label($"<b>{f}</b>", center);
                bool first = true;
                foreach (var s in MinionRules.In(f))
                {
                    if (!first) GUILayout.Label("│", link, GUILayout.Height(12));
                    first = false;
                    var state = MinionRules.StateOf(c, s, out int at);
                    string mark = state == MinionRules.NodeState.Learned ? "✔"
                        : state == MinionRules.NodeState.Next ? $"Lv {at}"
                        : state == MinionRules.NodeState.Locked ? $"🔒 {MinionRules.Gate(s) ?? "no room"}"
                        : MinionRules.Gate(s) ?? "later";
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = state == MinionRules.NodeState.Learned ? Learned : state == MinionRules.NodeState.Next ? NextCol
                        : state == MinionRules.NodeState.Locked ? LockedCol : LaterCol;
                    GUILayout.Box($"<b>{s}</b>\n<size=10>{mark}</size>", node, GUILayout.Height(46), GUILayout.ExpandWidth(true));
                    GUI.backgroundColor = old;
                }
                GUILayout.EndVertical();
                GUILayout.Space(6);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            GUILayout.Label("<color=#7be08a>■</color> learned  <color=#ffc840>■</color> next  <color=#8890a0>■</color> later  <color=#b04040>■</color> locked", HuntStyles.Hint);
        }

        // ---- Chores: one switch per auto job ----

        void DrawChores()
        {
            var roster = hook.Companions;
            bool any = false;
            void Job(MinionSkill skill, System.Action draw)
            {
                var who = roster.Best(skill);
                if (who == null) return;
                any = true;
                GUILayout.BeginVertical(HuntStyles.Card);
                GUILayout.Label($"<b>{skill}</b>  <size=11>{who.Name} (Lv {who.Level}) · {MinionActivity.Now(who)}</size>", HuntStyles.Text);
                draw();
                GUILayout.EndVertical();
            }
            Job(MinionSkill.Looter, () => CompanionSettings.FamiliarLoots = GUILayout.Toggle(CompanionSettings.FamiliarLoots, " Fetch dead drifts and floating loot"));
            Job(MinionSkill.Crewmate, () => CompanionSettings.CrewBuilds = GUILayout.Toggle(CompanionSettings.CrewBuilds, " Install modules and build parts from the bag"));
            Job(MinionSkill.Mender, () => CompanionSettings.Mends = GUILayout.Toggle(CompanionSettings.Mends, " Mend the harpoon and repair modules"));
            Job(MinionSkill.Builder, () =>
            {
                CompanionSettings.BuilderCrafts = GUILayout.Toggle(CompanionSettings.BuilderCrafts, " Craft raft parts, modules and vehicle parts");
                CompanionSettings.BuilderLevelsUp = GUILayout.Toggle(CompanionSettings.BuilderLevelsUp, " Level the raft up when ready (after the type is picked)");
            });
            Job(MinionSkill.Alchemist, () =>
            {
                CompanionSettings.AlchemistCrafts = GUILayout.Toggle(CompanionSettings.AlchemistCrafts, " Craft potions, oils and treats");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Keep {CompanionSettings.MysticKeep} of each", HuntStyles.Text, GUILayout.Width(150));
                CompanionSettings.MysticKeep = Mathf.RoundToInt(GUILayout.HorizontalSlider(CompanionSettings.MysticKeep, 0, 5));
                GUILayout.EndHorizontal();
            });
            Job(MinionSkill.Smith, () => CompanionSettings.MysticRefines = GUILayout.Toggle(CompanionSettings.MysticRefines, " Refine equipped gear with essence"));
            Job(MinionSkill.Tinker, () => CompanionSettings.TinkerCrafts = GUILayout.Toggle(CompanionSettings.TinkerCrafts, " Craft and equip better gear"));
            Job(MinionSkill.Salvager, () =>
            {
                CompanionSettings.SalvagerSorts = GUILayout.Toggle(CompanionSettings.SalvagerSorts, " Take junk apart");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Up to <color={BagPanel.RarityColor(CompanionSettings.TakeApartUpTo)}>{CompanionSettings.TakeApartUpTo}</color>", HuntStyles.Text, GUILayout.Width(150));
                CompanionSettings.TakeApartUpTo = (Rarity)Mathf.RoundToInt(GUILayout.HorizontalSlider((int)CompanionSettings.TakeApartUpTo, 0, (int)Rarity.Mythic));
                GUILayout.EndHorizontal();
            });
            Job(MinionSkill.Glutton, () => CompanionSettings.GluttonEats = GUILayout.Toggle(CompanionSettings.GluttonEats, " Eat the rest of the junk"));
            if (!any) GUILayout.Label("No minion has a job skill yet.", HuntStyles.Text);
        }

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
