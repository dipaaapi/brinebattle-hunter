using System;
using BrineBattle.Battle;
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

        BattleState state;
        Drift drift;
        Action<BattleOutcome> onEnd;
        float timer;
        float shake;
        Vector3 driftHome;

        Camera cam;
        Vector3 camPos;
        Quaternion camRot;
        Vector3 battleCamPos;
        Quaternion battleCamRot;

        public void Begin(Drift target, Action<BattleOutcome> onFinished)
        {
            drift = target;
            onEnd = onFinished;
            state = new BattleState(Combatant.Hunter(), Combatant.FromMonster(target.Monster), raft.MaxHp, new System.Random());
            state.Log.Add($"{target.Monster.displayName} attacks! Mode: {BattleSettings.Labels[(int)BattleSettings.Mode]}.");
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

            if (state.Outcome != BattleOutcome.Ongoing) return;
            var pick = BattleAI.Choose(state, BattleSettings.Mode);
            if (pick == null) { timer = 0f; return; }
            timer += Time.deltaTime;
            if (timer >= autoDelay) Play(pick.Value);
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

        void End()
        {
            var outcome = state.Outcome;
            state = null;
            drift = null;
            onEnd?.Invoke(outcome);
        }

        void OnGUI()
        {
            if (!Active) return;
            float w = 520, h = 300;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2f, Screen.height - h - 12, w, h), $"Battle — round {state.Round}", GUI.skin.window);

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
                GUILayout.Label(OutcomeText(state.Outcome));
                if (GUILayout.Button("Continue", GUILayout.Width(120))) End();
            }
            else
            {
                bool waiting = BattleAI.Choose(state, BattleSettings.Mode) == null;
                GUI.enabled = waiting;
                if (GUILayout.Button("Attack")) Play(BattleAction.Attack);
                GUI.enabled = waiting && state.CanUse(BattleAction.Harpoon);
                if (GUILayout.Button($"Harpoon ({BattleRules.HarpoonCost} st)")) Play(BattleAction.Harpoon);
                GUI.enabled = waiting;
                if (GUILayout.Button("Brace")) Play(BattleAction.Brace);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
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
