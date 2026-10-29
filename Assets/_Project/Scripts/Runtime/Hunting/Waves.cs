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
            return h;
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
