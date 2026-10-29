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

        BattleState state;
        Drift drift;
        Action<BattleOutcome> onEnd;
        float endTimer;
        float timer;
        float shake;
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
            endTimer = 0f;
            var hook = FindFirstObjectByType<HookController>();
            state = new BattleState(Combatant.Hunter(hook?.Gear), Combatant.FromMonster(target.Monster), raft.MaxHp, new System.Random());
            state.FirstRound = firstRound;
            state.Weapon = hook?.Harpoon;
            state.Bag = hook?.Inventory;
            if (hook != null)
            {
                foreach (var p in hook.Gear.Passives) state.Passives.Add(p);
                state.Actives.AddRange(hook.Gear.Actives);
            }
            string first = firstRound == Initiative.Hunter ? " You strike first." : firstRound == Initiative.Monster ? " It strikes first." : "";
            state.Log.Add($"{target.Monster.displayName} attacks! Mode: {BattleSettings.Labels[(int)BattleSettings.Mode]}.{first}");
            timer = 0f;
            driftHome = target.transform.position;

            cam = Camera.main;
            if (cam != null)
            {
                camPos = cam.transform.position;
                camRot = cam.transform.rotation;
                var mid = Vector3.Lerp(raft.transform.position, driftHome, 0.5f);
                battleCamPos = mid + new Vector3(0f, 7f, -7f);
                battleCamRot = Quaternion.LookRotation(mid - battleCamPos);
            }
        }

        void Update()
        {
            UpdateCamera();
            if (!Active) return;

            if (drift != null)
            {
                shake = Mathf.Max(0f, shake - Time.deltaTime * 3f);
                var p = driftHome + UnityEngine.Random.insideUnitSphere * shake * 0.3f;
                if (drift.Monster.habitat == Monsters.Habitat.Sky) p.y += 2f + Mathf.Sin(Time.time * 3f) * 0.3f;
                drift.transform.position = p;
            }

            if (state.Outcome != BattleOutcome.Ongoing)
            {
                endTimer += Time.deltaTime;
                if (endTimer >= 1.2f) End();
                return;
            }
            var autoItem = AutoItem();
            var autoSkill = autoItem == null ? BattleAI.ChooseSkill(state, BattleSettings.Mode) : GearActive.None;
            var pick = autoItem != null ? BattleAction.Item
                : autoSkill != GearActive.None ? BattleAction.Skill
                : BattleAI.Choose(state, BattleSettings.Mode);
            if (pick == null) { timer = 0f; return; }
            timer += Time.deltaTime;
            if (timer < autoDelay) return;
            if (autoItem != null) UseItem(autoItem);
            else if (autoSkill != GearActive.None) UseSkill(autoSkill);
            else Play(pick.Value);
        }

        void UpdateCamera()
        {
            if (cam == null) return;
            var pos = Active ? battleCamPos : camPos;
            var rot = Active ? battleCamRot : camRot;
            float k = 1f - Mathf.Exp(-cameraBlend * Time.deltaTime);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(cam.transform.position, pos, k), Quaternion.Slerp(cam.transform.rotation, rot, k));
            if (!Active && (cam.transform.position - camPos).sqrMagnitude < 0.0001f) cam = null;
        }

        void Play(BattleAction action)
        {
            int before = state.Monster.Hp;
            state.PlayRound(action);
            if (state.Monster.Hp < before) shake = 1f;
            timer = 0f;
        }

        bool Waiting() => AutoItem() == null && BattleAI.ChooseSkill(state, BattleSettings.Mode) == GearActive.None
            && BattleAI.Choose(state, BattleSettings.Mode) == null;

        void UseSkill(GearActive skill)
        {
            int before = state.Monster.Hp;
            state.PlaySkill(skill);
            if (state.Monster.Hp < before) shake = 1f;
            timer = 0f;
        }

        ItemDefinition AutoItem() => BattleSettings.AutoItems ? BattleAI.ChooseItem(state, BattleSettings.Mode) : null;

        void UseItem(ItemDefinition item)
        {
            state.PlayItem(item);
            timer = 0f;
        }

        void End()
        {
            var outcome = state.Outcome;
            state = null;
            InBattle = false;
            drift = null;
            onEnd?.Invoke(outcome);
        }

        void OnGUI()
        {
            if (!Active) return;
            GUI.depth = -5;
            float w = 520, h = state.Actives.Count > 0 ? 360 : 330;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, Screen.height - h - 12, w, h), $"Battle — round {state.Round}" + (state.CoatingRounds > 0 ? $" — {state.Coating} oil" : ""), HuntStyles.Window);

            Bar(state.Monster.Name, state.Monster.Hp, state.Monster.MaxHp, new Color(0.9f, 0.35f, 0.3f));
            Bar("Hunter", state.Hunter.Hp, state.Hunter.MaxHp, new Color(0.35f, 0.85f, 0.4f));
            Bar("Raft", state.RaftHp, state.RaftMaxHp, new Color(0.85f, 0.75f, 0.5f));
            Bar("Stamina", state.Stamina, BattleRules.MaxStamina, new Color(0.4f, 0.65f, 1f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode", GUILayout.Width(60));
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels);
            GUILayout.EndHorizontal();

            int from = Mathf.Max(0, state.Log.Count - 4);
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
                if (items.Count == 0) GUILayout.Label("<size=11>No consumables — craft them at the Workbench.</size>", new GUIStyle(GUI.skin.label) { richText = true });
                bool waiting = Waiting();
                foreach (var kv in items)
                {
                    var item = kv.Key;
                    string what = item.effect == ConsumableEffect.HealHunter ? $"+{item.effectAmount} HP"
                        : item.effect == ConsumableEffect.RepairRaft ? $"+{item.effectAmount} raft"
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
