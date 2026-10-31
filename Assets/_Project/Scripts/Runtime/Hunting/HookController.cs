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
        /// <summary>The sea journal of firsts (Claude's C-15).</summary>
        public World.Firsts Firsts { get; } = new World.Firsts();

        /// <summary>Marks a first and gives its reward the first time (C-15).</summary>
        public void First(string id)
        {
            if (!Firsts.Mark(id)) return;
            var e = World.Firsts.Get(id);
            Inventory.AddEssence(new EssenceKey(raft.Tier, Elements.Element.None), e.Essence);
            Mastery.Add(e.Mastery);
            Log($"★ Sea journal: {e.Title}! +{e.Essence} {raft.Tier} essence, +{e.Mastery} Mastery.");
            Juice.Pop($"★ {e.Title}!", raft.transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.35f));
            LootAdded?.Invoke();
        }
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
        /// <summary>The reel minigame in play (Ed's U-42), while reeling.</summary>
        ReelGame reel;
        bool tensionPressed, tensionHit, tensionAuto, tensionSemi, tensionByAuto;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = 0.05f;
            // Vertex colours, so the line can show the reel's strain (C-21).
            if (line.sharedMaterial == null || !line.sharedMaterial.shader.name.StartsWith("Sprites")) line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = Color.white;
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
            // Arrows to off-screen bosses, gorged monsters and lost parts (C-23), likewise added at runtime.
            if (!TryGetComponent<EdgeArrows>(out _)) gameObject.AddComponent<EdgeArrows>();
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
            // Firsts that are a state rather than an event (C-15).
            // (after the save has loaded, so a loaded raft doesn't pay them out twice)
            if (Time.timeSinceLevelLoad > 3f && raft.Tier > Rarity.Common) First("level");
            if (Time.timeSinceLevelLoad > 3f && raft.Grid.VesselCount > 0) First("vessel");
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
                var m = target.Monster;
                // Rarer and bigger: a narrower green zone and a wilder catch. Rough seas and storms shrink it too (U-15);
                // Alchemy Mastery and a Looter minion widen it.
                float width = ReelGame.ZoneWidth(m.sizeClass, m.rarity, World.WorldRules.ReelZoneScale(WorldClock.Weather),
                    ReelGame.Bonus(Mastery.Level, Companions.Best(MinionSkill.Looter) != null));
                // C-14: it fights in its own style; a weakened catch has less fight in it.
                var style = ReelGame.StyleOf(m.element, target.Actual, m.sizeClass);
                float speed = ReelGame.CatchSpeed(m.sizeClass, m.rarity) * (target.Actual == DriftState.Weakened ? 0.7f : 1f);
                reel = new ReelGame(width, speed, BattleSettings.Mode, rng, style);
                tensionPressed = tensionHit = false;
                tensionAuto = BattleSettings.Mode == BattleMode.Auto;
                tensionSemi = BattleSettings.Mode == BattleMode.SemiAuto;
                tensionByAuto = tensionAuto;
            }
        }

        void UpdateReeling()
        {
            if (!CheckTarget()) return;
            reel.Step(Time.deltaTime, HeldReel(), PressedTension());
            // The catch comes closer as the meter fills, and drifts back out as it drains.
            t = reel.Meter;
            // Reeled in until its body touches the raft's edge, not over the planks (owner's ask: no overlap).
            float stop = Mathf.Max(raft.EdgeDistance, (raft.Grid.Radius + 0.5f) * Raft.CellSize + target.BodyRadius + 0.1f);
            var edge = raft.transform.position + (reelFrom - raft.transform.position).normalized * stop;
            edge.y = reelFrom.y;
            target.transform.position = Vector3.Lerp(reelFrom, edge, Mathf.SmoothStep(0f, 1f, t));
            line.SetPosition(0, HookOrigin());
            line.SetPosition(1, target.transform.position);
            // C-21: the line turns from strained red to green as the meter fills, and thickens while it's inside the zone.
            line.startColor = line.endColor = Color.Lerp(new Color(1f, 0.45f, 0.35f), new Color(0.6f, 1f, 0.6f), reel.Meter);
            line.widthMultiplier = reel.Inside ? 1.4f : 1f;
            if (reel.Done)
            {
                tensionPressed = reel.Caught;
                tensionHit = reel.Clean;
                line.widthMultiplier = 1f;
                line.startColor = line.endColor = Color.white;
                if (reel.Clean) { First("clean_reel"); Juice.Shake(0.5f); Juice.Pop("CLEAN REEL!", target.transform.position + Vector3.up * 0.6f, new Color(0.5f, 1f, 0.6f)); }
                else if (reel.Escaped) Juice.Pop("It got away...", target.transform.position, new Color(1f, 0.6f, 0.5f));
                Resolve();
            }
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
                Finish($"{m.displayName} slipped off the hook. Hold or tap Space to keep it inside the green zone until the meter fills!", false, false);
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
                    pendingLoot = LootOf(target, tensionHit);
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

        /// <summary>A lost raft part gives just that part back; any other drift rolls its loot table.</summary>
        List<Drop<ItemDefinition>> LootOf(Drift d, bool clean)
        {
            if (d.Survivor)
            {
                // C-17: the survivor's thank-you gift: two rolls of their (richer) flotsam, plus a bit of essence.
                First("rescue");
                var gift = RollLoot(d.Monster, clean);
                gift.AddRange(RollLoot(d.Monster, false));
                Inventory.AddEssence(new EssenceKey(d.Monster.rarity, Elements.Element.None), 3);
                Log($"The survivor thanks you: \"Take these, and 3 {d.Monster.rarity} essence. I owe you!\"");
                return gift;
            }
            if (d.Carried == null) return RollLoot(d.Monster, clean);
            First("lost_part");
            return new List<Drop<ItemDefinition>> { new Drop<ItemDefinition>(d.Carried, 1) };
        }

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
            var drops = LootOf(d, false);
            MonsterAcquired?.Invoke(d.Monster);
            foreach (var x in drops) Inventory.Add(x.Item, x.Quantity);
            string got = drops.Count == 0 ? "nothing" : string.Join(", ", drops.Select(x => $"{x.Quantity} {x.Item.displayName}"));
            Log($"{source} caught a {d.Monster.displayName}: {got}.");
            if (drops.Count > 0) StorageBox.Instance?.Store(got, d.transform.position);
            PopLoot(drops, d.transform.position);
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
                var at = target != null ? target.transform.position : raft.transform.position;
                if (pendingLoot.Count > 0) StorageBox.Instance?.Store(Brief(pendingLoot), at);
                PopLoot(pendingLoot, at);
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

        /// <summary>C-21: a splash where the catch lands and a "+2 Shellback" for each drop.</summary>
        static void PopLoot(List<Drop<ItemDefinition>> drops, Vector3 at)
        {
            Juice.Splash(at, 0.8f);
            for (int i = 0; i < drops.Count; i++)
                Juice.Pop($"+{drops[i].Quantity} {drops[i].Item.displayName}", at + Vector3.up * (0.35f * i), Raft.PartColor(drops[i].Item.rarity));
        }

        /// <summary>
        /// Knocked out (Ed's ask): a big countdown in the middle of the screen and over the hunter, so you know
        /// when you're back up.
        /// </summary>
        void DrawRespawn()
        {
            float left = RestLeft;
            if (left <= 0f || BattleController.InBattle) return;
            float w = 340, h = 74;
            var r = new Rect((HuntStyles.W - w) / 2f, HuntStyles.H * 0.22f, w, h);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.08f, 0.13f, 0.88f), 0, 14);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.45f, 0.4f), 0, 2);
            GUI.Label(new Rect(r.x, r.y + 6, w, 26), "<size=18><b><color=#ff8a80>KNOCKED OUT</color></b></size>", RespawnStyle);
            GUI.Label(new Rect(r.x, r.y + 30, w, 20), $"Back on your feet in <b>{Mathf.CeilToInt(left)} s</b>", RespawnStyle);
            float share = 1f - left / Mathf.Max(1f, Battle.DefeatRules.RestSeconds);
            var bar = new Rect(r.x + 20, r.yMax - 16, w - 40, 8);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.6f), 0, 4);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * share, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.7f, 0.3f), 0, 4);
            // The same count over the hunter.
            if (hunterTf == null) hunterTf = raft.transform.Find("Hunter");
            if (hunterTf != null && Camera.main != null)
            {
                var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(hunterTf.position + Vector3.up * 1.1f));
                if (sp.z > 0f) GUI.Label(new Rect(sp.x - 60, HuntStyles.H - sp.y - 12, 120, 24), $"<b>Zzz… {Mathf.CeilToInt(left)}</b>", RespawnStyle);
            }
        }

        static GUIStyle respawnStyle;
        static GUIStyle RespawnStyle => respawnStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 14 };

        bool HeldReel()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && kb.spaceKey.isPressed) || (mouse != null && mouse.leftButton.isPressed);
        }

        bool PressedTension()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            return (kb != null && kb.spaceKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        /// <summary>The reel minigame (U-42): a tall bar with the green zone and the catch, and the catch meter beside it.</summary>
        void DrawTension()
        {
            if (reel == null) return;
            float w = 46, h = 300;
            var bar = new Rect(HuntStyles.W / 2f + 170, HuntStyles.H / 2f - h / 2f, w, h);
            var meter = new Rect(bar.xMax + 8, bar.y, 12, h);
            string label = tensionAuto ? "<b>AUTO</b> reel"
                : tensionSemi ? "<b>SEMI-AUTO</b>: hold or tap <b>Space</b>, it helps you"
                : "Hold or tap <b>Space</b>: keep the catch in the green!";
            GUI.Label(new Rect(bar.x - 110, bar.y - 44, 280, 40), label, Rich);
            GUI.Label(new Rect(bar.x - 110, bar.yMax + 8, 280, 22), $"<i>{ReelGame.Describe(reel.Style)}</i>", Rich);
            GUI.DrawTexture(new Rect(bar.x - 4, bar.y - 4, meter.xMax - bar.x + 8, h + 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.1f, 0.18f, 0.85f), 0, 8);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.1f, 0.25f, 0.4f, 0.95f), 0, 4);
            // 0 is the bottom of the bar.
            float Y(float v) => bar.yMax - v * h;
            var zone = new Rect(bar.x + 3, Y(reel.Zone + reel.Width), w - 6, reel.Width * h);
            GUI.DrawTexture(zone, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0,
                reel.Inside ? new Color(0.35f, 0.95f, 0.45f, 0.95f) : new Color(0.3f, 0.75f, 0.4f, 0.7f), 0, 4);
            // The catch: a small fish-shaped mark that shakes while it's inside the zone being reeled.
            float shake = reel.Inside ? Mathf.Sin(Time.time * 50f) * 1.5f : 0f;
            var fish = new Rect(bar.x + w / 2f - 11 + shake, Y(reel.Catch) - 8, 22, 16);
            GUI.DrawTexture(fish, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Tint.Of(target.Monster.element), 0, 8);
            GUI.DrawTexture(new Rect(fish.xMax - 2, fish.y + 3, 7, 10), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Tint.Of(target.Monster.element), 0, 2);
            // Catch meter: fills green inside the zone, drains outside.
            GUI.DrawTexture(meter, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.6f), 0, 4);
            var fill = Color.Lerp(new Color(1f, 0.35f, 0.25f), new Color(0.4f, 1f, 0.45f), reel.Meter);
            GUI.DrawTexture(new Rect(meter.x, meter.yMax - reel.Meter * h, meter.width, reel.Meter * h), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, fill, 0, 4);
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            DrawHud();
            if (phase == Phase.Reeling && target != null) DrawTension();
            DrawRespawn();
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
        /// An auto-loot module hooked a live monster: it can't swim off, so the battle starts at once in the
        /// Settings mode (Ed: Manual means manual everywhere), cancelling the hunter's own cast or reel. False only during another
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
            battle.ModuleCatch = true;
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
            bool moduleFight = battle.ModuleCatch;
            int gorged = target.GorgedParts;
            battle.Begin(target, tensionHit ? Initiative.Hunter : Initiative.Monster, outcome =>
            {
                if (outcome == BattleOutcome.Won)
                {
                    pendingLoot = RollLoot(m);
                    if (m.isBoss) BossTreasure(m);
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

        /// <summary>
        /// Boss treasure (Ed's ask): one more roll of its loot per Looter minion (up to 3, "Lucky") and a pile of
        /// its essence, then the treasure burst and the BOSS DEFEATED card.
        /// </summary>
        void BossTreasure(Monsters.MonsterDefinition m)
        {
            First("boss");
            int lucky = BossFightRules.LuckyRolls(Companions.All);
            for (int i = 0; i < lucky && m.loot != null; i++) pendingLoot.AddRange(m.loot.Roll(rng));
            pendingLoot = pendingLoot.GroupBy(d => d.Item).Select(g => new Drop<ItemDefinition>(g.Key, g.Sum(d => d.Quantity))).ToList();
            int essence = BossFightRules.TreasureEssence(m.rarity, lucky);
            Inventory.AddEssence(new EssenceKey(m.rarity, m.element), essence);
            var lines = pendingLoot.Select(d => $"{d.Quantity} × {d.Item.displayName} ({d.Item.rarity})").ToList();
            lines.Add($"{essence} × {new EssenceKey(m.rarity, m.element)}");
            if (lucky > 0) lines.Add($"Lucky: +{lucky} roll{(lucky == 1 ? "" : "s")} from your Looters");
            Log($"Boss treasure: {string.Join(", ", lines)}.");
            BossShow.Instance?.Victory(m.displayName, lines);
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
            if (petRegen >= HazardRules.PetRegenSeconds(Rafts.SetBonusRules.HealingHabitats(raft.Grid.Parts)))   // cozy habitats count twice (C-18)
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
            pendingPet.Favourite = rng.Next(4);   // C-16: what it loves to eat
            var next = MinionRules.NextSkill(pendingPet);
            resultTitle = $"Tamed {m.displayName}! A new minion";
            resultBody = $"Minion Lv {pendingPet.Level} · {pendingPet.Rarity} · {pendingPet.Element}\n{pendingPet.Stats}\n"
                + $"Skills: {MinionRules.Skills(pendingPet)}\nLoves eating {MinionRules.FoodName(pendingPet.Favourite)}"
                + (next != null ? $"\nNext: {next} at Lv {MinionRules.NextLearnLevel(pendingPet)}" : "");
        }

        void Adopt()
        {
            var m = target.Monster;
            var pet = pendingPet;
            Companions.Add(pet);
            Mastery.Add(AlchemyMastery.TamePoints);
            First("tame");
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
        /// Ed's U-29: the best Looter minion now and then catches the nearest dead drift or floating loot
        /// near the raft. Ed's ask: you see it work, throwing a net, casting its own hook or firing a harpoon,
        /// then pulling the catch in, instead of the loot just landing in the Bag.
        /// </summary>
        void FamiliarFetch()
        {
            if (!CompanionSettings.FamiliarLoots || WorldPause.Active || fetching) return;
            var best = Companions.Best(MinionSkill.Looter);
            if (best == null) return;
            fetchTimer += Time.deltaTime;
            if (fetchTimer < CompanionRules.FetchSeconds(best.Rank)) return;
            float reach = raft.EdgeDistance + CompanionRules.FetchReach;
            var center = Flat(raft.transform.position);
            var d = FindObjectsByType<Drift>(FindObjectsSortMode.None)
                .Where(x => x != null && !x.Hooked && x.Actual == DriftState.Dead && Vector3.Distance(Flat(x.transform.position), center) <= reach)
                .OrderBy(x => Vector3.Distance(Flat(x.transform.position), center)).FirstOrDefault();
            // Nothing in reach: keep watching, so it acts as soon as something drifts close.
            if (d == null || !d.TryClaim(best)) return;
            fetchTimer = 0f;
            var minion = GetComponent<RaftCompanions>()?.BodyOf(best);
            StartCoroutine(Fetch(best, d, minion != null ? minion : raft.transform.Find("Hunter")));
        }

        bool fetching;

        enum FetchTool { Net, Hook, Harpoon }

        /// <summary>The Looter's catch: the tool flies out to the drift, then it is pulled to the raft's edge and looted.</summary>
        System.Collections.IEnumerator Fetch(Companion who, Drift d, Transform from)
        {
            fetching = true;
            var tool = (FetchTool)UnityEngine.Random.Range(0, 3);
            var go = new GameObject("Minion fetch");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = tool == FetchTool.Net ? new Color(0.7f, 1f, 0.8f) : new Color(0.95f, 0.95f, 0.85f);
            lr.startWidth = lr.endWidth = 0.04f;
            lr.positionCount = 2;
            // The thing it throws: a flat net, a hook, or a harpoon spear, flying ahead of the line.
            var head = GameObject.CreatePrimitive(tool == FetchTool.Net ? PrimitiveType.Cylinder : tool == FetchTool.Hook ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(go.transform, true);
            head.transform.localScale = tool == FetchTool.Net ? new Vector3(0.9f, 0.02f, 0.9f) : tool == FetchTool.Hook ? Vector3.one * 0.18f : new Vector3(0.07f, 0.35f, 0.07f);
            Tint.Apply(head.GetComponent<Renderer>(), tool == FetchTool.Net ? new Color(0.45f, 0.8f, 0.55f) : tool == FetchTool.Hook ? new Color(0.8f, 0.8f, 0.85f) : new Color(0.55f, 0.4f, 0.25f));
            string verb = tool == FetchTool.Net ? "threw its net at" : tool == FetchTool.Hook ? "cast its hook at" : "fired its harpoon at";
            Log($"{who.Name} (Looter) {verb} the {(d.Carried != null ? d.Carried.displayName : d.Monster.displayName)}.");

            Vector3 Origin() => from != null ? from.position + Vector3.up * 0.2f : raft.transform.position + Vector3.up;
            const float throwTime = 0.5f, pullTime = 1.2f;
            for (float t = 0f; t < throwTime && d != null; )
            {
                if (!WorldPause.Active) t += Time.deltaTime;
                float k = t / throwTime;
                // A little arc for the net and the hook; the harpoon flies straight.
                var p = Vector3.Lerp(Origin(), d.transform.position, k) + Vector3.up * (tool == FetchTool.Harpoon ? 0f : Mathf.Sin(k * Mathf.PI) * 1.2f);
                head.transform.position = p;
                if (tool == FetchTool.Harpoon) head.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.transform.position - Origin());
                lr.SetPosition(0, Origin());
                lr.SetPosition(1, p);
                yield return null;
            }
            if (d != null)
            {
                var start = d.transform.position;
                var edge = raft.transform.position + Flat(start - raft.transform.position).normalized * raft.EdgeDistance * 0.8f;
                for (float t = 0f; t < pullTime && d != null; )
                {
                    if (!WorldPause.Active) t += Time.deltaTime;
                    var p = Vector3.Lerp(start, edge, t / pullTime);
                    p.y = 0.1f + Waves.Height(p.x, p.z) + Mathf.Sin(t * 12f) * 0.05f;
                    d.transform.position = p;
                    head.transform.position = p + Vector3.up * 0.15f;
                    lr.SetPosition(0, Origin());
                    lr.SetPosition(1, p);
                    yield return null;
                }
            }
            Destroy(go);
            fetching = false;
            if (d == null) yield break;
            string what = d.Carried != null ? d.Carried.displayName : d.Monster.displayName;
            AutoLoot(d, $"{who.Name} (Looter)", who);
            MinionActivity.Did(who, $"fetched a {what}");
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
            GUILayout.BeginArea(new Rect(10, 10, HudRight - 10, 400 + 34 * Mathf.Min(4, Companions.All.Count)));
            GUILayout.BeginVertical(HuntStyles.Box);
            HuntStyles.Section("Hunter & raft");
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
            GUILayout.Space(4);
            MinionRows();
            if (RestLeft > 0f) GUILayout.Label($"<color=#ffb347>Resting… {Mathf.CeilToInt(RestLeft)} s</color>", Rich);
            // Idea Round 4: time, weather and the forecast (C-12).
            if (WorldClock.Now != null)
            {
                GUILayout.Space(4);
                HuntStyles.Section("Sea");
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
            HuntStyles.Section($"Minions {Companions.All.Count}/{Companions.Capacity}");
            if (Companions.All.Count == 0) GUILayout.Label("<size=11>none yet</size>", Rich);
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
