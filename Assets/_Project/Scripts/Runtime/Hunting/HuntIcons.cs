using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Small pixel icons drawn from text maps (owner idea U-25: a kid should find things by picture,
    /// not by reading). Placeholders until the Phase 9 art pass. '#' = main colour, 'o' = accent, '.' = clear.
    /// </summary>
    public static class HuntIcons
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Bag => Make("bag", new Color(0.85f, 0.6f, 0.3f), new Color(1f, 0.9f, 0.5f),
            "....oooo....",
            "...o....o...",
            "...o....o...",
            "..########..",
            ".##########.",
            ".####oo####.",
            ".####oo####.",
            ".##########.",
            ".##########.",
            ".##########.",
            "..########..",
            "............");

        public static Texture2D Paw => Make("paw", new Color(1f, 0.6f, 0.75f), Color.white,
            "............",
            "..##....##..",
            "..##....##..",
            "##..####..##",
            "##..####..##",
            "....####....",
            "...######...",
            "..########..",
            "..########..",
            "...######...",
            "....####....",
            "............");

        public static Texture2D Book => Make("book", new Color(0.4f, 0.65f, 1f), Color.white,
            "............",
            ".####..####.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".#ooo##ooo#.",
            ".##########.",
            "............",
            "............");

        public static Texture2D Hammer => Make("hammer", new Color(0.75f, 0.75f, 0.8f), new Color(0.75f, 0.5f, 0.3f),
            "............",
            ".#######....",
            ".########...",
            ".#######....",
            "....oo......",
            "....oo......",
            "....oo......",
            "....oo......",
            "....oo......",
            "....oo......",
            "....oo......",
            "............");

        public static Texture2D Gear => Make("gear", new Color(0.7f, 0.75f, 0.8f), new Color(0.15f, 0.18f, 0.24f),
            "............",
            ".....##.....",
            "..#.####.#..",
            "...######...",
            "..###oo###..",
            ".###oooo###.",
            ".###oooo###.",
            "..###oo###..",
            "...######...",
            "..#.####.#..",
            ".....##.....",
            "............");

        public static Texture2D Raft => Make("raft", new Color(0.85f, 0.72f, 0.45f), new Color(0.3f, 0.6f, 0.95f),
            "............",
            ".....#......",
            ".....##.....",
            ".....###....",
            ".....####...",
            ".....#......",
            ".##########.",
            ".#.#.#.#.##.",
            ".##########.",
            "oooooooooooo",
            ".oo.oooo.oo.",
            "............");

        public static Texture2D Harpoon => Make("harpoon", new Color(0.75f, 0.8f, 0.9f), new Color(0.75f, 0.5f, 0.3f),
            "............",
            ".........##.",
            "........###.",
            ".......####.",
            "......o##...",
            ".....o......",
            "....o.......",
            "...o........",
            "..o.........",
            ".o..........",
            "o...........",
            "............");

        static Texture2D Make(string key, Color main, Color accent, params string[] rows)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            int h = rows.Length, w = rows[0].Length;
            t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char ch = rows[h - 1 - y][x];   // textures start at the bottom row
                    t.SetPixel(x, y, ch == '#' ? main : ch == 'o' ? accent : Color.clear);
                }
            t.Apply();
            cache[key] = t;
            return t;
        }
    }
}
