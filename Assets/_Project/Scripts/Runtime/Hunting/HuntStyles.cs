using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Shared IMGUI styles for the placeholder UI. Panels are opaque so they never show through each other.</summary>
    public static class HuntStyles
    {
        static GUIStyle worldLabel, window, box;
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
