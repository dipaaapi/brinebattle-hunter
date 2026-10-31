using System;

namespace BrineBattle.Audio
{
    /// <summary>Every sound effect in the game. Made in code at start by <see cref="SfxSynth"/> (no sound files).</summary>
    public enum Sfx
    {
        // UI (these still play in the Shipyard, where the world is halted)
        Click, Tab, MenuOpen, MenuClose, Error,
        // Hunting
        Throw, HookHit, ReelTick, Loot, Rare,
        // Battle
        Hit, Crit, Miss, Heal, Buff, Defeat, KnockOut,
        // Boss
        BossRumble, Enrage, TidalWave, BossDown,
        // Raft and Shipyard
        Place, Remove, Invalid, Hammer, LevelUp, ShipPick, Achievement,
        // Minions
        Tame, MinionLevel, CraftDone,
        // World
        Thunder, Splash,
    }

    public enum SfxWave { Square, Saw, Triangle, Sine, Noise }

    /// <summary>
    /// One voice of a sound effect, sfxr-style: a wave whose pitch slides from <see cref="From"/> to <see cref="To"/> Hz,
    /// starting <see cref="At"/> seconds in, with an attack / hold / release envelope.
    /// </summary>
    public struct SfxTone
    {
        public SfxWave Wave;
        public float From, To, At, Attack, Hold, Release, Volume, Vibrato;

        public float End => At + Attack + Hold + Release;

        public SfxTone(SfxWave wave, float from, float to, float at, float attack, float hold, float release, float volume, float vibrato = 0f)
        {
            Wave = wave; From = from; To = to; At = at; Attack = attack; Hold = hold; Release = release; Volume = volume; Vibrato = vibrato;
        }
    }

    /// <summary>
    /// Writes each sound effect as raw samples (Ed's ask: free, made in code, nothing to download). Plain C#
    /// (no Unity) so tests can check every sound is short, clean and never clips.
    /// </summary>
    public static class SfxSynth
    {
        public const int Rate = 22050;
        /// <summary>The loudest any sample may be after mixing the voices.</summary>
        public const float Peak = 0.8f;

        public static readonly Sfx[] All = (Sfx[])Enum.GetValues(typeof(Sfx));

        static float N(int midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12.0);

        static SfxTone T(SfxWave w, float from, float to, float at, float attack, float hold, float release, float vol, float vib = 0f)
            => new SfxTone(w, from, to, at, attack, hold, release, vol, vib);

        /// <summary>A quick run of notes, one every <paramref name="step"/> seconds; the last one rings longer.</summary>
        static SfxTone[] Run(SfxWave w, float step, float vol, float lastHold, params int[] notes)
        {
            var r = new SfxTone[notes.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                bool last = i == notes.Length - 1;
                r[i] = T(w, N(notes[i]), N(notes[i]), i * step, 0.005f, last ? lastHold : step * 0.6f, last ? 0.25f : 0.05f, vol, last ? 0.3f : 0f);
            }
            return r;
        }

        static SfxTone[] Join(params SfxTone[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var r = new SfxTone[n];
            int i = 0;
            foreach (var p in parts) foreach (var t in p) r[i++] = t;
            return r;
        }

        const SfxWave Sq = SfxWave.Square, Saw = SfxWave.Saw, Tri = SfxWave.Triangle, Sin = SfxWave.Sine, Noise = SfxWave.Noise;

        /// <summary>The voices that make up a sound.</summary>
        public static SfxTone[] Recipe(Sfx s)
        {
            switch (s)
            {
                case Sfx.Click: return new[] { T(Sq, 1200, 900, 0, 0.002f, 0.01f, 0.03f, 0.3f) };
                case Sfx.Tab: return new[] { T(Tri, 700, 1000, 0, 0.002f, 0.02f, 0.04f, 0.45f) };
                case Sfx.MenuOpen: return new[] { T(Tri, 400, 800, 0, 0.005f, 0.06f, 0.06f, 0.45f) };
                case Sfx.MenuClose: return new[] { T(Tri, 800, 400, 0, 0.005f, 0.06f, 0.06f, 0.45f) };
                case Sfx.Error: return new[] { T(Sq, 220, 200, 0, 0.003f, 0.05f, 0.03f, 0.25f), T(Sq, 196, 180, 0.1f, 0.003f, 0.06f, 0.05f, 0.25f) };

                case Sfx.Throw: return new[] { T(Noise, 3000, 800, 0, 0.03f, 0.08f, 0.14f, 0.35f), T(Sin, 700, 300, 0, 0.02f, 0.1f, 0.1f, 0.25f) };
                case Sfx.HookHit: return new[] { T(Sq, 300, 120, 0, 0.002f, 0.03f, 0.06f, 0.35f), T(Noise, 2000, 1000, 0, 0.002f, 0.02f, 0.04f, 0.3f) };
                case Sfx.ReelTick: return new[] { T(Sq, 1800, 1700, 0, 0.001f, 0.005f, 0.012f, 0.15f) };
                case Sfx.Loot: return Run(Sq, 0.06f, 0.25f, 0.06f, 72, 76);
                case Sfx.Rare: return Run(Tri, 0.06f, 0.4f, 0.2f, 79, 83, 86, 91);

                case Sfx.Hit: return new[] { T(Noise, 1500, 300, 0, 0.002f, 0.03f, 0.09f, 0.45f), T(Sq, 200, 80, 0, 0.002f, 0.03f, 0.07f, 0.3f) };
                case Sfx.Crit: return new[] { T(Noise, 2500, 200, 0, 0.002f, 0.05f, 0.15f, 0.5f), T(Saw, 400, 100, 0, 0.002f, 0.06f, 0.12f, 0.35f), T(Sq, 900, 900, 0, 0.002f, 0.02f, 0.03f, 0.2f) };
                case Sfx.Miss: return new[] { T(Noise, 4000, 2000, 0, 0.02f, 0.05f, 0.08f, 0.2f) };
                case Sfx.Heal: return Join(new[] { T(Sin, 300, 600, 0, 0.05f, 0.15f, 0.15f, 0.2f) }, Run(Tri, 0.07f, 0.35f, 0.15f, 72, 76, 79));
                case Sfx.Buff: return new[] { T(Tri, 500, 1000, 0, 0.02f, 0.12f, 0.08f, 0.4f, 0.5f) };
                case Sfx.Defeat: return new[] { T(Sq, 400, 60, 0, 0.003f, 0.12f, 0.22f, 0.3f), T(Noise, 800, 100, 0, 0.003f, 0.1f, 0.2f, 0.3f) };
                case Sfx.KnockOut: return Run(Tri, 0.15f, 0.4f, 0.3f, 67, 63, 60, 55);

                case Sfx.BossRumble: return new[] { T(Noise, 120, 60, 0, 0.3f, 0.9f, 0.5f, 0.6f), T(Saw, 55, 40, 0, 0.3f, 0.9f, 0.5f, 0.3f, 0.4f) };
                case Sfx.Enrage: return new[] { T(Saw, 110, 330, 0, 0.05f, 0.4f, 0.2f, 0.35f, 1f), T(Noise, 300, 900, 0, 0.05f, 0.3f, 0.2f, 0.25f) };
                case Sfx.TidalWave: return new[] { T(Noise, 200, 1500, 0, 0.15f, 0.2f, 0.05f, 0.4f), T(Noise, 1500, 200, 0.4f, 0.02f, 0.2f, 0.4f, 0.45f) };
                case Sfx.BossDown: return Run(Sq, 0.12f, 0.3f, 0.45f, 60, 64, 67, 72);

                case Sfx.Place: return new[] { T(Sq, 150, 90, 0, 0.002f, 0.03f, 0.05f, 0.35f), T(Noise, 600, 400, 0, 0.002f, 0.02f, 0.04f, 0.3f) };
                case Sfx.Remove: return new[] { T(Tri, 500, 200, 0, 0.003f, 0.04f, 0.07f, 0.45f) };
                case Sfx.Invalid: return new[] { T(Sq, 160, 160, 0, 0.003f, 0.04f, 0.02f, 0.25f), T(Sq, 160, 160, 0.08f, 0.003f, 0.04f, 0.04f, 0.25f) };
                case Sfx.Hammer: return new[]
                {
                    T(Noise, 1200, 900, 0, 0.001f, 0.01f, 0.03f, 0.35f), T(Sq, 900, 700, 0, 0.001f, 0.02f, 0.04f, 0.2f),
                    T(Noise, 1200, 900, 0.18f, 0.001f, 0.01f, 0.03f, 0.35f), T(Sq, 900, 700, 0.18f, 0.001f, 0.02f, 0.04f, 0.2f),
                };
                case Sfx.LevelUp: return Run(Sq, 0.07f, 0.28f, 0.3f, 60, 64, 67, 72, 76);
                case Sfx.ShipPick: return Run(Tri, 0.14f, 0.45f, 0.6f, 55, 62, 67, 74, 79);
                case Sfx.Achievement: return Run(Sq, 0.09f, 0.28f, 0.4f, 76, 79, 84, 88);

                case Sfx.Tame: return new[] { T(Sin, N(67), N(79), 0, 0.03f, 0.2f, 0.12f, 0.45f, 1f) };
                case Sfx.MinionLevel: return Run(Tri, 0.06f, 0.4f, 0.2f, 72, 79, 84);
                case Sfx.CraftDone: return Join(new[] { T(Noise, 1500, 1000, 0, 0.001f, 0.01f, 0.03f, 0.25f) }, Run(Sq, 0.08f, 0.22f, 0.2f, 84, 88));

                case Sfx.Thunder: return new[] { T(Noise, 3000, 500, 0, 0.002f, 0.04f, 0.12f, 0.45f), T(Noise, 200, 40, 0.05f, 0.05f, 0.5f, 1.2f, 0.7f) };
                case Sfx.Splash: return new[] { T(Noise, 1800, 300, 0, 0.01f, 0.08f, 0.25f, 0.45f) };
            }
            return new[] { T(Sq, 440, 440, 0, 0.002f, 0.02f, 0.02f, 0.2f) };
        }

        public static float Seconds(Sfx s)
        {
            float end = 0f;
            foreach (var t in Recipe(s)) end = Math.Max(end, t.End);
            return end + 0.02f;
        }

        /// <summary>The samples of one sound, mono at <see cref="Rate"/>, never louder than <see cref="Peak"/>.</summary>
        public static float[] Make(Sfx s, int seed = 3)
        {
            var tones = Recipe(s);
            var buf = new float[(int)(Seconds(s) * Rate)];
            var rng = new Random(seed + (int)s * 17);
            foreach (var t in tones) Add(buf, t, rng);
            float max = 0f;
            foreach (float v in buf) max = Math.Max(max, Math.Abs(v));
            if (max > Peak) for (int i = 0; i < buf.Length; i++) buf[i] *= Peak / max;
            return buf;
        }

        static void Add(float[] buf, SfxTone t, Random rng)
        {
            int start = (int)(t.At * Rate), len = (int)((t.End - t.At) * Rate);
            float dur = Math.Max(1e-4f, t.End - t.At);
            double phase = 0;
            float noise = 0f;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float time = i / (float)Rate;
                float k = time / dur;
                float freq = t.From * (float)Math.Pow(t.To / t.From, k);
                if (t.Vibrato > 0f) freq *= 1f + 0.02f * t.Vibrato * (float)Math.Sin(2 * Math.PI * 6 * time);
                double before = phase;
                phase = (phase + freq / Rate) % 1.0;
                float p = (float)phase, v;
                switch (t.Wave)
                {
                    case SfxWave.Square: v = p < 0.5f ? 1f : -1f; break;
                    case SfxWave.Saw: v = 2f * p - 1f; break;
                    case SfxWave.Triangle: v = 1f - 4f * Math.Abs(p - 0.5f); break;
                    case SfxWave.Sine: v = (float)Math.Sin(2 * Math.PI * p); break;
                    default:
                        // Sample-and-hold noise: a new random value every cycle, so the "pitch" colours the hiss.
                        if (phase < before || i == 0) noise = (float)(rng.NextDouble() * 2 - 1);
                        v = noise; break;
                }
                buf[start + i] += v * t.Volume * Envelope(t, time);
            }
        }

        /// <summary>0 → 1 over the attack, 1 through the hold, back to 0 over the release.</summary>
        public static float Envelope(SfxTone t, float time)
        {
            if (time < 0f) return 0f;
            if (time < t.Attack) return time / t.Attack;
            time -= t.Attack;
            if (time < t.Hold) return 1f;
            time -= t.Hold;
            return time < t.Release ? 1f - time / t.Release : 0f;
        }
    }

    /// <summary>When and how sounds play: the voice cap, the pitch wobble, and what still plays in the halted Shipyard.</summary>
    public static class SfxRules
    {
        /// <summary>At most this many sounds at once; a new one takes the oldest voice.</summary>
        public const int Voices = 10;
        /// <summary>The same sound again within this many seconds is skipped (no machine-gun stacking).</summary>
        public const float SameSoundGap = 0.05f;
        public const float DefaultVolume = 0.7f;

        /// <summary>UI and Shipyard sounds: they still play while the world is halted.</summary>
        public static bool IsUi(Sfx s) => s <= Sfx.Error || (s >= Sfx.Place && s <= Sfx.Achievement);

        /// <summary>Melodies keep their notes; everything else gets a slight random pitch so repeats don't sound copied.</summary>
        public static bool IsTune(Sfx s) => s == Sfx.Loot || s == Sfx.Rare || s == Sfx.Heal || s == Sfx.KnockOut || s == Sfx.BossDown
            || s == Sfx.LevelUp || s == Sfx.ShipPick || s == Sfx.Achievement || s == Sfx.MinionLevel || s == Sfx.CraftDone;

        /// <summary>Pitch for a play, from a 0-1 random number: 0.94-1.06 for effects, exactly 1 for tunes.</summary>
        public static float Pitch(Sfx s, double random01) => IsTune(s) ? 1f : 0.94f + 0.12f * (float)random01;

        /// <summary>Whether a sound plays now. In the Shipyard the world is halted: only UI sounds.</summary>
        public static bool Plays(Sfx s, bool worldHalted) => !worldHalted || IsUi(s);

        public static bool TooSoon(float lastPlayed, float now) => now - lastPlayed < SameSoundGap;

        /// <summary>The voice for a new sound: a free one (its sound ended) if any, else the one that ends soonest.</summary>
        public static int PickVoice(float[] endsAt, float now)
        {
            int best = 0;
            for (int i = 0; i < endsAt.Length; i++)
            {
                if (endsAt[i] <= now) return i;
                if (endsAt[i] < endsAt[best]) best = i;
            }
            return best;
        }
    }
}
