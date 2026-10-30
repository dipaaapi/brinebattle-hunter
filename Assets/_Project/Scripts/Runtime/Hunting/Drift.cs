using BrineBattle.Battle;
using BrineBattle.Encounters;
using BrineBattle.Monsters;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>A floating creature carried by the current. Its real state stays hidden until reeled in.</summary>
    public class Drift : MonoBehaviour
    {
        public MonsterDefinition Monster { get; private set; }
        public DriftState Actual { get; private set; }
        public DriftState Hint { get; private set; }
        /// <summary>
        /// Who has this drift: the player's hook, one hook arm, or one net. Only one at a time; nobody
        /// else can grab it until the owner lets go (or it's looted and gone).
        /// </summary>
        public object Owner { get; private set; }
        public bool Hooked => Owner != null;
        /// <summary>Bit a raft module and fled: modules leave it alone from now on, so one drift can't chew them down.</summary>
        /// <summary>Bit a module: modules leave it alone for a while as it swims off (not forever).</summary>
        public bool Spooked => Time.time < spookedUntil;
        float spookedUntil = -1f;
        /// <summary>Pressed against a raft part this frame (a module next to it should hook it, Ed's ask).</summary>
        public bool TouchingRaft { get; private set; }

        /// <summary>Takes the drift if nobody has it. Returns false if someone else already does.</summary>
        public bool TryClaim(object who)
        {
            if (Owner != null && Owner != who) return false;
            Owner = who;
            return true;
        }

        public void Release(object who)
        {
            if (Owner == who) Owner = null;
        }
        /// <summary>This drift's temperament, 0 calm – 1 aggressive (idea U-17).</summary>
        public float Aggression { get; set; } = 0.5f;

        /// <summary>Every drift on the sea, so they keep apart (owner's ask: nothing overlaps).</summary>
        static readonly System.Collections.Generic.List<Drift> all = new System.Collections.Generic.List<Drift>();
        /// <summary>Half the drift's footprint on the water.</summary>
        public float BodyRadius => 0.45f * Mathf.Max(transform.localScale.x, transform.localScale.z);

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        Vector3 velocity;
        float bobPhase;
        float lifetime;
        Vector3 current;
        static Raft raft;

        public void Init(MonsterDefinition monster, DriftState actual, DriftState hint, Vector3 velocity)
        {
            Monster = monster;
            Actual = actual;
            Hint = hint;
            this.velocity = current = velocity;
            bobPhase = Random.value * 10f;
            name = $"Drift ({monster.displayName})";
            Tint.Apply(GetComponent<Renderer>(), monster.isFlotsam ? Raft.PartColor(monster.rarity) : Tint.Of(monster.element));
            float s = 0.6f + 0.35f * monster.sizeClass;
            // Floating loot is a flat crate-like slab; creatures keep their capsule shape.
            transform.localScale = monster.isFlotsam ? new Vector3(s, s * 0.3f, s) : new Vector3(s, s * 0.5f, s * 1.4f);
        }

        /// <summary>An alive drift that bit a raft module swims away from it, faster.</summary>
        public void SwimOff(Vector3 away)
        {
            spookedUntil = Time.time + 20f;
            // Away from the raft sideways, but always carried downstream: never against the current.
            var down = current.sqrMagnitude > 0.0001f ? current.normalized : Vector3.right;
            var side = Vector3.ProjectOnPlane(away, down);
            side.y = 0f;
            velocity = down * Mathf.Max(2f, current.magnitude * 1.5f) + (side.sqrMagnitude > 0.0001f ? side.normalized * 1.2f : Vector3.zero);
        }

        /// <summary>Raft parts this monster ate (Phase 8). It comes back alive and stronger; 0 for a normal drift.</summary>
        public int GorgedParts { get; private set; }

        /// <summary>
        /// A monster that ate raft parts comes back: always alive, bigger, the hint says so, and the eaten
        /// parts stick out of its back (idea C-7).
        /// </summary>
        public void MakeGorged(System.Collections.Generic.IReadOnlyList<Items.ItemDefinition> parts)
        {
            GorgedParts = parts.Count;
            Actual = Hint = DriftState.Alive;
            Aggression = 1f;
            name = $"Drift (Gorged {Monster.displayName})";
            transform.localScale *= 1.25f;
            for (int i = 0; i < Mathf.Min(parts.Count, Rafts.AbsorbRules.MaxStackedParts); i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"Eaten {parts[i].displayName}";
                Destroy(chunk.GetComponent<Collider>());
                chunk.transform.SetParent(transform, false);
                // Sizes are in the drift's own (stretched) space.
                chunk.transform.localPosition = new Vector3((i % 2 == 0 ? -0.18f : 0.18f), 0.45f, 0.3f - 0.2f * i);
                chunk.transform.localRotation = Quaternion.Euler(20f * (i % 2 == 0 ? 1 : -1), 15f * i, 25f);
                chunk.transform.localScale = new Vector3(0.35f, 0.5f, 0.2f);
                Tint.Apply(chunk.GetComponent<Renderer>(), Raft.PartColor(parts[i]));
            }
        }

        /// <summary>How this drift fights: its monster's stats, grown by any raft parts it ate.</summary>
        public Combatant Fighter
        {
            get
            {
                var c = Rafts.AbsorbRules.Gorge(Combatant.FromMonster(Monster), GorgedParts);
                WorldClock.Empower(c);   // Idea Round 4: sun/moon and weather
                return c;
            }
        }

        /// <summary>After a successful sedate (GDD §4).</summary>
        public void MakeWeakened() => Actual = DriftState.Weakened;

        void Update()
        {
            if (Hooked) return;
            Ride(transform.position);
            if (WorldPause.Active) return;
            lifetime += Time.deltaTime;
            var p = transform.position + velocity * Time.deltaTime;
            if (raft == null) raft = FindFirstObjectByType<Raft>();
            if (raft != null) p = BumpOffParts(p);
            p = KeepApart(p);
            Ride(p);
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
            if (lifetime > 60f) Destroy(gameObject);
        }

        /// <summary>Floats on the swell, with a little bob of its own.</summary>
        void Ride(Vector3 p)
        {
            p.y = 0.1f + Waves.Height(p.x, p.z) + Mathf.Sin((Time.time + bobPhase) * 2f) * 0.04f;
            transform.position = p;
        }

        /// <summary>Pushes this drift out of any other drift it overlaps (hooked ones don't budge).</summary>
        Vector3 KeepApart(Vector3 p)
        {
            foreach (var other in all)
            {
                if (other == this) continue;
                var d = p - other.transform.position;
                d.y = 0f;
                float gap = BodyRadius + other.BodyRadius, dist = d.magnitude;
                if (dist >= gap) continue;
                var away = dist > 0.001f ? d / dist : Vector3.forward;
                // Both move half the overlap; next to a hooked drift this one moves all of it.
                p += away * (gap - dist) * (other.Hooked ? 1f : 0.5f);
            }
            return p;
        }

        /// <summary>Drifts slide along the raft's actual parts (not a circle around it) instead of passing through.</summary>
        Vector3 BumpOffParts(Vector3 p)
        {
            float half = Raft.CellSize * 0.5f + 0.25f * transform.localScale.x;
            TouchingRaft = false;
            foreach (var c in raft.Grid.Parts.Keys)
            {
                var d = p - raft.CellToWorld(c);
                float ox = half - Mathf.Abs(d.x), oz = half - Mathf.Abs(d.z);
                if (ox <= 0f || oz <= 0f) continue;
                TouchingRaft = true;
                // Push out of the part only; the current (velocity) stays the same, so the drift
                // slides along the raft's side and carries on downstream.
                float slide = velocity.magnitude * Time.deltaTime;
                if (ox < oz) { p.x += Mathf.Sign(d.x) * ox; p.z += SlideSign(d.z, velocity.z) * slide; }
                else { p.z += Mathf.Sign(d.z) * oz; p.x += SlideSign(d.x, velocity.x) * slide; }
            }
            return p;
        }

        /// <summary>Slide the way the current already leans; if it hits head-on, go round the nearer side.</summary>
        static float SlideSign(float offset, float along) =>
            Mathf.Abs(along) > 0.05f ? 0f : (offset >= 0f ? 1f : -1f);

        public static string HintText(DriftState s)
        {
            switch (s)
            {
                case DriftState.Dead: return "Looks dead";
                case DriftState.Weakened: return "Twitching...";
                default: return "Something moves!";
            }
        }

        /// <summary>The hunter and battle pet as they'd fight now; set by the HookController for the danger check.</summary>
        public static Combatant Hunter, Pet;

        void OnGUI()
        {
            HuntStyles.Begin();
            if (Hooked || WorldPause.Active || Camera.main == null) return;
            Vector3 sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(transform.position + Vector3.up));
            if (sp.z < 0) return;
            var rect = new Rect(sp.x - 70, HuntStyles.H - sp.y - 22, 140, 22);
            // Fog and night (U-21): far drifts are only shapes; get closer to read them.
            if (raft != null && WorldClock.Now != null)
            {
                var d = transform.position - raft.transform.position;
                d.y = 0f;
                if (d.magnitude > World.WorldRules.SightRange(WorldClock.Weather, WorldClock.Time))
                {
                    GUI.Label(rect, "???\n<i>Too far to see</i>", HuntStyles.WorldLabel);
                    return;
                }
            }
            // "Danger!" before a fight (Ed's call): this monster would likely beat the hunter and pet.
            if (Monster.isFlotsam)
            {
                GUI.Label(rect, $"{Monster.displayName}\n<i>Floating loot · {Monster.rarity}</i>", HuntStyles.WorldLabel);
                return;
            }
            bool danger = Hunter != null && DefeatRules.Danger(Hunter, Fighter, Pet);
            string hint = GorgedParts > 0 ? $"<color=#ffb36b>Gorged! Ate {GorgedParts} raft part{(GorgedParts == 1 ? "" : "s")}</color>" : HintText(Hint);
            // Idea C-11: what ailment its hits carry, so the right potion can be ready.
            if (Monster.inflicts != StatusEffect.None) hint += $"  <color=#c9a0ff>{StatusRules.Tag(Monster.inflicts)}</color>";
            GUI.Label(rect, $"{(danger ? "<color=#ff6b6b><b>⚠ Danger!</b></color> " : "")}{Monster.displayName}\n<i>{hint}</i>", HuntStyles.WorldLabel);
        }
    }
}
