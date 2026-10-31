using System;

namespace BrineBattle.World
{
    /// <summary>
    /// The clock and the weather as they are now, plus the forecast (C-12). Plain C#: the scene's
    /// WorldClock advances it, saves keep it, EditMode tests drive it.
    /// </summary>
    public class WorldState
    {
        /// <summary>Time of day, 0–1 (0 = dawn).</summary>
        public float Time01 = WorldRules.StartTime;
        public Weather Weather = Weather.Clear;
        /// <summary>The forecast: what comes after this weather (C-12).</summary>
        public Weather Next = Weather.Clear;
        /// <summary>Seconds until <see cref="Next"/> arrives.</summary>
        public float WeatherLeft = WorldRules.MaxWeatherSeconds;
        /// <summary>Floating-loot drifts still owed by the last storm (C-13).</summary>
        public int WrecksLeft;
        /// <summary>Full days lived through (shown in the HUD).</summary>
        public int Day = 1;

        public TimeOfDay Time => WorldRules.TimeOf(Time01);

        /// <summary>Raised with (old, new) when the weather changes.</summary>
        public event Action<Weather, Weather> WeatherChanged;
        /// <summary>Raised with the new part of the day when it changes.</summary>
        public event Action<TimeOfDay> TimeChanged;

        /// <summary>Passes <paramref name="seconds"/> of world time.</summary>
        public void Advance(float seconds, Random rng)
        {
            if (seconds <= 0f) return;
            var before = Time;
            float t = Time01 + seconds / WorldRules.DayLengthSeconds;
            while (t >= 1f) { t -= 1f; Day++; }
            Time01 = t;
            if (Time != before) TimeChanged?.Invoke(Time);

            WeatherLeft -= seconds;
            // A very long step (a big catch-up) still moves one weather at a time.
            while (WeatherLeft <= 0f)
            {
                var old = Weather;
                Weather = Next;
                Next = WorldRules.RollNext(Weather, Time, rng);
                WeatherLeft += WorldRules.RollSeconds(rng);
                if (old == Weather.Storm && Weather != Weather.Storm) WrecksLeft = WorldRules.WrecksAfterStorm;
                if (old != Weather) WeatherChanged?.Invoke(old, Weather);
            }
        }

        /// <summary>Takes one wreck-field drift if any are owed (C-13).</summary>
        public bool TakeWreck()
        {
            if (WrecksLeft <= 0) return false;
            WrecksLeft--;
            return true;
        }

        /// <summary>Forecast line for the HUD, e.g. "Storm in 40 s".</summary>
        public string Forecast =>
            Next == Weather ? $"{WorldRules.Name(Weather)} for {Math.Ceiling(WeatherLeft)} s more"
                            : $"{WorldRules.Name(Next)} in {Math.Ceiling(WeatherLeft)} s";
    }
}
