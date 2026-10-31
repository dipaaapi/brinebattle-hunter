using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Shared IMGUI look for the placeholder UI: the "deep sea" theme (Ed's ask with U-39: a new HUD and UI
    /// everywhere). <see cref="Begin"/> puts a themed skin on every panel, so plain buttons, toggles, tabs,
    /// sliders and scroll bars all match: navy panels with a teal title bar and rim, gold for what's pressed.
    /// Panels are opaque so they never show through each other. Real art replaces this in Phase 9.
    /// </summary>
    public static class HuntStyles
    {
        // Palette.
        public static readonly Color Ink = new Color(0.91f, 0.95f, 1f), Muted = new Color(0.64f, 0.72f, 0.82f),
            Navy = new Color(0.07f, 0.11f, 0.17f), Panel = new Color(0.09f, 0.15f, 0.22f), Raised = new Color(0.13f, 0.21f, 0.3f),
            Teal = new Color(0.25f, 0.82f, 0.79f), TealDeep = new Color(0.09f, 0.3f, 0.38f), Gold = new Color(1f, 0.81f, 0.29f);

        static GUIStyle worldLabel, window, box;
        static GUISkin skin;

        /// <summary>UI size (owner idea U-25: easy to read and tap). 1 = small, 1.25 = big (default), 1.5 = huge.</summary>
        public static float Scale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat("brinebattle.uiScale", 1.25f), 1f, 1.75f);
            set => PlayerPrefs.SetFloat("brinebattle.uiScale", Mathf.Clamp(value, 1f, 1.75f));
        }

        /// <summary>Screen size in UI units (what panels lay out in once <see cref="Begin"/> scales the UI).</summary>
        public static float W => Screen.width / Scale;
        public static float H => Screen.height / Scale;

        /// <summary>Call first in every OnGUI: the themed skin, at the chosen UI size.</summary>
        public static void Begin()
        {
            GUI.skin = Skin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        }

        /// <summary>A WorldToScreenPoint result in UI units; use with <see cref="H"/> - y as before.</summary>
        public static Vector3 Ui(Vector3 screenPoint) => new Vector3(screenPoint.x / Scale, screenPoint.y / Scale, screenPoint.z);

        /// <summary>The theme: Unity's default skin with the common controls redrawn in the palette.</summary>
        static GUISkin Skin
        {
            get
            {
                if (skin != null) return skin;
                skin = Object.Instantiate(GUI.skin);
                skin.hideFlags = HideFlags.HideAndDontSave;
                skin.label.normal.textColor = Ink;

                var b = skin.button;
                b.normal.background = b.focused.background = Round(new Color(0.12f, 0.22f, 0.32f), new Color(0.2f, 0.38f, 0.5f));
                b.hover.background = Round(new Color(0.16f, 0.3f, 0.43f), Teal);
                b.active.background = b.onActive.background = Round(Gold, new Color(1f, 0.92f, 0.6f));
                b.onNormal.background = b.onHover.background = Round(new Color(0.15f, 0.45f, 0.5f), Teal);
                b.normal.textColor = b.hover.textColor = b.focused.textColor = b.onNormal.textColor = b.onHover.textColor = Ink;
                b.active.textColor = b.onActive.textColor = Navy;
                b.border = new RectOffset(7, 7, 7, 7);
                b.padding = new RectOffset(10, 10, 5, 5);

                skin.box.normal.background = Round(Panel, new Color(0.18f, 0.3f, 0.42f));
                skin.box.border = new RectOffset(7, 7, 7, 7);
                skin.box.normal.textColor = Ink;

                var t = skin.toggle;
                t.normal.textColor = t.hover.textColor = t.onNormal.textColor = t.onHover.textColor = t.active.textColor = t.onActive.textColor = Ink;

                skin.textField.normal.background = Round(Navy, new Color(0.2f, 0.38f, 0.5f));
                skin.textField.focused.background = Round(Navy, Teal);
                skin.textField.border = new RectOffset(7, 7, 7, 7);
                skin.textField.normal.textColor = skin.textField.focused.textColor = Ink;

                skin.horizontalSlider.normal.background = Round(Navy, new Color(0.2f, 0.38f, 0.5f), 12);
                skin.horizontalSlider.border = new RectOffset(5, 5, 5, 5);
                skin.horizontalSliderThumb.normal.background = Round(Gold, new Color(1f, 0.95f, 0.75f), 12);
                skin.horizontalSliderThumb.hover.background = skin.horizontalSliderThumb.active.background = Round(new Color(1f, 0.9f, 0.5f), Color.white, 12);

                skin.verticalScrollbar.normal.background = Round(Navy, Navy, 12);
                skin.verticalScrollbar.border = new RectOffset(5, 5, 5, 5);
                skin.verticalScrollbar.fixedWidth = 10;
                skin.verticalScrollbarThumb.normal.background = Round(new Color(0.22f, 0.42f, 0.55f), Teal, 12);
                skin.verticalScrollbarThumb.border = new RectOffset(5, 5, 5, 5);
                skin.verticalScrollbarThumb.fixedWidth = 10;
                skin.horizontalScrollbar.normal.background = skin.verticalScrollbar.normal.background;
                skin.horizontalScrollbarThumb.normal.background = skin.verticalScrollbarThumb.normal.background;
                return skin;
            }
        }

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

        /// <summary>Opaque window: a teal title bar and rim around a navy body.</summary>
        public static GUIStyle Window
        {
            get
            {
                if (window == null)
                {
                    window = new GUIStyle(GUI.skin.window) { richText = true, fontStyle = FontStyle.Bold, fontSize = 15, alignment = TextAnchor.UpperLeft };
                    window.normal.background = window.onNormal.background = WindowTex();
                    window.border = new RectOffset(10, 10, 30, 10);
                    window.padding = new RectOffset(12, 12, 36, 10);
                    window.contentOffset = new Vector2(14, 0);
                    window.normal.textColor = window.onNormal.textColor = new Color(0.85f, 1f, 0.98f);
                }
                return window;
            }
        }

        /// <summary>Opaque box for the HUD.</summary>
        public static GUIStyle Box
        {
            get
            {
                if (box == null)
                {
                    box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 8, 8) };
                    box.normal.background = Round(new Color(Panel.r, Panel.g, Panel.b, 0.94f), new Color(0.2f, 0.4f, 0.5f), 18);
                    box.border = new RectOffset(9, 9, 9, 9);
                }
                return box;
            }
        }

        /// <summary>A small section title inside the HUD box or a panel: teal caps with a thin rule.</summary>
        public static void Section(string title)
        {
            GUILayout.Label($"<size=11><b><color=#3fd0c9>{title.ToUpperInvariant()}</color></b></size>", Text);
            var r = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(Teal.r, Teal.g, Teal.b, 0.35f), 0, 0);
            GUILayout.Space(2);
        }

        /// <summary>
        /// A big picture button for the HUD dock (owner idea U-25): icon, word and key. Lit (gold) when its menu is open.
        /// </summary>
        public static bool Tile(Rect r, Texture2D icon, string caption, string key, bool lit)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            var rim = lit ? Gold : hover ? Teal : new Color(0.2f, 0.38f, 0.5f);
            var bg = lit ? new Color(0.98f, 0.82f, 0.3f) : hover ? new Color(0.14f, 0.27f, 0.38f, 0.98f) : new Color(Panel.r, Panel.g, Panel.b, 0.95f);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, rim, 0, 12);
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, bg, 0, 10);
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
            var r = new Rect(areaWidth - 34, 4, 26, 22);
            bool hover = r.Contains(Event.current.mousePosition);
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, hover ? new Color(0.95f, 0.4f, 0.38f) : new Color(0.7f, 0.25f, 0.28f), 0, 11);
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
        public static int Tabs(int current, string[] labels)
        {
            int picked = GUILayout.Toolbar(current, labels, TabStyle, GUILayout.Height(32));
            GUILayout.Space(4);
            return picked;
        }

        /// <summary>One tile in a <see cref="TileGrid"/>.</summary>
        public struct GridTile
        {
            public string Title;
            /// <summary>Small pill in the top-right corner (a count), or null.</summary>
            public string Badge;
            /// <summary>The round swatch in the middle (element or type colour).</summary>
            public Color Swatch;
            /// <summary>Rim colour (rarity).</summary>
            public Color Rim;
            /// <summary>Not found yet: dark swatch and "???".</summary>
            public bool Locked;
        }

        static GUIStyle tileTitle, tileBadge;

        /// <summary>
        /// A grid of rounded tiles, like an app's inventory screen (Ed: the menus looked outdated). Wraps to
        /// <paramref name="width"/>; returns the clicked tile's index, or -1.
        /// </summary>
        public static int TileGrid(System.Collections.Generic.IList<GridTile> tiles, float width, int selected, float size = 92f)
        {
            tileTitle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = 11, richText = true, clipping = TextClipping.Clip };
            tileBadge ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, fontStyle = FontStyle.Bold };
            const float gap = 6f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((width + gap) / (size + gap)));
            int rows = Mathf.CeilToInt(tiles.Count / (float)cols);
            var area = GUILayoutUtility.GetRect(width, rows * (size + gap), GUILayout.Width(width));
            int clicked = -1;
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                var r = new Rect(area.x + (i % cols) * (size + gap), area.y + (i / cols) * (size + gap), size, size);
                bool hover = r.Contains(Event.current.mousePosition);
                var rim = i == selected ? Gold : t.Locked ? new Color(0.2f, 0.28f, 0.36f) : t.Rim;
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, rim, 0, 12);
                GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0,
                    hover ? new Color(0.17f, 0.27f, 0.38f) : Raised, 0, 10);
                float d = size * 0.42f;
                var sw = new Rect(r.center.x - d / 2f, r.y + 10, d, d);
                GUI.DrawTexture(sw, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, t.Locked ? new Color(0.04f, 0.06f, 0.09f) : t.Swatch, 0, d / 2f);
                if (!t.Locked) GUI.DrawTexture(new Rect(sw.x + d * 0.2f, sw.y + d * 0.12f, d * 0.3f, d * 0.18f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 1f, 1f, 0.35f), 0, d * 0.09f);
                GUI.Label(new Rect(r.x + 4, sw.yMax + 4, r.width - 8, r.yMax - sw.yMax - 6), t.Locked ? "???" : t.Title, tileTitle);
                if (!string.IsNullOrEmpty(t.Badge))
                {
                    var b = new Rect(r.xMax - 34, r.y + 4, 30, 16);
                    GUI.DrawTexture(b, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.03f, 0.06f, 0.1f, 0.9f), 0, 8);
                    GUI.Label(b, t.Badge, tileBadge);
                }
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) clicked = i;
            }
            return clicked;
        }

        /// <summary>A rarity's colour as a Color (the tile rim).</summary>
        public static Color RarityTint(Items.Rarity r)
        {
            ColorUtility.TryParseHtmlString(BagPanel.RarityColor(r), out var c);
            return c;
        }

        /// <summary>A small rounded label chip, e.g. "HP 20".</summary>
        public static void Chip(string text, Color color)
        {
            var content = new GUIContent(text);
            var style = Hint;
            var size = style.CalcSize(content);
            var r = GUILayoutUtility.GetRect(size.x + 14, 20, GUILayout.Width(size.x + 14));
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, 9);
            var old = style.normal.textColor;
            style.normal.textColor = Ink;
            GUI.Label(new Rect(r.x + 7, r.y + 2, size.x + 2, 18), content, style);
            style.normal.textColor = old;
        }

        /// <summary>"have/need", green when there is enough, red when not.</summary>
        public static string Count(int have, int need) => $"<color={(have >= need ? GoodHex : BadHex)}>{have}/{need}</color>";

        public const string GoodHex = "#7CFC9A", BadHex = "#ff8a80";

        /// <summary>A thin progress bar in a layout row, with a soft highlight on top.</summary>
        public static void Bar(float fill, Color color, float height = 10f)
        {
            var r = GUILayoutUtility.GetRect(40, height, GUILayout.ExpandWidth(true));
            r.y += 2;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.03f, 0.05f, 0.08f), 0, height / 2f);
            if (fill <= 0f) return;
            var f = new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height);
            GUI.DrawTexture(f, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, color, 0, height / 2f);
            GUI.DrawTexture(new Rect(f.x + 2, f.y + 1, Mathf.Max(0f, f.width - 4), Mathf.Max(1f, height * 0.3f)), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 1f, 1f, 0.22f), 0, height / 4f);
        }

        static GUIStyle tabStyle, text, hint, heading, richButton, card;
        public static GUIStyle TabStyle => tabStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
        /// <summary>Normal panel text: rich and wrapping.</summary>
        public static GUIStyle Text => text ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 13 };
        /// <summary>Small muted text under a heading.</summary>
        public static GUIStyle Hint
        {
            get
            {
                if (hint == null)
                {
                    hint = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 };
                    hint.normal.textColor = Muted;
                }
                return hint;
            }
        }
        public static GUIStyle Heading
        {
            get
            {
                if (heading == null)
                {
                    heading = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 16, fontStyle = FontStyle.Bold };
                    heading.normal.textColor = new Color(1f, 0.93f, 0.75f);
                }
                return heading;
            }
        }
        public static GUIStyle RichButton => richButton ??= new GUIStyle(GUI.skin.button) { richText = true, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 8, 5, 5) };
        /// <summary>A raised card inside a panel: one recipe, one minion, one item group.</summary>
        public static GUIStyle Card
        {
            get
            {
                if (card == null)
                {
                    card = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 7, 7), margin = new RectOffset(2, 2, 3, 3), richText = true };
                    card.normal.background = Round(Raised, new Color(0.22f, 0.36f, 0.48f));
                    card.border = new RectOffset(7, 7, 7, 7);
                }
                return card;
            }
        }

        static GUIStyle caption, darkCaption, key, darkKey, darkCaptionBig, bigButton;
        static GUIStyle Caption => caption ??= Make(14, FontStyle.Bold, Color.white);
        static GUIStyle DarkCaption => darkCaption ??= Make(14, FontStyle.Bold, new Color(0.1f, 0.1f, 0.12f));
        static GUIStyle Key => key ??= Make(10, FontStyle.Normal, Muted);
        static GUIStyle DarkKey => darkKey ??= Make(10, FontStyle.Normal, new Color(0.25f, 0.25f, 0.28f));
        static GUIStyle DarkCaptionBig => darkCaptionBig ??= Make(18, FontStyle.Bold, Color.white);
        public static GUIStyle BigButton => bigButton ??= new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true, fixedHeight = 42 };

        static GUIStyle Make(int size, FontStyle style, Color color)
        {
            var g = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = false };
            g.normal.textColor = color;
            return g;
        }

        // ---- Procedural textures (9-sliced through each style's border) ----

        /// <summary>A rounded box with a 1-pixel rim and soft edges, <paramref name="size"/> pixels square.</summary>
        static Texture2D Round(Color fill, Color rim, int size = 16)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float rad = size * 0.4f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Edge(x + 0.5f, y + 0.5f, size, size, rad);
                    var c = d > rad - 1.5f ? rim : fill;
                    c.a *= Mathf.Clamp01(rad - d + 0.5f);
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }

        /// <summary>The window: a teal title band over a navy body, rounded, with a teal rim.</summary>
        static Texture2D WindowTex()
        {
            const int w = 32, h = 48, header = 28;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            const float rad = 9f;
            var body = new Color(Panel.r, Panel.g, Panel.b, 0.985f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Edge(x + 0.5f, y + 0.5f, w, h, rad);
                    bool top = y >= h - header;   // texture rows run bottom-up; the title bar is at the top
                    var c = d > rad - 1.5f ? Teal
                        : y == h - header ? new Color(Teal.r, Teal.g, Teal.b, 0.8f)
                        : top ? Color.Lerp(TealDeep, new Color(0.12f, 0.4f, 0.48f), (y - (h - header)) / (float)header) : body;
                    c.a *= Mathf.Clamp01(rad - d + 0.5f);
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return t;
        }

        /// <summary>Grows toward the rounded outline, reaching <paramref name="rad"/> at the edge.</summary>
        static float Edge(float x, float y, int w, int h, float rad)
        {
            bool corner = (x < rad || x > w - rad) && (y < rad || y > h - rad);
            if (corner)
                return Vector2.Distance(new Vector2(x, y), new Vector2(Mathf.Clamp(x, rad, w - rad), Mathf.Clamp(y, rad, h - rad)));
            return rad - Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y));
        }
    }
}
