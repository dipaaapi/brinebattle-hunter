using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Arrows at the screen edge for what matters off-screen (Claude's C-23): a boss, a gorged monster
    /// and your lost raft parts floating away. Added at runtime by the HookController.
    /// </summary>
    public class EdgeArrows : MonoBehaviour
    {
        GUIStyle style;
        float scan;
        Drift[] drifts = new Drift[0];

        void OnGUI()
        {
            HuntStyles.Begin();
            var cam = Camera.main;
            if (cam == null || WorldPause.Active || BattleController.InBattle) return;
            // Look for drifts a few times a second; drawing every frame.
            if (Time.unscaledTime >= scan) { scan = Time.unscaledTime + 0.3f; drifts = FindObjectsByType<Drift>(FindObjectsSortMode.None); }
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold, richText = true };
            foreach (var d in drifts)
            {
                if (d == null || d.Hooked) continue;
                string what; Color color;
                if (d.Monster.isBoss) { what = "BOSS"; color = new Color(1f, 0.3f, 0.3f); }
                else if (d.GorgedParts > 0) { what = "Gorged"; color = new Color(1f, 0.7f, 0.4f); }
                else if (d.Carried != null) { what = "Your part"; color = new Color(1f, 0.85f, 0.4f); }
                else if (d.Survivor) { what = "Survivor"; color = new Color(1f, 0.6f, 0.2f); }
                else continue;
                var vp = cam.WorldToViewportPoint(d.transform.position);
                bool behind = vp.z < 0f;
                if (!behind && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f) continue;   // on screen: its own label shows
                var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (behind) dir = -dir;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                // Push the point out to the border of a box inset from the screen edge.
                float k = 0.44f / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                var at = new Vector2(0.5f + dir.x * k, 0.5f + dir.y * k);
                float x = at.x * HuntStyles.W, y = (1f - at.y) * HuntStyles.H;
                float angle = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
                var prev = GUI.matrix;
                var prevColor = GUI.color;
                GUI.color = color;
                GUIUtility.RotateAroundPivot(angle, new Vector2(x, y));
                GUI.Label(new Rect(x - 14, y - 14, 28, 28), "<size=28><b>></b></size>", style);
                GUI.matrix = prev;
                GUI.Label(new Rect(x - 50, y + 12, 100, 18), what, style);
                GUI.color = prevColor;
            }
        }
    }
}
