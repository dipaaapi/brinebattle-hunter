using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The sea's swell (owner's ask: the raft should rock on real waves). A few long sine waves in
    /// different directions; the ocean mesh, the raft and the drifts all read the same height, so
    /// everything rides the same water.
    /// </summary>
    public static class Waves
    {
        // x, z = direction (normalised in Height), w = wavelength in metres, amplitude and speed below.
        static readonly Vector4[] waves =
        {
            new Vector4(1f, 0.2f, 0f, 10f),
            new Vector4(0.4f, 1f, 0f, 7f),
            new Vector4(-0.7f, 0.6f, 0f, 4.5f),
            new Vector4(0.9f, -0.5f, 0f, 3f),
        };
        // Tall enough to read from the top-down camera (owner's ask: real waves, not lines).
        static readonly float[] amplitude = { 0.38f, 0.22f, 0.11f, 0.06f };
        static readonly float[] speed = { 1.1f, 0.9f, 1.4f, 1.2f };

        /// <summary>
        /// Calmer water around a low-level raft (owner's ask: a big raft was sinking into the swell; a
        /// higher raft level has thicker planks and rides bigger waves). Inside
        /// <see cref="CalmRadius"/> of <see cref="CalmCenter"/> the waves drop to <see cref="CalmShare"/>,
        /// rising back to full over <see cref="CalmFade"/> metres. The raft sets these every frame.
        /// </summary>
        public static Vector3 CalmCenter;
        public static float CalmRadius;
        /// <summary>Set by the raft from its level (<see cref="Rafts.RaftRules.SeaShare"/>); 1 = full waves.</summary>
        public static float CalmShare = 1f;
        public const float CalmFade = 5f;
        /// <summary>Weather's wave height (Idea Round 4): rough seas and storms swell, fog flattens. Set by the WorldClock.</summary>
        public static float WeatherScale = 1f;

        static float Calm(float x, float z)
        {
            if (CalmRadius <= 0f) return 1f;
            float d = new Vector2(x - CalmCenter.x, z - CalmCenter.z).magnitude;
            return Mathf.Lerp(CalmShare, 1f, Mathf.SmoothStep(0f, 1f, (d - CalmRadius) / CalmFade));
        }

        /// <summary>Water height (metres above y = 0) at a world point now.</summary>
        public static float Height(float x, float z) => Height(x, z, Time.time);

        public static float Height(float x, float z, float t)
        {
            float h = 0f;
            for (int i = 0; i < waves.Length; i++)
            {
                var w = waves[i];
                var dir = new Vector2(w.x, w.y).normalized;
                float k = 2f * Mathf.PI / w.w;
                h += amplitude[i] * Mathf.Sin(k * (dir.x * x + dir.y * z) - speed[i] * t * k * 1.2f + i * 1.7f);
            }
            return h * Calm(x, z) * WeatherScale;
        }

        /// <summary>How the water leans around a point, as the surface's up direction.</summary>
        public static Vector3 Normal(float x, float z, float span = 1.5f)
        {
            float dx = Height(x + span, z) - Height(x - span, z);
            float dz = Height(x, z + span) - Height(x, z - span);
            return new Vector3(-dx, 2f * span, -dz).normalized;
        }
    }
}
