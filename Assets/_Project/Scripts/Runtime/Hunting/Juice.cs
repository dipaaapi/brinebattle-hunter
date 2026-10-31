using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Small feel-good effects (Claude's C-21): a splash where a catch lands, "+2 Shellback" pop-ups that rise
    /// from it, and a short camera shake on a clean reel. Visual only; the sounds come with the audio pass
    /// (Phase 9). Created on first use.
    /// </summary>
    [DefaultExecutionOrder(1000)]   // after the camera rig has placed the camera this frame
    public class Juice : MonoBehaviour
    {
        static Juice instance;
        static Juice I
        {
            get
            {
                if (instance == null) instance = new GameObject("Juice").AddComponent<Juice>();
                return instance;
            }
        }

        readonly List<(string text, Vector3 at, Color color, float born)> pops = new List<(string, Vector3, Color, float)>();
        float shake;
        GUIStyle style;

        /// <summary>A white ring that spreads on the water.</summary>
        public static void Splash(Vector3 at, float size = 1f) => I.StartCoroutine(I.Ring(at, size));

        /// <summary>Text that floats up from a world point and fades.</summary>
        public static void Pop(string text, Vector3 at, Color color) => I.pops.Add((text, at, color, Time.unscaledTime));

        public static void Shake(float amount) => I.shake = Mathf.Max(I.shake, amount);

        System.Collections.IEnumerator Ring(Vector3 at, float size)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Splash";
            Destroy(ring.GetComponent<Collider>());
            Tint.Apply(ring.GetComponent<Renderer>(), new Color(0.88f, 0.96f, 1f));
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                ring.transform.position = new Vector3(at.x, Waves.Height(at.x, at.z) + 0.05f, at.z);
                float s = Mathf.Lerp(0.3f, 1.6f, k) * size;
                ring.transform.localScale = new Vector3(s, 0.02f * (1f - k) + 0.002f, s);
                yield return null;
            }
            Destroy(ring);
        }

        void LateUpdate()
        {
            if (shake <= 0f) return;
            var cam = Camera.main;
            if (cam != null) cam.transform.position += Random.insideUnitSphere * shake * 0.12f;
            shake = Mathf.Max(0f, shake - Time.unscaledDeltaTime * 2.5f);
        }

        void OnGUI()
        {
            if (pops.Count == 0 || Camera.main == null) return;
            HuntStyles.Begin();
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold, richText = true };
            const float life = 1.4f;
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                float age = Time.unscaledTime - p.born;
                if (age > life) { pops.RemoveAt(i); continue; }
                var sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(p.at + Vector3.up * (0.6f + age * 1.2f)));
                if (sp.z < 0f) continue;
                var c = p.color;
                c.a = 1f - Mathf.Clamp01((age - life * 0.6f) / (life * 0.4f));
                var prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, c.a * 0.7f);
                GUI.Label(new Rect(sp.x - 99, HuntStyles.H - sp.y - 11 - i * 2, 200, 24), p.text, style);
                GUI.color = c;
                GUI.Label(new Rect(sp.x - 100, HuntStyles.H - sp.y - 12 - i * 2, 200, 24), p.text, style);
                GUI.color = prev;
            }
        }
    }
}
