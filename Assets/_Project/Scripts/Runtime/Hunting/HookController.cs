using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Encounters;
using BrineBattle.Items;
using BrineBattle.Loot;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Phase 2 loop: click a drift to cast, reel it in, then resolve Dead / Weakened / Alive
    /// with the raft-size gate. Placeholder IMGUI until a real UI pass.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class HookController : MonoBehaviour
    {
        enum Phase { Idle, Casting, Reeling, Result, Battle }

        public Raft raft;
        public BattleController battle;
        public RaftBuilder builder;
        public float castTime = 0.35f;
        public float reelTime = 1.8f;
        [Range(0f, 1f)] public float sedateChance = 0.5f;

        public Inventory Inventory { get; } = new Inventory();
        /// <summary>Harpoon wear (idea C-4).</summary>
        public Durability Harpoon { get; } = new Durability(Battle.BattleRules.HarpoonDurability);
        [Tooltip("Given once at start so the auto-loot module can be tried (idea U-13).")]
        public ItemDefinition starterModule;
        /// <summary>Raised after loot is added, so the bag can auto-salvage misfits.</summary>
        public event System.Action LootAdded;
        /// <summary>Raised when a monster is harvested, finished or defeated (collection book).</summary>
        public event System.Action<Monsters.MonsterDefinition> MonsterAcquired;
        /// <summary>Raised by the HUD button; the collection book listens.</summary>
        public event System.Action BookToggleRequested;
        readonly List<string> log = new List<string>();
        readonly System.Random rng = new System.Random();

        LineRenderer line;
        Phase phase;
        Drift target;
        float t;
        Vector3 reelFrom;
        List<Drop<ItemDefinition>> pendingLoot;
        string resultTitle, resultBody;

        // Line tension (idea C-2)
        float tensionStart, tensionWidth, tensionSpeed, tensionTime;
        bool tensionPressed, tensionHit, tensionAuto, tensionSemi, tensionByAuto;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.05f;
            line.enabled = false;
            if (starterModule != null) Inventory.Add(starterModule, 1);
        }

        void Update()
        {
            WorldPause.Prompt = phase == Phase.Result;
            switch (phase)
            {
                case Phase.Idle: UpdateIdle(); break;
                case Phase.Casting: UpdateCasting(); break;
                case Phase.Reeling: UpdateReeling(); break;
            }
        }

        void UpdateIdle()
        {
            if (builder != null && builder.Active) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 200f)) return;
            var drift = hit.collider.GetComponent<Drift>();
            if (drift == null) return;

            float dist = Vector3.Distance(Flat(drift.transform.position), Flat(raft.transform.position));
            if (dist > raft.HookReach)
            {
                Log($"{drift.Monster.displayName} is out of reach.");
                return;
            }
            target = drift;
            target.Hooked = true;
            phase = Phase.Casting;
            t = 0f;
            line.enabled = true;
        }

        void UpdateCasting()
        {
            if (!CheckTarget()) return;
            t += Time.deltaTime / castTime;
            var from = HookOrigin();
            line.SetPosition(0, from);
            line.SetPosition(1, Vector3.Lerp(from, target.transform.position, Mathf.Clamp01(t)));
            if (t >= 1f)
            {
                phase = Phase.Reeling;
                t = 0f;
                reelFrom = target.transform.position;
                int size = target.Monster.sizeClass;
                tensionWidth = TensionRules.ZoneWidth(size);
                tensionSpeed = TensionRules.Speed(size);
                tensionStart = TensionRules.RandomZoneStart(tensionWidth, rng);
                tensionTime = 0f;
                tensionPressed = tensionHit = false;
                tensionAuto = BattleSettings.Mode == BattleMode.Auto;
                tensionSemi = BattleSettings.Mode == BattleMode.SemiAuto;
                tensionByAuto = false;
                if (tensionAuto) AutoPress();
            }
        }

        void UpdateReeling()
        {
            if (!CheckTarget()) return;
            t += Time.deltaTime / reelTime;
            tensionTime += Time.deltaTime;
            if (!tensionPressed && PressedTension())
            {
                tensionPressed = true;
                tensionHit = TensionRules.IsHit(TensionRules.Marker(tensionTime, tensionSpeed), tensionStart, tensionWidth);
            }
            // Semi-auto: the player may press first; otherwise the reel presses itself late.
            if (!tensionPressed && tensionSemi && t >= TensionRules.SemiAutoPressAt) AutoPress();
            var edge = raft.transform.position + (reelFrom - raft.transform.position).normalized * raft.EdgeDistance;
            edge.y = reelFrom.y;
            target.transform.position = Vector3.Lerp(reelFrom, edge, Mathf.SmoothStep(0f, 1f, t));
            line.SetPosition(0, HookOrigin());
            line.SetPosition(1, target.transform.position);
            if (t >= 1f) Resolve();
        }

        bool CheckTarget()
        {
            if (target != null) return true;
            line.enabled = false;
            phase = Phase.Idle;
            return false;
        }

        void Resolve()
        {
            line.enabled = false;
            var m = target.Monster;
            if (!tensionPressed)
            {
                // Never set the hook: the catch slips away whatever it was.
                Finish($"{m.displayName} slipped off the hook. Press Space in the green zone to set it!", false, false);
                return;
            }
            phase = Phase.Result;
            string hintNote = target.Hint == target.Actual ? "The hint was right." : $"The hint was wrong ({Drift.HintText(target.Hint)}).";
            hintNote += (tensionByAuto ? " Auto reel:" : "") + (tensionHit ? $" Clean reel! (+{TensionRules.CleanReelBonus} per drop)" : " Sloppy reel.");
            pendingLoot = null;

            switch (target.Actual)
            {
                case DriftState.Dead:
                    resultTitle = $"{m.displayName} — Dead";
                    pendingLoot = RollLoot(m, tensionHit);
                    Finish($"Harvested {m.displayName}{(tensionHit ? " (clean reel)" : "")}: {Brief(pendingLoot)}", true, true);
                    return;
                case DriftState.Weakened:
                    resultTitle = $"{m.displayName} — Weakened";
                    resultBody = $"{hintNote}\nIt can barely move. Finish it for loot, or tame it.";
                    break;
                default:
                    if (EncounterRules.IsGated(target.Actual, raft.size, m.sizeClass))
                    {
                        resultTitle = $"{m.displayName} — ALIVE and too big!";
                        resultBody = $"{hintNote}\nYour raft (size {raft.size}) can't fight a size {m.sizeClass} monster.";
                    }
                    else
                    {
                        resultTitle = $"{m.displayName} — ALIVE!";
                        Log($"{m.displayName} was ALIVE! {(tensionHit ? "You strike first." : "It strikes first.")}");
                        StartBattle();
                        return;
                        resultBody = $"{hintNote}\nIt lunges at the raft. Battle starts!\n" +
                            (tensionHit ? "Your clean reel lets you strike first." : "It caught you off guard and strikes first.");
                    }
                    break;
            }
        }

        List<Drop<ItemDefinition>> RollLoot(Monsters.MonsterDefinition m) => RollLoot(m, tensionHit);

        List<Drop<ItemDefinition>> RollLoot(Monsters.MonsterDefinition m, bool clean)
        {
            var drops = m.loot != null ? m.loot.Roll(rng) : new List<Drop<ItemDefinition>>();
            if (!clean) return drops;
            return drops.Select(d => new Drop<ItemDefinition>(d.Item, d.Quantity + TensionRules.CleanReelBonus)).ToList();
        }

        static string Brief(List<Drop<ItemDefinition>> drops) =>
            drops.Count == 0 ? "nothing" : string.Join(", ", drops.Select(d => $"{d.Quantity} {d.Item.displayName}"));

        static string Describe(List<Drop<ItemDefinition>> drops) =>
            drops.Count == 0 ? "  (nothing)" : string.Join("\n", drops.Select(d => $"  {d.Quantity} × {d.Item.displayName} ({d.Item.rarity})"));

        /// <summary>A raft module harvested a drift without the hook (idea U-13). No clean-reel bonus.</summary>
        public void AutoLoot(Drift d, string source)
        {
            if (d == null || d.Hooked) return;
            var drops = RollLoot(d.Monster, false);
            MonsterAcquired?.Invoke(d.Monster);
            foreach (var x in drops) Inventory.Add(x.Item, x.Quantity);
            string got = drops.Count == 0 ? "nothing" : string.Join(", ", drops.Select(x => $"{x.Quantity} {x.Item.displayName}"));
            Log($"{source} caught a {d.Monster.displayName}: {got}.");
            LootAdded?.Invoke();
            Destroy(d.gameObject);
        }

        void Finish(string message, bool keepLoot, bool destroy)
        {
            if (keepLoot && pendingLoot != null)
            {
                if (target != null) MonsterAcquired?.Invoke(target.Monster);
                foreach (var d in pendingLoot) Inventory.Add(d.Item, d.Quantity);
                Log(message);
                LootAdded?.Invoke();
            }
            else Log(message);
            if (target != null)
            {
                if (destroy) Destroy(target.gameObject);
                else target.Hooked = false;
            }
            target = null;
            pendingLoot = null;
            phase = Phase.Idle;
        }

        void AutoPress()
        {
            tensionPressed = true;
            tensionByAuto = true;
            tensionHit = rng.NextDouble() < TensionRules.AutoCleanChance;
        }

        bool PressedTension()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && kb.spaceKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        void DrawTension()
        {
            float w = 360, h = 26;
            var r = new Rect((Screen.width - w) / 2f, Screen.height - 120, w, h);
            string result = tensionHit ? "clean reel!" : "sloppy reel";
            string label = tensionAuto ? (t < 0.5f ? "<b>AUTO</b> reel..." : $"<b>AUTO</b> — {result}")
                : tensionSemi ? (!tensionPressed ? "<b>SEMI-AUTO</b> — press Space first, or it reels for you"
                    : tensionByAuto ? $"<b>SEMI-AUTO</b> reeled for you — {result}" : $"<b>SEMI-AUTO</b> — you: {result}")
                : tensionPressed ? (tensionHit ? "<b>Clean reel!</b>" : "<b>Sloppy...</b>") : "Press <b>Space</b> or click in the green zone!";
            var prev = GUI.color;
            if (tensionAuto) GUI.color = new Color(1f, 1f, 1f, 0.45f);
            GUI.Label(new Rect(r.x, r.y - 22, w, 20), label, Rich);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.6f), 0, 0);
            var zone = new Rect(r.x + tensionStart * w, r.y, tensionWidth * w, h);
            GUI.DrawTexture(zone, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.3f, 0.85f, 0.4f, 0.9f), 0, 0);
            float m = TensionRules.Marker(tensionTime, tensionSpeed);
            GUI.DrawTexture(new Rect(r.x + m * w - 2, r.y - 4, 4, h + 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.white, 0, 0);
            GUI.color = prev;
        }

        void OnGUI()
        {
            DrawHud();
            if (phase == Phase.Reeling && target != null) DrawTension();
            if (phase != Phase.Result || target == null) return;

            var m = target.Monster;
            GUI.depth = -5;
            var rect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 120, 400, 240);
            GUILayout.BeginArea(rect, resultTitle, HuntStyles.Window);
            GUILayout.Space(6);
            GUILayout.Label(resultBody);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            switch (target.Actual)
            {
                case DriftState.Dead:
                    if (GUILayout.Button("Harvest")) Finish($"Harvested {m.displayName}.", true, true);
                    break;
                case DriftState.Weakened:
                    if (GUILayout.Button("Finish it"))
                    {
                        pendingLoot = RollLoot(m);
                        Finish($"Finished {m.displayName}: {string.Join(", ", pendingLoot.Select(d => $"{d.Quantity} {d.Item.displayName}"))}", true, true);
                    }
                    if (GUILayout.Button("Tame (Phase 7)")) Finish($"Taming {m.displayName} isn't possible yet (Phase 7). Let it go.", false, true);
                    break;
                default:
                    if (EncounterRules.IsGated(target.Actual, raft.size, m.sizeClass))
                    {
                        if (GUILayout.Button("Cut line")) Finish($"Cut the line. {m.displayName} drifts away.", false, false);
                        if (GUILayout.Button("Flee")) Finish($"Fled from {m.displayName}. (It will eat raft parts in Phase 8.)", false, true);
                        if (GUILayout.Button($"Sedate ({sedateChance:P0})"))
                        {
                            if (rng.NextDouble() < sedateChance) Sedated();
                            else Finish($"Sedative failed! {m.displayName} snapped the line.", false, true);
                        }
                    }
                    else if (GUILayout.Button("Fight!")) StartBattle();
                    break;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void StartBattle()
        {
            if (battle == null)
            {
                Finish($"No BattleController in the scene; released {target.Monster.displayName}.", false, true);
                return;
            }
            phase = Phase.Battle;
            var m = target.Monster;
            battle.Begin(target, tensionHit ? Initiative.Hunter : Initiative.Monster, outcome =>
            {
                if (outcome == BattleOutcome.Won)
                {
                    pendingLoot = RollLoot(m);
                    Finish($"Victory! Defeated {m.displayName}: {Brief(pendingLoot)}", true, true);
                }
                else
                {
                    string why = outcome == BattleOutcome.LostRaft ? "broke your raft" : "knocked you out";
                    Finish($"Defeat. {m.displayName} {why} and swam off. (It will eat raft parts in Phase 8.)", false, true);
                }
            });
        }

        void Sedated()
        {
            var m = target.Monster;
            target.MakeWeakened();
            resultTitle = $"{m.displayName} — Sedated";
            resultBody = "The sedative worked. It is now Weakened.";
            Log($"Sedated {m.displayName}.");
        }

        void DrawHud()
        {
            DrawLog();
            if (BattleController.InBattle) return;
            GUILayout.BeginArea(new Rect(10, 10, HudRight - 10, Screen.height - LogHeight - 30));
            GUILayout.BeginVertical(HuntStyles.Box);
            GUILayout.Label("<b>BrineBattle Hunter — Phase 5</b>", Rich);
            GUILayout.Label("Click a drift in range to hook it.");
            GUILayout.Label("Battle mode (Settings)");
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Raft: size {raft.size} · {raft.Grid.Count} parts · {raft.Tier} tier · {raft.MaxHp} HP");
            GUILayout.EndHorizontal();
            int stacks = Inventory.Items.Count(), essence = Inventory.Essence.Sum(kv => kv.Value);
            GUILayout.Label($"<b>Bag</b>: {stacks} stacks, {essence} essence", Rich);
            GUILayout.BeginHorizontal();
            GUILayout.Label(Harpoon.Broken ? "Harpoon: <color=#f77>BROKEN</color>" : $"Harpoon: {Harpoon}", Rich);
            GUI.enabled = !Harpoon.Full && Inventory.MaterialCount() >= 1;
            if (GUILayout.Button("Repair +10 (1 mat)", GUILayout.Width(130)) && Inventory.SpendMaterials(1)) Harpoon.Repair(10);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bag (B)")) BagToggleRequested?.Invoke();
            if (GUILayout.Button("Book (C)")) BookToggleRequested?.Invoke();
            if (builder != null && GUILayout.Button("Build (R)")) builder.Toggle();
            GUILayout.EndHorizontal();
            GUILayout.Label(BattleSettings.Mode == BattleMode.Auto ? "Reeling: AUTO (50% clean)"
                : BattleSettings.Mode == BattleMode.SemiAuto ? "Reeling: SEMI-AUTO (press first, or auto 50%)"
                : "Reeling: press Space in the green zone.");
            if (builder != null && builder.Active) GUILayout.Label("<i>Building — see the panel on the right.</i>", Rich);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        const int LogRows = 3;
        const float LogHeight = 18f * LogRows + 12f;

        /// <summary>Activity log: its own small box in the bottom-left corner, newest on top, 3 rows.</summary>
        void DrawLog()
        {
            if (log.Count == 0) return;
            var r = new Rect(10, Screen.height - LogHeight - 10, HudRight - 10, LogHeight);
            GUI.Box(r, GUIContent.none, HuntStyles.Box);
            for (int i = 0; i < Mathf.Min(LogRows, log.Count); i++)
            {
                var row = new Rect(r.x + 8, r.y + 6 + i * 18, r.width - 16, 18);
                GUI.Label(row, new GUIContent(log[i], log[i]), LogStyle);
            }
        }

        static GUIStyle logStyle;
        static GUIStyle LogStyle => logStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false, clipping = TextClipping.Clip, richText = true };

        /// <summary>Right edge of the HUD column; panels open to the right of it.</summary>
        public const float HudRight = 270f;

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true };

        /// <summary>Raised by the HUD button; the bag panel listens.</summary>
        public event System.Action BagToggleRequested;

        public void Log(string message)
        {
            if (log.Count > 0 && log[0] == message) return;
            log.Insert(0, message);
            if (log.Count > 6) log.RemoveAt(log.Count - 1);
        }

        Vector3 HookOrigin() => raft.transform.position + Vector3.up * 1.2f;
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
