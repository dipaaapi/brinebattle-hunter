using BrineBattle.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Phase 6 Settings menu (Esc or the HUD button): battle mode and auto-use of consumables,
    /// both saved, plus the controls. Placeholder IMGUI like the other panels.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // before RaftBuilder, so Esc that closes build mode doesn't also open Settings
    public class SettingsPanel : MonoBehaviour
    {
        static bool open => WorldPause.Open == Menu.Settings;

        void Update()
        {
            AutoSave();
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Settings)) WorldPause.Toggle(Menu.Settings);
        }

        /// <summary>
        /// Every 5 minutes of play, into the existing save file, only in Semi-auto and Auto (owner's call).
        /// Waits while a menu, prompt or battle is open so it never saves mid-action.
        /// </summary>
        void AutoSave()
        {
            if (BattleSettings.Mode == BattleMode.Manual || !SaveSystem.Exists) return;
            autoTimer += Time.unscaledDeltaTime;
            if (autoTimer < SaveSystem.AutoSaveSeconds) return;
            if (BattleController.InBattle || WorldPause.Open != Menu.None || WorldPause.Prompt) return;
            autoTimer = 0f;
            saveMessage = SaveSystem.Save(auto: true);
            saveInfo = null;
        }

        void OnGUI()
        {
            if (!open) return;
            GUI.depth = -5;
            float w = Mathf.Min(420, Screen.width - HookController.HudRight - 22), h = Mathf.Min(520, Screen.height - 40);
            GUILayout.BeginArea(new Rect(Screen.width - w - 12, 20, w, h), "Settings", HuntStyles.Window);

            GUILayout.Label("<b>Battle mode</b> (also switchable during a battle)", Rich);
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels);
            GUILayout.Label(BattleSettings.Mode == BattleMode.Auto ? "<size=11>Auto plays every turn and reels for you (50% clean).</size>"
                : BattleSettings.Mode == BattleMode.SemiAuto ? "<size=11>Basic attacks are automatic; you decide the important turns. Reel: press first, or it auto-presses.</size>"
                : "<size=11>You pick every action. Reel: press Space in the green zone.</size>", Rich);

            GUILayout.Space(8);
            BattleSettings.AutoItems = GUILayout.Toggle(BattleSettings.AutoItems,
                $" Auto-use consumables at {Mathf.RoundToInt(BattleAI.LowHp * 100)}% HP or below (Semi-auto and Auto)");

            GUILayout.Space(8);
            GUILayout.Label("<b>Controls</b>", Rich);
            GUILayout.Label("<size=11>Click a drift: cast · Space: reel · B: Bag · C: Book · R: Build · Esc: Settings</size>", Rich);

            GUILayout.Space(8);
            GUILayout.Label("<b>Save game</b> (one save file: bag, gear, raft, modules, companions, book)", Rich);
            saveInfo ??= SaveSystem.Describe() ?? "no save yet";
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Last save: {saveInfo}", GUILayout.Width(190));
            if (GUILayout.Button("Save")) { saveMessage = SaveSystem.Save(); saveInfo = null; autoTimer = 0f; }
            GUI.enabled = SaveSystem.Exists;
            if (GUILayout.Button("Load")) { saveMessage = SaveSystem.Load(); autoTimer = 0f; }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(BattleSettings.Mode == BattleMode.Manual
                ? "<size=11>Auto-save is off in Manual. In Semi-auto and Auto it saves to this file every 5 minutes once a save exists.</size>"
                : $"<size=11>Auto-save: every 5 minutes into this file{(SaveSystem.Exists ? $" (next in {Mathf.CeilToInt((SaveSystem.AutoSaveSeconds - autoTimer) / 60f)} min)" : " once you save or load")}.</size>", Rich);
            if (!string.IsNullOrEmpty(saveMessage)) GUILayout.Label($"<size=11>{saveMessage}</size>", Rich);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=10>{SaveSystem.Folder}</size>", Rich);
            if (GUILayout.Button("Open folder", GUILayout.Width(90)))
            {
                System.IO.Directory.CreateDirectory(SaveSystem.Folder);
                Application.OpenURL("file:///" + SaveSystem.Folder.Replace('\\', '/'));
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close (Esc)")) WorldPause.Close(Menu.Settings);
            GUILayout.EndArea();
        }

        string saveInfo;
        string saveMessage;
        static float autoTimer;

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
    }
}
