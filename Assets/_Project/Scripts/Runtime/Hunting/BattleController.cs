using System;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Runs a <see cref="BattleState"/> in the scene: battle camera, mode switching, action buttons
    /// and a placeholder IMGUI panel. The rules themselves live in BrineBattle.Battle.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public Raft raft;
        [Tooltip("Seconds between rounds when Semi-auto or Auto picks the action.")]
        public float autoDelay = 0.8f;
        public float cameraBlend = 3f;

        public bool Active => state != null;
        /// <summary>True while any battle runs: the world around the raft holds still.</summary>
        public static bool InBattle { get; private set; }
        /// <summary>The monster being fought, so the hunter can face it (Ed's ask).</summary>
        public static Drift Foe { get; private set; }
        /// <summary>The battle being fought, or null (the boss show reads its HP and enrage).</summary>
        public static BattleState Current { get; private set; }
        /// <summary>True while the battle camera is still easing back; the camera rig waits for it.</summary>
        public static bool CameraBusy { get; private set; }

        /// <summary>
        /// True for a battle an auto-loot module started (an aggressive live catch). It plays in the mode
        /// set in Settings like any other battle (Ed: Manual means manual everywhere).
        /// </summary>
        public bool ModuleCatch { get; set; }
        static BattleMode Mode => BattleSettings.Mode;

        BattleState state;
        BattleStage stage;
        int shownEvents;
        Drift drift;
        Action<BattleOutcome> onEnd;
        float endTimer;
        float timer;
        Vector3 driftHome;

        Camera cam;
        Vector3 camPos;
        Quaternion camRot;
        Vector3 battleCamPos;
        Quaternion battleCamRot;

        public void Begin(Drift target, Initiative firstRound, Action<BattleOutcome> onFinished)
        {
            drift = target;
            onEnd = onFinished;
            InBattle = true;
            Foe = target;
            endTimer = 0f;
            var hook = FindFirstObjectByType<HookController>();
            state = new BattleState(hook != null ? hook.Fighter() : Combatant.Hunter(), target.Fighter, raft.MaxHp, new System.Random());
            state.FirstRound = firstRound;
            Current = state;
            state.Weapon = hook?.Harpoon;
            state.Bag = hook?.Inventory;
            // A knocked-out pet (U-32: HP carries over) stays on the raft and rests.
            if (hook?.Companions.Active != null && !hook.Companions.Active.KnockedOut) state.Companion = Combatant.FromCompanion(hook.Companions.Active);
            WorldClock.Empower(state.Companion);
            fighter = state.Companion != null ? hook.Companions.Active : null;
            var crew = hook?.Companions.Best(Companions.MinionSkill.Crewmate);
            if (crew != null) state.CrewRepair = Companions.CompanionRules.CrewRaftRepair(crew.Rank);
            var mystic = hook?.Companions.Best(Companions.MinionSkill.Alchemist);
            if (mystic != null) state.SupportHeal = Companions.CompanionRules.MysticBattleHeal(mystic.Rank);
            // Grand boss fights (Ed's ask): every minion helps, and the boss show plays the entrance first.
            if (target.Monster.isBoss)
            {
                state.IsBoss = true;
                if (hook != null)
                {
                    state.AllyAttack = BossFightRules.AllyStrike(hook.Companions.All, hook.Companions.Active);
                    foreach (var line in BossFightRules.Contributions(hook.Companions.All, hook.Companions.Active)) state.Log.Add(line + ".");
                }
                if (!TryGetComponent(out bossShow)) bossShow = gameObject.AddComponent<BossShow>();
                bossShow.Intro(target);
            }
            // Battle visuals (idea U-19): created here so older Hook scenes get them without a rebuild.
            if (stage == null && !TryGetComponent(out stage)) stage = gameObject.AddComponent<BattleStage>();
            stage.Begin(raft, target, state.Companion);
            shownEvents = 0;
            if (hook != null)
            {
                foreach (var p in hook.Gear.Passives) state.Passives.Add(p);
                state.Actives.AddRange(hook.Gear.Actives);
            }
            string first = firstRound == Initiative.Hunter ? " You strike first." : firstRound == Initiative.Monster ? " It strikes first." : "";
            state.Log.Add($"{target.Monster.displayName} attacks! Mode: {BattleSettings.Labels[(int)Mode]}{(ModuleCatch ? " (a module caught it)" : "")}.{first}");
            // Idea Round 4: say who the time and weather favour (the stats were already changed).
            if (WorldClock.Now != null)
            {
                string Note(Combatant c) => c == null ? "" : World.WorldRules.Note(c.Element, WorldClock.Time, WorldClock.Weather);
                var notes = new System.Collections.Generic.List<string>();
                void Add(string who, Combatant c) { var n = Note(c); if (n.Length > 0) notes.Add($"{who}: {n}"); }
                Add(target.Monster.displayName, state.Monster);
                Add("You", state.Hunter);
                Add(state.Companion?.Name, state.Companion);
                if (notes.Count > 0) state.Log.Add($"{WorldClock.Summary}. {string.Join("; ", notes)}.");
            }
            timer = 0f;
            driftHome = target.transform.position;

            cam = Camera.main;
            if (cam != null)
            {
                camPos = cam.transform.position;
                camRot = cam.transform.rotation;
                var mid = Vector3.Lerp(raft.transform.position, driftHome, 0.5f);
                // Aim a little right of the fight so it sits left of the battle panel.
                var aim = mid + Vector3.right * 2.2f;
                battleCamPos = aim + new Vector3(0f, 7f, -7f);
                battleCamRot = Quaternion.LookRotation(aim - battleCamPos);
            }
        }

        void Update()
        {
            UpdateCamera();
            if (!Active) return;

            Feed();
            if (stage.Busy || (bossShow != null && bossShow.Busy)) { timer = 0f; return; }

            if (state.Outcome != BattleOutcome.Ongoing)
            {
                endTimer += Time.deltaTime;
                if (endTimer >= (state.IsBoss ? 2.4f : 1.2f)) End();
                return;
            }
            var autoItem = AutoItem();
            var autoSkill = autoItem == null ? BattleAI.ChooseSkill(state, Mode) : GearActive.None;
            var pick = autoItem != null ? BattleAction.Item
                : autoSkill != GearActive.None ? BattleAction.Skill
                : BattleAI.Choose(state, Mode);
            if (pick == null) { timer = 0f; return; }
            timer += Time.deltaTime;
            if (timer < autoDelay) return;
            if (autoItem != null) UseItem(autoItem);
            else if (autoSkill != GearActive.None) UseSkill(autoSkill);
            else Play(pick.Value);
        }

        void UpdateCamera()
        {
            CameraBusy = cam != null;
            if (cam == null) return;
            var pos = Active ? battleCamPos : camPos;
            var rot = Active ? battleCamRot : camRot;
            float k = 1f - Mathf.Exp(-cameraBlend * Time.deltaTime);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(cam.transform.position, pos, k), Quaternion.Slerp(cam.transform.rotation, rot, k));
            if (!Active && (cam.transform.position - camPos).sqrMagnitude < 0.0001f) cam = null;
        }

        void Play(BattleAction action)
        {
            state.PlayRound(action);
            Feed();
            timer = 0f;
        }

        /// <summary>Hands the stage what happened since it last looked, so it animates in order.</summary>
        void Feed()
        {
            if (state.Events.Count <= shownEvents) return;
            stage.Play(state.Events.GetRange(shownEvents, state.Events.Count - shownEvents));
            shownEvents = state.Events.Count;
        }

        bool Waiting() => !stage.Busy && AutoItem() == null && BattleAI.ChooseSkill(state, Mode) == GearActive.None
            && BattleAI.Choose(state, Mode) == null;

        void UseSkill(GearActive skill)
        {
            state.PlaySkill(skill);
            Feed();
            timer = 0f;
        }

        ItemDefinition AutoItem() => BattleSettings.AutoItems ? BattleAI.ChooseItem(state, Mode) : null;

        void UseItem(ItemDefinition item)
        {
            state.PlayItem(item);
            Feed();
            timer = 0f;
        }

        BossShow bossShow;

        /// <summary>The pet fighting in this battle, so its HP and ailment can be kept afterwards (U-32).</summary>
        Companions.Companion fighter;

        void End()
        {
            var outcome = state.Outcome;
            if (state.IsBoss && outcome != BattleOutcome.Won) bossShow?.Stop();
            Current = null;
            FindFirstObjectByType<HookController>()?.TakeVitals(state.Hunter, fighter, state.Companion);
            fighter = null;
            stage.Clear();
            state = null;
            InBattle = false;
            Foe = null;
            ModuleCatch = false;
            drift = null;
            onEnd?.Invoke(outcome);
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!Active) return;
            GUI.depth = -5;
            // Bottom-right, so the fight itself stays in view (the camera aims to the left of this).
            float w = Mathf.Min(480, HuntStyles.W * 0.42f), h = (state.Actives.Count > 0 ? 340 : 310) + (state.Companion != null ? 24 : 0);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, HuntStyles.H - h - 12, w, h), $"Battle — round {state.Round}" + (state.CoatingRounds > 0 ? $" — {state.Coating} oil" : ""), HuntStyles.Window);

            Bar(state.Monster.Name, state.Monster.Hp, state.Monster.MaxHp, new Color(0.9f, 0.35f, 0.3f));
            Bar("Hunter", state.Hunter.Hp, state.Hunter.MaxHp, new Color(0.35f, 0.85f, 0.4f));
            Bar("Raft", state.RaftHp, state.RaftMaxHp, new Color(0.85f, 0.75f, 0.5f));
            if (state.Companion != null) Bar(state.Companion.Name, state.Companion.Hp, state.Companion.MaxHp, new Color(0.75f, 0.5f, 0.95f));
            Bar("Stamina", state.Stamina, BattleRules.MaxStamina, new Color(0.4f, 0.65f, 1f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode", GUILayout.Width(60));
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels);
            GUILayout.EndHorizontal();

            int from = Mathf.Max(0, state.Log.Count - 3);
            for (int i = from; i < state.Log.Count; i++) GUILayout.Label(state.Log[i]);
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (state.Outcome != BattleOutcome.Ongoing)
            {
                GUILayout.Label($"<b>{OutcomeText(state.Outcome)}</b>", new GUIStyle(GUI.skin.label) { richText = true });
            }
            else
            {
                bool waiting = Waiting();
                GUI.enabled = waiting;
                if (GUILayout.Button("Attack")) Play(BattleAction.Attack);
                GUI.enabled = waiting && state.CanUse(BattleAction.Harpoon);
                if (GUILayout.Button(state.Weapon != null && state.Weapon.Broken ? "Harpoon (broken)" : $"Harpoon ({BattleRules.HarpoonCost} st · {state.Weapon})")) Play(BattleAction.Harpoon);
                GUI.enabled = waiting;
                if (GUILayout.Button("Brace")) Play(BattleAction.Brace);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            if (state.Outcome == BattleOutcome.Ongoing && state.Actives.Count > 0)
            {
                // Gear actives (GDD §9): cost stamina, take the turn.
                GUILayout.BeginHorizontal();
                bool waiting = Waiting();
                foreach (var skill in state.Actives)
                {
                    GUI.enabled = waiting && state.CanUseSkill(skill);
                    if (GUILayout.Button($"{GearSkillRules.Label(skill)} ({GearSkillRules.Describe(skill)})")) UseSkill(skill);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (state.Outcome == BattleOutcome.Ongoing && state.Bag != null)
            {
                // Consumables (Phase 6): using one takes the hunter's turn. Semi-auto and Auto use them when low (BattleAI.ChooseItem).
                var items = state.Bag.Items.Where(kv => kv.Key.effect != ConsumableEffect.None).ToList();
                GUILayout.BeginHorizontal();
                if (items.Count == 0) GUILayout.Label("<size=11>No consumables.</size>", new GUIStyle(GUI.skin.label) { richText = true });
                bool waiting = Waiting();
                foreach (var kv in items)
                {
                    var item = kv.Key;
                    string what = item.effect == ConsumableEffect.HealHunter ? $"+{item.effectAmount} HP"
                        : item.effect == ConsumableEffect.RepairRaft ? $"+{item.effectAmount} raft"
                        : item.effect == ConsumableEffect.HealCompanion ? $"+{item.effectAmount} minion"
                        : $"{item.element}, {item.effectAmount} rounds";
                    GUI.enabled = waiting && state.CanUseItem(item);
                    if (GUILayout.Button($"{item.displayName} ×{kv.Value} ({what})")) UseItem(item);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        static string OutcomeText(BattleOutcome o)
        {
            switch (o)
            {
                case BattleOutcome.Won: return "Victory!";
                case BattleOutcome.LostRaft: return "Your raft broke apart...";
                default: return "You were knocked out...";
            }
        }

        static void Bar(string label, int value, int max, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            var r = GUILayoutUtility.GetRect(200, 16, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.5f), 0, 0);
            var fill = new Rect(r.x, r.y, r.width * Mathf.Clamp01(value / (float)Mathf.Max(1, max)), r.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 0);
            GUILayout.Label($"{value}/{max}", GUILayout.Width(70));
            GUILayout.EndHorizontal();
        }
    }
}
