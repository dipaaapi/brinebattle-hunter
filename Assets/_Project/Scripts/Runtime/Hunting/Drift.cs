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
        public bool Hooked { get; set; }

        Vector3 velocity;
        float bobPhase;
        float lifetime;
        static Raft raft;

        public void Init(MonsterDefinition monster, DriftState actual, DriftState hint, Vector3 velocity)
        {
            Monster = monster;
            Actual = actual;
            Hint = hint;
            this.velocity = velocity;
            bobPhase = Random.value * 10f;
            name = $"Drift ({monster.displayName})";
            Tint.Apply(GetComponent<Renderer>(), Tint.Of(monster.element));
            float s = 0.6f + 0.35f * monster.sizeClass;
            transform.localScale = new Vector3(s, s * 0.5f, s * 1.4f);
        }

        /// <summary>An alive drift that bit a raft module swims away from it, faster.</summary>
        public void SwimOff(Vector3 away)
        {
            if (away.sqrMagnitude < 0.0001f) away = Vector3.forward;
            velocity = away.normalized * Mathf.Max(2f, velocity.magnitude * 1.5f);
        }

        /// <summary>After a successful sedate (GDD §4).</summary>
        public void MakeWeakened() => Actual = DriftState.Weakened;

        void Update()
        {
            if (Hooked || WorldPause.Active) return;
            lifetime += Time.deltaTime;
            var p = transform.position + velocity * Time.deltaTime;
            if (raft == null) raft = FindFirstObjectByType<Raft>();
            if (raft != null) p = BumpOffParts(p);
            p.y = 0.1f + Mathf.Sin((Time.time + bobPhase) * 2f) * 0.08f;
            transform.position = p;
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
            if (lifetime > 60f) Destroy(gameObject);
        }

        /// <summary>Drifts slide along the raft's actual parts (not a circle around it) instead of passing through.</summary>
        Vector3 BumpOffParts(Vector3 p)
        {
            float half = Raft.CellSize * 0.5f + 0.25f * transform.localScale.x;
            foreach (var c in raft.Grid.Parts.Keys)
            {
                var d = p - raft.CellToWorld(c);
                float ox = half - Mathf.Abs(d.x), oz = half - Mathf.Abs(d.z);
                if (ox <= 0f || oz <= 0f) continue;
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

        void OnGUI()
        {
            if (Hooked || WorldPause.Active || Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);
            if (sp.z < 0) return;
            var rect = new Rect(sp.x - 70, Screen.height - sp.y - 22, 140, 22);
            GUI.Label(rect, $"{Monster.displayName}\n<i>{HintText(Hint)}</i>", HuntStyles.WorldLabel);
        }
    }
}
