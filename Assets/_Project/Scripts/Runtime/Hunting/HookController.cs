using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
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
        /// <summary>Weapon and armor slots (Phase 6); their power feeds the hunter in battle.</summary>
        public Equipment Gear { get; } = new Equipment();
        /// <summary>Tamed monsters (Phase 7); the active one joins battles.</summary>
        public CompanionRoster Companions { get; } = new CompanionRoster();
        /// <summary>Raised when a monster is tamed (collection book Pets page).</summary>
        public event System.Action<Monsters.MonsterDefinition> MonsterTamed;
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
        /// <summary>A tame that worked, waiting for the player to pick its role (U-23).</summary>
        Companion pendingPet;
        float crewTimer;
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
            // Settings menu (Phase 6) needs no scene wiring, so older Hook scenes get it too.
            if (FindFirstObjectByType<SettingsPanel>() == null) new GameObject("Settings").AddComponent<SettingsPanel>();
            // Companions on the raft (Phase 7), likewise added at runtime.
            if (!TryGetComponent<RaftCompanions>(out _)) gameObject.AddComponent<RaftCompanions>().hook = this;
        }

        void Update()
        {
            WorldPause.Prompt = phase == Phase.Result;
            CrewMend();
            switch (phase)
            {
                case Phase.Idle: UpdateIdle(); break;
                case Phase.Casting: UpdateCasting(); break;
                case Phase.Reeling: UpdateReeling(); break;
            }
        }

        void UpdateIdle()
        {
            if (WorldPause.Open != Menu.None) return;
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
            if (!drift.TryClaim(this))
            {
                Log($"{drift.Monster.displayName} is already being caught by a raft module.");
                return;
            }
            target = drift;
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
                    if (EncounterRules.IsAggressive(target.Aggression))
                    {
                        KillWeakened("too aggressive to tame");
                        return;
                    }
                    // Auto mode: no prompt when there is nothing to choose (owner's call); the log says what happened.
                    if (BattleSettings.Mode == BattleMode.Auto && Companions.Full)
                    {
                        KillWeakened("your pets are full (no room to tame)");
                        return;
                    }
                    resultTitle = $"{m.displayName} — Weakened, calm";
                    resultBody = $"{hintNote}\nIt can barely move and isn't hostile. Finish it for loot, or tame it.";
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
        public void AutoLoot(Drift d, string source, object claimer)
        {
            if (d == null || !d.TryClaim(claimer)) return;
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
                else target.Release(this);
            }
            target = null;
            pendingLoot = null;
            pendingPet = null;
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
            var r = new Rect((HuntStyles.W - w) / 2f, HuntStyles.H - 120, w, h);
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
            HuntStyles.Begin();
            DrawHud();
            if (phase == Phase.Reeling && target != null) DrawTension();
            if (phase != Phase.Result || target == null) return;

            var m = target.Monster;
            GUI.depth = -5;
            var rect = new Rect(HuntStyles.W / 2f - 200, HuntStyles.H / 2f - 120, 400, 240);
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
                case DriftState.Weakened when pendingPet != null:
                    foreach (CompanionRole role in System.Enum.GetValues(typeof(CompanionRole)))
                        if (GUILayout.Button(role.ToString(), GUILayout.Height(28))) Adopt(role);
                    break;
                case DriftState.Weakened:
                    if (GUILayout.Button("Finish it"))
                    {
                        pendingLoot = RollLoot(m);
                        raft.Grid.Kills++;
                        Finish($"Finished {m.displayName}: {string.Join(", ", pendingLoot.Select(d => $"{d.Quantity} {d.Item.displayName}"))}", true, true);
                    }
                    GUI.enabled = !Companions.Full;
                    if (GUILayout.Button(Companions.Full ? "Tame (no room)" : $"Tame ({CompanionRules.TameChance(m.rarity):P0})")) Tame();
                    GUI.enabled = true;
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

        /// <summary>A module catch is always fought, whatever its size (owner's call); only a scene without a BattleController can't.</summary>
        public bool CanFight(Drift d) => d != null && battle != null;

        /// <summary>
        /// An auto-loot module hooked a live monster: it can't swim off, so the battle starts at once and
        /// plays in Auto (owner's call), cancelling the hunter's own cast or reel. False only during another
        /// battle or a prompt (the world is paused then); the module holds it and starts right after.
        /// </summary>
        public bool ModuleBattle(Drift d, object module)
        {
            if (!CanFight(d) || BattleController.InBattle || WorldPause.Active) return false;
            // The module's catch comes first (owner's call): drop the hunter's own cast or reel.
            if (phase == Phase.Casting || phase == Phase.Reeling)
            {
                line.enabled = false;
                if (target != null && target != d) { target.Release(this); Log($"You let go of the {target.Monster.displayName}: a module caught a live one!"); }
                target = null;
                phase = Phase.Idle;
            }
            if (phase != Phase.Idle) return false;
            d.Release(module);
            if (!d.TryClaim(this)) return false;
            target = d;
            tensionHit = false;   // it ambushes the raft: the monster strikes first
            pendingLoot = null;
            battle.ForceAuto = true;
            StartBattle();
            return true;
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
                    raft.Grid.Kills++;
                    Finish($"Victory! Defeated {m.displayName}: {Brief(pendingLoot)}", true, true);
                }
                else
                {
                    string why = outcome == BattleOutcome.LostRaft ? "broke your raft" : "knocked you out";
                    Finish($"Defeat. {m.displayName} {why} and swam off. (It will eat raft parts in Phase 8.)", false, true);
                }
            });
        }

        /// <summary>Idea U-17: a weakened aggressive monster is finished off without a prompt.</summary>
        void KillWeakened(string why)
        {
            var m = target.Monster;
            pendingLoot = RollLoot(m);
            raft.Grid.Kills++;
            Finish($"{m.displayName} was weakened but {why}, so you finished it: {Brief(pendingLoot)}", true, true);
        }

        void Tame()
        {
            var m = target.Monster;
            if (rng.NextDouble() >= CompanionRules.TameChance(m.rarity))
            {
                Finish($"{m.displayName} shied away from your hand and slipped back into the sea.", false, true);
                return;
            }
            pendingPet = Companion.FromMonster(m);
            resultTitle = $"Tamed {m.displayName}! Pick its role";
            resultBody = $"{pendingPet.Stats}\n"
                + $"Familiar: {CompanionRules.Describe(CompanionRole.Familiar)}.\n"
                + $"Crew: {CompanionRules.Describe(CompanionRole.Crew)}.\n"
                + $"Mystic: {CompanionRules.Describe(CompanionRole.Mystic)}.";
        }

        void Adopt(CompanionRole role)
        {
            var m = target.Monster;
            pendingPet.Role = role;
            Companions.Add(pendingPet);
            pendingPet = null;
            MonsterTamed?.Invoke(m);
            ApplyCompanionBonuses();
            Finish($"Tamed {m.displayName} as your {role}: it {CompanionRules.Describe(role)}.", false, true);
        }

        /// <summary>Mystic: cheaper refines. Called when the roster changes.</summary>
        public void ApplyCompanionBonuses() =>
            Gear.RefineDiscount = Companions.Best(CompanionRole.Mystic) != null ? CompanionRules.MysticRefineDiscount : 0;

        /// <summary>Crew: between fights, +1 harpoon durability now and then (modules: see RaftCompanions).</summary>
        void CrewMend()
        {
            var crew = Companions.Best(CompanionRole.Crew);
            if (crew == null || WorldPause.Open != Menu.None || BattleController.InBattle) return;
            crewTimer += Time.deltaTime;
            if (crewTimer < CompanionRules.CrewMendSeconds) return;
            crewTimer = 0f;
            if (Harpoon.Full) return;
            Harpoon.Repair(1);
            Log($"{crew.Name} (Crew) mended the harpoon (+1).");
        }

        void Sedated()
        {
            var m = target.Monster;
            target.MakeWeakened();
            if (EncounterRules.IsAggressive(target.Aggression))
            {
                Log($"Sedated {m.displayName}.");
                KillWeakened("sedated but still aggressive");
                return;
            }
            resultTitle = $"{m.displayName} — Sedated";
            resultBody = "The sedative worked. It is now Weakened.";
            Log($"Sedated {m.displayName}.");
        }

        void DrawHud()
        {
            DrawLog();
            if (BattleController.InBattle) return;
            // Kept short and big (owner idea U-25): what to do, two bars, four buttons. Details live in the menus.
            GUILayout.BeginArea(new Rect(10, 10, HudRight - 10, HuntStyles.H - LogHeight - 30));
            GUILayout.BeginVertical(HuntStyles.Box);
            GUILayout.Label("<size=16><b>BrineBattle Hunter</b></size>", Rich);
            GUILayout.Label(builder != null && builder.Active ? "<color=#ffd866>Building: use the panel on the right.</color>"
                : "<color=#ffd866>Click a floating monster to hook it!</color>", Rich);
            GUILayout.Space(4);
            Meter($"Raft Lv {(int)raft.Tier + 1} ({raft.Tier}, size {raft.size})", 1f, $"{raft.MaxHp} HP", new Color(0.85f, 0.75f, 0.5f));
            GUILayout.BeginHorizontal();
            Meter(Harpoon.Broken ? "Harpoon <color=#f77>BROKEN</color>" : "Harpoon", Harpoon.Current / (float)Mathf.Max(1, Harpoon.Max), $"{Harpoon.Current}/{Harpoon.Max}", new Color(0.4f, 0.65f, 1f));
            GUI.enabled = !Harpoon.Full && Inventory.MaterialCount() >= 1;
            if (GUILayout.Button(new GUIContent("Fix", "Repair the harpoon +10 for 1 material"), GUILayout.Width(44), GUILayout.Height(34)) && Inventory.SpendMaterials(1)) Harpoon.Repair(10);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (Companions.All.Count > 0)
                GUILayout.Label("Pets: " + string.Join(", ", Companions.All.Select(c => $"{c.Name} ({c.Role})")), Rich);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bag\n<size=10>B</size>", BigButton)) BagToggleRequested?.Invoke();
            if (GUILayout.Button("Book\n<size=10>C</size>", BigButton)) BookToggleRequested?.Invoke();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (builder != null && GUILayout.Button("Build\n<size=10>R</size>", BigButton)) builder.Toggle();
            if (GUILayout.Button("Settings\n<size=10>Esc</size>", BigButton)) WorldPause.Toggle(Menu.Settings);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=11>Battles: {BattleSettings.Labels[(int)BattleSettings.Mode]}</size>", Rich);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"<size=11>{SettingsPanel.AutoSaveStatus}</size>", RichRight);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>A labelled bar: easier to read at a glance than numbers.</summary>
        void Meter(string label, float fill, string value, Color color)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"{label}  <size=11>{value}</size>", Rich);
            var r = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.5f), 0, 3);
            var f = new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height);
            GUI.DrawTexture(f, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 3);
            GUILayout.EndVertical();
        }

        static GUIStyle richRight;
        static GUIStyle RichRight => richRight ??= new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleRight };

        static GUIStyle bigButton;
        static GUIStyle BigButton => bigButton ??= new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, richText = true, fixedHeight = 46 };

        const int LogRows = 3;
        /// <summary>Oldest entries are deleted past this.</summary>
        const int MaxLog = 99;
        Vector2 logScroll;
        const float LogHeight = 18f * LogRows + 12f;

        /// <summary>Activity log: its own small box in the bottom-left corner, newest on top, 3 rows.</summary>
        void DrawLog()
        {
            if (log.Count == 0) return;
            var r = new Rect(10, HuntStyles.H - LogHeight - 10, HudRight - 10, LogHeight);
            GUI.Box(r, GUIContent.none, HuntStyles.Box);
            var view = new Rect(r.x + 6, r.y + 6, r.width - 8, r.height - 12);
            var content = new Rect(0, 0, view.width - 16, log.Count * 18);
            logScroll = GUI.BeginScrollView(view, logScroll, content, false, true);
            for (int i = 0; i < log.Count; i++)
                GUI.Label(new Rect(2, i * 18, content.width - 2, 18), new GUIContent(log[i], log[i]), LogStyle);
            GUI.EndScrollView();
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
            if (log.Count > MaxLog) log.RemoveAt(log.Count - 1);
            logScroll = Vector2.zero;   // jump to the newest so you see it arrive
        }

        Vector3 HookOrigin() => raft.transform.position + Vector3.up * 1.2f;
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
