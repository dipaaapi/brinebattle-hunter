using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The Shipyard (Ed's ask, 2026-10-31: a separate place to customise the raft, not on the sea behind the Build
    /// menu). A full-screen virtual dock with only your raft or ship in the middle. Drag or WASD / arrow keys turn it
    /// (left/right and tilt up/down), the mouse wheel zooms, Reset view puts it back. Hover a cell to see what a click
    /// does; click places the picked part (or swaps a built one), right-click takes a part off. The parts come from
    /// the bag in a slim strip on the left, so nothing covers the raft. Minions don't help here.
    /// The whole game is halted while it's open (world time scale 0); the dock itself animates on real time.
    /// Changes go straight onto the real raft. Open with Y or Build → Open the Shipyard. Added at runtime by HookController.
    /// </summary>
    public class Shipyard : MonoBehaviour
    {
        public HookController hook;

        public static bool IsOpen => WorldPause.Open == Menu.Shipyard;
        public static void Toggle() => WorldPause.Toggle(Menu.Shipyard);

        static readonly Vector3 DockAt = new Vector3(0f, -600f, 0f);
        GameObject dock;
        Transform model;
        Camera cam;
        RenderTexture rt;
        readonly Dictionary<Collider, Cell> cellOf = new Dictionary<Collider, Cell>();
        readonly Dictionary<Cell, Renderer> tileOf = new Dictionary<Cell, Renderer>();
        readonly Dictionary<Cell, Color> colorOf = new Dictionary<Cell, Color>();
        bool wasOpen, dirty;
        float savedTimeScale = 1f;
        float yaw = ShipyardRules.StartYaw, pitch = ShipyardRules.StartPitch, zoom = ShipyardRules.StartZoom;
        ItemDefinition selected;
        bool removeMode;
        string message = "";
        Cell? hoverCell;
        string hoverText = "";
        Vector2 pressAt, lastMouse;
        bool pressing;
        Vector2 stripScroll;
        Rect stripRect, topRect, bottomRect;
        GUIStyle titleStyle;

        RaftGrid Grid => hook.raft.Grid;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.yKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Shipyard || WorldPause.Open == Menu.Build))
            {
                if (WorldPause.Open == Menu.Build) WorldPause.Close(Menu.Build);
                Toggle();
            }
            if (kb != null && IsOpen && kb.escapeKey.wasPressedThisFrame) WorldPause.Close(Menu.Shipyard);

            if (IsOpen != wasOpen)
            {
                if (IsOpen) Enter(); else Exit();
                wasOpen = IsOpen;
            }
            if (!IsOpen) return;
            // Keep the world halted even if something else touched the time scale.
            Time.timeScale = ShipyardRules.WorldTimeScale(true, savedTimeScale);
            if (dirty) Rebuild();
            Controls(kb);
            Aim();
        }

        void Enter()
        {
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = ShipyardRules.WorldTimeScale(true, savedTimeScale);
            if (dock == null) BuildDock();
            dock.SetActive(true);
            Grid.Changed += MarkDirty;
            if (selected != null && hook.Inventory.Count(selected) == 0) selected = null;
            message = "";
            Rebuild();
        }

        void Exit()
        {
            Time.timeScale = ShipyardRules.WorldTimeScale(false, savedTimeScale);
            if (hook != null && hook.raft != null) Grid.Changed -= MarkDirty;
            if (dock != null) dock.SetActive(false);
            hoverCell = null;
        }

        void OnDestroy()
        {
            if (wasOpen) Time.timeScale = savedTimeScale;
            if (rt != null) rt.Release();
        }

        void MarkDirty() => dirty = true;

        // ---- The dock ----

        void BuildDock()
        {
            dock = new GameObject("Shipyard Dock");
            dock.transform.position = DockAt;
            var floor = Prim(PrimitiveType.Cube, dock.transform, new Vector3(0f, -0.9f, 0f), new Vector3(22f, 0.4f, 22f), new Color(0.25f, 0.18f, 0.12f));
            for (int i = -5; i <= 5; i++)   // planks of the dock floor
                Prim(PrimitiveType.Cube, floor.transform.parent, new Vector3(i * 2f, -0.68f, 0f), new Vector3(0.08f, 0.04f, 22f), new Color(0.18f, 0.12f, 0.08f));
            Prim(PrimitiveType.Cube, dock.transform, new Vector3(0f, 3f, 11f), new Vector3(22f, 8f, 0.4f), new Color(0.1f, 0.16f, 0.24f));   // back wall
            foreach (int side in new[] { -1, 1 })
            {
                Prim(PrimitiveType.Cylinder, dock.transform, new Vector3(side * 9f, 1.5f, 9f), new Vector3(0.5f, 2.5f, 0.5f), new Color(0.35f, 0.25f, 0.15f));   // posts
                Prim(PrimitiveType.Sphere, dock.transform, new Vector3(side * 9f, 4.2f, 9f), Vector3.one * 0.7f, new Color(1f, 0.85f, 0.45f));   // lamps
            }
            Prim(PrimitiveType.Cylinder, dock.transform, new Vector3(0f, -0.65f, 0f), new Vector3(12f, 0.03f, 12f), new Color(0.15f, 0.35f, 0.45f));   // the slip under the raft
            model = new GameObject("Raft model").transform;
            model.SetParent(dock.transform, false);

            var camGo = new GameObject("Shipyard Camera");
            camGo.transform.SetParent(dock.transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.11f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.depth = -50;
            var lightGo = new GameObject("Shipyard Light");
            lightGo.transform.SetParent(dock.transform, false);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 40f;
            l.intensity = 3f;
            l.transform.localPosition = new Vector3(0f, 9f, -6f);
        }

        static GameObject Prim(PrimitiveType t, Transform parent, Vector3 local, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(t);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            Tint.Apply(go.GetComponent<Renderer>(), c);
            return go;
        }

        /// <summary>Rebuilds the raft model from the real grid, plus slots on the open cells when placing.</summary>
        void Rebuild()
        {
            dirty = false;
            if (model == null) return;
            foreach (Transform child in model) Destroy(child.gameObject);
            cellOf.Clear();
            tileOf.Clear();
            colorOf.Clear();
            var g = Grid;
            foreach (var kv in g.Parts)
            {
                var p = kv.Value;
                bool module = p.type == ItemType.Module, habitat = p.type == ItemType.Habitat;
                float h = module ? 0.7f : habitat ? 0.5f : 0.35f;
                var color = module ? new Color(0.25f, 0.55f, 0.45f) : habitat ? new Color(0.3f, 0.75f, 0.9f) : Raft.PartColor(p);
                Cellular(kv.Key, new Vector3(module ? 0.8f : 0.98f, h, module ? 0.8f : 0.98f), h / 2f, color);
            }
            if (!removeMode && selected != null)
                foreach (var c in g.OpenCells())
                {
                    bool fits = g.CanPlace(c, selected) == PlaceResult.Ok;
                    Cellular(c, new Vector3(0.9f, 0.06f, 0.9f), 0.03f, fits ? new Color(0.35f, 0.9f, 0.45f) : new Color(0.3f, 0.33f, 0.38f));
                }
            // The middle of the raft at the centre of the turn.
            if (g.Count > 0)
            {
                float cx = (float)g.Parts.Keys.Average(c => c.X), cz = (float)g.Parts.Keys.Average(c => c.Z);
                foreach (Transform child in model) child.localPosition -= new Vector3(cx * Raft.CellSize, 0f, cz * Raft.CellSize);
            }
        }

        void Cellular(Cell c, Vector3 scale, float y, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Cell {c.X},{c.Z}";
            go.transform.SetParent(model, false);
            go.transform.localPosition = new Vector3(c.X * Raft.CellSize, y, c.Z * Raft.CellSize);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            Tint.Apply(r, color);
            cellOf[go.GetComponent<Collider>()] = c;
            tileOf[c] = r;
            colorOf[c] = color;
        }

        // ---- View and input ----

        void Aim()
        {
            yaw = ShipyardRules.WrapYaw(yaw);
            pitch = ShipyardRules.ClampPitch(pitch);
            zoom = ShipyardRules.ClampZoom(zoom);
            if (model != null) model.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var dir = Quaternion.Euler(pitch, 0f, 0f) * Vector3.back;
            cam.transform.position = DockAt + dir * zoom + Vector3.up * 0.3f;
            cam.transform.LookAt(DockAt + Vector3.up * 0.3f);
            int w = Mathf.Max(64, Screen.width), h = Mathf.Max(64, Screen.height);
            if (rt == null || rt.width != w || rt.height != h)
            {
                if (rt != null) rt.Release();
                rt = new RenderTexture(w, h, 24) { name = "Shipyard view" };
                cam.targetTexture = rt;
            }
        }

        void Controls(Keyboard kb)
        {
            float dt = Time.unscaledDeltaTime;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) yaw += 90f * dt;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) yaw -= 90f * dt;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) pitch += 60f * dt;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) pitch -= 60f * dt;
            }
            var mouse = Mouse.current;
            if (mouse == null || cam == null) return;
            Vector2 mp = mouse.position.ReadValue();
            var ui = new Vector2(mp.x, Screen.height - mp.y) / HuntStyles.Scale;
            bool overUi = stripRect.Contains(ui) || topRect.Contains(ui) || bottomRect.Contains(ui);

            float wheel = mouse.scroll.ReadValue().y;
            if (!overUi && Mathf.Abs(wheel) > 0.01f) zoom -= Mathf.Sign(wheel) * 1.2f;

            // Hover: what a click would do here.
            hoverCell = null;
            hoverText = "";
            if (!overUi && Physics.Raycast(cam.ViewportPointToRay(new Vector3(mp.x / Screen.width, mp.y / Screen.height, 0f)), out var hit, 100f)
                && cellOf.TryGetValue(hit.collider, out var cell))
            {
                hoverCell = cell;
                var act = ShipyardRules.Preview(Grid, cell, selected, removeMode, out var why);
                hoverText = act == ShipyardRules.Action.None ? $"<color={HuntStyles.BadHex}>{why}</color>"
                    : act == ShipyardRules.Action.Remove ? $"Click: take off the {Grid.Parts[cell].displayName}"
                    : act == ShipyardRules.Action.Replace ? $"Click: swap the {Grid.Parts[cell].displayName} for the {selected.displayName}"
                    : $"Click: place the {selected.displayName}";
            }
            Highlight();

            // Left: press and drag turns the raft; a press without moving is a click. Right-click removes.
            if (mouse.leftButton.wasPressedThisFrame && !overUi) { pressing = true; pressAt = lastMouse = mp; }
            if (pressing && mouse.leftButton.isPressed)
            {
                var d = mp - lastMouse;
                yaw -= d.x * 0.35f;
                pitch -= d.y * 0.25f;
                lastMouse = mp;
            }
            if (pressing && mouse.leftButton.wasReleasedThisFrame)
            {
                pressing = false;
                if (ShipyardRules.IsClick((mp - pressAt).magnitude) && hoverCell != null) Use(hoverCell.Value, removeMode);
            }
            if (mouse.rightButton.wasPressedThisFrame && !overUi && hoverCell != null) Use(hoverCell.Value, true);
        }

        void Highlight()
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);
            foreach (var kv in tileOf)
            {
                if (kv.Value == null) continue;
                bool on = hoverCell != null && kv.Key.Equals(hoverCell.Value);
                var act = on ? ShipyardRules.Preview(Grid, kv.Key, selected, removeMode, out _) : ShipyardRules.Action.None;
                var c = !on ? colorOf[kv.Key]
                    : act == ShipyardRules.Action.None ? new Color(1f, 0.35f, 0.3f)
                    : act == ShipyardRules.Action.Remove ? new Color(1f, 0.55f, 0.25f) * pulse
                    : new Color(1f, 0.9f, 0.35f) * pulse;
                Tint.Apply(kv.Value, c);
            }
        }

        /// <summary>Places, swaps or removes at <paramref name="c"/> on the real raft (the hammer and every rule apply).</summary>
        void Use(Cell c, bool remove)
        {
            var g = Grid;
            var bag = hook.Inventory;
            var act = ShipyardRules.Preview(g, c, selected, remove, out var why);
            switch (act)
            {
                case ShipyardRules.Action.Remove:
                    if (g.Remove(c, out var part) == RemoveResult.Ok) { bag.Add(part, 1); message = $"Took off {part.displayName}; it's back in the bag."; }
                    break;
                case ShipyardRules.Action.Replace:
                    if (g.Replace(c, selected, out var old) == PlaceResult.Ok)
                    {
                        bag.Remove(selected, 1);
                        bag.Add(old, 1);
                        message = $"Swapped {old.displayName} for {selected.displayName}.";
                    }
                    break;
                case ShipyardRules.Action.Place:
                    if (g.Place(c, selected) == PlaceResult.Ok) { bag.Remove(selected, 1); message = $"Placed {selected.displayName}. Size {g.Size}, {g.MaxHp} HP."; }
                    break;
                default:
                    message = why;
                    break;
            }
            if (selected != null && bag.Count(selected) == 0) selected = null;
            dirty = true;
        }

        // ---- Screen ----

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!IsOpen || hook == null) return;
            GUI.depth = -40;   // on top of the HUD and every other menu
            float W = HuntStyles.W, H = HuntStyles.H;
            if (rt != null) GUI.DrawTexture(new Rect(0, 0, W, H), rt, ScaleMode.StretchToFill, false);
            titleStyle ??= new GUIStyle(HuntStyles.Heading) { fontSize = 20 };
            var g = Grid;

            // Top bar.
            topRect = new Rect(0, 0, W, 48);
            GUI.DrawTexture(topRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.1f, 0.15f, 0.92f), 0, 0);
            GUILayout.BeginArea(new Rect(14, 6, W - 28, 40));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"⚓ Shipyard · <b>{g.LevelName}</b> <size=13>({g.FormName}) · size {g.Size} · {g.Count} parts · {g.MaxHp} HP</size>", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset view", GUILayout.Width(110), GUILayout.Height(32))) { yaw = ShipyardRules.StartYaw; pitch = ShipyardRules.StartPitch; zoom = ShipyardRules.StartZoom; }
            if (HuntStyles.ColorButton("Done", HuntStyles.Go, GUILayout.Width(100))) WorldPause.Close(Menu.Shipyard);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Left strip: tools and parts from the bag.
            stripRect = new Rect(10, 58, 250, H - 58 - 70);
            GUILayout.BeginArea(stripRect, HuntStyles.Box);
            int tool = GUILayout.Toolbar(removeMode ? 1 : 0, new[] { "Place", "Remove" }, GUILayout.Height(30));
            if ((tool == 1) != removeMode) { removeMode = tool == 1; dirty = true; }
            var hammer = RaftRules.BestHammer(hook.Inventory);
            bool hasHammer = RaftRules.HasHammer(hook.Inventory, g.Tier);
            GUILayout.Label(hasHammer ? $"<size=11>Hammer: {hammer.displayName}</size>"
                : $"<size=11><color={HuntStyles.BadHex}>Hammer: {(hammer != null ? hammer.displayName + " is too weak" : "none")}</color></size>", HuntStyles.Text);
            HuntStyles.Section("Parts in the bag");
            stripScroll = GUILayout.BeginScrollView(stripScroll);
            var parts = hook.Inventory.Items.Where(kv => kv.Key.type == ItemType.Raft || kv.Key.type == ItemType.Module || kv.Key.type == ItemType.Habitat)
                .OrderBy(kv => kv.Key.type).ThenByDescending(kv => kv.Key.rarity).ToList();
            if (parts.Count == 0) GUILayout.Label("No raft parts in the bag.", HuntStyles.Hint);
            ItemDefinition tip = null;
            foreach (var kv in parts)
            {
                var p = kv.Key;
                bool fits = SalvageRules.IsCompatible(p.type, p.rarity, g.Tier) && g.FitsType(p);
                var old = GUI.backgroundColor;
                if (selected == p && !removeMode) GUI.backgroundColor = HuntStyles.Go;
                string label = $"{kv.Value} × <color={BagPanel.RarityColor(p.rarity)}>{p.displayName}</color>" + (fits ? "" : $"\n<size=10><color={HuntStyles.BadHex}>won't fit</color></size>");
                if (GUILayout.Button(label, HuntStyles.RichButton, GUILayout.Height(fits ? 30 : 40))) { selected = p; removeMode = false; dirty = true; }
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) tip = p;
                GUI.backgroundColor = old;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (tip != null) Tooltip(tip, g);

            // Bottom: what the hovered cell would do, the last result and the controls.
            bottomRect = new Rect(270, H - 62, W - 280, 54);
            GUILayout.BeginArea(bottomRect, HuntStyles.Box);
            GUILayout.Label(hoverText.Length > 0 ? hoverText : message, HuntStyles.Text);
            GUILayout.Label("<size=11>Drag or WASD / arrows: turn · wheel: zoom · click: place / swap · right-click: take off · Esc or Y: done</size>", HuntStyles.Hint);
            GUILayout.EndArea();

            // Nothing below the Shipyard reacts to the mouse while it's open.
            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel) e.Use();
        }

        void Tooltip(ItemDefinition p, RaftGrid g)
        {
            string what = p.type == ItemType.Module ? $"Auto-loot module Lv {p.moduleLevel}: catches floating loot up to {p.rarity}"
                : p.type == ItemType.Habitat ? "Habitat: room for one more minion, faster healing"
                : $"Raft part: {RaftRules.PartHp(p.rarity, g.Tier)} HP{(p.raftKind != RaftKind.Plain ? $" · {EvolutionRules.LineName(p.raftKind)} part" : "")}";
            string fit = !SalvageRules.IsCompatible(p.type, p.rarity, g.Tier) ? $"Too weak for {g.LevelName} ({g.Tier} or better)"
                : !g.FitsType(p) ? "For another ship line" : "Fits";
            var r = new Rect(stripRect.xMax + 8, Mathf.Min(HuntStyles.H - 110, 70), 300, 70);
            GUILayout.BeginArea(r, HuntStyles.Box);
            GUILayout.Label($"<b>{p.displayName}</b> <size=11>· {p.rarity}</size>", HuntStyles.Text);
            GUILayout.Label($"<size=11>{what} · {fit}</size>", HuntStyles.Hint);
            GUILayout.EndArea();
        }
    }
}
