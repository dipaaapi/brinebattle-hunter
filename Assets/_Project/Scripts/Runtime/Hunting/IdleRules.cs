using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Falling asleep at the game (Ed's scenario): after this many minutes with no key, click or mouse
    /// move, the game saves and pauses until the player is back, so nothing fights or loses while they're away.
    /// </summary>
    public static class IdleRules
    {
        public static readonly int[] Choices = { 0, 5, 10, 20 };
        public static readonly string[] Labels = { "Off", "5 min", "10 min", "20 min" };
        public const int DefaultMinutes = 10;
        /// <summary>A battle lost after this long with no input pauses the game at once.</summary>
        public const float AwayAfterLossSeconds = 60f;

        /// <summary>0 = never pause.</summary>
        public static int Minutes
        {
            get => PlayerPrefs.GetInt("brinebattle.idleMinutes", DefaultMinutes);
            set => PlayerPrefs.SetInt("brinebattle.idleMinutes", value);
        }
    }

    /// <summary>Plain idle clock, for EditMode tests: input resets it, and it says when to fall asleep.</summary>
    public class IdleClock
    {
        public float Idle { get; private set; }

        /// <summary>True on the tick the idle time first reaches the limit (minutes; 0 = off).</summary>
        public bool Tick(float seconds, bool hadInput, int minutes)
        {
            if (hadInput) { Idle = 0f; return false; }
            float before = Idle;
            Idle += seconds;
            if (minutes <= 0) return false;
            float limit = minutes * 60f;
            return before < limit && Idle >= limit;
        }

        public void Reset() => Idle = 0f;
    }
}
