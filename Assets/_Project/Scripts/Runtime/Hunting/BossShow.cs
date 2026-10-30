using System.Collections.Generic;
using BrineBattle.Battle;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// The show around a boss fight (Ed's ask: grand from start to loot). Entrance: the screen dims, the sea
    /// rumbles, a red BOSS banner with its name and level, then it rises big with a splash and an aura in its
    /// element colour. Fight: a boss HP bar across the top and an ENRAGED flash. Defeat: a slow-motion last
    /// hit, a treasure burst onto the raft and a BOSS DEFEATED card. Placeholder primitives until Phase 9.
    /// Added to the battle object by BattleController.
    /// </summary>
    public class BossShow : MonoBehaviour
    {
        public const float IntroSeconds = 2.8f;

        Drift boss;
        string title;
        Color aura;
        Vector3 baseScale;
        GameObject auraDisc, splash;
        float introAt = -99f, shake, enragedAt = -99f, slowUntil, cardUntil;
        bool wasEnraged, finished;
        readonly List<(Transform tf, Vector3 from, Vector3 to, float at)> treasure = new List<(Transform, Vector3, Vector3, float)>();
        string cardTitle;
        List<string> cardLines;
        GUIStyle banner, small;

        public static BossShow Instance { get; private set; }
        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; Time.timeScale = 1f; }

        float Now => Time.unscaledTime;
        /// <summary>True while the entrance plays; the battle waits for it.</summary>
        public bool Busy => boss != null && Now - introAt < IntroSeconds;

        public void Intro(Drift drift)
        {
            Clear();
            boss = drift;
            var m = drift.Monster;
            title = $"{m.displayName}  ·  Lv {(int)m.rarity + 1} boss";
            aura = Tint.Of(m.element);
            baseScale = drift.transform.localScale;
            drift.transform.localScale = baseScale * 0.2f;
            introAt = Now;
            shake = 0.6f;
            wasEnraged = finished = false;
            auraDisc = Disc(drift.transform, "Boss aura", aura * 0.9f, 2.2f);
        }

        /// <summary>The treasure burst and the BOSS DEFEATED card (called by HookController with what dropped).</summary>
        public void Victory(string bossName, List<string> drops)
        {
            cardTitle = $"BOSS DEFEATED — {bossName}";
            cardLines = drops;
            cardUntil = Now + 7f;
            var raft = FindFirstObjectByType<Raft>();
            var from = boss != null ? boss.transform.position : raft != null ? raft.transform.position + Vector3.right * 3f : Vector3.zero;
            for (int i = 0; i < Mathf.Max(4, drops.Count * 2); i++)
            {
                var go = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                go.name = "Treasure";
                Destroy(go.GetComponent<Collider>());
                go.transform.position = from;
                go.transform.localScale = Vector3.one * 0.22f;
                Tint.Apply(go.GetComponent<Renderer>(), i % 3 == 0 ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.82f, 0.25f));
                var land = (raft != null ? raft.transform.position : Vector3.zero) + new Vector3(Random.Range(-1.2f, 1.2f), 0.4f, Random.Range(-1.2f, 1.2f));
                treasure.Add((go.transform, from, land, Now + i * 0.08f));
            }
            Clear(keepCard: true);
        }

        /// <summary>Ends the show without a victory (the hunter lost).</summary>
        public void Stop() => Clear();

        void Clear(bool keepCard = false)
        {
            if (boss != null) boss.transform.localScale = baseScale;
            if (auraDisc != null) Destroy(auraDisc);
            if (splash != null) Destroy(splash);
            boss = null;
            if (!keepCard) cardUntil = 0f;
        }

        static GameObject Disc(Transform parent, string name, Color c, float size)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = name;
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(parent, false);
            d.transform.localPosition = Vector3.down * 0.35f;
            d.transform.localScale = new Vector3(size, 0.02f, size);
            Tint.Apply(d.GetComponent<Renderer>(), c);
            return d;
        }

        void Update()
        {
            var state = BattleController.Current;
            if (boss != null)
            {
                float t = Now - introAt;
                // Rise: small and deep → big, with a splash ring, after the warning (1.0-1.8 s).
                float rise = Mathf.Clamp01((t - 1.0f) / 0.8f);
                boss.transform.localScale = baseScale * Mathf.Lerp(0.2f, 1.6f, 1f - (1f - rise) * (1f - rise));
                if (rise > 0f && splash == null && t < 2f)
                {
                    splash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    splash.name = "Boss splash";
                    Destroy(splash.GetComponent<Collider>());
                    Tint.Apply(splash.GetComponent<Renderer>(), new Color(0.85f, 0.95f, 1f));
                    shake = 1f;
                }
                if (splash != null)
                {
                    float s = Mathf.Clamp01((t - 1.0f) / 1.2f);
                    splash.transform.position = boss.transform.position + Vector3.down * 0.3f;
                    splash.transform.localScale = new Vector3(1f + s * 5f, 0.03f, 1f + s * 5f);
                    if (s >= 1f) { Destroy(splash); splash = null; }
                }
                if (auraDisc != null)
                {
                    float pulse = 1f + Mathf.Sin(Now * (wasEnraged ? 9f : 4f)) * 0.08f;
                    float size = (wasEnraged ? 2.8f : 2.2f) * pulse;
                    auraDisc.transform.localScale = new Vector3(size, 0.02f, size);
                }
                if (state != null && state.Enraged && !wasEnraged)
                {
                    wasEnraged = true;
                    enragedAt = Now;
                    shake = 1f;
                    if (auraDisc != null) Tint.Apply(auraDisc.GetComponent<Renderer>(), Color.Lerp(aura, new Color(1f, 0.15f, 0.1f), 0.6f));
                }
                // Defeat: slow motion on the last hit.
                if (state != null && state.Outcome == BattleOutcome.Won && !finished)
                {
                    finished = true;
                    slowUntil = Now + 1.1f;
                    shake = 0.8f;
                }
            }
            if (Now < slowUntil) Time.timeScale = 0.35f;
            else if (slowUntil > 0f) { Time.timeScale = 1f; slowUntil = 0f; }

            for (int i = treasure.Count - 1; i >= 0; i--)
            {
                var (tf, from, to, at) = treasure[i];
                if (tf == null) { treasure.RemoveAt(i); continue; }
                float k = Mathf.Clamp01((Now - at) / 1.2f);
                var p = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 3f;
                tf.position = p;
                tf.Rotate(0f, 400f * Time.unscaledDeltaTime, 200f * Time.unscaledDeltaTime);
                if (Now - at > 2.2f) { Destroy(tf.gameObject); treasure.RemoveAt(i); }
            }
        }

        void LateUpdate()
        {
            if (shake <= 0f) return;
            var cam = Camera.main;
            if (cam != null) cam.transform.position += Random.insideUnitSphere * shake * 0.18f;
            shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 1.5f);
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            GUI.depth = -8;
            banner ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold, richText = true };
            small ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, richText = true, wordWrap = true };
            var state = BattleController.Current;
            float t = Now - introAt;

            if (boss != null && t < IntroSeconds)
            {
                // Dim and warn.
                float dim = Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((IntroSeconds - t) / 0.5f);
                GUI.DrawTexture(new Rect(0, 0, HuntStyles.W, HuntStyles.H), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0.05f, 0.55f * dim), 0, 0);
                var r = new Rect(0, HuntStyles.H * 0.28f, HuntStyles.W, 110);
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.55f, 0.02f, 0.02f, 0.85f * dim), 0, 0);
                bool blink = Mathf.Repeat(t * 3f, 1f) < 0.7f;
                GUI.Label(new Rect(r.x, r.y + 6, r.width, 54), blink ? "<color=#ffdddd>⚠  BOSS  ⚠</color>" : "", banner);
                GUI.Label(new Rect(r.x, r.y + 62, r.width, 36), $"<color=#ffe08a>{title}</color>", small);
            }

            if (boss != null && state != null && t >= IntroSeconds * 0.8f)
            {
                // Boss HP bar across the top.
                float w = Mathf.Min(720, HuntStyles.W - 120);
                var r = new Rect(HuntStyles.W / 2f - w / 2f, 14, w, 22);
                GUI.Label(new Rect(r.x, r.y - 2, r.width, 20), $"<color=#ffe08a>{title}</color>{(state.Enraged ? "  <color=#ff5a4a>ENRAGED</color>" : "")}", small);
                r.y += 20;
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.7f), 0, 4);
                float fill = Mathf.Clamp01(state.Monster.Hp / (float)Mathf.Max(1, state.Monster.MaxHp));
                var col = state.Enraged ? new Color(1f, 0.25f, 0.15f) : new Color(0.85f, 0.2f, 0.35f);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * fill, r.height - 4), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, col, 0, 3);
                GUI.DrawTexture(new Rect(r.x + r.width * BossFightRules.EnrageAt, r.y, 2, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 1f, 1f, 0.6f), 0, 0);
                GUI.Label(new Rect(r.x, r.y + 1, r.width, r.height), $"{state.Monster.Hp}/{state.Monster.MaxHp}", small);
            }

            if (Now - enragedAt < 1.6f)
                GUI.Label(new Rect(0, HuntStyles.H * 0.3f, HuntStyles.W, 60), "<color=#ff4a3a>ENRAGED!</color>", banner);

            if (Now < cardUntil && cardLines != null)
            {
                float w = 440, h = 120 + 24 * cardLines.Count;
                var r = new Rect(HuntStyles.W / 2f - w / 2f, HuntStyles.H * 0.22f, w, h);
                GUILayout.BeginArea(r, HuntStyles.Window);
                GUILayout.Label($"<color=#ffd34a>{cardTitle}</color>", new GUIStyle(small) { fontSize = 20 });
                GUILayout.Space(6);
                foreach (var l in cardLines) GUILayout.Label($"✦ {l}", HuntStyles.Text);
                GUILayout.FlexibleSpace();
                if (HuntStyles.ColorButton("Collect", HuntStyles.Go)) cardUntil = 0f;
                GUILayout.EndArea();
            }
        }
    }
}
