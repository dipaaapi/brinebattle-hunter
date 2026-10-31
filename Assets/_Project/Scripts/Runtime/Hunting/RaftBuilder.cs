using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Phase 5 build mode (R): place raft parts from the bag on open cells, take parts off,
    /// and spend Monster Essence to raise the raft tier. Placeholder IMGUI + ghost tiles.
    /// </summary>
    public class RaftBuilder : MonoBehaviour
    {

        public Raft raft;
        public HookController hook;
        public RaftModules modules;
        public List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public bool Active => WorldPause.Open == Menu.Build;

        Rect panelRect;
        Vector2 panelScroll;
        RecipeCategory tab;
        string message = "";

        public void Toggle()
        {
            WorldPause.Toggle(Menu.Build);
            message = "";
            page = Page.Build;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Toggle();
            if (kb != null && Active && kb.escapeKey.wasPressedThisFrame) Toggle();
            if (!Active || raft == null) return;

            // Parts are placed in the Shipyard now (Ed's ask: the Build menu covered the raft at sea).
        }

        /// <summary>
        /// What the next level needs, each ✔ or ✘ with have/need. How-to text lives only in
        /// Settings → FAQ (Ed's call).
        /// </summary>
        void DrawRequirements()
        {
            var g = raft.Grid;
            var inv = hook.Inventory;
            int kills = g.KillsNeeded, mats = g.MaterialsNeeded, cost = g.EssenceNeeded;
            var (guarded, _) = g.UpstreamModules();
            var hammer = RaftRules.BestHammer(inv);
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.Label($"<b>{EvolutionRules.Label(g.Chosen, g.Level + 1)} needs</b>", HuntStyles.Text);
            Step(g.Kills >= kills, $"Monsters beaten: {Mathf.Min(g.Kills, kills)}/{kills}");
            Step(inv.MaterialCount() >= mats, $"Monster materials: {Mathf.Min(inv.MaterialCount(), mats)}/{mats}");
            Step(inv.EssenceAtLeast(g.Tier) >= cost, $"{g.Tier}+ essence: {Mathf.Min(inv.EssenceAtLeast(g.Tier), cost)}/{cost}");
            Step(guarded >= g.ModulesNeeded, $"Modules on the left edge: {Mathf.Min(guarded, g.ModulesNeeded)}/{g.ModulesNeeded}");
            Step(RaftRules.HasHammer(inv, g.Tier), $"{g.Tier} hammer or better: {(hammer != null ? hammer.displayName : "none")}");
            if (EvolutionRules.BossGate(g.Chosen, g.Level) && g.BossesNeeded) Step(g.BossBeaten(g.Tier), BossLine(g.Tier));
            int part = EvolutionRules.PartNeeded(g.Chosen, g.Level);
            if (part > 0) Step(g.HasVessel(part), $"{VesselRules.PartName(g.Chosen, part)} on the ship");
            GUILayout.EndVertical();
        }

        DriftSpawner spawner;

        /// <summary>"Beat the Uncommon boss, Tidecaller Eel (comes once enough monsters are beaten)".</summary>
        string BossLine(Rarity level)
        {
            if (spawner == null) spawner = FindFirstObjectByType<DriftSpawner>();
            var boss = spawner != null ? spawner.BossFor(level) : null;
            var g = raft.Grid;
            string name = boss != null ? $", {boss.displayName}" : "";
            string when = g.BossBeaten(level) ? "" : g.Kills >= g.KillsNeeded ? " (it's out there now)" : " (comes once enough monsters are beaten)";
            return $"Beat the {level} boss{name}{when}";
        }

        /// <summary>
        /// The ship's parts (Ed's ask: ships have more parts to build): five, opening at ship Lv 20, 40, 60, 80
        /// and 99, made at the Workbench (Raft → Vehicle) and put on here. A ship can't level past a part's level
        /// until that part is on. No leaving the sea any more: the goal is fame.
        /// </summary>
        void DrawVehicle()
        {
            var g = raft.Grid;
            var kind = g.Chosen;
            if (kind == RaftKind.Plain) return;
            var inv = hook.Inventory;
            GUILayout.Space(6);
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.Label($"<b>{EvolutionRules.LineName(kind)} parts</b>  <size=12>{g.VesselCount}/{VesselRules.Parts} on</size>", HuntStyles.Text);
            for (int slot = 1; slot <= VesselRules.Parts; slot++)
            {
                int lv = VesselRules.UnlockLevel(slot);
                var item = recipes.Where(r => r != null && r.outputs.Count > 0 && r.outputs[0].item != null)
                    .Select(r => r.outputs[0].item).FirstOrDefault(i => i.type == ItemType.Vessel && i.raftKind == kind && i.vesselSlot == slot);
                string name = VesselRules.PartName(kind, slot);
                GUILayout.BeginHorizontal();
                if (g.HasVessel(slot)) GUILayout.Label($"<color={HuntStyles.GoodHex}>✔ {name}</color>", HuntStyles.Text);
                else if (g.Level < lv) GUILayout.Label($"<color=#999999>🔒 {name} · opens at ship Lv {lv}</color>", HuntStyles.Text);
                else if (item != null && inv.Count(item) > 0)
                {
                    GUILayout.Label($"<b>{name}</b> · in the Bag", HuntStyles.Text);
                    string why = g.VesselBlocker(item, inv);
                    GUI.enabled = why == null;
                    if (HuntStyles.ColorButton(why == null ? "Put it on" : why, HuntStyles.Go, GUILayout.Width(150)) && g.InstallVessel(item, inv))
                        hook.Log($"The {name} is on your {EvolutionRules.LineName(kind)} ({g.VesselCount}/{VesselRules.Parts}).");
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label($"<color={HuntStyles.BadHex}>✘</color> <b>{name}</b> · not made yet", HuntStyles.Text);
                    if (GUILayout.Button("Make it", GUILayout.Width(80), GUILayout.Height(26))) OpenBench(RecipeSub.Vehicle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        static void Step(bool done, string title) =>
            GUILayout.Label(done ? $"<color={HuntStyles.GoodHex}>✔ {title}</color>" : $"<color={HuntStyles.BadHex}>✘</color> <b>{title}</b>", HuntStyles.Text);

        /// <summary>
        /// Ed's ask: right after the type is picked, parts of the other types on the raft go back to the
        /// bag and basic planks take their place.
        /// </summary>
        void ResetForType()
        {
            var back = raft.Grid.ResetOffType(raft.starterPart, hook.Inventory);
            if (back.Count == 0) return;
            var names = back.GroupBy(p => p.displayName).Select(x => x.Count() > 1 ? $"{x.Count()}× {x.Key}" : x.Key);
            hook.Log($"Parts for other raft types came off and are back in the bag ({string.Join(", ", names)}); basic planks took their place.");
        }

        // Ed's ask: Level up and the Tree are one page, the raft skill tree.
        enum Page { Build, Workbench, Modules, Tree }
        RaftKind path = RaftKind.Sea;
        /// <summary>The type tab shown in the raft tree; follows the picked type once there is one.</summary>
        RaftKind treeTab = RaftKind.Plain;
        static readonly string[] PageLabels = { "Build", "Workbench", "Modules", "Raft tree" };
        Page page;
        bool readyOnly;
        readonly Dictionary<RecipeCategory, RecipeSub> subTab = new Dictionary<RecipeCategory, RecipeSub>();

        /// <summary>Opens Build on a Workbench sub-tab (the Quest tab's Go button and the modules page use it).</summary>
        public void OpenBench(RecipeSub sub)
        {
            if (!Active) Toggle();
            page = Page.Workbench;
            tab = CraftRules.CategoryOf(sub);
            subTab[tab] = sub;
            panelScroll = Vector2.zero;
        }

        /// <summary>Opens the raft skill tree page (the HUD and the level-up log point here).</summary>
        public void OpenTree() { if (!Active) Toggle(); page = Page.Tree; panelScroll = Vector2.zero; }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!Active || raft == null) return;
            // Wide enough for every tab and sub-tab (Ed: no sideways scrolling).
            float w = Mathf.Min(640, HuntStyles.W - HookController.HudRight - 22), h = HuntStyles.H - 120;
            panelRect = new Rect(HuntStyles.W - w - 12, 20, w, h);
            GUI.depth = -5;
            var g = raft.Grid;
            GUILayout.BeginArea(panelRect, $"Build · {g.LevelName} ({g.FormName})", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { Toggle(); GUILayout.EndArea(); return; }

            // Owner idea U-25 (Ed: "hindi user friendly"): four short pages instead of one long scroll.
            var labels = (string[])PageLabels.Clone();
            int ready = recipes.Count(r => r != null && hook.Knows(r) && CraftRules.CanCraft(hook.Inventory, r));
            if (ready > 0) labels[1] = $"Workbench ({ready})";
            if ((!g.AtMax && g.UpgradeBlocker(hook.Inventory) == null) || g.CanChoose) labels[3] = "Raft tree ✔";
            var picked = (Page)HuntStyles.Tabs((int)page, labels);
            if (picked != page) { page = picked; panelScroll = Vector2.zero; }

            // Vertical scrolling only: content is kept to the panel's width.
            panelScroll = GUILayout.BeginScrollView(panelScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            GUILayout.BeginVertical(GUILayout.MaxWidth(w - 40));
            switch (page)
            {
                case Page.Build: DrawBuild(); break;
                case Page.Workbench: DrawWorkbench(w); break;
                case Page.Modules: DrawModules(); break;
                default: DrawTree(); break;
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message, HuntStyles.Text);
            GUILayout.EndArea();
        }

        void DrawBuild()
        {
            var g = raft.Grid;
            GUILayout.Label($"Size {g.Size} · {g.Count} parts · {g.MaxHp} HP", HuntStyles.Text);
            int next = 0;
            for (int n = 1; n <= RaftRules.StarterParts + RaftRules.PartsPerSize; n++)
                if (RaftRules.SizeFor(g.Count + n) > g.Size) { next = n; break; }
            if (next > 0 && g.Size < 10) GUILayout.Label($"{next} more part(s) to reach size {g.Size + 1}.", HuntStyles.Hint);

            // Phase 8: the raft's type and what it gives, with how many parts of each kind are on it.
            var type = g.Type;
            var kinds = g.Parts.Values.Where(p => p.type == ItemType.Raft).GroupBy(p => p.raftKind).ToDictionary(x => x.Key, x => x.Count());
            int Kind(RaftKind k) => kinds.TryGetValue(k, out var n) ? n : 0;
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.Label($"<b>{g.LevelName}</b> · {g.FormName}  <size=12>{(g.IsShip ? type.Bonus.ToString() : "ship line at raft Lv 50")}</size>", HuntStyles.Text);
            if (g.IsShip) GUILayout.Label($"<size=12>{g.Chosen} parts {Kind(g.Chosen)} · plain {Kind(RaftKind.Plain)}</size>", HuntStyles.Text);
            if (hook.Gorged.Count > 0)
                GUILayout.Label($"<size=12><color={HuntStyles.BadHex}>{hook.Gorged.Entries.Sum(e => e.Parts.Count)} part(s) inside gorged monsters</color></size>", HuntStyles.Text);
            GUILayout.EndVertical();

            var hammerNow = RaftRules.BestHammer(hook.Inventory);
            bool hasHammer = RaftRules.HasHammer(hook.Inventory, raft.Tier);
            GUILayout.BeginHorizontal(HuntStyles.Card);
            GUILayout.Label(HuntIcons.Hammer, GUILayout.Width(28), GUILayout.Height(28));
            GUILayout.Label(hasHammer ? $"Hammer: <b>{hammerNow.displayName}</b>"
                : $"<color={HuntStyles.BadHex}>Hammer: {(hammerNow != null ? hammerNow.displayName + " is too weak" : "none")}</color>", HuntStyles.Text);
            if (!hasHammer && GUILayout.Button("Make one", GUILayout.Width(90), GUILayout.Height(28))) OpenBench(RecipeSub.Hammers);
            GUILayout.EndHorizontal();

            // Ed's ask: the raft is customised in the Shipyard, a separate view where nothing covers it.
            if (HuntStyles.ColorButton("⚓ Open the Shipyard (Y)", HuntStyles.Go)) { WorldPause.Close(Menu.Build); Shipyard.Toggle(); }
            if (hook.Inventory.Items.Any(kv => (kv.Key.type == ItemType.Raft || kv.Key.type == ItemType.Module) && !SalvageRules.IsCompatible(kv.Key.type, kv.Key.rarity, g.Tier)))
                if (GUILayout.Button("Make stronger parts (Reinforce)", GUILayout.Height(28))) OpenBench(RecipeSub.Parts);
        }

        /// <summary>
        /// Recipe cards (Ed: the list didn't say how many are missing). Each ingredient shows have/need in
        /// green or red, ready recipes come first, and a tab's label counts how many are ready.
        /// </summary>
        void DrawWorkbench(float w)
        {
            var inv = hook.Inventory;
            int Ready(System.Func<RecipeDefinition, bool> where) =>
                recipes.Count(r => r != null && where(r) && hook.Knows(r) && CraftRules.CanCraft(inv, r));
            // Ed's ask: Gear, Player (potions + oils), Pet, Raft; dismantling lives in the Salvage Bin.
            var cats = new[] { RecipeCategory.Gear, RecipeCategory.Player, RecipeCategory.Pet, RecipeCategory.Raft };
            if (tab == RecipeCategory.Dismantle) tab = RecipeCategory.Gear;
            var labels = cats.Select(c => { int n = Ready(r => CraftRules.CategoryOf(r) == c); return n > 0 ? $"{CraftRules.CategoryLabels[(int)c]} ({n})" : CraftRules.CategoryLabels[(int)c]; }).ToArray();
            tab = cats[HuntStyles.Tabs(System.Array.IndexOf(cats, tab), labels)];
            var subs = CraftRules.SubsOf(tab);
            if (!subTab.TryGetValue(tab, out var sub) || System.Array.IndexOf(subs, sub) < 0) sub = subs[0];
            var subLabels = subs.Select(x => { int n = Ready(r => CraftRules.SubOf(r) == x); return n > 0 ? $"{CraftRules.Label(x)} ({n})" : CraftRules.Label(x); }).ToArray();
            sub = subTab[tab] = subs[GUILayout.Toolbar(System.Array.IndexOf(subs, sub), subLabels, GUILayout.Height(26))];
            GUILayout.BeginHorizontal();
            readyOnly = GUILayout.Toggle(readyOnly, " Show only what I can make now");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Dismantle → Salvage Bin", GUILayout.Width(170))) SalvageBin.Toggle();
            GUILayout.EndHorizontal();

            // Recipes above the hunter's Alchemy Mastery stay "???" at the end (Idea Round 3, U-14).
            var inTab = recipes.Where(r => r != null && CraftRules.SubOf(r) == sub).ToList();
            var shown = CraftRules.ByReadiness(inv, inTab.Where(r => hook.Knows(r)));
            if (readyOnly) shown = shown.Where(r => CraftRules.CanCraft(inv, r)).ToList();
            if (shown.Count == 0) GUILayout.Label(readyOnly ? "Nothing ready here yet." : "No recipes here yet.", HuntStyles.Text);
            foreach (var r in shown) RecipeCard(r);
            if (!readyOnly)
                foreach (var r in inTab.Where(r => !hook.Knows(r)).OrderBy(AlchemyMastery.Needed))
                {
                    GUILayout.BeginVertical(HuntStyles.Card);
                    GUILayout.Label($"<b>???</b>  <color={HuntStyles.BadHex}>{LockReason(hook, r)}</color>", HuntStyles.Text);
                    GUILayout.EndVertical();
                }
        }

        /// <summary>
        /// The raft tree (Ed's ask: Level up and Tree in one place). Top: the evolution road (raft Lv 1-50 to a
        /// simple boat, then the picked ship Lv 1-100, then the next evolution "Coming soon"). At raft Lv 50 the
        /// ship line is picked once: Sea level (STR), Aerial (INT) or Undersea (DEX). Then what the next level
        /// needs with the level-up button, the ship's parts, and the line's branch of materials and parts.
        /// </summary>
        void DrawTree()
        {
            var g = raft.Grid;
            var chosen = g.Chosen;
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.Label($"<b>{g.LevelName}</b> · {g.FormName} · {g.Tier}" + (chosen != RaftKind.Plain ? $"  <size=12>{g.Type.Bonus}</size>" : ""), HuntStyles.Text);
            EvolutionRoad(g);
            GUILayout.EndVertical();

            // The ship pick: once, at raft Lv 50 with the Rare boss beaten.
            if (chosen == RaftKind.Plain && g.Level >= EvolutionRules.RaftMax)
            {
                GUILayout.Label("<b>Pick your ship line</b>  <size=12>once only; the ship starts again at Lv 1 and evolves to Lv 100</size>", HuntStyles.Text);
                GUILayout.BeginHorizontal();
                if (path == RaftKind.Plain) path = RaftTree.Kinds[0];
                foreach (var kind in RaftTree.Kinds)
                {
                    GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(150));
                    GUILayout.Label($"<b>{EvolutionRules.LineName(kind)}</b> · <color=#ffd866>{EvolutionRules.Stat(kind)}</color>\n<size=11>{RaftTree.Look(kind)}</size>\n<size=11>{EvolutionRules.Bonus(kind, 1)} (grows with its level)</size>", HuntStyles.Text);
                    var old = GUI.backgroundColor;
                    if (path == kind) GUI.backgroundColor = HuntStyles.Go;
                    if (GUILayout.Button(path == kind ? "▶ Picked" : "Pick", GUILayout.Height(26))) path = kind;
                    GUI.backgroundColor = old;
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                string why = g.ChooseBlocker();
                if (why != null) Step(false, why);
                else if (HuntStyles.ColorButton($"Make it a {EvolutionRules.LineName(path)} ({EvolutionRules.Stat(path)})", HuntStyles.Go) && g.Choose(path))
                {
                    treeTab = path;
                    hook.Log($"Your boat is now a {EvolutionRules.LineName(path)}: {EvolutionRules.FormName(path, 1)}, Lv 1. It evolves up to Lv {EvolutionRules.ShipMax}.");
                    ResetForType();
                }
                return;
            }

            // Level up.
            if (g.AtMax) GUILayout.Label($"<b>Lv {g.MaxLevel}.</b> The next evolution: <color=#ffd866>{EvolutionRules.NextEvolution}</color>.", HuntStyles.Text);
            else
            {
                DrawRequirements();
                string blocker = g.UpgradeBlocker(hook.Inventory);
                if (blocker == null)
                {
                    if (HuntStyles.ColorButton($"Level up → {EvolutionRules.Label(chosen, g.Level + 1)}", HuntStyles.Go) && g.TryUpgrade(hook.Inventory))
                        hook.Log($"{g.LevelName}! {g.FormName}, max HP {g.MaxHp}.");
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("Not ready yet (see ✘)", GUILayout.Height(32));
                    GUI.enabled = true;
                }
            }
            DrawVehicle();
            if (chosen == RaftKind.Plain) return;

            // The picked line's branch of materials and parts, by tier.
            GUILayout.Space(6);
            treeTab = chosen;
            DrawBranch(chosen);
        }

        /// <summary>
        /// The evolution road: the raft's forms (Lv 1, 10, 20, 30, 40, 50 boat), or the ship's (Lv 1, 25, 50, 75,
        /// 100) and a locked "Coming soon" stop, filled up to the current level.
        /// </summary>
        void EvolutionRoad(RaftGrid g)
        {
            bool ship = g.IsShip;
            int[] stops = ship ? new[] { 1, 25, 50, 75, 100 } : new[] { 1, 10, 20, 30, 40, 50 };
            int count = stops.Length + (ship ? 1 : 0);
            var road = GUILayoutUtility.GetRect(200, 70, GUILayout.ExpandWidth(true));
            float step = (road.width - 60) / (count - 1), y = road.y + 22;
            var color = KindColor(g.Chosen);
            for (int i = 0; i < count - 1; i++)
            {
                bool done = i + 1 < stops.Length && g.Level >= stops[i + 1];
                GUI.DrawTexture(new Rect(road.x + 30 + i * step, y - 2, step, 4), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, done ? color : new Color(0.2f, 0.27f, 0.35f), 0, 2);
            }
            for (int i = 0; i < count; i++)
            {
                float x = road.x + 30 + i * step;
                bool soon = i >= stops.Length;
                int lv = soon ? 0 : stops[i];
                bool open = !soon && g.Level >= lv;
                bool here = !soon && open && (i + 1 >= stops.Length || g.Level < stops[i + 1]);
                float r = here ? 15 : 12;
                if (here) GUI.DrawTexture(new Rect(x - r - 4, y - r - 4, 2 * r + 8, 2 * r + 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.81f, 0.29f, 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f)), 0, r + 4);
                GUI.DrawTexture(new Rect(x - r, y - r, 2 * r, 2 * r), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, open ? color : new Color(0.16f, 0.22f, 0.3f), 0, r);
                GUI.Label(new Rect(x - r, y - r, 2 * r, 2 * r), soon ? "<color=#7a8899><b>?</b></color>" : open ? $"<b>{lv}</b>" : $"<color=#7a8899><b>{lv}</b></color>", RoadNum);
                string name = soon ? EvolutionRules.NextEvolution : EvolutionRules.FormName(g.Chosen, lv);
                GUI.Label(new Rect(x - 46, y + 17, 92, 30), here ? $"<color=#ffd866><b>{name}</b></color>" : open ? $"<color=#9fe89f>{name}</color>" : $"<color=#7a8899>{name}</color>", RoadNum);
            }
        }

        /// <summary>
        /// One type's branch (Ed: the old list was cut off and hard to follow): a road of six level stops
        /// along the top (done, your raft, locked), then a card per level with what it lets you make, two a row.
        /// </summary>
        void DrawBranch(RaftKind kind)
        {
            var g = raft.Grid;
            var chosen = g.Chosen;
            if (chosen != RaftKind.Plain && kind != chosen)
            {
                // Not the raft's type: locked, and it doesn't branch out.
                GUILayout.BeginVertical(HuntStyles.Card);
                GUILayout.Label($"<color=#999999><b>{kind}</b> · {RaftTree.Look(kind)}</color>", HuntStyles.Heading);
                GUILayout.Label($"<color=#888888>Your ship is a {EvolutionRules.LineName(chosen)}. This branch stays locked.</color>", HuntStyles.Text);
                GUILayout.EndVertical();
                return;
            }
            GUILayout.Label($"<b>{kind}</b> · {RaftTree.Look(kind)}", HuntStyles.Text);
            GUILayout.Label(RaftTypeRules.BonusOf(kind).ToString(), HuntStyles.Hint);

            // The road: six tier stops (Common … Mythic) joined by a line, filled up to the ship's tier.
            int here = (int)g.Tier + 1;
            var road = GUILayoutUtility.GetRect(200, 64, GUILayout.ExpandWidth(true));
            float step = (road.width - 40) / 5f, y = road.y + 22;
            var kindColor = KindColor(kind);
            for (int lv = 1; lv <= 6; lv++)
            {
                float x = road.x + 20 + (lv - 1) * step;
                if (lv < 6)
                {
                    bool doneLink = RaftTree.Unlocked(g, kind, lv + 1);
                    GUI.DrawTexture(new Rect(x, y - 2, step, 4), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, doneLink ? kindColor : new Color(0.2f, 0.27f, 0.35f), 0, 2);
                }
            }
            for (int lv = 1; lv <= 6; lv++)
            {
                float x = road.x + 20 + (lv - 1) * step;
                bool open = RaftTree.Unlocked(g, kind, lv);
                float r = lv == here ? 15 : 12;
                if (lv == here) GUI.DrawTexture(new Rect(x - r - 4, y - r - 4, 2 * r + 8, 2 * r + 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.81f, 0.29f, 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f)), 0, r + 4);
                GUI.DrawTexture(new Rect(x - r, y - r, 2 * r, 2 * r), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, open ? kindColor : new Color(0.16f, 0.22f, 0.3f), 0, r);
                GUI.Label(new Rect(x - r, y - r, 2 * r, 2 * r), open ? $"<b>{lv}</b>" : $"<color=#7a8899><b>{lv}</b></color>", RoadNum);
                GUI.Label(new Rect(x - 40, y + 18, 80, 18), lv == here ? "<color=#ffd866><b>your ship</b></color>" : open ? $"<color=#9fe89f>{(Rarity)(lv - 1)}</color>" : $"<color=#7a8899>{(Rarity)(lv - 1)}</color>", RoadNum);
            }

            // A card per level, two a row.
            var nodes = recipes.Where(r => r != null && r.kind != RecipeKind.Dismantle && RaftTree.KindOf(r) == kind).ToList();
            float cardW = (panelRect.width - 70) / 2f;
            for (int lv = 1; lv <= 6; lv += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = lv; k <= lv + 1; k++)
                {
                    bool open = RaftTree.Unlocked(g, kind, k);
                    var made = nodes.Where(r => RaftTree.LevelOf(r.outputs[0].item) == k).Select(r => r.outputs[0].item.displayName).Distinct().ToList();
                    GUILayout.BeginVertical(HuntStyles.Card, GUILayout.Width(cardW), GUILayout.MinHeight(84));
                    string head = open ? $"<color={HuntStyles.GoodHex}><b>{(Rarity)(k - 1)}</b></color>" : $"<color=#8a96a6><b>{(Rarity)(k - 1)}</b></color>";
                    string tag = k == here ? "  <size=11><color=#ffd866>your ship</color></size>" : open ? "" : "  <size=11><color=#8a96a6>locked</color></size>";
                    GUILayout.Label(head + tag, HuntStyles.Text);
                    foreach (var m in made)
                        GUILayout.Label(open ? $"<size=12>• {m}</size>" : $"<size=12><color=#7a8899>• {m}</color></size>", HuntStyles.Text);
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }

        static Color KindColor(RaftKind k) =>
            k == RaftKind.Sea ? new Color(0.6f, 0.55f, 0.5f) : k == RaftKind.Aerial ? new Color(0.45f, 0.7f, 1f) : k == RaftKind.Undersea ? new Color(0.85f, 0.45f, 0.35f) : new Color(0.5f, 0.75f, 0.55f);

        static GUIStyle roadNum;
        static GUIStyle RoadNum => roadNum ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 12 };

        /// <summary>Why a recipe is still "???": Alchemy Mastery, or the raft tree branch (U-31).</summary>
        public static string LockReason(HookController hook, RecipeDefinition r)
        {
            if (!hook.Mastery.Knows(r)) return $"Alchemy Mastery Lv {AlchemyMastery.Needed(r)}";
            var made = r.outputs.Count > 0 ? r.outputs[0].item : null;
            if (ShapeRules.IsKit(made)) return ShapeRules.Lock(made.shape);
            var kind = RaftTree.KindOf(r);
            int lv = RaftTree.LevelOf(r.outputs[0].item);
            var chosen = hook.raft != null ? hook.raft.Grid.Chosen : RaftKind.Plain;
            return chosen != RaftKind.Plain && chosen != kind ? $"Raft tree: only for {kind} rafts (yours is {chosen})"
                : $"Raft tree: {kind} Lv {lv} (pick {kind} and level the raft up)";
        }

        /// <summary>One recipe with have/need per ingredient and a Make button (also used by the Salvage Bin).</summary>
        public void RecipeCard(RecipeDefinition r)
        {
            var inv = hook.Inventory;
            bool ok = CraftRules.CanCraft(inv, r);
            string verb = r.kind == RecipeKind.Upgrade ? "Upgrade" : r.kind == RecipeKind.Dismantle ? "Dismantle" : "Make";
            var main = r.outputs.FirstOrDefault(o => o.item != null);
            GUILayout.BeginVertical(HuntStyles.Card);
            string title = main == null ? r.displayName
                : $"{(main.count > 1 ? main.count + " × " : "")}<color={BagPanel.RarityColor(main.item.rarity)}>{main.item.displayName}</color>";
            if (r.kind == RecipeKind.Dismantle) title = "Take apart → " + CraftRules.Describe(r.outputs);
            GUILayout.Label($"<b>{title}</b>", HuntStyles.Heading);
            if (main != null && r.kind != RecipeKind.Dismantle && !string.IsNullOrEmpty(main.item.description))
                GUILayout.Label(main.item.description, HuntStyles.Hint);

            foreach (var i in r.inputs)
            {
                if (i.item == null) continue;
                int have = inv.Count(i.item);
                bool enough = have >= i.count;
                GUILayout.Label($"{(enough ? $"<color={HuntStyles.GoodHex}>✔</color>" : $"<color={HuntStyles.BadHex}>✘</color>")} {HuntStyles.Count(have, i.count)}  {i.item.displayName}"
                    + (enough ? "" : $"  <color={HuntStyles.BadHex}>need {i.count - have} more</color>"), HuntStyles.Text);
            }

            // Shaped module kits are alchemy (Ed's ask: the hunter is an ordinary person; minions do it).
            var maker = main != null && ShapeRules.IsKit(main.item) ? MinionMaker() : null;
            if (main != null && ShapeRules.IsKit(main.item) && maker == null)
            {
                GUI.enabled = false;
                GUILayout.Button("Needs a Builder or Alchemist minion", GUILayout.Height(28));
                GUI.enabled = true;
            }
            else if (ok)
            {
                if (HuntStyles.ColorButton(maker != null ? $"{maker.Name} makes it!" : $"{verb}!", HuntStyles.Go)) Craft(r, verb);
            }
            else
            {
                int missing = CraftRules.Missing(inv, r);
                GUI.enabled = false;
                GUILayout.Button($"Missing {missing} item{(missing == 1 ? "" : "s")}", GUILayout.Height(28));
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
        }

        /// <summary>The minion that does alchemy for shaped kits: a Builder, else an Alchemist; null if none.</summary>
        Companions.Companion MinionMaker() => hook.Companions.Best(Companions.MinionSkill.Builder) ?? hook.Companions.Best(Companions.MinionSkill.Alchemist);

        void Craft(RecipeDefinition r, string verb)
        {
            CraftRules.Craft(hook.Inventory, r);
            if (r.kind != RecipeKind.Dismantle) { hook.Mastery.Add(AlchemyMastery.CraftPoints); hook.Fame.Craft(hook.Voyage.Difficulty); }
            message = $"{verb}: got {CraftRules.Describe(r.outputs)}.";
            // Taking apart goes through the Salvage Bin (Ed's ask); making pops out of the storage box.
            if (r.kind == RecipeKind.Dismantle) SalvageBin.Instance?.Took($"{CraftRules.Describe(r.inputs)} → {CraftRules.Describe(r.outputs)}", 3, new Color(0.8f, 0.45f, 1f));
            else StorageBox.Instance?.Crafted(CraftRules.Describe(r.outputs));
            // An Alchemist minion (Phase 7, U-39) sometimes makes one more of a gear, potion or oil.
            var mystic = hook.Companions.Best(Companions.MinionSkill.Alchemist);
            var cat = CraftRules.CategoryOf(r);
            // The Alchemist's incantation circle shows for every craft you make while it is aboard (it plays once the Workbench closes).
            if (mystic != null && r.kind != RecipeKind.Dismantle && hook.TryGetComponent<RaftCompanions>(out var circle)) circle.Incantation(mystic);
            if (mystic != null && r.kind == RecipeKind.Craft && cat != RecipeCategory.Raft && cat != RecipeCategory.Dismantle && cat != RecipeCategory.Pet
                && Random.value < Companions.CompanionRules.MysticExtraChance(mystic.Rank))
            {
                foreach (var o in r.outputs) hook.Inventory.Add(o.item, o.count);
                message += $" {mystic.Name} (Alchemist) made an extra one!";
            }
            hook.Log(message);
        }

        void DrawModules()
        {
            var g = raft.Grid;
            var (guarded, rows) = g.UpstreamModules();
            GUILayout.Label($"Left edge: {HuntStyles.Count(guarded, g.ModulesNeeded)} rows guarded (of {rows})", HuntStyles.Text);
            // Layout bonuses (C-18): show what's active and how to get them.
            int lined = g.Parts.Keys.Count(c => SetBonusRules.ModuleReach(g.Parts, c) > 0f);
            int cozy = g.Parts.Keys.Count(c => SetBonusRules.Cozy(g.Parts, c));
            GUILayout.Label($"<size=11>Layout bonuses: {lined} module(s) in a line of {SetBonusRules.LineLength}+ (+{SetBonusRules.LineReach:0} m reach), "
                + $"{cozy} cozy habitat(s) (planks on all 4 sides: double minion healing).</size>", HuntStyles.Text);
            if (modules == null || modules.Slots.Count == 0)
            {
                GUILayout.Label("No modules on the raft yet.", HuntStyles.Text);
                if (GUILayout.Button("Make a Drift Net", GUILayout.Height(30))) OpenBench(RecipeSub.Modules);
                return;
            }
            foreach (var kv in modules.Slots)
            {
                var s = kv.Value;
                GUILayout.BeginVertical(HuntStyles.Card);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{s.Item.displayName}</b>  <size=11>{kv.Key}</size>", HuntStyles.Text);
                GUI.enabled = !s.Durability.Full;
                if (GUILayout.Button($"Repair ({RaftModules.CostText(s)})", GUILayout.Width(180), GUILayout.Height(26))) message = modules.Repair(s);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                float fill = s.Durability.Max > 0 ? (float)s.Durability.Current / s.Durability.Max : 0f;
                HuntStyles.Bar(fill, s.Durability.Broken ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.4f, 0.8f, 0.5f));
                if (s.Durability.Broken) GUILayout.Label($"<color={HuntStyles.BadHex}>Broken</color>", HuntStyles.Hint);
                GUILayout.EndVertical();
            }
            if (hook.Companions.Best(Companions.MinionSkill.Mender) != null)
            {
                // One slider per module kind, not per module (owner idea U-25: less clutter). The Mender repairs at or below it.
                GUILayout.Label("<b>Mender auto-repair</b>", HuntStyles.Text);
                foreach (var kind in modules.Slots.Values.Select(x => x.Item).Distinct())
                {
                    float at = CompanionSettings.CrewRepairAt(kind.id);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{kind.displayName}: {Mathf.RoundToInt(at * 100)}%", GUILayout.Width(170));
                    float pickedAt = Mathf.Round(GUILayout.HorizontalSlider(at, 0f, 0.9f) * 10f) / 10f;
                    if (!Mathf.Approximately(pickedAt, at)) CompanionSettings.SetCrewRepairAt(kind.id, pickedAt);
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}
