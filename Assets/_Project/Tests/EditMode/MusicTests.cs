using System;
using BrineBattle.Audio;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    /// <summary>Phase 9 BGM: layered loops made in code, mixed by what's happening (Claude's C-20).</summary>
    public class MusicTests
    {
        static float V(MusicMoment m, MusicLayer l) => MusicRules.Targets(m)[(int)l];

        [Test]
        public void DayTuneByDayBellsAtNight()
        {
            var day = new MusicMoment { Daylight = 1f };
            var night = new MusicMoment { Daylight = 0f };
            Assert.Greater(V(day, MusicLayer.DayTune), 0.9f);
            Assert.AreEqual(0f, V(day, MusicLayer.NightBells));
            Assert.Greater(V(night, MusicLayer.NightBells), 0.9f);
            Assert.AreEqual(0f, V(night, MusicLayer.DayTune));
            Assert.Greater(V(day, MusicLayer.Ocean), 0f, "the sea bed always plays");
        }

        [Test]
        public void BattleAndBossLayersTakeOver()
        {
            var fight = new MusicMoment { Daylight = 1f, InBattle = true };
            Assert.Greater(V(fight, MusicLayer.Battle), 0.5f);
            Assert.AreEqual(0f, V(fight, MusicLayer.DayTune));
            Assert.AreEqual(0f, V(fight, MusicLayer.Boss));
            var boss = new MusicMoment { Daylight = 1f, InBattle = true, BossBattle = true };
            Assert.Greater(V(boss, MusicLayer.Boss), 0.5f);
            Assert.Less(MusicRules.FadeSeconds(MusicLayer.Battle), MusicRules.FadeSeconds(MusicLayer.DayTune), "fights cut in fast");
        }

        [Test]
        public void DangerDrumsAndWeather()
        {
            Assert.Greater(V(new MusicMoment { Danger = true }, MusicLayer.Danger), 0f);
            Assert.AreEqual(0f, V(new MusicMoment(), MusicLayer.Danger));
            Assert.Greater(V(new MusicMoment { Storm = true }, MusicLayer.Storm), V(new MusicMoment { Rain = true }, MusicLayer.Storm));
            Assert.Less(V(new MusicMoment { Daylight = 1f, MenuOpen = true }, MusicLayer.DayTune), V(new MusicMoment { Daylight = 1f }, MusicLayer.DayTune), "quieter behind menus");
        }

        [Test]
        public void EveryLayerIsALoopOfTheSameLengthAndNotSilent()
        {
            foreach (MusicLayer l in Enum.GetValues(typeof(MusicLayer)))
            {
                var data = MusicSynth.Make(l);
                Assert.AreEqual(MusicSynth.LoopSamples, data.Length, l.ToString());
                float peak = 0f;
                foreach (var x in data) { Assert.IsFalse(float.IsNaN(x), l.ToString()); peak = Math.Max(peak, Math.Abs(x)); }
                Assert.That(peak, Is.InRange(0.3f, 0.61f), $"{l} is audible and never clips");
            }
            Assert.That(MusicSynth.LoopSeconds, Is.InRange(20f, 30f));
        }
    }
}
