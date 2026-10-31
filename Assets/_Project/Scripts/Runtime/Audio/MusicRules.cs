using System;

namespace BrineBattle.Audio
{
    /// <summary>The music's layers (Claude's C-20). All loop together in time; only their volumes change.</summary>
    public enum MusicLayer { Ocean = 0, DayTune = 1, NightBells = 2, Danger = 3, Battle = 4, Boss = 5, Storm = 6 }

    /// <summary>What the world is doing right now, as far as the music cares.</summary>
    public struct MusicMoment
    {
        /// <summary>0 night … 1 full day.</summary>
        public float Daylight;
        public bool Storm, Rain;
        /// <summary>A drift whose hint says it's alive is close to the raft.</summary>
        public bool Danger;
        public bool InBattle, BossBattle;
        /// <summary>A menu is open: everything but the bed goes quieter.</summary>
        public bool MenuOpen;
    }

    /// <summary>
    /// Layered music (Claude's C-20, Phase 9 BGM): one set of loops covers every state. A calm ocean bed
    /// always plays; a day tune by day, soft bells at night; drums when something alive is near; the battle
    /// layer in fights and a brass layer for bosses; rain and thunder in bad weather. Plain C# for tests.
    /// </summary>
    public static class MusicRules
    {
        public static readonly int Layers = Enum.GetValues(typeof(MusicLayer)).Length;

        /// <summary>Target volume 0-1 for each layer at this moment.</summary>
        public static float[] Targets(MusicMoment m)
        {
            var v = new float[Layers];
            float day = Clamp01(m.Daylight);
            bool fight = m.InBattle || m.BossBattle;
            v[(int)MusicLayer.Ocean] = fight ? 0.45f : 0.8f;
            // Calm tunes step back in a fight and when danger is close.
            float calm = fight ? 0f : m.Danger ? 0.35f : 1f;
            v[(int)MusicLayer.DayTune] = day * calm * (m.Storm ? 0.3f : 1f);
            v[(int)MusicLayer.NightBells] = (1f - day) * calm;
            v[(int)MusicLayer.Danger] = !fight && m.Danger ? 0.8f : 0f;
            v[(int)MusicLayer.Battle] = fight ? 0.9f : 0f;
            v[(int)MusicLayer.Boss] = m.BossBattle ? 0.9f : 0f;
            v[(int)MusicLayer.Storm] = m.Storm ? 0.8f : m.Rain ? 0.4f : 0f;
            if (m.MenuOpen)
                for (int i = 0; i < v.Length; i++)
                    if (i != (int)MusicLayer.Ocean) v[i] *= 0.5f;
            return v;
        }

        /// <summary>Seconds a layer takes to fade fully in or out: fights cut in fast, moods drift slowly.</summary>
        public static float FadeSeconds(MusicLayer l) =>
            l == MusicLayer.Battle || l == MusicLayer.Boss ? 0.8f : l == MusicLayer.Danger ? 1.5f : 4f;

        static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
    }
}
