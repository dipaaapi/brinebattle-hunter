using System;
using BrineBattle.Audio;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    /// <summary>Ed's ask: sound effects made in code (free, nothing downloaded), with a voice cap and slight random pitch.</summary>
    public class SfxTests
    {
        [Test]
        public void EverySoundIsShortCleanAndNeverClips()
        {
            foreach (var s in SfxSynth.All)
            {
                var data = SfxSynth.Make(s);
                Assert.Greater(data.Length, 0, s.ToString());
                Assert.LessOrEqual(data.Length, SfxSynth.Rate * 2.2f, $"{s} is under ~2 s");
                float peak = 0f;
                foreach (float v in data)
                {
                    Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), $"{s} has a bad sample");
                    peak = Math.Max(peak, Math.Abs(v));
                }
                Assert.LessOrEqual(peak, SfxSynth.Peak + 1e-4f, $"{s} clips");
                Assert.Greater(peak, 0.05f, $"{s} is silent");
                Assert.Less(Math.Abs(data[data.Length - 1]), 0.02f, $"{s} ends with a click");
            }
        }

        [Test]
        public void TheSameSeedMakesTheSameSound()
        {
            CollectionAssert.AreEqual(SfxSynth.Make(Sfx.Thunder, 5), SfxSynth.Make(Sfx.Thunder, 5));
        }

        [Test]
        public void TheEnvelopeRisesHoldsAndFades()
        {
            var t = new SfxTone(SfxWave.Square, 440, 440, 0, 0.1f, 0.2f, 0.1f, 1f);
            Assert.AreEqual(0f, SfxSynth.Envelope(t, -0.01f));
            Assert.AreEqual(0.5f, SfxSynth.Envelope(t, 0.05f), 1e-4f);
            Assert.AreEqual(1f, SfxSynth.Envelope(t, 0.2f));
            Assert.AreEqual(0.5f, SfxSynth.Envelope(t, 0.35f), 1e-4f);
            Assert.AreEqual(0f, SfxSynth.Envelope(t, 0.5f));
        }

        [Test]
        public void ANewSoundTakesAFreeVoiceElseTheOneEndingSoonest()
        {
            var ends = new float[SfxRules.Voices];
            for (int i = 0; i < ends.Length; i++) ends[i] = 10f + i;
            ends[4] = 1f;
            Assert.AreEqual(4, SfxRules.PickVoice(ends, 2f), "voice 4 has finished");
            ends[4] = 30f;
            Assert.AreEqual(0, SfxRules.PickVoice(ends, 2f), "all busy: steal the one ending soonest");
            Assert.AreEqual(10, SfxRules.Voices);
        }

        [Test]
        public void EffectsWobbleInPitchButTunesStayInTune()
        {
            Assert.AreEqual(0.94f, SfxRules.Pitch(Sfx.Hit, 0), 1e-4f);
            Assert.AreEqual(1.06f, SfxRules.Pitch(Sfx.Hit, 1), 1e-4f);
            Assert.AreEqual(1f, SfxRules.Pitch(Sfx.LevelUp, 0.3));
            Assert.AreEqual(1f, SfxRules.Pitch(Sfx.Achievement, 0.9));
            Assert.IsTrue(SfxRules.TooSoon(1f, 1.02f), "the same sound twice in 20 ms is skipped");
            Assert.IsFalse(SfxRules.TooSoon(1f, 1.2f));
        }

        [Test]
        public void OnlyUiSoundsPlayWhileTheShipyardHaltsTheWorld()
        {
            foreach (var s in new[] { Sfx.Click, Sfx.MenuOpen, Sfx.Error, Sfx.Place, Sfx.Remove, Sfx.Invalid, Sfx.LevelUp, Sfx.Achievement })
                Assert.IsTrue(SfxRules.Plays(s, true), s.ToString());
            foreach (var s in new[] { Sfx.Hit, Sfx.Thunder, Sfx.Splash, Sfx.Throw, Sfx.BossRumble, Sfx.Tame, Sfx.CraftDone })
            {
                Assert.IsFalse(SfxRules.Plays(s, true), s.ToString());
                Assert.IsTrue(SfxRules.Plays(s, false), s.ToString());
            }
        }
    }
}
