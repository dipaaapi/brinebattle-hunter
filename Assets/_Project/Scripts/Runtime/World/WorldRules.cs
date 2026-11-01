using System;
using BrineBattle.Battle;
using BrineBattle.Elements;

namespace BrineBattle.World
{
    /// <summary>Part of the day (owner idea U-10). Dawn and dusk are twilight: nobody is boosted.</summary>
    public enum TimeOfDay { Dawn, Day, Dusk, Night }

    /// <summary>The sea's weather (U-15, U-21, C-3).</summary>
    public enum Weather { Clear, Rain, Rough, Storm, Fog }

    /// <summary>Who a creature draws power from (U-10). Most take it from their element.</summary>
    public enum Affinity { None, Sun, Moon }

    /// <summary>
    /// World conditions (Idea Round 4: U-10, U-15, U-21, C-3, C-12, C-13), numbers tunable.
    /// - **Day and night:** a full day is <see cref="DayLengthSeconds"/>. Sun creatures are stronger by day,
    ///   Moon creatures by night; more creatures are alive at night and hints are a bit worse.
    /// - **Weather** changes every 1–2.5 minutes. Rough seas and storms make bigger waves, faster drifts,
    ///   a shorter hook reach and a smaller reel sweet spot, but more creatures and more floating loot.
    ///   Fog and night hide drifts that are far away and make hints less reliable.
    /// - **Thunderstrikes** (storms only) set a raft part on fire; the hook is jammed until it's put out,
    ///   and a fire left too long burns the part away.
    /// - **Forecast (C-12):** the next weather is known ahead, so the player can get ready.
    /// - **Wreck field (C-13):** when a storm clears, the next few drifts are floating loot one rarity higher.
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class WorldRules
    {
        // ---- Day and night ----

        /// <summary>Real seconds for one full day (dawn → day → dusk → night).</summary>
        public const float DayLengthSeconds = 480f;
        /// <summary>Where each part of the day starts, as a share of the day (0–1).</summary>
        public const float DayStarts = 0.08f, DuskStarts = 0.5f, NightStarts = 0.58f;
        /// <summary>A new game starts in the morning.</summary>
        public const float StartTime = 0.12f;

        public static TimeOfDay TimeOf(float day01)
        {
            float t = Wrap01(day01);
            if (t < DayStarts) return TimeOfDay.Dawn;
            if (t < DuskStarts) return TimeOfDay.Day;
            if (t < NightStarts) return TimeOfDay.Dusk;
            return TimeOfDay.Night;
        }

        /// <summary>How bright the sun is, 0 at night … 1 at noon, with soft dawn and dusk.</summary>
        public static float Daylight(float day01)
        {
            float t = Wrap01(day01);
            // The sun is up from 0 (dawn) to NightStarts; a sine bump over that span.
            if (t >= NightStarts) return 0f;
            return (float)Math.Sin(Math.PI * t / NightStarts);
        }

        /// <summary>
        /// Where the sun (or, at night, the moon) is on its arc across the sky: 0 rising in the east,
        /// 0.5 overhead, 1 setting. The sun crosses from dawn to nightfall, the moon through the night.
        /// </summary>
        public static float SkyArc(float day01, out bool moon)
        {
            float t = Wrap01(day01);
            moon = t >= NightStarts;
            return moon ? (t - NightStarts) / (1f - NightStarts) : t / NightStarts;
        }

        public static float Wrap01(float v)
        {
            v %= 1f;
            return v < 0f ? v + 1f : v;
        }

        public static string Name(TimeOfDay t)
        {
            switch (t)
            {
                case TimeOfDay.Dawn: return "Dawn";
                case TimeOfDay.Day: return "Day";
                case TimeOfDay.Dusk: return "Dusk";
                default: return "Night";
            }
        }

        // ---- Sun and moon ----

        /// <summary>Sun: fire, magma, sand, steam. Moon: water, ice, mud. The rest (earth, wind, lightning, none): neither.</summary>
        public static Affinity AffinityOf(Element e)
        {
            switch (e)
            {
                case Element.Fire:
                case Element.Magma:
                case Element.Sand:
                case Element.Steam:
                    return Affinity.Sun;
                case Element.Water:
                case Element.Ice:
                case Element.Mud:
                    return Affinity.Moon;
                default:
                    return Affinity.None;
            }
        }

        /// <summary>In its own time: +20% attack and +10% defense. In the opposite time: −10% attack.</summary>
        public const float AffinityAttackUp = 0.2f, AffinityDefenseUp = 0.1f, AffinityAttackDown = 0.1f;

        /// <summary>+1 when the time favours this affinity, −1 when it's against it, 0 otherwise (and at twilight).</summary>
        public static int Favour(Affinity a, TimeOfDay t)
        {
            if (a == Affinity.None || t == TimeOfDay.Dawn || t == TimeOfDay.Dusk) return 0;
            bool sunTime = t == TimeOfDay.Day;
            return (a == Affinity.Sun) == sunTime ? 1 : -1;
        }

        /// <summary>The element a weather boosts by <see cref="WeatherAttackUp"/>: storms lightning, rain water, fog wind.</summary>
        public static Element Boosted(Weather w)
        {
            switch (w)
            {
                case Weather.Storm: return Element.Lightning;
                case Weather.Rain: return Element.Water;
                case Weather.Fog: return Element.Wind;
                default: return Element.None;
            }
        }

        public const float WeatherAttackUp = 0.25f;

        /// <summary>
        /// Changes a fighter's attack and defense for the time and weather (in place). Returns a short note
        /// such as "Moon power" or "" when nothing changed.
        /// </summary>
        public static string Apply(Combatant c, Affinity a, TimeOfDay t, Weather w)
        {
            if (c == null) return "";
            string note = "";
            int f = Favour(a, t);
            if (f > 0)
            {
                c.Attack = Up(c.Attack, AffinityAttackUp);
                c.Defense = Up(c.Defense, AffinityDefenseUp);
                note = a == Affinity.Sun ? "Sun power" : "Moon power";
            }
            else if (f < 0)
            {
                c.Attack = Math.Max(1, (int)Math.Round(c.Attack * (1f - AffinityAttackDown)));
                note = a == Affinity.Sun ? "Sun fading" : "Moon hidden";
            }
            if (w != Weather.Clear && c.Element == Boosted(w))
            {
                c.Attack = Up(c.Attack, WeatherAttackUp);
                note = note.Length > 0 ? $"{note}, {Name(w)} power" : $"{Name(w)} power";
            }
            return note;
        }

        /// <summary>The same, with the affinity taken from the fighter's element (the hunter's comes from its gear).</summary>
        public static string Apply(Combatant c, TimeOfDay t, Weather w) => c == null ? "" : Apply(c, AffinityOf(c.Element), t, w);

        /// <summary>What the time and weather do for an element, e.g. "Moon power", without changing anyone.</summary>
        public static string Note(Element e, TimeOfDay t, Weather w) =>
            Apply(new Combatant { Element = e, Attack = 10, Defense = 10 }, t, w);

        static int Up(int v, float share) => v + Math.Max(1, (int)Math.Round(v * share));

        /// <summary>How likely a creature of this affinity is to show up now: ×2 in its own time, ×0.5 against.</summary>
        public static float SpawnWeight(Affinity a, TimeOfDay t)
        {
            int f = Favour(a, t);
            return f > 0 ? 2f : f < 0 ? 0.5f : 1f;
        }

        // ---- Weather ----

        public static string Name(Weather w)
        {
            switch (w)
            {
                case Weather.Rain: return "Rain";
                case Weather.Rough: return "Rough sea";
                case Weather.Storm: return "Storm";
                case Weather.Fog: return "Fog";
                default: return "Clear";
            }
        }

        /// <summary>A weather lasts between these many seconds.</summary>
        public const float MinWeatherSeconds = 60f, MaxWeatherSeconds = 150f;

        /// <summary>
        /// Weights for the next weather. Storms only grow out of rain or a rough sea (so the forecast
        /// gives a warning), never twice in a row; fog is likelier at night and dawn.
        /// </summary>
        public static float NextWeight(Weather from, Weather to, TimeOfDay t)
        {
            if (to == from) return from == Weather.Clear ? 2f : from == Weather.Storm ? 0f : 0.5f;
            switch (to)
            {
                case Weather.Clear: return from == Weather.Storm ? 5f : 4f;
                case Weather.Rain: return 2f;
                case Weather.Rough: return from == Weather.Storm ? 1f : 1.5f;
                case Weather.Storm: return from == Weather.Rain || from == Weather.Rough ? 2f : 0f;
                case Weather.Fog: return t == TimeOfDay.Night || t == TimeOfDay.Dawn ? 2f : 0.75f;
                default: return 0f;
            }
        }

        public static Weather RollNext(Weather from, TimeOfDay t, Random rng)
        {
            float total = 0f;
            for (int i = 0; i <= (int)Weather.Fog; i++) total += NextWeight(from, (Weather)i, t);
            double r = rng.NextDouble() * total;
            for (int i = 0; i <= (int)Weather.Fog; i++)
            {
                r -= NextWeight(from, (Weather)i, t);
                if (r < 0) return (Weather)i;
            }
            return Weather.Clear;
        }

        public static float RollSeconds(Random rng) =>
            MinWeatherSeconds + (float)rng.NextDouble() * (MaxWeatherSeconds - MinWeatherSeconds);

        /// <summary>How tall the waves are compared with a clear day.</summary>
        public static float WaveScale(Weather w)
        {
            switch (w)
            {
                case Weather.Rain: return 1.15f;
                case Weather.Rough: return 1.5f;
                case Weather.Storm: return 1.8f;
                case Weather.Fog: return 0.6f;
                default: return 1f;
            }
        }

        /// <summary>How fast the current carries drifts.</summary>
        public static float DriftSpeedScale(Weather w) => w == Weather.Storm ? 1.6f : w == Weather.Rough ? 1.35f : 1f;

        /// <summary>Metres taken off the hook's reach: the raft rocks too much to throw far.</summary>
        public static float ReachPenalty(Weather w) => w == Weather.Storm ? 3f : w == Weather.Rough ? 2f : 0f;

        /// <summary>The reel's sweet spot is this much of its normal width (the line jerks about).</summary>
        public static float ReelZoneScale(Weather w) => w == Weather.Storm ? 0.6f : w == Weather.Rough ? 0.75f : 1f;

        /// <summary>Alive-roll multiplier: storms and night stir creatures up; calm clear days leave more dead.</summary>
        public static float AliveScale(Weather w, TimeOfDay t) =>
            (w == Weather.Storm ? 1.6f : w == Weather.Rough ? 1.3f : 1f) * (t == TimeOfDay.Night ? 1.4f : 1f);

        public static float DeadScale(Weather w, TimeOfDay t) => w == Weather.Clear && t == TimeOfDay.Day ? 1.3f : 1f;

        /// <summary>Extra share of drifts that are floating loot: storms break up wrecks.</summary>
        public static float FlotsamBonus(Weather w) => w == Weather.Storm ? 0.15f : w == Weather.Rough ? 0.05f : 0f;

        /// <summary>How much worse hints are: fog a lot, night and rain a little. They add up.</summary>
        public static float HintPenalty(Weather w, TimeOfDay t)
        {
            float p = w == Weather.Fog ? 0.25f : w == Weather.Rain || w == Weather.Storm ? 0.05f : 0f;
            if (t == TimeOfDay.Night) p += 0.1f;
            return p;
        }

        /// <summary>Hints can't drop below a coin toss between three states.</summary>
        public const float MinHintAccuracy = 0.34f;

        public static float HintAccuracy(float baseAccuracy, Weather w, TimeOfDay t) =>
            baseAccuracy <= MinHintAccuracy ? baseAccuracy : Math.Max(MinHintAccuracy, baseAccuracy - HintPenalty(w, t));

        /// <summary>
        /// How far (metres from the raft) a drift's name and hint can be read: fog 10, night 16, both 8.
        /// Infinity when it's clear enough to see everything.
        /// </summary>
        /// <summary>How much further the raft's lantern lets you see at night (Claude's C-19).</summary>
        public const float LanternReach = 6f;

        /// <summary>The raft lantern (Lv 2 and up) pushes night sight out; it doesn't help against fog by day.</summary>
        public static float SightRange(Weather w, TimeOfDay t, bool lantern)
        {
            float s = SightRange(w, t);
            return lantern && t == TimeOfDay.Night && !float.IsInfinity(s) ? s + LanternReach : s;
        }

        /// <summary>Moon creatures glow at night (C-19): they can be read from any distance in the dark.</summary>
        public static bool Glows(Element e, TimeOfDay t) => t == TimeOfDay.Night && AffinityOf(e) == Affinity.Moon;

        public static float SightRange(Weather w, TimeOfDay t)
        {
            bool fog = w == Weather.Fog, night = t == TimeOfDay.Night;
            if (fog && night) return 8f;
            if (fog) return 10f;
            if (night) return 16f;
            if (w == Weather.Storm) return 18f;
            return float.PositiveInfinity;
        }

        // ---- Thunderstrikes (U-15) ----

        /// <summary>In a storm, every this many seconds lightning may strike the raft.</summary>
        public const float StrikeCheckSeconds = 20f;
        public const double BaseStrikeChance = 0.35;

        /// <summary>
        /// Chance per check. Fortress rafts are low and armoured (half), Sky rafts stand tall (×1.5).
        /// Only in storms.
        /// </summary>
        public static double StrikeChance(Weather w, Rafts.RaftKind raftMain)
        {
            if (w != Weather.Storm) return 0;
            double c = BaseStrikeChance;
            if (raftMain == Rafts.RaftKind.Sea) c *= 0.5;
            else if (raftMain == Rafts.RaftKind.Aerial) c *= 1.5;
            return Math.Min(1, c);
        }

        /// <summary>Seconds a fire burns before the part is lost.</summary>
        public const float FireBurnSeconds = 15f;
        /// <summary>A Crew companion puts a fire out by itself after this many seconds.</summary>
        public const float CrewDousesAfter = 5f;
        /// <summary>Clicking a fire puts it out after this many clicks (one bucket each).</summary>
        public const int DouseClicks = 2;

        // ---- Wreck field (C-13) ----

        /// <summary>After a storm clears, this many drifts in a row are floating loot one rarity higher.</summary>
        public const int WrecksAfterStorm = 3;
    }
}
