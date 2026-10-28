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

        /// <summary>After a successful sedate (GDD §4).</summary>
        public void MakeWeakened() => Actual = DriftState.Weakened;

        void Update()
        {
            if (Hooked) return;
            lifetime += Time.deltaTime;
            var p = transform.position + velocity * Time.deltaTime;
            p.y = 0.1f + Mathf.Sin((Time.time + bobPhase) * 2f) * 0.08f;
            transform.position = p;
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity);
            if (lifetime > 60f) Destroy(gameObject);
        }

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
            if (Hooked || Camera.main == null) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up);
            if (sp.z < 0) return;
            var rect = new Rect(sp.x - 70, Screen.height - sp.y - 22, 140, 22);
            GUI.Label(rect, $"{Monster.displayName}\n<i>{HintText(Hint)}</i>", HuntStyles.WorldLabel);
        }
    }
}
