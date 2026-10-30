using System;

namespace BrineBattle.Hunting
{
    /// <summary>Camera limits (Ed's ask: zoom and rotate). Plain C# for EditMode tests.</summary>
    public static class CameraRules
    {
        public const float MinDistance = 8f, MaxDistance = 45f;
        public const float MinPitch = 25f, MaxPitch = 85f;
        /// <summary>Each wheel notch or +/- press moves 12% closer or farther.</summary>
        public const float ZoomStep = 0.12f;
        public const float DragDegrees = 0.25f, KeyTurnSpeed = 90f;

        public static float ClampDistance(float d) => Math.Clamp(d, MinDistance, MaxDistance);
        public static float ClampPitch(float p) => Math.Clamp(p, MinPitch, MaxPitch);

        /// <summary>One zoom step: <paramref name="direction"/> −1 = closer, +1 = farther.</summary>
        public static float Zoom(float distance, float direction) =>
            ClampDistance(distance * (1f + ZoomStep * Math.Sign(direction)));
    }
}
