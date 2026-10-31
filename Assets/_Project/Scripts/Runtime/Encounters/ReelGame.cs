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
    /// <summary>How a catch fights on the reel bar (Claude's C-14): a second, rough hint at what's on the line.</summary>
    public enum CatchStyle
    {
        /// <summary>Barely moves: dead.</summary>
        Limp,
        /// <summary>Swims about evenly.</summary>
        Swimmer,
        /// <summary>Snakes up and down fast, like an eel (water, ice, steam).</summary>
        ZigZag,
        /// <summary>Sits still, then jumps far, like a crab (earth, mud, sand, magma).</summary>
        Jumper,
        /// <summary>Sinks slowly, then darts upward, like a gull (wind, lightning).</summary>
        Darter,
        /// <summary>Heavy: keeps pulling to the bottom (big live monsters).</summary>
        Sinker,
    }

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

        /// <summary>The fighting style of a catch: dead ones go limp, big live ones sink, the rest by element.</summary>
        public static CatchStyle StyleOf(Elements.Element e, DriftState state, int sizeClass)
        {
            if (state == DriftState.Dead) return CatchStyle.Limp;
            if (state == DriftState.Alive && sizeClass >= 4) return CatchStyle.Sinker;
            switch (e)
            {
                case Elements.Element.Water: case Elements.Element.Ice: case Elements.Element.Steam: return CatchStyle.ZigZag;
                case Elements.Element.Earth: case Elements.Element.Mud: case Elements.Element.Sand: case Elements.Element.Magma: return CatchStyle.Jumper;
                case Elements.Element.Wind: case Elements.Element.Lightning: return CatchStyle.Darter;
                default: return CatchStyle.Swimmer;
            }
        }

        /// <summary>A few words for the reel label.</summary>
        public static string Describe(CatchStyle s)
        {
            switch (s)
            {
                case CatchStyle.Limp: return "It hangs limp on the line...";
                case CatchStyle.ZigZag: return "It snakes up and down!";
                case CatchStyle.Jumper: return "It sits... then jumps!";
                case CatchStyle.Darter: return "It darts upward!";
                case CatchStyle.Sinker: return "It's heavy, pulling down hard!";
                default: return "It swims about.";
            }
        }

        public BattleMode Mode { get; }
        public CatchStyle Style { get; }
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

        float velocity, goal, nextGoal, pause;
        int zig = 1;
        readonly Random rng;

        public ReelGame(float width, float speed, BattleMode mode, Random rng, CatchStyle style = CatchStyle.Swimmer)
        {
            Width = width;
            Style = style;
            // Limp catches drift slowly whatever their size.
            Speed = style == CatchStyle.Limp ? speed * 0.35f : speed;
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
                Zone = MoveTowards(Zone, want, Speed * 1.6f * dt);
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

        /// <summary>The catch swims toward a goal that changes now and then, in its own style (C-14).</summary>
        void MoveCatch(float dt)
        {
            float R() => (float)rng.NextDouble();
            float speed = Speed;
            nextGoal -= dt;
            switch (Style)
            {
                case CatchStyle.Limp:
                    if (nextGoal <= 0f) { goal = Clamp(Catch + (R() - 0.5f) * 0.3f, 0f, 1f); nextGoal = 0.8f + R(); }
                    break;
                case CatchStyle.ZigZag:
                    // Quick swings the other way each time.
                    if (nextGoal <= 0f) { zig = -zig; goal = Clamp(Catch + zig * (0.25f + R() * 0.2f), 0f, 1f); nextGoal = 0.35f + R() * 0.25f; }
                    speed *= 1.2f;
                    break;
                case CatchStyle.Jumper:
                    // Still for a moment, then a long fast jump.
                    if (pause > 0f) { pause -= dt; return; }
                    if (nextGoal <= 0f) { goal = R(); nextGoal = 0.5f; }
                    speed *= 2.2f;
                    if (Math.Abs(Catch - goal) < 0.01f) { pause = 0.6f + R() * 0.8f; nextGoal = 0f; }
                    break;
                case CatchStyle.Darter:
                    // Sinks slowly, then shoots up.
                    if (nextGoal <= 0f) { goal = goal < Catch ? Clamp(Catch + 0.35f + R() * 0.3f, 0f, 1f) : Clamp(Catch - 0.2f, 0f, 1f); nextGoal = goal > Catch ? 0.4f : 0.9f + R() * 0.6f; }
                    speed *= goal > Catch ? 2f : 0.5f;
                    break;
                case CatchStyle.Sinker:
                    // Keeps to the bottom, rising only now and then.
                    if (nextGoal <= 0f) { goal = R() < 0.25f ? 0.4f + R() * 0.4f : R() * 0.3f; nextGoal = 0.6f + R() * 0.8f; }
                    break;
                default:
                    if (nextGoal <= 0f) { goal = R(); nextGoal = 0.4f + R() * 0.9f; }
                    if (rng.NextDouble() < 0.15f) speed *= 1.8f;   // now and then a sudden dart
                    break;
            }
            Catch = Clamp(MoveTowards(Catch, goal, speed * dt), 0f, 1f);
        }

        static float MoveTowards(float a, float b, float step) => Math.Abs(b - a) <= step ? b : a + Math.Sign(b - a) * step;
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
