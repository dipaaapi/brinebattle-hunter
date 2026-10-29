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

        /// <summary>
        /// A big picture button for the HUD dock (owner idea U-25): icon, word and key. Lit when its menu is open.
        /// </summary>
        public static bool Tile(Rect r, Texture2D icon, string caption, string key, bool lit)
        {
            var bg = lit ? new Color(0.98f, 0.82f, 0.3f) : new Color(0.12f, 0.16f, 0.22f, 0.95f);
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover && !lit) bg = new Color(0.2f, 0.27f, 0.36f, 0.98f);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, bg, 0, 10);
            float s = r.height * 0.5f;
            GUI.DrawTexture(new Rect(r.center.x - s / 2f, r.y + 6, s, s), icon, ScaleMode.ScaleToFit);
            var text = lit ? DarkCaption : Caption;
            GUI.Label(new Rect(r.x, r.y + s + 6, r.width, 18), caption, text);
            GUI.Label(new Rect(r.x, r.y + s + 21, r.width, 14), key, lit ? DarkKey : Key);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>A round × in a panel's top-right corner (inside its area). True when clicked.</summary>
        public static bool CloseX(float areaWidth)
        {
            var r = new Rect(areaWidth - 34, 4, 28, 24);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.8f, 0.3f, 0.3f), 0, 8);
            GUI.Label(r, "×", DarkCaptionBig);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>A coloured button so the main choice stands out (green go, red risky, blue other).</summary>
        public static bool ColorButton(string text, Color color, params GUILayoutOption[] options)
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool hit = GUILayout.Button(text, BigButton, options);
            GUI.backgroundColor = old;
            return hit;
        }

        public static readonly Color Go = new Color(0.35f, 0.85f, 0.45f), Risky = new Color(1f, 0.45f, 0.4f), Other = new Color(0.45f, 0.65f, 1f);

        /// <summary>A panel's section tabs (U-25: one short page at a time instead of one long scroll).</summary>
        public static int Tabs(int current, string[] labels) => GUILayout.Toolbar(current, labels, TabStyle, GUILayout.Height(32));

        /// <summary>"have/need", green when there is enough, red when not.</summary>
        public static string Count(int have, int need) => $"<color={(have >= need ? GoodHex : BadHex)}>{have}/{need}</color>";

        public const string GoodHex = "#7CFC9A", BadHex = "#ff8a80";

        /// <summary>A thin progress bar in a layout row.</summary>
        public static void Bar(float fill, Color color, float height = 10f)
        {
            var r = GUILayoutUtility.GetRect(40, height, GUILayout.ExpandWidth(true));
            r.y += 2;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.05f, 0.06f, 0.08f), 0, height / 2f);
            if (fill > 0f)
                GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, height / 2f);
        }

        static GUIStyle tabStyle, text, hint, heading, richButton, card;
        static Texture2D cardBg;
        public static GUIStyle TabStyle => tabStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
        /// <summary>Normal panel text: rich and wrapping.</summary>
        public static GUIStyle Text => text ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 13 };
        /// <summary>Small grey help text under a heading.</summary>
        public static GUIStyle Hint
        {
            get
            {
                if (hint == null)
                {
                    hint = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 };
                    hint.normal.textColor = new Color(0.68f, 0.73f, 0.8f);
                }
                return hint;
            }
        }
        public static GUIStyle Heading => heading ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 15, fontStyle = FontStyle.Bold };
        public static GUIStyle RichButton => richButton ??= new GUIStyle(GUI.skin.button) { richText = true, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 4, 4) };
        /// <summary>A lighter box inside a panel: one recipe, one pet, one item group.</summary>
        public static GUIStyle Card
        {
            get
            {
                if (card == null || cardBg == null)
                {
                    cardBg = Solid(new Color(0.17f, 0.2f, 0.26f, 1f));
                    card = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6), margin = new RectOffset(2, 2, 3, 3), richText = true };
                    card.normal.background = cardBg;
                }
                return card;
            }
        }

        static GUIStyle caption, darkCaption, key, darkKey, darkCaptionBig, bigButton;
        static GUIStyle Caption => caption ??= Make(14, FontStyle.Bold, Color.white);
        static GUIStyle DarkCaption => darkCaption ??= Make(14, FontStyle.Bold, new Color(0.1f, 0.1f, 0.12f));
        static GUIStyle Key => key ??= Make(10, FontStyle.Normal, new Color(0.7f, 0.75f, 0.8f));
        static GUIStyle DarkKey => darkKey ??= Make(10, FontStyle.Normal, new Color(0.25f, 0.25f, 0.28f));
        static GUIStyle DarkCaptionBig => darkCaptionBig ??= Make(18, FontStyle.Bold, Color.white);
        public static GUIStyle BigButton => bigButton ??= new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true, fixedHeight = 42 };

        static GUIStyle Make(int size, FontStyle style, Color color)
        {
            var g = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = false };
            g.normal.textColor = color;
            return g;
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
