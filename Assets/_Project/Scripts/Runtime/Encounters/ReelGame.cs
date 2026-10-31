using System;
using BrineBattle.Battle;

namespace BrineBattle.Encounters
{
    /// <summary>
    /// The reel minigame (Ed's U-42, a classic fishing-game feel). The catch darts up and down a vertical
    /// 0-1 bar; the player keeps the green catch zone on it by holding (or tapping) Space to lift the zone,
    /// letting go to let it sink. Inside the zone the catch meter fills, outside it drains: full = caught,
    /// empty = it slips off the hook. Semi-auto nudges the zone toward the catch; Auto plays it for you.
    /// Plain C# so EditMode tests can drive it frame by frame.
    /// </summary>
    public class ReelGame
    {
        // ---- Tuning ----
        public const float BaseZone = 0.34f;
        public const float ZonePerSize = 0.03f;
        public const float ZonePerRarity = 0.025f;
        public const float MinZone = 0.1f;
        public const float MaxZone = 0.5f;
        /// <summary>The meter starts a quarter full; about 2-3 s in the zone catches it.</summary>
        public const float StartMeter = 0.25f;
        public const float FillPerSecond = 0.33f;
        public const float DrainPerSecond = 0.22f;
        /// <summary>Caught with this little time outside the zone (seconds) counts as a clean reel.</summary>
        public const float CleanSlack = 0.3f;
        const float Lift = 2.6f, Sink = 1.7f, TapKick = 0.35f, MaxRise = 1.3f, MaxFall = 1.1f;

        /// <summary>Green zone width: rarer and bigger catches are harder; <paramref name="bonus"/> widens it (gear, Mastery, Looter).</summary>
        public static float ZoneWidth(int sizeClass, Items.Rarity rarity, float weatherScale = 1f, float bonus = 0f)
        {
            float w = BaseZone - ZonePerSize * (Math.Max(1, sizeClass) - 1) - ZonePerRarity * (int)rarity;
            return Clamp(w * weatherScale + bonus, MinZone, MaxZone);
        }

        /// <summary>How fast the catch darts about, in bar lengths per second: bigger and rarer is wilder.</summary>
        public static float CatchSpeed(int sizeClass, Items.Rarity rarity) => 0.3f + 0.07f * (Math.Max(1, sizeClass) - 1) + 0.05f * (int)rarity;

        /// <summary>Extra zone width from Alchemy Mastery (+0.5% a level, up to +6%) and a Looter minion (+4%).</summary>
        public static float Bonus(int masteryLevel, bool looter) => Math.Min(0.06f, 0.005f * Math.Max(0, masteryLevel - 1)) + (looter ? 0.04f : 0f);

        public BattleMode Mode { get; }
        public float Width { get; }
        public float Speed { get; }
        /// <summary>Bottom of the green zone, 0-1.</summary>
        public float Zone { get; private set; }
        /// <summary>Where the catch is, 0-1.</summary>
        public float Catch { get; private set; }
        /// <summary>Catch meter, 0-1.</summary>
        public float Meter { get; private set; } = StartMeter;
        public float TimeOutside { get; private set; }
        public bool Inside => Catch >= Zone && Catch <= Zone + Width;
        public bool Caught => Meter >= 1f;
        public bool Escaped => Meter <= 0f;
        public bool Done => Caught || Escaped;
        public bool Clean => Caught && TimeOutside <= CleanSlack;

        float velocity, goal, nextGoal;
        readonly Random rng;

        public ReelGame(float width, float speed, BattleMode mode, Random rng)
        {
            Width = width;
            Speed = speed;
            Mode = mode;
            this.rng = rng;
            Catch = goal = 0.3f + (float)rng.NextDouble() * 0.4f;
            Zone = Clamp(Catch - width / 2f, 0f, 1f - width);
        }

        /// <param name="held">Space or the mouse button is held.</param>
        /// <param name="tapped">Pressed this frame: a quick kick up, so spamming works as well as holding.</param>
        public void Step(float dt, bool held, bool tapped)
        {
            if (Done || dt <= 0f) return;
            MoveCatch(dt);
            float want = Clamp(Catch - Width / 2f, 0f, 1f - Width);
            if (Mode == BattleMode.Auto)
            {
                // Computer-played: follows the catch a step behind, so it can still slip outside now and then.
                Zone = MoveTowards(Zone, want, Speed * 1.15f * dt);
            }
            else
            {
                velocity += (held ? Lift : -Sink) * dt;
                if (tapped) velocity += TapKick;
                velocity = Clamp(velocity, -MaxFall, MaxRise);
                Zone += velocity * dt;
                if (Zone <= 0f) { Zone = 0f; velocity = Math.Max(0f, velocity); }
                if (Zone >= 1f - Width) { Zone = 1f - Width; velocity = Math.Min(0f, velocity) * 0.3f; }
                // Semi-auto: the zone is pulled toward the catch, so light play is enough.
                if (Mode == BattleMode.SemiAuto) Zone = MoveTowards(Zone, want, Speed * 0.7f * dt);
            }
            if (Inside) Meter += FillPerSecond * dt;
            else { Meter -= DrainPerSecond * dt; TimeOutside += dt; }
            Meter = Clamp(Meter, 0f, 1f);
        }

        /// <summary>The catch swims toward a goal that changes every so often, now and then with a sudden dart.</summary>
        void MoveCatch(float dt)
        {
            nextGoal -= dt;
            if (nextGoal <= 0f)
            {
                goal = (float)rng.NextDouble();
                nextGoal = 0.4f + (float)rng.NextDouble() * 0.9f;
            }
            float burst = rng.NextDouble() < 0.15f ? 1.8f : 1f;
            Catch = Clamp(MoveTowards(Catch, goal, Speed * burst * dt), 0f, 1f);
        }

        static float MoveTowards(float a, float b, float step) => Math.Abs(b - a) <= step ? b : a + Math.Sign(b - a) * step;
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
