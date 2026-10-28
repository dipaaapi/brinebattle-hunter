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

        public bool Active { get; private set; }

        Mode mode;
        ItemDefinition selected;
        readonly List<GameObject> ghosts = new List<GameObject>();
        GameObject hover;
        Rect panelRect;
        Vector2 panelScroll;
        string message = "";

        public void Toggle()
        {
            Active = !Active;
            if (!Active) ClearGhosts();
            message = "";
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) Toggle();
            if (kb != null && Active && kb.escapeKey.wasPressedThisFrame) Toggle();
            if (!Active || raft == null) return;

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

        static string PlaceText(PlaceResult r)
        {
            switch (r)
            {
                case PlaceResult.Occupied: return "There's already a part there.";
                case PlaceResult.NotAdjacent: return "Parts must touch the raft.";
                case PlaceResult.OutOfBounds: return "Too far out (max 9x9).";
                case PlaceResult.BelowTier: return "That part's rarity is below the raft tier.";
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
                }
            }

            int cost = RaftRules.UpgradeCost(g.Tier);
            if (cost > 0)
            {
                int have = hook.Inventory.EssenceAtLeast(g.Tier);
                GUILayout.Label($"Upgrade to {g.Tier + 1}: {cost} essence ({g.Tier}+), you have {have}. Parts below the new tier can't be added after.");
                GUI.enabled = have >= cost;
                if (GUILayout.Button($"Upgrade raft tier → {g.Tier + 1}"))
                {
                    if (g.TryUpgrade(hook.Inventory)) hook.Log($"Raft upgraded to {g.Tier} tier. Max HP {g.MaxHp}.");
                }
                GUI.enabled = true;
            }
            else GUILayout.Label("Raft is at the highest tier.");

            GUILayout.Label(message, Rich);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static GUIStyle rich, toggle;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
        static GUIStyle ToggleStyle => toggle ??= new GUIStyle(GUI.skin.toggle) { richText = true };
    }
}
