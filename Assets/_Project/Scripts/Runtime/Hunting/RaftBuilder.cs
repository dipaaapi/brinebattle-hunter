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
            var guiPoint = new Vector2(mp.x, Screen.height - mp.y);
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
                default: message = "No part there."; break;
            }
        }

        static string Have(int have, int need) =>
            $"<color={(have >= need ? "#7CFC9A" : "#ff8a80")}>{Mathf.Min(have, need)}/{need}</color>";

        static string PlaceText(PlaceResult r)
        {
            switch (r)
            {
                case PlaceResult.Occupied: return "There's already a part there.";
                case PlaceResult.NotAdjacent: return "Parts must touch the raft.";
                case PlaceResult.OutOfBounds: return "Too far out (max 9x9).";
                case PlaceResult.BelowTier: return "That part's rarity is below the raft tier.";
                case PlaceResult.NeedsModules: return "Install more auto-loot modules on the left edge first (half of its rows need one).";
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
            for (int i = 0; i < ghosts.Count; i++)
            {
                bool on = i < open.Count;
                ghosts[i].SetActive(on);
                if (!on) continue;
                ghosts[i].transform.position = raft.CellToWorld(open[i]) + Vector3.up * 0.02f;
                Tint.Apply(ghosts[i].GetComponent<Renderer>(), color);
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
                ? selected != null && raft.Grid.CanPlace(cell.Value, selected) == PlaceResult.Ok
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

        void OnGUI()
        {
            if (!Active || raft == null) return;
            float w = 380, h = Screen.height - 40;
            panelRect = new Rect(Screen.width - w - 12, 20, w, h);
            GUI.depth = -5;
            GUILayout.BeginArea(panelRect, "Build raft (R / Esc to close)", HuntStyles.Window);

            if (GUI.Button(new Rect(w - 34, 4, 26, 20), "X")) { Toggle(); GUILayout.EndArea(); return; }
            panelScroll = GUILayout.BeginScrollView(panelScroll);
            var g = raft.Grid;
            GUILayout.Label($"Size {g.Size} · {g.Count} parts · {g.MaxHp} HP · tier <color={BagPanel.RarityColor(g.Tier)}>{g.Tier}</color>", Rich);
            int next = 0;
            for (int n = 1; n <= RaftRules.StarterParts + RaftRules.PartsPerSize; n++)
                if (RaftRules.SizeFor(g.Count + n) > g.Size) { next = n; break; }
            if (next > 0 && g.Size < 10) GUILayout.Label($"{next} more part(s) to reach size {g.Size + 1}.");
            var (guarded, rows) = g.UpstreamModules();
            GUILayout.Label($"Left edge modules: {guarded}/{rows} rows (need {g.ModulesNeeded} to widen or upgrade) "
                + (g.ModulesReady ? "<color=#7CFC9A>ready</color>" : "<color=#ff8a80>install modules on the left first</color>"), Rich);

            GUILayout.Label("1. Pick a part  2. Click a green tile next to the raft.\nYellow cursor = OK, red = not allowed.", Rich);
            mode = (Mode)GUILayout.Toolbar((int)mode, new[] { "Place", "Remove" });

            GUILayout.Label("<b>Raft parts in your bag</b>", Rich);
            GUILayout.BeginVertical();
            var parts = hook.Inventory.Items.Where(kv => kv.Key.type == ItemType.Raft || kv.Key.type == ItemType.Module).ToList();
            if (parts.Count == 0) GUILayout.Label("  (none — harvest some)");
            foreach (var kv in parts)
            {
                var p = kv.Key;
                bool fits = SalvageRules.IsCompatible(p.type, p.rarity, g.Tier);
                string label = $"{kv.Value} × <color={BagPanel.RarityColor(p.rarity)}>{p.displayName}</color> {(fits ? "" : "<color=#f77>(below tier)</color>")}";
                GUI.enabled = fits;
                if (GUILayout.Toggle(selected == p, label, ToggleStyle) && selected != p) { selected = p; mode = Mode.Place; }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();

            if (recipes.Count > 0)
            {
                GUILayout.Label("<b>Workbench</b> (uses the bag)", Rich);
                // Two rows of tabs so all five fit the panel width.
                tab = (RecipeCategory)GUILayout.SelectionGrid((int)tab, CraftRules.CategoryLabels, 3, GUILayout.Width(w - 50));
                GUILayout.Label($"<size=11>{CraftRules.CategoryHints[(int)tab]}</size>", Rich);
                var shown = recipes.Where(r => r != null && CraftRules.CategoryOf(r) == tab).ToList();
                if (shown.Count == 0) GUILayout.Label("  (no recipes here yet)");
                foreach (var r in shown)
                {
                    string verb = r.kind == RecipeKind.Upgrade ? "Upgrade" : r.kind == RecipeKind.Dismantle ? "Dismantle" : "Craft";
                    GUI.enabled = CraftRules.CanCraft(hook.Inventory, r);
                    if (GUILayout.Button($"{verb}: {CraftRules.Describe(r.inputs)} → {CraftRules.Describe(r.outputs)}", ButtonWrap))
                    {
                        CraftRules.Craft(hook.Inventory, r);
                        message = $"{verb}: got {CraftRules.Describe(r.outputs)}.";
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
                    GUI.enabled = true;
                }
            }

            if (modules != null && modules.Slots.Count > 0)
            {
                GUILayout.Label("<b>Modules</b> (auto-loot what touches them)", Rich);
                foreach (var kv in modules.Slots)
                {
                    var s = kv.Value;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{s.Item.displayName} {kv.Key}: {(s.Durability.Broken ? "<color=#f77>broken</color>" : s.Durability.ToString())}", Rich);
                    GUI.enabled = !s.Durability.Full;
                    if (GUILayout.Button($"Repair ({RaftModules.CostText(s)})", GUILayout.Width(170))) message = modules.Repair(s);
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    if (hook.Companions.Best(Companions.CompanionRole.Crew) != null)
                    {
                        // Per module kind: the Crew repairs it at or below this share (Phase 7, owner's call).
                        float at = CompanionSettings.CrewRepairAt(s.Item.id);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"<size=11>  Crew repairs at ≤ {Mathf.RoundToInt(at * 100)}%</size>", Rich, GUILayout.Width(170));
                        float picked = Mathf.Round(GUILayout.HorizontalSlider(at, 0f, 0.9f) * 10f) / 10f;
                        if (!Mathf.Approximately(picked, at)) CompanionSettings.SetCrewRepairAt(s.Item.id, picked);
                        GUILayout.EndHorizontal();
                    }
                }
            }

            int cost = RaftRules.UpgradeCost(g.Tier);
            if (cost > 0)
            {
                var inv = hook.Inventory;
                int kills = RaftRules.KillsToUpgrade(g.Tier), mats = RaftRules.MaterialsToUpgrade(g.Tier);
                GUILayout.Label($"<b>Upgrade to {g.Tier + 1}</b> (parts below the new tier can't be added after):\n"
                    + $"  Kills since last upgrade {Have(g.Kills, kills)} · monster materials {Have(inv.MaterialCount(), mats)}\n"
                    + $"  {g.Tier}+ essence {Have(inv.EssenceAtLeast(g.Tier), cost)} · left-edge modules {Have(guarded, g.ModulesNeeded)}", Rich);
                string blocker = g.UpgradeBlocker(inv);
                GUI.enabled = blocker == null;
                if (GUILayout.Button($"Upgrade raft tier → {g.Tier + 1}"))
                {
                    if (g.TryUpgrade(inv)) hook.Log($"Raft upgraded to {g.Tier} tier. Max HP {g.MaxHp}.");
                }
                GUI.enabled = true;
                if (blocker != null) GUILayout.Label($"<size=11>{blocker}</size>", Rich);
            }
            else GUILayout.Label("Raft is at the highest tier.");

            GUILayout.Label(message, Rich);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static GUIStyle rich, toggle, buttonWrap;
        static GUIStyle ButtonWrap => buttonWrap ??= new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        static GUIStyle ToggleStyle => toggle ??= new GUIStyle(GUI.skin.toggle) { richText = true };
    }
}
