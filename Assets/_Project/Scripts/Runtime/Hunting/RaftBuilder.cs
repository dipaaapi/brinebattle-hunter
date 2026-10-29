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
        enum Mode { Place, Remove }

        public Raft raft;
        public HookController hook;
        public RaftModules modules;
        public List<RecipeDefinition> recipes = new List<RecipeDefinition>();

        public bool Active => WorldPause.Open == Menu.Build;

        Mode mode;
        ItemDefinition selected;
        readonly List<GameObject> ghosts = new List<GameObject>();
        GameObject hover;
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
            if (!Active || raft == null)
            {
                if (ghosts.Count > 0 || hover != null) ClearGhosts();
                return;
            }

            if (selected != null && hook.Inventory.Count(selected) == 0) selected = null;
            RefreshGhosts();

            var mouse = Mouse.current;
            if (mouse == null || Camera.main == null) return;
            Vector2 mp = mouse.position.ReadValue();
            var guiPoint = new Vector2(mp.x, Screen.height - mp.y) / HuntStyles.Scale;
            bool overPanel = panelRect.Contains(guiPoint);

            Cell? cell = overPanel ? null : CellUnderMouse(mp);
            UpdateHover(cell);
            if (overPanel || cell == null || !mouse.leftButton.wasPressedThisFrame) return;

            if (mode == Mode.Place) TryPlace(cell.Value);
            else TryRemove(cell.Value);
        }

        Cell? CellUnderMouse(Vector2 screen)
        {
            var ray = Camera.main.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, raft.transform.position + Vector3.up * 0.15f);
            if (!plane.Raycast(ray, out float d)) return null;
            return raft.WorldToCell(ray.GetPoint(d));
        }

        void TryPlace(Cell c)
        {
            if (selected == null) { message = "Pick a raft part from the list first."; return; }
            // Clicking a built cell swaps its part for the selected one (Ed's ask: replace, not remove then place).
            if (raft.Grid.Has(c))
            {
                var rr = raft.Grid.Replace(c, selected, out var old);
                if (rr == PlaceResult.Ok)
                {
                    hook.Inventory.Remove(selected, 1);
                    hook.Inventory.Add(old, 1);
                    message = $"Replaced {old.displayName} with {selected.displayName}; the old one is back in the bag.";
                    hook.Log($"Replaced {old.displayName} with {selected.displayName} at {c}.");
                }
                else message = rr == PlaceResult.Same ? "That part is already there." : PlaceText(rr);
                return;
            }
            var r = raft.Grid.Place(c, selected);
            if (r == PlaceResult.Ok)
            {
                hook.Inventory.Remove(selected, 1);
                message = $"Placed {selected.displayName}.";
                hook.Log($"Built {selected.displayName} at {c}. Raft is size {raft.size}.");
            }
            else message = PlaceText(r);
        }

        void TryRemove(Cell c)
        {
            var r = raft.Grid.Remove(c, out var part);
            switch (r)
            {
                case RemoveResult.Ok:
                    hook.Inventory.Add(part, 1);
                    message = $"Took off {part.displayName}; it's back in the bag.";
                    break;
                case RemoveResult.WouldSplit: message = "That part holds the raft together."; break;
                case RemoveResult.LastPart: message = "You can't remove the last part."; break;
                case RemoveResult.NeedsHammer: message = HammerText; break;
                default: message = "No part there."; break;
            }
        }

        /// <summary>
        /// What the next raft level needs, each ✔ or ✘ with have/need. How-to text lives only in
        /// Settings → FAQ (Ed's call).
        /// </summary>
        void DrawRequirements()
        {
            var g = raft.Grid;
            var inv = hook.Inventory;
            int kills = RaftRules.KillsToUpgrade(g.Tier), mats = RaftRules.MaterialsToUpgrade(g.Tier), cost = RaftRules.UpgradeCost(g.Tier);
            var (guarded, _) = g.UpstreamModules();
            var hammer = RaftRules.BestHammer(inv);
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.Label($"<b>Raft Lv {(int)g.Tier + 2} ({g.Tier + 1}) needs</b>", HuntStyles.Text);
            Step(g.Kills >= kills, $"Monsters beaten: {Mathf.Min(g.Kills, kills)}/{kills}");
            Step(inv.MaterialCount() >= mats, $"Monster materials: {Mathf.Min(inv.MaterialCount(), mats)}/{mats}");
            Step(inv.EssenceAtLeast(g.Tier) >= cost, $"{g.Tier}+ essence: {Mathf.Min(inv.EssenceAtLeast(g.Tier), cost)}/{cost}");
            Step(guarded >= g.ModulesNeeded, $"Modules on the left edge: {Mathf.Min(guarded, g.ModulesNeeded)}/{g.ModulesNeeded}");
            Step(RaftRules.HasHammer(inv, g.Tier), $"{g.Tier} hammer or better: {(hammer != null ? hammer.displayName : "none")}");
            GUILayout.EndVertical();
        }

        static void Step(bool done, string title) =>
            GUILayout.Label(done ? $"<color={HuntStyles.GoodHex}>✔ {title}</color>" : $"<color={HuntStyles.BadHex}>✘</color> <b>{title}</b>", HuntStyles.Text);

        const string HammerText = "Your hammer is too weak for this raft level.";

        static string PlaceText(PlaceResult r)
        {
            switch (r)
            {
                case PlaceResult.Occupied: return "There's already a part there.";
                case PlaceResult.NotAdjacent: return "Parts must touch the raft.";
                case PlaceResult.OutOfBounds: return "Too far out (max 9x9).";
                case PlaceResult.BelowTier: return "That part is too weak for this raft level.";
                case PlaceResult.NeedsModules: return "Not enough modules on the left edge yet.";
                case PlaceResult.NeedsHammer: return HammerText;
                default: return "That isn't a raft part.";
            }
        }

        void RefreshGhosts()
        {
            var open = mode == Mode.Place ? raft.Grid.OpenCells().ToList() : new List<Cell>();
            while (ghosts.Count < open.Count)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.name = "Ghost";
                Destroy(g.GetComponent<Collider>());
                g.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
                ghosts.Add(g);
            }
            bool fits = selected != null && SalvageRules.IsCompatible(selected.type, selected.rarity, raft.Tier);
            var color = fits ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.9f, 0.9f, 0.9f);
            // Upgrade guide: the left-edge spots that still need a module glow yellow.
            var gaps = page != Page.Workbench && !raft.Grid.ModulesReady ? new HashSet<Cell>(raft.Grid.UpstreamGaps()) : new HashSet<Cell>();
            var gapColor = new Color(1f, 0.85f, 0.2f) * (0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 6f));
            for (int i = 0; i < ghosts.Count; i++)
            {
                bool on = i < open.Count;
                ghosts[i].SetActive(on);
                if (!on) continue;
                ghosts[i].transform.position = raft.CellToWorld(open[i]) + Vector3.up * 0.02f;
                Tint.Apply(ghosts[i].GetComponent<Renderer>(), gaps.Contains(open[i]) ? gapColor : color);
            }
        }

        void UpdateHover(Cell? cell)
        {
            if (hover == null)
            {
                hover = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hover.name = "Build Cursor";
                Destroy(hover.GetComponent<Collider>());
                hover.transform.localScale = new Vector3(1.05f, 0.4f, 1.05f);
            }
            hover.SetActive(cell != null);
            if (cell == null) return;
            hover.transform.position = raft.CellToWorld(cell.Value) + Vector3.up * 0.1f;
            bool ok = mode == Mode.Place
                ? selected != null && (raft.Grid.Has(cell.Value) ? raft.Grid.CanReplace(cell.Value, selected) : raft.Grid.CanPlace(cell.Value, selected)) == PlaceResult.Ok
                : raft.Grid.CanRemove(cell.Value) == RemoveResult.Ok;
            Tint.Apply(hover.GetComponent<Renderer>(), ok ? new Color(1f, 1f, 0.4f) : new Color(1f, 0.35f, 0.3f));
        }

        void ClearGhosts()
        {
            foreach (var g in ghosts) if (g != null) Destroy(g);
            ghosts.Clear();
            if (hover != null) Destroy(hover);
            hover = null;
        }

        enum Page { Build, Workbench, Modules, LevelUp }
        static readonly string[] PageLabels = { "Build", "Workbench", "Modules", "Level up" };
        Page page;
        bool readyOnly;

        void OpenBench(RecipeCategory category) { page = Page.Workbench; tab = category; panelScroll = Vector2.zero; }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!Active || raft == null) return;
            float w = Mathf.Min(460, HuntStyles.W - HookController.HudRight - 22), h = HuntStyles.H - 120;
            panelRect = new Rect(HuntStyles.W - w - 12, 20, w, h);
            GUI.depth = -5;
            var g = raft.Grid;
            GUILayout.BeginArea(panelRect, $"Build raft · Lv {(int)g.Tier + 1} ({g.Tier})", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { Toggle(); GUILayout.EndArea(); return; }

            // Owner idea U-25 (Ed: "hindi user friendly"): four short pages instead of one long scroll.
            var labels = (string[])PageLabels.Clone();
            int ready = recipes.Count(r => r != null && hook.Mastery.Knows(r) && CraftRules.CanCraft(hook.Inventory, r));
            if (ready > 0) labels[1] = $"Workbench ({ready})";
            if (RaftRules.UpgradeCost(g.Tier) > 0 && g.UpgradeBlocker(hook.Inventory) == null) labels[3] = "Level up ✔";
            var picked = (Page)HuntStyles.Tabs((int)page, labels);
            if (picked != page) { page = picked; panelScroll = Vector2.zero; }

            panelScroll = GUILayout.BeginScrollView(panelScroll);
            switch (page)
            {
                case Page.Build: DrawBuild(); break;
                case Page.Workbench: DrawWorkbench(w); break;
                case Page.Modules: DrawModules(); break;
                default: DrawLevelUp(); break;
            }
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

            var hammerNow = RaftRules.BestHammer(hook.Inventory);
            bool hasHammer = RaftRules.HasHammer(hook.Inventory, raft.Tier);
            GUILayout.BeginHorizontal(HuntStyles.Card);
            GUILayout.Label(HuntIcons.Hammer, GUILayout.Width(28), GUILayout.Height(28));
            GUILayout.Label(hasHammer ? $"Hammer: <b>{hammerNow.displayName}</b>"
                : $"<color={HuntStyles.BadHex}>Hammer: {(hammerNow != null ? hammerNow.displayName + " is too weak" : "none")}</color>", HuntStyles.Text);
            if (!hasHammer && GUILayout.Button("Make one", GUILayout.Width(90), GUILayout.Height(28))) OpenBench(RecipeCategory.Raft);
            GUILayout.EndHorizontal();

            mode = (Mode)HuntStyles.Tabs((int)mode, new[] { "Place / replace", "Remove" });
            if (mode == Mode.Remove) return;

            var parts = hook.Inventory.Items.Where(kv => kv.Key.type == ItemType.Raft || kv.Key.type == ItemType.Module || kv.Key.type == ItemType.Habitat).ToList();
            if (parts.Count == 0)
            {
                GUILayout.Label("No raft parts in the bag.", HuntStyles.Text);
                if (GUILayout.Button("Open the Raft recipes", GUILayout.Height(30))) OpenBench(RecipeCategory.Raft);
                return;
            }
            foreach (var kv in parts)
            {
                var p = kv.Key;
                bool fits = SalvageRules.IsCompatible(p.type, p.rarity, g.Tier);
                string label = $"{kv.Value} × <color={BagPanel.RarityColor(p.rarity)}>{p.displayName}</color>"
                    + (fits ? "" : $"  <size=11><color={HuntStyles.BadHex}>too weak for Lv {(int)g.Tier + 1}</color></size>");
                GUI.enabled = fits;
                var old = GUI.backgroundColor;
                if (selected == p) GUI.backgroundColor = HuntStyles.Go;
                if (GUILayout.Button((selected == p ? "▶ " : "") + label, HuntStyles.RichButton, GUILayout.Height(32))) { selected = p; mode = Mode.Place; }
                GUI.backgroundColor = old;
                GUI.enabled = true;
            }
            if (parts.Any(kv => !SalvageRules.IsCompatible(kv.Key.type, kv.Key.rarity, g.Tier)))
                if (GUILayout.Button("Make stronger parts (Reinforce)", GUILayout.Height(28))) OpenBench(RecipeCategory.Raft);
        }

        /// <summary>
        /// Recipe cards (Ed: the list didn't say how many are missing). Each ingredient shows have/need in
        /// green or red, ready recipes come first, and a tab's label counts how many are ready.
        /// </summary>
        void DrawWorkbench(float w)
        {
            var inv = hook.Inventory;
            var labels = new string[CraftRules.CategoryLabels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int n = recipes.Count(r => r != null && (int)CraftRules.CategoryOf(r) == i && hook.Mastery.Knows(r) && CraftRules.CanCraft(inv, r));
                labels[i] = n > 0 ? $"{CraftRules.CategoryLabels[i]} ({n})" : CraftRules.CategoryLabels[i];
            }
            tab = (RecipeCategory)GUILayout.SelectionGrid((int)tab, labels, 3, HuntStyles.TabStyle, GUILayout.Height(60));
            readyOnly = GUILayout.Toggle(readyOnly, " Show only what I can make now");

            // Recipes above the hunter's Alchemy Mastery stay "???" at the end (Idea Round 3, U-14).
            var inTab = recipes.Where(r => r != null && CraftRules.CategoryOf(r) == tab).ToList();
            var shown = CraftRules.ByReadiness(inv, inTab.Where(r => hook.Mastery.Knows(r)));
            if (readyOnly) shown = shown.Where(r => CraftRules.CanCraft(inv, r)).ToList();
            if (shown.Count == 0) GUILayout.Label(readyOnly ? "Nothing ready here yet." : "No recipes here yet.", HuntStyles.Text);
            foreach (var r in shown) RecipeCard(r);
            if (!readyOnly)
                foreach (var r in inTab.Where(r => !hook.Mastery.Knows(r)).OrderBy(AlchemyMastery.Needed))
                {
                    GUILayout.BeginVertical(HuntStyles.Card);
                    GUILayout.Label($"<b>???</b>  <color={HuntStyles.BadHex}>Alchemy Mastery Lv {AlchemyMastery.Needed(r)}</color>", HuntStyles.Text);
                    GUILayout.EndVertical();
                }
        }

        void RecipeCard(RecipeDefinition r)
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

            if (ok)
            {
                if (HuntStyles.ColorButton($"{verb}!", HuntStyles.Go)) Craft(r, verb);
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

        void Craft(RecipeDefinition r, string verb)
        {
            CraftRules.Craft(hook.Inventory, r);
            if (r.kind != RecipeKind.Dismantle) hook.Mastery.Add(AlchemyMastery.CraftPoints);
            message = $"{verb}: got {CraftRules.Describe(r.outputs)}.";
            StorageBox.Instance?.Crafted(CraftRules.Describe(r.outputs));
            // A Mystic companion (Phase 7) sometimes makes one more of a gear, potion or oil.
            var mystic = hook.Companions.Best(Companions.CompanionRole.Mystic);
            var cat = CraftRules.CategoryOf(r);
            if (mystic != null && r.kind == RecipeKind.Craft && cat != RecipeCategory.Raft && cat != RecipeCategory.Dismantle
                && Random.value < Companions.CompanionRules.MysticExtraChance(mystic.Rarity))
            {
                foreach (var o in r.outputs) hook.Inventory.Add(o.item, o.count);
                message += $" {mystic.Name} (Mystic) made an extra one!";
            }
            hook.Log(message);
        }

        void DrawModules()
        {
            var g = raft.Grid;
            var (guarded, rows) = g.UpstreamModules();
            GUILayout.Label($"Left edge: {HuntStyles.Count(guarded, g.ModulesNeeded)} rows guarded (of {rows})", HuntStyles.Text);
            if (modules == null || modules.Slots.Count == 0)
            {
                GUILayout.Label("No modules on the raft yet.", HuntStyles.Text);
                if (GUILayout.Button("Make a Drift Net", GUILayout.Height(30))) OpenBench(RecipeCategory.Raft);
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
            if (hook.Companions.Best(Companions.CompanionRole.Crew) != null)
            {
                // One slider per module kind, not per module (owner idea U-25: less clutter). The Crew repairs at or below it.
                GUILayout.Label("<b>Crew auto-repair</b>", HuntStyles.Text);
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

        void DrawLevelUp()
        {
            var g = raft.Grid;
            if (RaftRules.UpgradeCost(g.Tier) <= 0) { GUILayout.Label("The raft is at the highest level.", HuntStyles.Text); return; }
            DrawRequirements();
            string blocker = g.UpgradeBlocker(hook.Inventory);
            if (blocker == null)
            {
                if (HuntStyles.ColorButton($"Level up the raft → Lv {(int)g.Tier + 2} ({g.Tier + 1})", HuntStyles.Go)
                    && g.TryUpgrade(hook.Inventory))
                    hook.Log($"Raft leveled up to Lv {(int)g.Tier + 1} ({g.Tier})! Max HP {g.MaxHp}.");
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("Not ready yet (see ✘)", GUILayout.Height(32));
                GUI.enabled = true;
            }
        }
    }
}
