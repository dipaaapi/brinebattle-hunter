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
        /// <summary>Difficulty, sea and the raft types that left (Ed's ask).</summary>
        public Rafts.Voyage Voyage { get; } = new Rafts.Voyage();
        /// <summary>The running game's voyage, for drifts that scale their monster.</summary>
        public static Rafts.Voyage CurrentVoyage { get; private set; }
        /// <summary>Harpoon wear (idea C-4).</summary>
        public Durability Harpoon { get; } = new Durability(Battle.BattleRules.HarpoonDurability);
        /// <summary>Weapon and armor slots (Phase 6); their power feeds the hunter in battle.</summary>
        public Equipment Gear { get; } = new Equipment();
        /// <summary>Tamed monsters (Phase 7); the active one joins battles.</summary>
        public CompanionRoster Companions { get; } = new CompanionRoster();
        /// <summary>Alchemy Mastery (Idea Round 3): better hints, more recipes, essence combining.</summary>
        public AlchemyMastery Mastery { get; } = new AlchemyMastery();
        /// <summary>The hunter's HP and ailment between battles (U-32): they carry into the next fight.</summary>
        public Combatant HunterBody { get; } = Combatant.Hunter();
        float hunterRegen, petRegen, ailmentTick;
        /// <summary>Monsters that ate raft parts and will come back for them (Phase 8).</summary>
        public Rafts.GorgedLog Gorged { get; } = new Rafts.GorgedLog();
        /// <summary>Raised when a monster is tamed (collection book Pets page).</summary>
        public event System.Action<Monsters.MonsterDefinition> MonsterTamed;
        [Tooltip("Given once at start so the auto-loot module can be tried (idea U-13).")]
        public ItemDefinition starterModule;
        [Tooltip("The hammer a new game starts with (building needs a hammer of the raft's level).")]
        public ItemDefinition starterHammer;
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
            if (starterHammer != null) Inventory.Add(starterHammer, 1);
            // Building needs a hammer from this bag (Ed's ask).
            raft.Grid.Tools = Inventory;
            // Each raft level has a boss to beat before the next (Ed's ask).
            raft.Grid.BossesNeeded = true;
            CurrentVoyage = Voyage;
            Voyage.Difficulty = GameStart.Difficulty;
            // Leaving the sea and the ending screen (Ed's ask), likewise added at runtime.
            if (FindFirstObjectByType<VoyageController>() == null)
            {
                var v = new GameObject("Voyage").AddComponent<VoyageController>();
                v.hook = this;
                v.raft = raft;
            }
            // Settings menu (Phase 6) needs no scene wiring, so older Hook scenes get it too.
            if (FindFirstObjectByType<SettingsPanel>() == null) new GameObject("Settings").AddComponent<SettingsPanel>();
            // Pets menu (Ed's ask: its own menu, not in the Bag), likewise added at runtime.
            if (FindFirstObjectByType<PetsPanel>() == null) new GameObject("Pets").AddComponent<PetsPanel>().hook = this;
            // Quest tab (Ed's ask), likewise.
            if (FindFirstObjectByType<QuestPanel>() == null) new GameObject("Quests").AddComponent<QuestPanel>().hook = this;
            if (FindFirstObjectByType<IdleWatch>() == null) new GameObject("Idle Watch").AddComponent<IdleWatch>();
            // Day, night and weather (Idea Round 4), likewise added at runtime.
            if (FindFirstObjectByType<WorldClock>() == null)
            {
                var clock = new GameObject("World Clock").AddComponent<WorldClock>();
                clock.raft = raft;
                clock.hook = this;
            }
            // Zoom and rotate the view (Ed's ask), likewise added at runtime.
            if (Camera.main != null && !Camera.main.TryGetComponent<CameraRig>(out _)) Camera.main.gameObject.AddComponent<CameraRig>().raft = raft;
            // The hunter's idle, pulling and knocked-out motions (Ed's ask), likewise added at runtime.
            if (!TryGetComponent<HunterMotion>(out _)) gameObject.AddComponent<HunterMotion>().hook = this;
            // Companions on the raft (Phase 7), likewise added at runtime.
            if (!TryGetComponent<RaftCompanions>(out _)) gameObject.AddComponent<RaftCompanions>().hook = this;
            // Storage box = crafting table (owner's ask), likewise added at runtime.
            if (StorageBox.Instance == null && !TryGetComponent<StorageBox>(out _)) gameObject.AddComponent<StorageBox>().raft = raft;
            // Salvage Bin (Ed's ask: dismantling has its own box), likewise added at runtime.
            if (SalvageBin.Instance == null && !TryGetComponent<SalvageBin>(out _))
            {
                var bin = gameObject.AddComponent<SalvageBin>();
                bin.raft = raft;
                bin.hook = this;
            }
            // Habitats on the raft give companion room.
            raft.Grid.Changed += SyncHabitats;
            SyncHabitats();
            Mastery.LeveledUp += OnMasteryLevel;
        }

        // Main menu: Continue loads the save here (New game starts fresh).
        void Start() => StartCoroutine(GameStart.Apply(this));

        void Update()
        {
            WorldPause.Prompt = phase == Phase.Result;
            CrewMend();
            Rest();
            FamiliarFetch();
            switch (phase)
            {
                case Phase.Idle: UpdateIdle(); break;
                case Phase.Casting: UpdateCasting(); break;
                case Phase.Reeling: UpdateReeling(); break;
            }
        }

        /// <summary>After a lost battle the hunter rests before hooking again (Ed's call). Unscaled time.</summary>
        float restUntil;
        public float RestLeft => Mathf.Max(0f, restUntil - Time.unscaledTime);

        void UpdateIdle()
        {
            Drift.Hunter = Fighter();
            Drift.Pet = Companions.Active != null ? Combatant.FromCompanion(Companions.Active) : null;
            WorldClock.Empower(Drift.Pet);
            if (WorldPause.Open != Menu.None) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || Camera.main == null) return;
            if (RestLeft > 0f) { Log($"Still resting after that defeat: {Mathf.CeilToInt(RestLeft)} s."); return; }
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 200f)) return;
            // A thunderstrike fire (U-15): click it to throw seawater on it.
            if (hit.collider.GetComponent<SalvageBinClick>() != null) { SalvageBin.Toggle(); return; }
            var fire = hit.collider.GetComponent<RaftFire>();
            if (fire != null)
            {
                Log(fire.Douse() ? "You put out the fire." : $"Splash! Once more to put it out.");
                return;
            }
            var drift = hit.collider.GetComponent<Drift>();
            if (drift == null) return;
            if (WorldClock.Burning) { Log("Put out the fire on the raft first!"); return; }

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
                // Rough seas and storms jerk the line about: a smaller sweet spot (U-15).
                tensionWidth = TensionRules.ZoneWidth(size) * World.WorldRules.ReelZoneScale(WorldClock.Weather);
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
            // Reeled in until its body touches the raft's edge, not over the planks (owner's ask: no overlap).
            float stop = Mathf.Max(raft.EdgeDistance, (raft.Grid.Radius + 0.5f) * Raft.CellSize + target.BodyRadius + 0.1f);
            var edge = raft.transform.position + (reelFrom - raft.transform.position).normalized * stop;
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
                    Mastery.Add(AlchemyMastery.HarvestPoints);
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
                        KillWeakened("no room for another minion");
                        return;
                    }
                    resultTitle = $"{m.displayName} — Weakened, calm";
                    resultBody = $"{hintNote}\nIt can barely move and isn't hostile. Finish it for loot, or tame it.";
                    break;
                default:
                    Ambush(target);
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
            if (clean) drops = drops.Select(d => new Drop<ItemDefinition>(d.Item, d.Quantity + TensionRules.CleanReelBonus)).ToList();
            // Harder difficulties give less loot (Ed's ask); bosses always drop in full.
            if (m.isBoss) return drops;
            return drops.Select(d => new Drop<ItemDefinition>(d.Item, Rafts.DifficultyRules.ScaleLoot(d.Quantity, Voyage.Difficulty, rng)))
                .Where(d => d.Quantity > 0).ToList();
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
            if (drops.Count > 0) StorageBox.Instance?.Store(got, d.transform.position);
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
                if (pendingLoot.Count > 0) StorageBox.Instance?.Store(Brief(pendingLoot), target != null ? target.transform.position : raft.transform.position);
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
            var rect = new Rect(HuntStyles.W / 2f - 220, HuntStyles.H / 2f - 130, 440, 260);
            GUILayout.BeginArea(rect, resultTitle, HuntStyles.Window);
            GUILayout.Space(6);
            GUILayout.Label(resultBody);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            switch (target.Actual)
            {
                case DriftState.Dead:
                    if (HuntStyles.ColorButton("Harvest", HuntStyles.Go)) { Mastery.Add(AlchemyMastery.HarvestPoints); Finish($"Harvested {m.displayName}.", true, true); }
                    break;
                case DriftState.Weakened when pendingPet != null:
                    if (HuntStyles.ColorButton("Welcome aboard", HuntStyles.Go)) Adopt();
                    break;
                case DriftState.Weakened:
                    if (HuntStyles.ColorButton("Finish it", HuntStyles.Risky))
                    {
                        pendingLoot = RollLoot(m);
                        CountKill();
                        Thrash(target);
                        Finish($"Finished {m.displayName}: {string.Join(", ", pendingLoot.Select(d => $"{d.Quantity} {d.Item.displayName}"))}", true, true);
                    }
                    GUI.enabled = !Companions.Full && !m.isBoss;   // bosses can't be tamed
                    if (HuntStyles.ColorButton(m.isBoss ? "Tame (a boss can't be tamed)" : Companions.Full ? "Tame (no room)" : $"Tame ({CompanionRules.TameChance(m.rarity):P0})", HuntStyles.Go)) Tame();
                    GUI.enabled = true;
                    break;
                default:
                    if (EncounterRules.IsGated(target.Actual, raft.size, m.sizeClass))
                    {
                        if (HuntStyles.ColorButton("Cut line", HuntStyles.Other)) Finish($"Cut the line. {m.displayName} drifts away.", false, false);
                        if (HuntStyles.ColorButton("Flee", HuntStyles.Other)) Finish($"Fled from {m.displayName}.{Eat(target)}", false, true);
                        if (HuntStyles.ColorButton($"Sedate ({sedateChance:P0})", HuntStyles.Go))
                        {
                            if (rng.NextDouble() < sedateChance) Sedated();
                            else Finish($"Sedative failed! {m.displayName} snapped the line.", false, true);
                        }
                    }
                    else if (HuntStyles.ColorButton("Fight!", HuntStyles.Go)) StartBattle();
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
            if (!CanFight(d) || BattleController.InBattle || WorldPause.Active || RestLeft > 0f) return false;
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

        /// <summary>
        /// A module caught a weakened, calm drift (Ed: modules must hook what touches them). The hunter
        /// gets the same Finish / Tame choice as a hand catch; in Auto with no pet room it's finished for loot.
        /// False while the hunter is busy: the module keeps holding it.
        /// </summary>
        public bool ModuleCatchCalm(Drift d, object module)
        {
            if (phase != Phase.Idle || BattleController.InBattle || WorldPause.Open != Menu.None) return false;
            d.Release(module);
            if (!d.TryClaim(this)) return false;
            target = d;
            tensionHit = false;
            pendingLoot = null;
            var m = d.Monster;
            phase = Phase.Result;
            if (BattleSettings.Mode == BattleMode.Auto && Companions.Full)
            {
                KillWeakened("no room for another minion");
                return true;
            }
            resultTitle = $"{m.displayName} — Weakened, calm";
            resultBody = "A raft module caught it. It can barely move and isn't hostile. Finish it for loot, or tame it.";
            return true;
        }

        /// <summary>Room for companions follows the habitats on the raft.</summary>
        public void SyncHabitats()
        {
            if (raft != null) Companions.Habitats = raft.Grid.HabitatCount;
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
            // Module battles don't cost raft parts (Phase 8, Claude's default until Ed decides).
            bool moduleFight = battle.ForceAuto;
            int gorged = target.GorgedParts;
            battle.Begin(target, tensionHit ? Initiative.Hunter : Initiative.Monster, outcome =>
            {
                if (outcome == BattleOutcome.Won)
                {
                    pendingLoot = RollLoot(m);
                    string back = gorged > 0 ? Recover(m) : "";
                    CountKill();
                    Thrash(target);
                    Finish($"Victory! Defeated {(gorged > 0 ? "the gorged " : "")}{m.displayName}: {Brief(pendingLoot)}{back}", true, true);
                }
                else
                {
                    string why = outcome == BattleOutcome.LostRaft ? "broke your raft" : "knocked you out";
                    Finish($"Defeat. {m.displayName} {why} and swam off.{(moduleFight ? "" : Eat(target))}", false, true);
                    Defeated();
                    IdleWatch.Lost();
                }
            });
        }

        void OnMasteryLevel(int level)
        {
            string unlocked = builder != null ? string.Join(", ", AlchemyMastery.NewAt(builder.recipes, level)
                .Select(r => r.outputs.Count > 0 && r.outputs[0].item != null ? r.outputs[0].item.displayName : r.displayName)) : "";
            Log($"Alchemy Mastery Lv {level}! Hints are right {AlchemyMastery.HintAccuracy(level):P0} of the time."
                + (unlocked.Length > 0 ? $" New recipes: {unlocked}." : "")
                + (level == AlchemyMastery.CombineLevel ? " You can now combine essence (Bag → Essence)." : ""));
        }

        /// <summary>A recipe the hunter can see: Alchemy Mastery high enough and, for typed raft items, its tree branch open (U-31).</summary>
        public bool Knows(RecipeDefinition r) => Mastery.Knows(r) && (raft == null || Rafts.RaftTree.Knows(raft.Grid, r));

        /// <summary>The hunter in battle: gear plus the raft type's bonus (Phase 8).</summary>
        public Combatant Fighter()
        {
            var h = Rafts.RaftTypeRules.Apply(Combatant.Hunter(Gear), raft != null ? raft.Grid.Type.Bonus : default);
            h.Hp = Mathf.Clamp(HunterBody.Hp, 0, h.MaxHp);
            h.Status = HunterBody.Status;
            h.StatusRounds = HunterBody.StatusRounds;
            // Idea Round 4: gear with a sun or moon element makes the hunter stronger at that time.
            WorldClock.Empower(h);
            return h;
        }

        /// <summary>After a battle: the hunter and the pet that fought keep their HP and ailments (U-32).</summary>
        public void TakeVitals(Combatant hunter, Companion pet, Combatant petBody)
        {
            if (hunter != null)
            {
                HunterBody.Hp = hunter.Hp;
                HunterBody.Status = hunter.Status;
                HunterBody.StatusRounds = hunter.StatusRounds;
            }
            if (pet != null && petBody != null)
            {
                pet.Hp = petBody.Hp;
                pet.Status = petBody.Status;
                pet.StatusRounds = petBody.StatusRounds;
            }
        }

        /// <summary>
        /// Outside battle (U-32, C-10): the hunter and pets heal a little at a time, faster for pets with
        /// habitats on the raft; ailments keep ticking every few seconds but never take the last HP.
        /// </summary>
        void Rest()
        {
            if (BattleController.InBattle || WorldPause.Asleep) return;
            float dt = Time.deltaTime;
            hunterRegen += dt;
            if (hunterRegen >= HazardRules.HunterRegenSeconds)
            {
                hunterRegen = 0f;
                if (HunterBody.Hp < HunterBody.MaxHp && HunterBody.Status == StatusEffect.None) HunterBody.Hp++;
            }
            petRegen += dt;
            if (petRegen >= HazardRules.PetRegenSeconds(raft.Grid.HabitatCount))
            {
                petRegen = 0f;
                foreach (var c in Companions.All)
                {
                    bool wasOut = c.KnockedOut;
                    if (c.Hp < c.MaxHp && c.Status == StatusEffect.None) c.Hp++;
                    if (wasOut && !c.KnockedOut) Log($"{c.Name} is back on its feet.");
                }
            }
            ailmentTick += dt;
            if (ailmentTick < StatusRules.OutsideRoundSeconds) return;
            ailmentTick = 0f;
            var before = HunterBody.Status;
            StatusRules.Tick(HunterBody, 1);
            if (before != StatusEffect.None && HunterBody.Status == StatusEffect.None) Log($"You are no longer {StatusRules.Name(before).ToLowerInvariant()}.");
            foreach (var c in Companions.All)
            {
                if (c.Status == StatusEffect.None) continue;
                var was = c.Status;
                int hp = c.Hp, rounds = c.StatusRounds;
                var s = c.Status;
                StatusRules.Tick(ref hp, c.MaxHp, ref s, ref rounds, Mathf.Min(1, c.Hp));
                c.Hp = hp; c.Status = s; c.StatusRounds = rounds;
                if (s == StatusEffect.None) Log($"{c.Name} is no longer {StatusRules.Name(was).ToLowerInvariant()}.");
            }
        }

        /// <summary>Potions that would help the hunter (null pet) or a pet right now, outside battle.</summary>
        public List<ItemDefinition> TreatsFor(Companion pet)
        {
            var body = pet != null ? Combatant.FromCompanion(pet) : Fighter();
            return Inventory.Items.Select(kv => kv.Key).Where(i => i.type == ItemType.Consumable && Inventory.Count(i) > 0)
                .Where(i => WouldHelp(i, body, pet != null)).ToList();
        }

        static bool WouldHelp(ItemDefinition i, Combatant body, bool isPet)
        {
            var copy = new Combatant { MaxHp = body.MaxHp, Hp = body.Hp, Status = body.Status, StatusRounds = body.StatusRounds };
            return HazardRules.Treat(i, copy, isPet);
        }

        /// <summary>Uses a potion on the hunter (null) or a pet outside battle. True if it did something.</summary>
        public bool Treat(ItemDefinition item, Companion pet)
        {
            if (BattleController.InBattle || Inventory.Count(item) <= 0) return false;
            var body = pet != null ? Combatant.FromCompanion(pet) : HunterBody;
            bool wasOut = pet != null && pet.KnockedOut;
            if (!HazardRules.Treat(item, body, pet != null)) return false;
            Inventory.Remove(item, 1);
            if (pet != null) TakeVitals(null, pet, body);
            Log($"{item.displayName}: {(pet != null ? pet.Name : "you")} {(item.effect == ConsumableEffect.CureStatus ? "feel" + (pet != null ? "s" : "") + " better" : $"now {(pet != null ? "has" : "have")} {body.Hp} HP")}.{(wasOut && !pet.KnockedOut ? $" {pet.Name} is back on its feet." : "")}");
            return true;
        }

        /// <summary>A random pet on the raft that can still be hurt, or null.</summary>
        Companion RandomPet()
        {
            var up = Companions.All.Where(c => !c.KnockedOut).ToList();
            return up.Count == 0 ? null : up[rng.Next(up.Count)];
        }

        /// <summary>U-32: a live catch may lash out at the hunter or a pet while being reeled in.</summary>
        void Ambush(Drift d) =>
            Hurt(d, (pet, body) => HazardRules.Ambush(d.Fighter, HunterBody, body, d.Aggression, rng), "lashes out as you reel it in");

        /// <summary>U-32: a monster that was just beaten may thrash in its last moment.</summary>
        void Thrash(Drift d)
        {
            if (d != null) Hurt(d, (pet, body) => HazardRules.Outburst(d.Fighter, HunterBody, body, d.Aggression, d.GorgedParts > 0, rng), "thrashes one last time");
        }

        void Hurt(Drift d, System.Func<Companion, Combatant, Hazard> roll, string what)
        {
            if (d == null || d.Monster == null) return;
            var pet = RandomPet();
            var body = pet != null ? Combatant.FromCompanion(pet) : null;
            var h = roll(pet, body);
            if (h.Target == HazardTarget.None) return;
            bool onPet = h.Target == HazardTarget.Pet;
            int lost = HazardRules.Apply(h, onPet ? body : HunterBody);
            if (onPet) TakeVitals(null, pet, body);
            string who = onPet ? pet.Name : "you";
            string ail = h.Status != StatusEffect.None ? $" and {(onPet ? "it is" : "you are")} {StatusRules.Name(h.Status).ToLowerInvariant()}" : "";
            string ko = onPet && pet.KnockedOut ? $" {pet.Name} is knocked out and needs rest." : "";
            Log($"{d.Monster.displayName} {what}: {lost} damage to {who}{ail}!{ko}");
        }

        /// <summary>
        /// Phase 8 absorption: after a lost battle or a flee the monster eats one edge part and swims off
        /// gorged. Returns the line to add to the log ("" if nothing could be eaten).
        /// </summary>
        string Eat(Drift d)
        {
            if (d == null || d.Monster == null || raft == null) return "";
            var part = raft.Grid.Absorb(rng, out _);
            if (part == null) return "";
            Gorged.Add(d.Monster.id, part);
            return $" It ate your {part.displayName}! Beat the gorged {d.Monster.displayName} when it comes back to get it back.";
        }

        /// <summary>Beat a gorged monster: the parts it ate go to the Bag, plus one extra loot roll per part.</summary>
        string Recover(Monsters.MonsterDefinition m)
        {
            var parts = Gorged.Recover(m.id);
            if (parts.Count == 0) return "";
            foreach (var p in parts) pendingLoot.Add(new Drop<ItemDefinition>(p, 1));
            for (int i = 0; i < Rafts.AbsorbRules.BonusRolls(parts.Count); i++) pendingLoot.AddRange(RollLoot(m, false));
            return $" You got back {parts.Count} raft part{(parts.Count == 1 ? "" : "s")} (in the Bag) and extra loot!";
        }

        /// <summary>A lost battle (Ed's call): rest a while, and the waves take a few Common materials.</summary>
        void Defeated()
        {
            restUntil = Time.unscaledTime + DefeatRules.RestSeconds;
            // The rest brings the hunter back to full HP and clears ailments (the next fight starts fresh).
            HunterBody.Hp = HunterBody.MaxHp;
            HunterBody.Status = StatusEffect.None;
            HunterBody.StatusRounds = 0;
            var lost = DefeatRules.WashAway(Inventory, rng);
            Log(lost.Count > 0
                ? $"The waves washed away {string.Join(", ", lost.Select(l => $"{l.count} {l.item.displayName}"))}. Resting {DefeatRules.RestSeconds:0} s."
                : $"Resting {DefeatRules.RestSeconds:0} s.");
        }

        /// <summary>Idea U-17: a weakened aggressive monster is finished off without a prompt.</summary>
        void KillWeakened(string why)
        {
            var m = target.Monster;
            pendingLoot = RollLoot(m);
            CountKill();
            Thrash(target);
            Finish($"{m.displayName} was weakened but {why}, so you finished it: {Brief(pendingLoot)}", true, true);
        }

        /// <summary>A monster was beaten: counts toward the raft upgrade, and every companion grows one battle older (GDD §10).</summary>
        void CountKill()
        {
            raft.Grid.Kills++;
            if (target != null && target.Monster != null && target.Monster.isBoss)
            {
                raft.Grid.MarkBoss(target.Monster.rarity);
                Log($"You beat the Lv {(int)target.Monster.rarity + 1} boss, {target.Monster.displayName}!"
                    + (target.Monster.rarity == raft.Tier && raft.Tier < Rarity.Mythic ? " The raft can level up now." : ""));
            }
            Mastery.Add(AlchemyMastery.HarvestPoints);
            // Pets no longer age or die of old age (Ed's U-32); they grow by feeding and fusion.
            ApplyCompanionBonuses();
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
            pendingPet.Look = rng.Next(CompanionGrowth.Looks.Length);   // a random variety (looks only)
            var next = MinionRules.NextSkill(pendingPet);
            resultTitle = $"Tamed {m.displayName}! A new minion";
            resultBody = $"Minion Lv {pendingPet.Level} · {pendingPet.Rarity} · {pendingPet.Element}\n{pendingPet.Stats}\n"
                + $"Skills: {MinionRules.Skills(pendingPet)}"
                + (next != null ? $"\nNext: {next} at Lv {MinionRules.NextLearnLevel(pendingPet)}" : "");
        }

        void Adopt()
        {
            var m = target.Monster;
            var pet = pendingPet;
            Companions.Add(pet);
            Mastery.Add(AlchemyMastery.TamePoints);
            pendingPet = null;
            MonsterTamed?.Invoke(m);
            ApplyCompanionBonuses();
            Finish($"{m.displayName} joined as a minion (Lv {pet.Level}): {MinionRules.Skills(pet)}.", false, true);
        }

        /// <summary>Smith: cheaper refines. Called when the roster changes.</summary>
        public void ApplyCompanionBonuses() =>
            Gear.RefineDiscount = Companions.Best(MinionSkill.Smith) != null ? CompanionRules.MysticRefineDiscount : 0;

        /// <summary>Crew: between fights, +1 harpoon durability now and then (modules: see RaftCompanions).</summary>
        float fetchTimer;

        /// <summary>
        /// Ed's U-29: the best Familiar that isn't knocked out swims out now and then and brings back the
        /// nearest dead drift or floating loot near the raft, like a module would.
        /// </summary>
        void FamiliarFetch()
        {
            if (!CompanionSettings.FamiliarLoots || WorldPause.Active) return;
            var best = Companions.Best(MinionSkill.Looter);
            if (best == null) return;
            fetchTimer += Time.deltaTime;
            if (fetchTimer < CompanionRules.FetchSeconds(best.Rank)) return;
            fetchTimer = 0f;
            float reach = raft.EdgeDistance + CompanionRules.FetchReach;
            var center = Flat(raft.transform.position);
            var d = FindObjectsByType<Drift>(FindObjectsSortMode.None)
                .Where(x => x != null && !x.Hooked && x.Actual == DriftState.Dead && Vector3.Distance(Flat(x.transform.position), center) <= reach)
                .OrderBy(x => Vector3.Distance(Flat(x.transform.position), center)).FirstOrDefault();
            if (d != null) { AutoLoot(d, $"{best.Name} (Looter)", best); MinionActivity.Did(best, $"fetched a {d.Monster.displayName}"); }
        }

        void CrewMend()
        {
            var crew = Companions.Best(MinionSkill.Mender);
            if (crew == null || !CompanionSettings.Mends || WorldPause.Open != Menu.None || BattleController.InBattle) return;
            crewTimer += Time.deltaTime;
            if (crewTimer < CompanionRules.CrewMendSeconds) return;
            crewTimer = 0f;
            if (Harpoon.Full) return;
            Harpoon.Repair(1);
            Log($"{crew.Name} (Mender) mended the harpoon (+1).");
            MinionActivity.Did(crew, "mended the harpoon");
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
            // Owner idea U-25, second pass: pictures first, few words. Status card top-left, what to do
            // at the top, menus as big picture tiles along the bottom.
            GUILayout.BeginArea(new Rect(10, 10, HudRight - 10, 330 + 34 * Mathf.Min(4, Companions.All.Count)));
            GUILayout.BeginVertical(HuntStyles.Box);
            string ailing = HunterBody.Status != StatusEffect.None ? $" <color=#c9a0ff>{StatusRules.Tag(HunterBody.Status)}</color>" : "";
            StatusRow(HuntIcons.Paw, $"Hunter{ailing}", HunterBody.Hp / (float)Mathf.Max(1, HunterBody.MaxHp), $"{HunterBody.Hp}/{HunterBody.MaxHp} HP", new Color(0.4f, 0.85f, 0.45f));
            StatusRow(HuntIcons.Raft, $"Raft Lv {(int)raft.Tier + 1}", 1f, $"{raft.MaxHp} HP · size {raft.size} · {raft.Grid.Type}", new Color(0.85f, 0.75f, 0.5f));
            GUILayout.BeginHorizontal();
            StatusRow(HuntIcons.Harpoon, Harpoon.Broken ? "Harpoon <color=#f77>BROKEN</color>" : "Harpoon",
                Harpoon.Current / (float)Mathf.Max(1, Harpoon.Max), $"{Harpoon.Current}/{Harpoon.Max}", new Color(0.4f, 0.65f, 1f));
            GUI.enabled = !Harpoon.Full && Inventory.MaterialCount() >= 1;
            if (GUILayout.Button(new GUIContent("Fix", "Repair the harpoon +10 for 1 material"), GUILayout.Width(44), GUILayout.Height(34)) && Inventory.SpendMaterials(1)) Harpoon.Repair(10);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            StatusRow(HuntIcons.Book, $"Mastery Lv {Mastery.Level}", Mastery.Progress,
                Mastery.Level >= AlchemyMastery.MaxLevel ? "max" : $"{Mastery.Points - AlchemyMastery.PointsFor(Mastery.Level)}/{AlchemyMastery.PointsToNext(Mastery.Level)}", new Color(0.75f, 0.5f, 1f));
            MinionRows();
            if (RestLeft > 0f) GUILayout.Label($"<color=#ffb347>Resting… {Mathf.CeilToInt(RestLeft)} s</color>", Rich);
            // Idea Round 4: time, weather and the forecast (C-12).
            if (WorldClock.Now != null)
            {
                GUILayout.Label($"{WorldClock.Summary}  <size=11><color=#9fc7ff>Next: {WorldClock.Now.Forecast}</color></size>", Rich);
                if (WorldClock.Burning) GUILayout.Label("<color=#ff9a3c><b>Raft on fire! Click the fire.</b></color>", Rich);
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Battle mode and save, small, top-right.
            GUI.Label(new Rect(HuntStyles.W - 330, 8, 320, 20), $"<size=11>Battles: {BattleSettings.Labels[(int)BattleSettings.Mode]} · {SettingsPanel.AutoSaveStatus}</size>", RichRight);

            // Menu dock along the bottom.
            const float tile = 78f, gap = 8f;
            float x = (HuntStyles.W - (5 * tile + 4 * gap)) / 2f, y = HuntStyles.H - tile - 10f;
            if (HuntStyles.Tile(new Rect(x, y, tile, tile), HuntIcons.Bag, "Bag", "B", WorldPause.Open == Menu.Bag)) BagToggleRequested?.Invoke();
            x += tile + gap;
            if (HuntStyles.Tile(new Rect(x, y, tile, tile), HuntIcons.Paw, "Minions", "P", WorldPause.Open == Menu.Pets)) PetsPanel.Toggle();
            x += tile + gap;
            if (HuntStyles.Tile(new Rect(x, y, tile, tile), HuntIcons.Book, "Book", "C", WorldPause.Open == Menu.Book)) BookToggleRequested?.Invoke();
            x += tile + gap;
            if (builder != null && HuntStyles.Tile(new Rect(x, y, tile, tile), HuntIcons.Hammer, "Build", "R", builder.Active)) builder.Toggle();
            x += tile + gap;
            if (HuntStyles.Tile(new Rect(x, y, tile, tile), HuntIcons.Gear, "Settings", "Esc", WorldPause.Open == Menu.Settings)) WorldPause.Toggle(Menu.Settings);
        }

        /// <summary>
        /// Ed's U-39 HUD: one line per minion (up to 4) with its level, an EXP bar and the job it is doing now,
        /// instead of the old "Pets n/m" line.
        /// </summary>
        void MinionRows()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(HuntIcons.Paw, GUILayout.Width(22), GUILayout.Height(22));
            GUILayout.Label($"<b>Minions</b> {Companions.All.Count}/{Companions.Capacity}", Rich);
            GUILayout.EndHorizontal();
            foreach (var c in Companions.All.Take(4))
            {
                int need = MinionRules.ExpToNext(c.Level);
                string doing = c.KnockedOut ? $"<color={HuntStyles.BadHex}>knocked out</color>" : MinionActivity.Now(c);
                string fight = Companions.Active == c ? " ⚔" : "";
                GUILayout.BeginHorizontal();
                GUILayout.Space(26);
                Meter($"<color={BagPanel.RarityColor(c.Rarity)}>{c.Name}</color> Lv {c.Level}{fight}  <size=10><color=#9fc7ff>{doing}</color></size>",
                    need > 0 ? (float)c.Exp / need : 1f, need > 0 ? $"{c.Exp}/{need} EXP" : "max", new Color(0.55f, 0.8f, 1f));
                GUILayout.EndHorizontal();
            }
            if (Companions.All.Count > 4) GUILayout.Label($"<size=11>+{Companions.All.Count - 4} more (P)</size>", Rich);
        }

        /// <summary>Icon, name and a bar: one line of the status card.</summary>
        void StatusRow(Texture2D icon, string label, float fill, string value, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, GUILayout.Width(26), GUILayout.Height(26));
            Meter(label, fill, value, color);
            GUILayout.EndHorizontal();
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

        Transform hunterTf;
        /// <summary>The line starts at the hunter's hands, wherever the hunter stands (Ed's ask: walk to the edge to pull).</summary>
        Vector3 HookOrigin()
        {
            if (hunterTf == null) hunterTf = raft.transform.Find("Hunter");
            return hunterTf != null ? hunterTf.position + Vector3.up * 0.35f : raft.transform.position + Vector3.up * 1.2f;
        }

        /// <summary>The drift being cast at or reeled in, else null (for the hunter's pulling motion).</summary>
        public Drift HookTarget => phase == Phase.Casting || phase == Phase.Reeling ? target : null;
        public bool Reeling => phase == Phase.Reeling;
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
