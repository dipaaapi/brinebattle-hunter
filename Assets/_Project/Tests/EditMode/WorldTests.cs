using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Rafts;
using BrineBattle.World;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class WorldTests
    {
        [Test]
        public void DayRunsDawnDayDuskNight()
        {
            Assert.AreEqual(TimeOfDay.Dawn, WorldRules.TimeOf(0.02f));
            Assert.AreEqual(TimeOfDay.Day, WorldRules.TimeOf(0.3f));
            Assert.AreEqual(TimeOfDay.Dusk, WorldRules.TimeOf(0.54f));
            Assert.AreEqual(TimeOfDay.Night, WorldRules.TimeOf(0.8f));
            Assert.AreEqual(TimeOfDay.Day, WorldRules.TimeOf(1.3f), "wraps around");
            Assert.AreEqual(0f, WorldRules.Daylight(0.8f));
            Assert.Greater(WorldRules.Daylight(0.29f), WorldRules.Daylight(0.05f));
        }

        [Test]
        public void SunAndMoonCreaturesPeakAtTheirTime()
        {
            Assert.AreEqual(Affinity.Sun, WorldRules.AffinityOf(Element.Fire));
            Assert.AreEqual(Affinity.Moon, WorldRules.AffinityOf(Element.Water));
            Assert.AreEqual(Affinity.None, WorldRules.AffinityOf(Element.Earth));
            Assert.AreEqual(1, WorldRules.Favour(Affinity.Moon, TimeOfDay.Night));
            Assert.AreEqual(-1, WorldRules.Favour(Affinity.Moon, TimeOfDay.Day));
            Assert.AreEqual(0, WorldRules.Favour(Affinity.Sun, TimeOfDay.Dusk), "twilight favours nobody");

            var night = new Combatant { Element = Element.Water, Attack = 10, Defense = 10 };
            Assert.AreEqual("Moon power", WorldRules.Apply(night, TimeOfDay.Night, Weather.Clear));
            Assert.AreEqual(12, night.Attack);
            Assert.AreEqual(11, night.Defense);

            var day = new Combatant { Element = Element.Water, Attack = 10, Defense = 10 };
            WorldRules.Apply(day, TimeOfDay.Day, Weather.Clear);
            Assert.AreEqual(9, day.Attack);
            Assert.AreEqual(10, day.Defense);

            var earth = new Combatant { Element = Element.Earth, Attack = 10, Defense = 10 };
            Assert.AreEqual("", WorldRules.Apply(earth, TimeOfDay.Night, Weather.Clear));
            Assert.AreEqual(10, earth.Attack);
        }

        [Test]
        public void StormsPowerUpLightning()
        {
            var bolt = new Combatant { Element = Element.Lightning, Attack = 8 };
            Assert.AreEqual("Storm power", WorldRules.Apply(bolt, TimeOfDay.Day, Weather.Storm));
            Assert.AreEqual(10, bolt.Attack);
            Assert.AreEqual("", WorldRules.Note(Element.Lightning, TimeOfDay.Day, Weather.Clear));
        }

        [Test]
        public void SpawnsLeanTowardTheTimesCreatures()
        {
            Assert.Greater(WorldRules.SpawnWeight(Affinity.Moon, TimeOfDay.Night), WorldRules.SpawnWeight(Affinity.Sun, TimeOfDay.Night));
            Assert.AreEqual(1f, WorldRules.SpawnWeight(Affinity.None, TimeOfDay.Night));
        }

        [Test]
        public void StormsOnlyComeAfterRainOrRoughSeas()
        {
            Assert.AreEqual(0f, WorldRules.NextWeight(Weather.Clear, Weather.Storm, TimeOfDay.Day));
            Assert.AreEqual(0f, WorldRules.NextWeight(Weather.Fog, Weather.Storm, TimeOfDay.Day));
            Assert.Greater(WorldRules.NextWeight(Weather.Rain, Weather.Storm, TimeOfDay.Day), 0f);
            var rng = new System.Random(3);
            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual(Weather.Storm, WorldRules.RollNext(Weather.Clear, TimeOfDay.Night, rng));
        }

        [Test]
        public void RoughWeatherMakesHuntingHarder()
        {
            Assert.Greater(WorldRules.WaveScale(Weather.Storm), WorldRules.WaveScale(Weather.Rough));
            Assert.Greater(WorldRules.WaveScale(Weather.Rough), WorldRules.WaveScale(Weather.Clear));
            Assert.Greater(WorldRules.ReachPenalty(Weather.Storm), 0f);
            Assert.Less(WorldRules.ReelZoneScale(Weather.Rough), 1f);
            Assert.Greater(WorldRules.DriftSpeedScale(Weather.Storm), 1f);
            Assert.Greater(WorldRules.AliveScale(Weather.Storm, TimeOfDay.Night), WorldRules.AliveScale(Weather.Clear, TimeOfDay.Day));
            Assert.Greater(WorldRules.FlotsamBonus(Weather.Storm), 0f);
        }

        [Test]
        public void FogAndNightHideFarDriftsAndSpoilHints()
        {
            Assert.IsTrue(float.IsPositiveInfinity(WorldRules.SightRange(Weather.Clear, TimeOfDay.Day)));
            Assert.Less(WorldRules.SightRange(Weather.Fog, TimeOfDay.Night), WorldRules.SightRange(Weather.Fog, TimeOfDay.Day));
            Assert.Less(WorldRules.SightRange(Weather.Fog, TimeOfDay.Day), WorldRules.SightRange(Weather.Clear, TimeOfDay.Night));
            Assert.AreEqual(0.7f, WorldRules.HintAccuracy(0.7f, Weather.Clear, TimeOfDay.Day), 1e-5f);
            Assert.AreEqual(0.35f, WorldRules.HintAccuracy(0.7f, Weather.Fog, TimeOfDay.Night), 1e-5f);
            Assert.AreEqual(WorldRules.MinHintAccuracy, WorldRules.HintAccuracy(0.4f, Weather.Fog, TimeOfDay.Night), 1e-5f);
        }

        [Test]
        public void LightningOnlyInStormsAndFortressesShrugItOff()
        {
            Assert.AreEqual(0, WorldRules.StrikeChance(Weather.Rain, RaftKind.Plain));
            Assert.Less(WorldRules.StrikeChance(Weather.Storm, RaftKind.Fortress), WorldRules.StrikeChance(Weather.Storm, RaftKind.Plain));
            Assert.Greater(WorldRules.StrikeChance(Weather.Storm, RaftKind.Sky), WorldRules.StrikeChance(Weather.Storm, RaftKind.Plain));
        }

        [Test]
        public void ClockRollsDaysAndWeather()
        {
            var w = new WorldState { Time01 = 0.9f, Weather = Weather.Clear, Next = Weather.Clear, WeatherLeft = 1000f };
            w.Advance(WorldRules.DayLengthSeconds * 0.2f, new System.Random(1));
            Assert.AreEqual(2, w.Day);
            Assert.AreEqual(0.1f, w.Time01, 1e-4f);

            w = new WorldState { Weather = Weather.Rain, Next = Weather.Storm, WeatherLeft = 10f };
            Weather changedTo = Weather.Clear;
            w.WeatherChanged += (from, to) => changedTo = to;
            w.Advance(11f, new System.Random(1));
            Assert.AreEqual(Weather.Storm, w.Weather);
            Assert.AreEqual(Weather.Storm, changedTo);
            Assert.Greater(w.WeatherLeft, 0f);
            Assert.AreNotEqual(Weather.Storm, w.Next, "never two storms in a row");
            StringAssert.Contains("in", w.Forecast);
        }

        [Test]
        public void ClearingStormLeavesAWreckField()
        {
            var w = new WorldState { Weather = Weather.Storm, Next = Weather.Clear, WeatherLeft = 1f };
            w.Advance(2f, new System.Random(1));
            Assert.AreEqual(WorldRules.WrecksAfterStorm, w.WrecksLeft);
            for (int i = 0; i < WorldRules.WrecksAfterStorm; i++) Assert.IsTrue(w.TakeWreck());
            Assert.IsFalse(w.TakeWreck());
        }
    }
}
