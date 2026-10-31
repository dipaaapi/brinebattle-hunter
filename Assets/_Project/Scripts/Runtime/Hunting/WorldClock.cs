using System.Collections.Generic;
using BrineBattle.Battle;
using BrineBattle.Rafts;
using BrineBattle.World;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Day, night and weather in the scene (Idea Round 4). Runs the <see cref="WorldState"/> clock,
    /// lights the sky, fogs the sea, scales the waves and, in storms, throws lightning at the raft.
    /// The rules live in <see cref="WorldRules"/>; this only shows them. Time stops in menus and battles.
    /// </summary>
    public class WorldClock : MonoBehaviour
    {
        public Raft raft;
        public HookController hook;

        /// <summary>The world now; null before the clock starts (rules then act as a clear day).</summary>
        public static WorldState Now { get; private set; }
        public static TimeOfDay Time => Now?.Time ?? TimeOfDay.Day;
        public static Weather Weather => Now?.Weather ?? Weather.Clear;
        /// <summary>The raft hangs a lantern from Lv 2 (Claude's C-19); it lights at dusk and night.</summary>
        public static bool Lantern { get; private set; }

        /// <summary>Raft parts on fire right now; the hook is jammed while any burn (U-15).</summary>
        public static readonly List<RaftFire> Fires = new List<RaftFire>();
        public static bool Burning => Fires.Count > 0;

        readonly System.Random rng = new System.Random();
        Light sun;
        Camera cam;
        float strikeTimer, flash;

        void Awake()
        {
            Now = new WorldState();
            Now.Next = WorldRules.RollNext(Now.Weather, Now.Time, rng);
            Now.WeatherLeft = WorldRules.RollSeconds(rng);
            Now.WeatherChanged += OnWeather;
            Now.TimeChanged += t => hook?.Log(TimeLine(t));
        }

        void OnDestroy()
        {
            if (Now != null) Now.WeatherChanged -= OnWeather;
            Now = null;
            Fires.Clear();
            Waves.WeatherScale = 1f;
            RenderSettings.fog = false;
        }

        void Start()
        {
            cam = Camera.main;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            Waves.WeatherScale = WorldRules.WaveScale(Now.Weather);
            // Shadows swing round as the sun crosses the sky, so the time of day shows on the raft too.
            if (sun != null && sun.shadows == LightShadows.None) sun.shadows = LightShadows.Soft;
        }

        /// <summary>A loaded save puts the clock back where it was.</summary>
        public void Restore(WorldState saved)
        {
            if (saved == null) return;
            Now.Time01 = saved.Time01;
            Now.Day = saved.Day;
            Now.Weather = saved.Weather;
            Now.Next = saved.Next;
            Now.WeatherLeft = saved.WeatherLeft;
            Now.WrecksLeft = saved.WrecksLeft;
            Waves.WeatherScale = WorldRules.WaveScale(Now.Weather);
        }

        static string TimeLine(TimeOfDay t)
        {
            switch (t)
            {
                case TimeOfDay.Day: return "The sun is up. Sun creatures are stronger.";
                case TimeOfDay.Dusk: return "The sun is setting.";
                case TimeOfDay.Night: return "Night falls. Moon creatures are stronger, more drifts are alive, and it's hard to see far.";
                default: return "Dawn breaks.";
            }
        }

        void OnWeather(Weather from, Weather to)
        {
            string line;
            switch (to)
            {
                case Weather.Rain: line = "It starts to rain. Water creatures are stronger; hints are a bit worse."; break;
                case Weather.Rough: line = "The sea turns rough: big waves, fast drifts, shorter hook throws."; break;
                case Weather.Storm: line = "A storm! Lightning can set the raft on fire. More creatures and floating loot."; break;
                case Weather.Fog: line = "Fog rolls in. You can't make out far drifts, and hints are unreliable."; break;
                default: line = "The sky clears."; break;
            }
            if (from == Weather.Storm && to != Weather.Storm) { line += " Wreckage from the storm is drifting your way!"; hook?.First("storm"); }
            hook?.Log(line);
        }

        void Update()
        {
            if (!WorldPause.Active)
            {
                Now.Advance(UnityEngine.Time.deltaTime, rng);
                Storm(UnityEngine.Time.deltaTime);
            }
            // Waves ease into the new weather over a few seconds.
            Waves.WeatherScale = Mathf.MoveTowards(Waves.WeatherScale, WorldRules.WaveScale(Now.Weather), UnityEngine.Time.deltaTime * 0.15f);
            Look();
        }

        void Storm(float dt)
        {
            if (Now.Weather != Weather.Storm || raft == null) { strikeTimer = 0f; return; }
            strikeTimer += dt;
            if (strikeTimer < WorldRules.StrikeCheckSeconds) return;
            strikeTimer = 0f;
            if (rng.NextDouble() >= WorldRules.StrikeChance(Now.Weather, raft.Grid.Type.Main)) return;

            var options = raft.Grid.EdibleParts();
            options.RemoveAll(c => Fires.Exists(f => f != null && f.Cell.Equals(c)));
            flash = 1f;
            if (options.Count == 0)
            {
                hook?.Log("Lightning strikes the sea right next to the raft!");
                Bolt(raft.transform.position + new Vector3(3f, 0f, 2f));
                return;
            }
            var cell = options[rng.Next(options.Count)];
            var at = raft.CellToWorld(cell);
            Bolt(at);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(transform, false);
            var fire = go.AddComponent<RaftFire>();
            fire.Init(this, raft, cell);
            hook?.Log($"Lightning hit the raft! A {raft.Grid.Parts[cell].displayName} is on fire. Click the fire to put it out before it burns away. You can't hook until it's out.");
        }

        void Bolt(Vector3 at)
        {
            var bolt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bolt.name = "Lightning";
            Destroy(bolt.GetComponent<Collider>());
            bolt.transform.position = at + Vector3.up * 15f;
            bolt.transform.localScale = new Vector3(0.15f, 30f, 0.15f);
            bolt.transform.rotation = Quaternion.Euler(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
            Tint.Apply(bolt.GetComponent<Renderer>(), new Color(1f, 1f, 0.75f));
            Destroy(bolt, 0.25f);
        }

        /// <summary>A fire was put out (by click or by Crew) or burned the part away.</summary>
        internal void FireOut(RaftFire fire, bool burned)
        {
            Fires.Remove(fire);
            if (burned)
            {
                var part = raft.Grid.Burn(fire.Cell);
                hook?.Log(part != null ? $"The fire burned your {part.displayName} away." : "The fire burned itself out.");
            }
        }

        // ---- Looks ----

        static readonly Color DaySky = new Color(0.55f, 0.75f, 0.9f), NightSky = new Color(0.05f, 0.08f, 0.18f),
            DuskSky = new Color(0.95f, 0.55f, 0.4f), StormSky = new Color(0.25f, 0.28f, 0.32f), FogSky = new Color(0.7f, 0.74f, 0.78f);

        void Look()
        {
            float t = Now.Time01;
            float light = WorldRules.Daylight(t);
            var w = Now.Weather;
            float gloom = w == Weather.Storm ? 0.55f : w == Weather.Rain ? 0.8f : w == Weather.Fog ? 0.85f : 1f;

            // Sky colour: night → dawn/dusk orange → day blue, then dimmed by weather.
            float twilight = 1f - Mathf.Abs(light - 0.35f) / 0.35f;
            var sky = Color.Lerp(NightSky, DaySky, light);
            if (light > 0f && light < 0.7f) sky = Color.Lerp(sky, DuskSky, Mathf.Clamp01(twilight) * 0.6f);
            if (w == Weather.Storm || w == Weather.Rain) sky = Color.Lerp(sky, StormSky, w == Weather.Storm ? 0.6f : 0.35f);
            if (w == Weather.Fog) sky = Color.Lerp(sky, FogSky * Mathf.Max(0.25f, light), 0.6f);
            flash = Mathf.MoveTowards(flash, 0f, UnityEngine.Time.deltaTime * 4f);
            sky = Color.Lerp(sky, Color.white, flash * 0.7f);
            if (cam != null) cam.backgroundColor = sky;
            LanternAndGlow(light);

            if (sun != null)
            {
                // The sun swings across the sky; at night a dim blue moon light stays so the raft can be seen.
                float angle = Mathf.Lerp(10f, 170f, Mathf.Clamp01(t / WorldRules.NightStarts));
                sun.transform.rotation = light > 0f ? Quaternion.Euler(angle, -30f, 0f) : Quaternion.Euler(60f, 150f, 0f);
                sun.color = light > 0f ? Color.Lerp(new Color(1f, 0.6f, 0.4f), Color.white, light) : new Color(0.55f, 0.65f, 1f);
                sun.intensity = Mathf.Max(0.35f, 1.2f * light) * gloom + flash * 2f;
            }
            RenderSettings.ambientLight = Color.Lerp(new Color(0.18f, 0.2f, 0.32f), new Color(0.55f, 0.6f, 0.7f), light) * Mathf.Lerp(0.8f, 1f, gloom) + Color.white * flash * 0.4f;

            // Fog limits sight; the camera sits far above the raft, so the fog starts at the raft.
            // On clear days a light haze still melts the far sea into the sky, so its edge never shows (Ed's ask).
            float sight = WorldRules.SightRange(w, Now.Time, Lantern);
            bool fogOn = cam != null && raft != null;
            RenderSettings.fog = fogOn;
            if (fogOn)
            {
                float camDist = Vector3.Distance(cam.transform.position, raft.transform.position);
                float start = float.PositiveInfinity, end = float.PositiveInfinity;
                if (!float.IsInfinity(sight)) { start = camDist + sight * 0.4f; end = camDist + sight * 1.8f; }
                float edge = NearestSeaEdge();
                if (edge < end)
                {
                    end = edge;
                    start = Mathf.Min(start, Mathf.Max(camDist + 6f, edge * 0.55f));
                }
                RenderSettings.fog = !float.IsInfinity(end);
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = sky;
                RenderSettings.fogStartDistance = Mathf.Min(start, end - 1f);
                RenderSettings.fogEndDistance = end;
            }
        }

        // ---- Raft lantern and glowing Moon creatures (Claude's C-19) ----

        Light lamp;
        GameObject lampPost;
        float glowScan;

        void LanternAndGlow(float light)
        {
            Lantern = raft != null && raft.Tier >= Items.Rarity.Uncommon;
            bool lit = Lantern && light < 0.6f;
            if (Lantern && lampPost == null)
            {
                // A pole with a glowing bulb at the back of the raft, and a warm point light.
                lampPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lampPost.name = "Raft lantern";
                Destroy(lampPost.GetComponent<Collider>());
                lampPost.transform.SetParent(raft.transform, false);
                lampPost.transform.localScale = new Vector3(0.06f, 0.7f, 0.06f);
                Tint.Apply(lampPost.GetComponent<Renderer>(), new Color(0.3f, 0.22f, 0.15f));
                var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(bulb.GetComponent<Collider>());
                bulb.name = "Bulb";
                bulb.transform.SetParent(lampPost.transform, false);
                bulb.transform.localPosition = Vector3.up * 1.05f;
                bulb.transform.localScale = new Vector3(4f, 0.45f, 4f);
                lamp = bulb.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.color = new Color(1f, 0.8f, 0.45f);
                lamp.range = 9f;
            }
            if (lampPost != null)
            {
                lampPost.SetActive(Lantern);
                // Back (upstream-left) corner of the starter square, out of the hunter's way.
                lampPost.transform.localPosition = new Vector3(-Raft.CellSize, 1.0f, -Raft.CellSize);
                lamp.enabled = lit;
                lamp.intensity = lit ? Mathf.Lerp(2.5f, 0.8f, light / 0.6f) : 0f;
                Tint.Apply(lamp.GetComponent<Renderer>(), lit ? new Color(1f, 0.9f, 0.55f) : new Color(0.5f, 0.45f, 0.35f));
            }
            // Moon creatures glow in the dark: a brighter tint at night, their own colour by day.
            if (UnityEngine.Time.time < glowScan) return;
            glowScan = UnityEngine.Time.time + 0.5f;
            foreach (var d in FindObjectsByType<Drift>(FindObjectsSortMode.None))
            {
                if (d == null || d.Monster == null || d.Monster.isFlotsam || d.Monster.isBoss || d.Carried != null) continue;
                if (WorldRules.AffinityOf(d.Monster.element) != Affinity.Moon) continue;
                var c = Tint.Of(d.Monster.element);
                Tint.Apply(d.GetComponent<Renderer>(), WorldRules.Glows(d.Monster.element, Now.Time) ? Color.Lerp(c, new Color(0.6f, 1f, 1f), 0.5f) * 1.6f : c);
            }
        }

        Renderer ocean;

        /// <summary>
        /// How far (in view depth) the nearest on-screen point of the sea's edge is, a little short of it so
        /// the haze is full there; infinity when no edge is on screen.
        /// </summary>
        float NearestSeaEdge()
        {
            if (ocean == null)
            {
                var go = GameObject.Find("Ocean");
                if (go == null || !go.TryGetComponent(out ocean)) return float.PositiveInfinity;
            }
            var b = ocean.bounds;
            float y = ocean.transform.position.y, best = float.PositiveInfinity;
            var fwd = cam.transform.forward;
            const int n = 32;
            for (int side = 0; side < 4; side++)
                for (int i = 0; i <= n; i++)
                {
                    float k = i / (float)n;
                    var p = side == 0 ? new Vector3(Mathf.Lerp(b.min.x, b.max.x, k), y, b.min.z)
                        : side == 1 ? new Vector3(Mathf.Lerp(b.min.x, b.max.x, k), y, b.max.z)
                        : side == 2 ? new Vector3(b.min.x, y, Mathf.Lerp(b.min.z, b.max.z, k))
                        : new Vector3(b.max.x, y, Mathf.Lerp(b.min.z, b.max.z, k));
                    var vp = cam.WorldToViewportPoint(p);
                    if (vp.z <= 0f || vp.x < -0.2f || vp.x > 1.2f || vp.y < -0.2f || vp.y > 1.2f) continue;
                    best = Mathf.Min(best, Vector3.Dot(p - cam.transform.position, fwd));
                }
            return best * 0.92f;
        }

        // ---- Sun and moon in the sky (Ed: "sun and moon wala naman ako nakikita") ----

        /// <summary>
        /// The camera looks down at the sea, so the sky itself is never on screen. A small sky strip at
        /// the top-right shows the sun (by day) or the moon (at night) moving along its arc, dimmed by clouds.
        /// </summary>
        void OnGUI()
        {
            if (Now == null || WorldPause.Open != Menu.None || WorldPause.Asleep) return;
            HuntStyles.Begin();
            // Ed's ask: readable at any hour. A dark card: sun or moon on the left, day / time / weather on the
            // right, and the day's clock as a bar along the bottom.
            const float w = 210f, h = 62f;
            var box = new Rect(HuntStyles.W - w - 12f, 30f, w, h);
            GUI.DrawTexture(box, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.09f, 0.15f, 0.92f), 0, 12);
            GUI.DrawTexture(box, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.25f, 0.82f, 0.79f, 0.6f), 1.5f, 12);

            bool moon = WorldRules.Daylight(Now.Time01) <= 0f;
            bool cloudy = Now.Weather == Weather.Storm || Now.Weather == Weather.Rain || Now.Weather == Weather.Fog;
            const float r = 14f;
            var c = new Vector2(box.x + 26f, box.y + 24f);
            var disc = new Rect(c.x - r, c.y - r, 2f * r, 2f * r);
            if (!moon)
            {
                GUI.DrawTexture(new Rect(c.x - r * 1.5f, c.y - r * 1.5f, r * 3f, r * 3f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.85f, 0.3f, 0.22f), 0, r * 1.5f);
                GUI.DrawTexture(disc, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.82f, 0.25f), 0, r);
            }
            else
            {
                GUI.DrawTexture(disc, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.9f, 0.93f, 1f), 0, r);
                GUI.DrawTexture(new Rect(disc.x + r * 0.7f, disc.y - r * 0.3f, 2f * r, 2f * r), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.09f, 0.15f), 0, r);
            }
            if (cloudy)
            {
                var cloud = new Color(0.78f, 0.8f, 0.86f, 0.95f);
                GUI.DrawTexture(new Rect(c.x - 14f, c.y + 2f, 22f, 10f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, cloud, 0, 5f);
                GUI.DrawTexture(new Rect(c.x - 3f, c.y - 2f, 20f, 12f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, cloud, 0, 6f);
            }

            GUI.Label(new Rect(box.x + 52f, box.y + 6f, w - 58f, 20f), $"<b>Day {Now.Day} · {WorldRules.Name(Now.Time)}</b>", SkyLabel);
            GUI.Label(new Rect(box.x + 52f, box.y + 24f, w - 58f, 18f), $"<size=12><color={WeatherHex(Now.Weather)}>{WorldRules.Name(Now.Weather)}</color></size>", SkyLabel);

            // The day's clock: dark for night, light for day, a white tick for now.
            var bar = new Rect(box.x + 10f, box.yMax - 12f, w - 20f, 5f);
            const int seg = 24;
            for (int i = 0; i < seg; i++)
            {
                float t = (i + 0.5f) / seg, d = WorldRules.Daylight(t);
                var col = Color.Lerp(new Color(0.12f, 0.16f, 0.35f), new Color(0.98f, 0.82f, 0.4f), d);
                GUI.DrawTexture(new Rect(bar.x + bar.width * i / seg, bar.y, bar.width / seg + 0.5f, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, col, 0, 0);
            }
            float x = bar.x + bar.width * Mathf.Repeat(Now.Time01, 1f);
            GUI.DrawTexture(new Rect(x - 1.5f, bar.y - 3f, 3f, bar.height + 6f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.white, 0, 1.5f);
        }

        static string WeatherHex(Weather w)
        {
            switch (w)
            {
                case Weather.Storm: return "#ff8a80";
                case Weather.Rough: return "#ffc864";
                case Weather.Rain: return "#9ecbff";
                case Weather.Fog: return "#c8d2dc";
                default: return "#a5f0b4";
            }
        }

        static GUIStyle skyLabel;
        static GUIStyle SkyLabel => skyLabel ??= new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft, fontSize = 13, normal = { textColor = Color.white } };

        /// <summary>Applies time and weather to a fighter (in place); returns a note like "Moon power" or "".</summary>
        public static string Empower(Combatant c) => Now == null ? "" : WorldRules.Apply(c, Now.Time, Now.Weather);

        /// <summary>HUD line: "Day 2 · Night · Rain".</summary>
        public static string Summary => Now == null ? "" : $"Day {Now.Day} · {WorldRules.Name(Now.Time)} · {WorldRules.Name(Now.Weather)}";
    }

    /// <summary>A raft part on fire after a thunderstrike (U-15). Click it to douse; a Crew pet helps.</summary>
    public class RaftFire : MonoBehaviour
    {
        public Cell Cell { get; private set; }
        public float Burned => burned;
        public int ClicksLeft => clicks;
        WorldClock clock;
        Raft raft;
        float burned;
        int clicks = WorldRules.DouseClicks;

        public void Init(WorldClock clock, Raft raft, Cell cell)
        {
            this.clock = clock;
            this.raft = raft;
            Cell = cell;
            name = $"Fire {cell}";
            WorldClock.Fires.Add(this);
            Tint.Apply(GetComponent<Renderer>(), new Color(1f, 0.45f, 0.1f));
            Place();
        }

        void Place()
        {
            var p = raft.CellToWorld(Cell);
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 12f);
            transform.position = p + Vector3.up * 0.5f;
            transform.localScale = new Vector3(0.8f, 1.1f, 0.8f) * pulse;
        }

        /// <summary>One bucket of seawater. True when the fire is out.</summary>
        public bool Douse()
        {
            if (--clicks > 0) return false;
            Out(false);
            return true;
        }

        void Update()
        {
            if (raft == null || !raft.Grid.Has(Cell)) { Out(false); return; }
            Place();
            if (WorldPause.Active) return;
            burned += Time.deltaTime;
            // Crewmate minions keep the raft safe (Ed likes it automatic).
            var crew = clock.hook != null ? clock.hook.Companions.Best(Companions.MinionSkill.Crewmate) : null;
            if (crew != null && !crew.KnockedOut && burned >= WorldRules.CrewDousesAfter)
            {
                clock.hook.Log($"{crew.Name} put out the fire.");
                Out(false);
                return;
            }
            if (burned >= WorldRules.FireBurnSeconds) Out(true);
        }

        bool done;
        void Out(bool burnedAway)
        {
            if (done) return;
            done = true;
            clock.FireOut(this, burnedAway);
            Destroy(gameObject);
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (WorldPause.Active || Camera.main == null) return;
            var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(transform.position + Vector3.up));
            if (sp.z < 0) return;
            int left = Mathf.CeilToInt(WorldRules.FireBurnSeconds - burned);
            GUI.Label(new Rect(sp.x - 70, HuntStyles.H - sp.y - 22, 140, 22), $"<color=#ff9a3c><b>FIRE! {left} s</b></color>\n<i>Click to put out</i>", HuntStyles.WorldLabel);
        }
    }
}
