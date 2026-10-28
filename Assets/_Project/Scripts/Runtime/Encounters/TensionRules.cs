using System;

namespace BrineBattle.Encounters
{
    /// <summary>
    /// Line tension while reeling (idea C-2): a marker sweeps a 0-1 bar and the player presses
    /// once. Hitting the sweet spot means a clean reel and the hunter strikes first if it's alive.
    /// </summary>
    public static class TensionRules
    {
        public const float BaseZone = 0.26f;
        public const float ZoneShrinkPerSize = 0.03f;
        public const float MinZone = 0.08f;
        public const float BaseSpeed = 1.1f;
        public const float SpeedPerSize = 0.15f;

        /// <summary>Bigger monsters pull harder: smaller sweet spot.</summary>
        public static float ZoneWidth(int sizeClass) =>
            Math.Max(MinZone, BaseZone - ZoneShrinkPerSize * (Math.Max(1, sizeClass) - 1));

        /// <summary>Bar sweeps per second, faster for bigger monsters.</summary>
        public static float Speed(int sizeClass) => BaseSpeed + SpeedPerSize * (Math.Max(1, sizeClass) - 1);

        /// <summary>Marker position 0-1 bouncing back and forth.</summary>
        public static float Marker(float time, float speed)
        {
            float x = time * speed % 2f;
            if (x < 0f) x += 2f;
            return x <= 1f ? x : 2f - x;
        }

        public static bool IsHit(float marker, float zoneStart, float zoneWidth) =>
            marker >= zoneStart && marker <= zoneStart + zoneWidth;

        /// <summary>Random sweet spot that fits inside the bar.</summary>
        public static float RandomZoneStart(float zoneWidth, Random rng) =>
            (float)rng.NextDouble() * (1f - zoneWidth);
    }
}
