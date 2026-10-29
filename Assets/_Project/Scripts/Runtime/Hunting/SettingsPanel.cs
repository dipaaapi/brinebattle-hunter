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
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Settings)) WorldPause.Toggle(Menu.Settings);
        }

        void OnGUI()
        {
            if (!open) return;
            GUI.depth = -5;
            float w = Mathf.Min(420, Screen.width - HookController.HudRight - 22), h = 330;
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

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close (Esc)")) WorldPause.Close(Menu.Settings);
            GUILayout.EndArea();
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
    }
}
