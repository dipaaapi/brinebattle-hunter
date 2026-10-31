using System;

namespace BrineBattle.Audio
{
    /// <summary>
    /// Writes the music loops as raw samples (Phase 9 BGM, placeholder until real tracks): a slow sea-shanty
    /// feel in D minor, 84 BPM, 8 bars (Dm – Bb – F – C, two bars each). Every layer is the same length so
    /// they loop together. Plain C# (no Unity) so tests can check the loops are clean.
    /// </summary>
    public static class MusicSynth
    {
        public const int Rate = 22050;
        public const float Bpm = 84f;
        public const int Bars = 8;
        public static float Beat => 60f / Bpm;
        public static float LoopSeconds => Bars * 4 * Beat;
        public static int LoopSamples => (int)(LoopSeconds * Rate);

        // Chord roots and notes (MIDI) for each two-bar step: Dm, Bb, F, C.
        static readonly int[][] Chords =
        {
            new[] { 50, 53, 57 },   // D F A
            new[] { 46, 50, 53 },   // Bb D F
            new[] { 53, 57, 60 },   // F A C
            new[] { 48, 52, 55 },   // C E G
        };

        static int[] ChordAt(float beat) => Chords[(int)(beat / 8f) % Chords.Length];

        public static float[] Make(MusicLayer layer, int seed = 7)
        {
            var buf = new float[LoopSamples];
            var rng = new Random(seed + (int)layer * 31);
            switch (layer)
            {
                case MusicLayer.Ocean: Ocean(buf, rng); break;
                case MusicLayer.DayTune: DayTune(buf); break;
                case MusicLayer.NightBells: NightBells(buf); break;
                case MusicLayer.Danger: Danger(buf, rng); break;
                case MusicLayer.Battle: Battle(buf, rng); break;
                case MusicLayer.Boss: Boss(buf); break;
                case MusicLayer.Storm: Storm(buf, rng); break;
            }
            Normalize(buf, layer == MusicLayer.Ocean ? 0.5f : 0.6f);
            return buf;
        }

        static float Freq(float midi) => 440f * (float)Math.Pow(2.0, (midi - 69f) / 12f);
        static float Sin(double phase) => (float)Math.Sin(phase);

        /// <summary>Adds a voice at a time, wrapping round the loop's end so the seam is seamless.</summary>
        static void Add(float[] buf, float start, float seconds, Func<float, float> voice)
        {
            int s0 = (int)(start * Rate), n = (int)(seconds * Rate);
            for (int i = 0; i < n; i++) buf[(s0 + i) % buf.Length] += voice(i / (float)Rate);
        }

        // ---- Layers ----

        /// <summary>A soft chord pad over a slow swell of filtered noise: always on.</summary>
        static void Ocean(float[] buf, Random rng)
        {
            for (int step = 0; step < Bars / 2; step++)
            {
                float t0 = step * 8 * Beat, len = 8 * Beat + 0.8f;
                foreach (int note in Chords[step % Chords.Length])
                {
                    float f = Freq(note);
                    Add(buf, t0, len, t =>
                    {
                        float env = Math.Min(1f, t / 1.2f) * Math.Min(1f, (len - t) / 1.2f);
                        double ph = 2 * Math.PI * f * t + 0.004 * Math.Sin(2 * Math.PI * 4.5 * t) * f;
                        return env * 0.18f * (Sin(ph) + 0.3f * Sin(ph * 2) + 0.12f * Sin(ph * 3));
                    });
                }
            }
            // The sea: brown noise that swells every 6 s or so (a whole number of swells per loop).
            int swells = Math.Max(1, (int)Math.Round(LoopSeconds / 6f));
            float b = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                b = (b + (float)(rng.NextDouble() * 2 - 1) * 0.02f) * 0.995f;
                float swell = 0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * swells * i / buf.Length);
                buf[i] += b * (0.4f + 1.4f * swell);
            }
        }

        /// <summary>A marimba-like shanty motif over the chords, by day.</summary>
        static void DayTune(float[] buf)
        {
            // (beat, note offset in the D minor pentatonic, length in beats)
            int[] scale = { 62, 65, 67, 69, 72, 74, 77 };   // D F G A C D F
            var motif = new (float beat, int step, float len)[]
            {
                (0, 0, 1), (1, 2, 0.5f), (1.5f, 3, 0.5f), (2, 4, 1), (3, 3, 1),
                (4, 2, 1.5f), (5.5f, 1, 0.5f), (6, 0, 2),
                (8, 1, 1), (9, 2, 0.5f), (9.5f, 1, 0.5f), (10, 0, 1), (11, 1, 1),
                (12, 2, 2), (14, 3, 1), (15, 2, 1),
            };
            for (int rep = 0; rep < 2; rep++)
                foreach (var (beat, step, len) in motif)
                {
                    // The second time round it climbs a little higher.
                    int note = scale[Math.Min(scale.Length - 1, step + (rep == 1 && beat >= 12 ? 2 : 0))];
                    float f = Freq(note), dur = len * Beat + 0.6f;
                    Add(buf, (beat + rep * 16) * Beat, dur, t =>
                    {
                        float env = (float)Math.Exp(-t * 5.5f) * Math.Min(1f, t * 200f);
                        double ph = 2 * Math.PI * f * t;
                        return env * 0.35f * (Sin(ph) + 0.25f * Sin(ph * 4) * (float)Math.Exp(-t * 18f));
                    });
                }
        }

        /// <summary>Sparse glassy bells on the chord tones, at night.</summary>
        static void NightBells(float[] buf)
        {
            for (float beat = 0; beat < Bars * 4; beat += 3f)
            {
                var chord = ChordAt(beat);
                float f = Freq(chord[(int)(beat / 3f) % chord.Length] + 24);
                Add(buf, beat * Beat, 3.2f, t =>
                {
                    float env = (float)Math.Exp(-t * 1.6f) * Math.Min(1f, t * 300f);
                    double ph = 2 * Math.PI * f * t;
                    return env * 0.22f * (Sin(ph) + 0.4f * Sin(ph * 2.76) * (float)Math.Exp(-t * 3f));
                });
            }
        }

        /// <summary>Low toms and a shaker: something alive is near.</summary>
        static void Danger(float[] buf, Random rng)
        {
            for (float beat = 0; beat < Bars * 4; beat++)
            {
                if (beat % 2 == 0 || beat % 8 == 7)
                    Add(buf, beat * Beat, 0.5f, t =>
                    {
                        double f = 55 + 70 * Math.Exp(-t * 18);
                        return (float)Math.Exp(-t * 7) * 0.9f * Sin(2 * Math.PI * f * t);
                    });
                for (int h = 0; h < 2; h++)
                    Add(buf, (beat + h * 0.5f) * Beat, 0.08f, t => (float)Math.Exp(-t * 60) * 0.25f * (float)(rng.NextDouble() * 2 - 1));
            }
        }

        /// <summary>A driving bass in eighths with a snare on the backbeat: battle.</summary>
        static void Battle(float[] buf, Random rng)
        {
            for (float beat = 0; beat < Bars * 4; beat += 0.5f)
            {
                int root = ChordAt(beat)[0] - 12;
                float f = Freq(root + ((int)(beat * 2) % 4 == 3 ? 7 : 0));
                Add(buf, beat * Beat, 0.3f, t =>
                {
                    float env = (float)Math.Exp(-t * 9) * Math.Min(1f, t * 400f);
                    double ph = f * t % 1.0;   // saw, softened
                    return env * 0.5f * (float)(ph * 2 - 1) * 0.6f + env * 0.4f * Sin(2 * Math.PI * f * t);
                });
            }
            for (float beat = 0; beat < Bars * 4; beat++)
            {
                if (beat % 2 == 1)
                    Add(buf, beat * Beat, 0.25f, t => (float)Math.Exp(-t * 16) * (0.5f * (float)(rng.NextDouble() * 2 - 1) + 0.3f * Sin(2 * Math.PI * 190 * t)));
                else
                    Add(buf, beat * Beat, 0.4f, t => (float)Math.Exp(-t * 9) * 0.8f * Sin(2 * Math.PI * (50 + 60 * Math.Exp(-t * 25)) * t));
                for (int h = 0; h < 4; h++)
                    Add(buf, (beat + h * 0.25f) * Beat, 0.05f, t => (float)Math.Exp(-t * 90) * 0.12f * (float)(rng.NextDouble() * 2 - 1));
            }
        }

        /// <summary>Brass stabs on the chords and a timpani roll at the end of each phrase: boss fights.</summary>
        static void Boss(float[] buf)
        {
            for (float beat = 0; beat < Bars * 4; beat += 4f)
            {
                var chord = ChordAt(beat);
                foreach (float off in new[] { 0f, 1.5f })
                    foreach (int note in chord)
                    {
                        float f = Freq(note);
                        Add(buf, (beat + off) * Beat, 0.7f, t =>
                        {
                            float env = Math.Min(1f, t * 30f) * (float)Math.Exp(-t * 3.5f);
                            double ph = 2 * Math.PI * f * t;
                            return env * 0.2f * (Sin(ph) + 0.6f * Sin(ph * 2) + 0.4f * Sin(ph * 3) + 0.25f * Sin(ph * 4));
                        });
                    }
            }
            for (int phrase = 0; phrase < Bars / 2; phrase++)
                for (int k = 0; k < 8; k++)
                {
                    float at = ((phrase + 1) * 8 - 1 + k / 8f) * Beat;
                    Add(buf, at, 0.4f, t => (float)Math.Exp(-t * 8) * (0.3f + k * 0.06f) * Sin(2 * Math.PI * (73.4 + 15 * Math.Exp(-t * 20)) * t));
                }
        }

        /// <summary>Rain (bright noise) over a low rumble that rolls in now and then.</summary>
        static void Storm(float[] buf, Random rng)
        {
            float lp = 0f, rumble = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.35f;
                rumble = (rumble + n * 0.01f) * 0.998f;
                float roll = 0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * 2 * i / buf.Length);   // two rolls a loop
                buf[i] += (n - lp) * 0.25f + rumble * 2.5f * roll;
            }
        }

        static void Normalize(float[] buf, float peak)
        {
            float max = 0f;
            foreach (var x in buf) max = Math.Max(max, Math.Abs(x));
            if (max < 1e-6f) return;
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }
    }
}
