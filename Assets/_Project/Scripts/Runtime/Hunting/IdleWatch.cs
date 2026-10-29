using BrineBattle.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Ed's scenario: the player falls asleep with the game running. After <see cref="IdleRules.Minutes"/>
    /// with no input the game pauses (no new hooks or module battles, no auto-save) and shows a sleep screen;
    /// it saves first in Semi-auto and Auto. A battle already running finishes first. Any key or click wakes it.
    /// </summary>
    public class IdleWatch : MonoBehaviour
    {
        readonly IdleClock clock = new IdleClock();
        static IdleWatch instance;
        bool knockedOut;

        void Awake() => instance = this;

        /// <summary>
        /// Ed: a stronger monster can beat the hunter before the idle timer runs out. A loss while nobody
        /// has touched anything for <see cref="IdleRules.AwayAfterLossSeconds"/> pauses the game right away.
        /// </summary>
        public static void Lost()
        {
            if (instance == null || IdleRules.Minutes <= 0 || instance.clock.Idle < IdleRules.AwayAfterLossSeconds) return;
            instance.knockedOut = true;
            instance.Sleep();
        }
        bool due;
        float asleepFor;
        string saved;

        void Update()
        {
            bool input = HadInput();
            if (WorldPause.Asleep)
            {
                asleepFor += Time.unscaledDeltaTime;
                if (input) Wake();
                return;
            }
            if (clock.Tick(Time.unscaledDeltaTime, input, IdleRules.Minutes)) due = true;
            if (input) due = false;
            // Let a running battle end (it plays itself in Auto) before dozing off.
            if (due && !BattleController.InBattle) Sleep();
        }

        void Sleep()
        {
            due = false;
            asleepFor = 0f;
            WorldPause.Asleep = true;
            saved = BattleSettings.Mode != BattleMode.Manual && SaveSystem.Exists ? SaveSystem.Save(auto: true) : null;
        }

        void Wake()
        {
            WorldPause.Asleep = false;
            knockedOut = false;
            clock.Reset();
            saved = null;
        }

        static bool HadInput()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            if (mouse == null) return false;
            return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
                || mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.scroll.ReadValue().sqrMagnitude > 0f;
        }

        void OnGUI()
        {
            if (!WorldPause.Asleep) return;
            HuntStyles.Begin();
            GUI.depth = -20;
            var all = new Rect(0, 0, HuntStyles.W, HuntStyles.H);
            GUI.DrawTexture(all, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.02f, 0.03f, 0.08f, 0.85f), 0, 0);
            float cy = HuntStyles.H / 2f;
            GUI.Label(new Rect(0, cy - 80, HuntStyles.W, 50), "<size=40>Zzz…</size>", Center);
            GUI.Label(new Rect(0, cy - 25, HuntStyles.W, 30), knockedOut ? "<size=22><b>You were knocked out while away</b></size>" : "<size=22><b>You fell asleep</b></size>", Center);
            int min = Mathf.FloorToInt(asleepFor / 60f);
            string line = $"The raft waits quietly. Nothing fights or gets lost while you're away{(min > 0 ? $" ({min} min)" : "")}.";
            GUI.Label(new Rect(0, cy + 10, HuntStyles.W, 24), line, Center);
            if (saved != null) GUI.Label(new Rect(0, cy + 34, HuntStyles.W, 24), "<size=12>Game saved.</size>", Center);
            GUI.Label(new Rect(0, cy + 70, HuntStyles.W, 30), "<size=18><color=#ffd866>Press any key or click to continue</color></size>", Center);
        }

        static GUIStyle center;
        static GUIStyle Center
        {
            get
            {
                if (center == null)
                {
                    center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 14 };
                    center.normal.textColor = Color.white;
                }
                return center;
            }
        }
    }
}
