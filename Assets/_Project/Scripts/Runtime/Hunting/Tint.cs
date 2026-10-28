using BrineBattle.Elements;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Placeholder colours until the art pass (Phase 9).</summary>
    public static class Tint
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static MaterialPropertyBlock block;

        public static void Apply(Renderer r, Color c)
        {
            if (r == null) return;
            block ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, c);
            r.SetPropertyBlock(block);
        }

        public static Color Of(Element e)
        {
            switch (e)
            {
                case Element.Water: return new Color(0.25f, 0.55f, 1f);
                case Element.Fire: return new Color(1f, 0.4f, 0.2f);
                case Element.Earth: return new Color(0.55f, 0.4f, 0.25f);
                case Element.Wind: return new Color(0.7f, 1f, 0.75f);
                case Element.Steam: return new Color(0.9f, 0.9f, 0.95f);
                case Element.Mud: return new Color(0.4f, 0.32f, 0.2f);
                case Element.Ice: return new Color(0.7f, 0.95f, 1f);
                case Element.Magma: return new Color(0.8f, 0.2f, 0.1f);
                case Element.Lightning: return new Color(1f, 0.95f, 0.3f);
                case Element.Sand: return new Color(0.9f, 0.8f, 0.5f);
                default: return Color.gray;
            }
        }
    }
}
