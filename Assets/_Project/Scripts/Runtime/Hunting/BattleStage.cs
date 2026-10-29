using System.Collections;
using System.Collections.Generic;
using BrineBattle.Battle;
using BrineBattle.Elements;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Plays a battle's <see cref="BattleEvent"/>s on screen with placeholder shapes: attackers
    /// lunge, harpoons and skills fly as projectiles, targets flash and shake, damage numbers pop
    /// up, knocked-out fighters sink. Idea U-19 (first pass). Added at runtime by BattleController.
    /// </summary>
    public class BattleStage : MonoBehaviour
    {
        const float LungeTime = 0.32f;
        const float ShotTime = 0.35f;
        const float Gap = 0.12f;

        class Actor
        {
            public Transform Tf;
            public Renderer Renderer;
            public Color Color;
            public Vector3 Home;     // world position the actor rests at
            public Vector3 Offset;   // lunge
            public float Shake;
            public float Flash;
            public Color FlashColor;
            public float Sink;       // 0..1 when knocked out
            public bool Bob;         // sky monsters hover
        }

        class Popup
        {
            public Vector3 World;
            public string Text;
            public Color Color;
            public float Age;
            public float Size;
        }

        readonly Dictionary<BattleActor, Actor> actors = new Dictionary<BattleActor, Actor>();
        readonly Queue<BattleEvent> queue = new Queue<BattleEvent>();
        readonly List<Popup> popups = new List<Popup>();
        readonly List<GameObject> spawned = new List<GameObject>();
        Coroutine runner;
        GUIStyle popupStyle;

        /// <summary>True while events are still animating; the controller waits before the next round.</summary>
        public bool Busy => queue.Count > 0 || runner != null;

        public void Begin(Raft raft, Drift drift, Combatant companion)
        {
            Clear();
            var hunterTf = raft.transform.Find("Hunter");
            if (hunterTf != null) Add(BattleActor.Hunter, hunterTf, false);
            Add(BattleActor.Raft, raft.transform, false);
            Add(BattleActor.Monster, drift.transform, drift.Monster.habitat == Monsters.Habitat.Sky);
            if (drift.Monster.habitat == Monsters.Habitat.Sky) actors[BattleActor.Monster].Home += Vector3.up * 2f;

            if (companion != null && hunterTf != null)
            {
                // A small stand-in beside the hunter, coloured by the companion's element.
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Companion ({companion.Name})";
                Destroy(go.GetComponent<Collider>());
                var toMonster = drift.transform.position - hunterTf.position;
                toMonster.y = 0f;
                var side = Vector3.Cross(Vector3.up, toMonster.normalized);
                go.transform.position = hunterTf.position + side * 0.9f + toMonster.normalized * 0.4f - Vector3.up * 0.3f;
                go.transform.localScale = Vector3.one * 0.45f;
                Tint.Apply(go.GetComponent<Renderer>(), Tint.Of(companion.Element));
                spawned.Add(go);
                Add(BattleActor.Companion, go.transform, false, Tint.Of(companion.Element));
            }
        }

        /// <summary>Queues events from the battle state to play in order.</summary>
        public void Play(IEnumerable<BattleEvent> events)
        {
            foreach (var e in events) queue.Enqueue(e);
            if (runner == null && queue.Count > 0) runner = StartCoroutine(Run());
        }

        /// <summary>Puts everyone back where they were and removes stand-ins.</summary>
        public void Clear()
        {
            if (runner != null) StopCoroutine(runner);
            runner = null;
            queue.Clear();
            popups.Clear();
            foreach (var a in actors.Values)
            {
                if (a.Tf == null) continue;
                a.Tf.position = a.Home;
                if (a.Renderer != null) Tint.Apply(a.Renderer, a.Color);
            }
            actors.Clear();
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
        }

        void Add(BattleActor id, Transform tf, bool bob, Color? color = null)
        {
            var r = tf.GetComponent<Renderer>();
            actors[id] = new Actor
            {
                Tf = tf,
                Renderer = r,
                Color = color ?? (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white),
                Home = tf.position,
                Bob = bob,
            };
            // Drifts already carry an element tint in a property block; keep that colour.
            if (id == BattleActor.Monster && tf.TryGetComponent<Drift>(out var d)) actors[id].Color = Tint.Of(d.Monster.element);
        }

        IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                var e = queue.Dequeue();
                yield return Animate(e);
                yield return new WaitForSeconds(Gap);
            }
            runner = null;
        }

        IEnumerator Animate(BattleEvent e)
        {
            actors.TryGetValue(e.From, out var from);
            actors.TryGetValue(e.To, out var to);
            switch (e.Kind)
            {
                case BattleEventKind.Strike:
                    if (from != null && to != null) yield return Lunge(from, to, () => Impact(to, e));
                    else Impact(to, e);
                    break;
                case BattleEventKind.Shot:
                case BattleEventKind.BigShot:
                    if (from != null && to != null)
                        yield return Projectile(from, to, e.Kind == BattleEventKind.BigShot ? 0.7f : 0.3f, e.Kind == BattleEventKind.BigShot ? Tint.Of(Element.Water) : ShotColor(e.Element));
                    Impact(to, e);
                    break;
                case BattleEventKind.Recoil:
                    Impact(to, e);
                    Pop(to, "Thorns", new Color(1f, 0.6f, 0.2f), 18, 0.6f);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventKind.Heal:
                    if (to != null) { to.Flash = 1f; to.FlashColor = new Color(0.4f, 1f, 0.5f); }
                    Pop(to, $"+{e.Amount}", new Color(0.4f, 1f, 0.5f), 28);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case BattleEventKind.Blocked:
                    System.Action blocked = () =>
                    {
                        if (to != null) { to.Flash = 1f; to.FlashColor = new Color(0.6f, 0.8f, 1f); }
                        Pop(to, "Blocked!", new Color(0.6f, 0.8f, 1f), 24);
                    };
                    if (from != null && to != null) yield return Lunge(from, to, blocked);
                    else blocked();
                    break;
                case BattleEventKind.Buff:
                    if (to != null) { to.Flash = 1f; to.FlashColor = e.Element != Element.None ? Tint.Of(e.Element) : new Color(0.6f, 0.8f, 1f); }
                    Pop(to, e.Text, to != null ? to.FlashColor : Color.white, 22);
                    yield return new WaitForSeconds(0.35f);
                    break;
                case BattleEventKind.KnockedOut:
                    Pop(to, e.To == BattleActor.Raft ? "Broken!" : "KO!", new Color(1f, 0.35f, 0.3f), 30);
                    for (float t = 0; t < 0.7f; t += Time.deltaTime)
                    {
                        if (to != null) to.Sink = t / 0.7f;
                        yield return null;
                    }
                    break;
            }
        }

        static Color ShotColor(Element e) => e == Element.None ? new Color(0.85f, 0.85f, 0.8f) : Tint.Of(e);

        /// <summary>The attacker darts at the target and back; <paramref name="onHit"/> runs at the moment of contact.</summary>
        IEnumerator Lunge(Actor from, Actor to, System.Action onHit)
        {
            bool hit = false;
            var dir = to.Home - from.Home;
            float reach = Mathf.Min(dir.magnitude * 0.6f, Mathf.Max(0f, dir.magnitude - 0.8f));
            dir = dir.normalized * reach;
            for (float t = 0; t < LungeTime; t += Time.deltaTime)
            {
                float k = t / LungeTime;
                // Out fast, back slower.
                from.Offset = dir * (k < 0.4f ? Mathf.SmoothStep(0, 1, k / 0.4f) : 1f - Mathf.SmoothStep(0, 1, (k - 0.4f) / 0.6f));
                if (k >= 0.4f && !hit) { hit = true; onHit?.Invoke(); }
                yield return null;
            }
            from.Offset = Vector3.zero;
            if (!hit) onHit?.Invoke();
        }

        IEnumerator Projectile(Actor from, Actor to, float size, Color color)
        {
            var go = GameObject.CreatePrimitive(size > 0.5f ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            go.name = "Projectile";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = size > 0.5f ? Vector3.one * size : new Vector3(0.12f, 0.45f, 0.12f);
            Tint.Apply(go.GetComponent<Renderer>(), color);
            spawned.Add(go);
            var a = Position(from) + Vector3.up * 0.3f;
            var b = Position(to);
            for (float t = 0; t < ShotTime; t += Time.deltaTime)
            {
                float k = t / ShotTime;
                var p = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                var next = Vector3.Lerp(a, b, Mathf.Min(1f, k + 0.05f)) + Vector3.up * Mathf.Sin(Mathf.Min(1f, k + 0.05f) * Mathf.PI) * 0.8f;
                go.transform.position = p;
                if (size <= 0.5f && next != p) go.transform.up = (next - p).normalized;
                yield return null;
            }
            spawned.Remove(go);
            Destroy(go);
        }

        void Impact(Actor to, BattleEvent e)
        {
            if (to == null) return;
            to.Shake = 1f;
            to.Flash = 1f;
            to.FlashColor = e.To == BattleActor.Monster ? Color.white : new Color(1f, 0.35f, 0.3f);
            if (e.Amount > 0) Pop(to, e.Amount.ToString(), e.To == BattleActor.Monster ? new Color(1f, 0.95f, 0.5f) : new Color(1f, 0.45f, 0.4f), e.Kind == BattleEventKind.BigShot ? 36 : 28);
        }

        void Pop(Actor at, string text, Color color, float size, float lift = 1f)
        {
            if (at == null || string.IsNullOrEmpty(text)) return;
            popups.Add(new Popup { World = Position(at) + Vector3.up * lift, Text = text, Color = color, Size = size });
        }

        static Vector3 Position(Actor a) => a.Tf != null ? a.Tf.position : a.Home;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (var a in actors.Values)
            {
                if (a.Tf == null) continue;
                var p = a.Home + a.Offset;
                if (a.Bob) p.y += Mathf.Sin(Time.time * 3f) * 0.3f;
                if (a.Shake > 0f)
                {
                    p += Random.insideUnitSphere * a.Shake * 0.25f;
                    a.Shake = Mathf.Max(0f, a.Shake - dt * 3f);
                }
                p.y -= a.Sink * 1.2f;
                // The raft only shakes; moving the whole raft far would drag the camera's subject.
                a.Tf.position = p;
                if (a.Renderer != null)
                {
                    a.Flash = Mathf.Max(0f, a.Flash - dt * 4f);
                    Tint.Apply(a.Renderer, Color.Lerp(a.Color, a.FlashColor, a.Flash));
                }
            }
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                popups[i].Age += dt;
                if (popups[i].Age > 1.1f) popups.RemoveAt(i);
            }
        }

        void OnGUI()
        {
            if (popups.Count == 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            popupStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.depth = -6;
            foreach (var p in popups)
            {
                var sp = cam.WorldToScreenPoint(p.World + Vector3.up * p.Age * 0.8f);
                if (sp.z < 0f) continue;
                float alpha = Mathf.Clamp01(1.4f - p.Age * 1.3f);
                popupStyle.fontSize = Mathf.RoundToInt(p.Size * (p.Age < 0.12f ? 1f + (0.12f - p.Age) * 4f : 1f));
                var r = new Rect(sp.x - 80, Screen.height - sp.y - 20, 160, 40);
                popupStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), p.Text, popupStyle);
                popupStyle.normal.textColor = new Color(p.Color.r, p.Color.g, p.Color.b, alpha);
                GUI.Label(r, p.Text, popupStyle);
            }
        }
    }
}
