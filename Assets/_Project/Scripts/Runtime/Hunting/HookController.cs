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
        public float castTime = 0.35f;
        public float reelTime = 1.2f;
        [Range(0f, 1f)] public float sedateChance = 0.5f;

        readonly Dictionary<ItemDefinition, int> inventory = new Dictionary<ItemDefinition, int>();
        readonly List<string> log = new List<string>();
        readonly System.Random rng = new System.Random();

        LineRenderer line;
        Phase phase;
        Drift target;
        float t;
        Vector3 reelFrom;
        List<Drop<ItemDefinition>> pendingLoot;
        string resultTitle, resultBody;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.05f;
            line.enabled = false;
        }

        void Update()
        {
            switch (phase)
            {
                case Phase.Idle: UpdateIdle(); break;
                case Phase.Casting: UpdateCasting(); break;
                case Phase.Reeling: UpdateReeling(); break;
            }
        }

        void UpdateIdle()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Camera.main == null) return;
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 200f)) return;
            var drift = hit.collider.GetComponent<Drift>();
            if (drift == null) return;

            float dist = Vector3.Distance(Flat(drift.transform.position), Flat(raft.transform.position));
            if (dist > raft.hookRange)
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
            }
        }

        void UpdateReeling()
        {
            if (!CheckTarget()) return;
            t += Time.deltaTime / reelTime;
            var edge = raft.transform.position + (reelFrom - raft.transform.position).normalized * 2.2f;
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
            phase = Phase.Result;
            line.enabled = false;
            var m = target.Monster;
            string hintNote = target.Hint == target.Actual ? "The hint was right." : $"The hint was wrong ({Drift.HintText(target.Hint)}).";
            pendingLoot = null;

            switch (target.Actual)
            {
                case DriftState.Dead:
                    resultTitle = $"{m.displayName} — Dead";
                    pendingLoot = RollLoot(m);
                    resultBody = $"{hintNote}\nHarvest:\n{Describe(pendingLoot)}";
                    break;
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
                        resultBody = $"{hintNote}\nIt lunges at the raft. Battle starts!";
                    }
                    break;
            }
        }

        List<Drop<ItemDefinition>> RollLoot(Monsters.MonsterDefinition m) =>
            m.loot != null ? m.loot.Roll(rng) : new List<Drop<ItemDefinition>>();

        static string Describe(List<Drop<ItemDefinition>> drops) =>
            drops.Count == 0 ? "  (nothing)" : string.Join("\n", drops.Select(d => $"  {d.Quantity} × {d.Item.displayName} ({d.Item.rarity})"));

        void Finish(string message, bool keepLoot, bool destroy)
        {
            if (keepLoot && pendingLoot != null)
                foreach (var d in pendingLoot)
                    inventory[d.Item] = (inventory.TryGetValue(d.Item, out var n) ? n : 0) + d.Quantity;
            Log(message);
            if (target != null)
            {
                if (destroy) Destroy(target.gameObject);
                else target.Hooked = false;
            }
            target = null;
            pendingLoot = null;
            phase = Phase.Idle;
        }

        void OnGUI()
        {
            DrawHud();
            if (phase != Phase.Result || target == null) return;

            var m = target.Monster;
            var rect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 120, 400, 240);
            GUILayout.BeginArea(rect, resultTitle, GUI.skin.window);
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
            battle.Begin(target, outcome =>
            {
                if (outcome == BattleOutcome.Won)
                {
                    pendingLoot = RollLoot(m);
                    Finish($"Defeated {m.displayName}: {string.Join(", ", pendingLoot.Select(d => $"{d.Quantity} {d.Item.displayName}"))}", true, true);
                }
                else
                {
                    string why = outcome == BattleOutcome.LostRaft ? "broke your raft" : "knocked you out";
                    Finish($"{m.displayName} {why} and swam off. (It will eat raft parts in Phase 8.)", false, true);
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
            GUILayout.BeginArea(new Rect(10, 10, 260, Screen.height - 20));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b>BrineBattle Hunter — Phase 3</b>", Rich);
            GUILayout.Label("Click a drift in range to hook it.");
            GUILayout.Label("Battle mode (Settings)");
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Raft size: {raft.size}");
            if (GUILayout.Button("-", GUILayout.Width(24))) raft.size = Mathf.Max(1, raft.size - 1);
            if (GUILayout.Button("+", GUILayout.Width(24))) raft.size = Mathf.Min(10, raft.size + 1);
            GUILayout.EndHorizontal();
            GUILayout.Label("<b>Inventory</b>", Rich);
            if (inventory.Count == 0) GUILayout.Label("  (empty)");
            foreach (var kv in inventory) GUILayout.Label($"  {kv.Value} × {kv.Key.displayName}");
            GUILayout.Label("<b>Log</b>", Rich);
            foreach (var l in log) GUILayout.Label(l);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true };

        void Log(string message)
        {
            log.Insert(0, message);
            if (log.Count > 6) log.RemoveAt(log.Count - 1);
        }

        Vector3 HookOrigin() => raft.transform.position + Vector3.up * 1.2f;
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
