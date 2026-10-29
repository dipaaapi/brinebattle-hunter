using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Shared IMGUI styles for the placeholder UI. Panels are opaque so they never show through each other.</summary>
    public static class HuntStyles
    {
        static GUIStyle worldLabel, window, box;

        /// <summary>UI size (owner idea U-25: easy to read and tap). 1 = small, 1.25 = big (default), 1.5 = huge.</summary>
        public static float Scale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat("brinebattle.uiScale", 1.25f), 1f, 1.75f);
            set => PlayerPrefs.SetFloat("brinebattle.uiScale", Mathf.Clamp(value, 1f, 1.75f));
        }

        /// <summary>Screen size in UI units (what panels lay out in once <see cref="Begin"/> scales the UI).</summary>
        public static float W => Screen.width / Scale;
        public static float H => Screen.height / Scale;

        /// <summary>Call first in every OnGUI: draws everything at the chosen UI size.</summary>
        public static void Begin() => GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));

        /// <summary>A WorldToScreenPoint result in UI units; use with <see cref="H"/> - y as before.</summary>
        public static Vector3 Ui(Vector3 screenPoint) => new Vector3(screenPoint.x / Scale, screenPoint.y / Scale, screenPoint.z);
        static Texture2D windowBg, boxBg;

        public static GUIStyle WorldLabel
        {
            get
            {
                if (worldLabel == null)
                {
                    worldLabel = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.LowerCenter,
                        richText = true,
                        fontSize = 12,
                    };
                    worldLabel.normal.textColor = Color.white;
                }
                return worldLabel;
            }
        }

        /// <summary>Opaque window with a title bar.</summary>
        public static GUIStyle Window
        {
            get
            {
                if (window == null || windowBg == null)
                {
                    windowBg = Solid(new Color(0.11f, 0.13f, 0.17f, 0.98f));
                    window = new GUIStyle(GUI.skin.window) { richText = true, fontStyle = FontStyle.Bold };
                    window.normal.background = window.onNormal.background = windowBg;
                    window.normal.textColor = window.onNormal.textColor = new Color(0.9f, 0.93f, 1f);
                }
                return window;
            }
        }

        /// <summary>Opaque box for the HUD.</summary>
        public static GUIStyle Box
        {
            get
            {
                if (box == null || boxBg == null)
                {
                    boxBg = Solid(new Color(0.09f, 0.11f, 0.15f, 0.92f));
                    box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
                    box.normal.background = boxBg;
                }
                return box;
            }
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
