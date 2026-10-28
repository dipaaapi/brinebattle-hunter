using System;

namespace BrineBattle.Elements
{
    /// <summary>
    /// Element combination and type chart. Plain C# so EditMode tests cover it;
    /// the multipliers are tunable through <see cref="ElementChart"/>.
    /// </summary>
    public static class ElementRules
    {
        public const float DefaultStrong = 1.5f;
        public const float DefaultWeak = 0.75f;
        public const float MinMultiplier = 0.5f;
        public const float MaxMultiplier = 2f;

        public static bool IsMain(Element e) => e >= Element.Water && e <= Element.Wind;
        public static bool IsSecondary(Element e) => e >= Element.Steam && e <= Element.Sand;

        /// <summary>Two different main elements make a secondary one; anything else returns None.</summary>
        public static Element Combine(Element a, Element b)
        {
            if (!IsMain(a) || !IsMain(b) || a == b) return Element.None;
            if (a > b) (a, b) = (b, a);
            switch (a, b)
            {
                case (Element.Water, Element.Fire): return Element.Steam;
                case (Element.Water, Element.Earth): return Element.Mud;
                case (Element.Water, Element.Wind): return Element.Ice;
                case (Element.Fire, Element.Earth): return Element.Magma;
                case (Element.Fire, Element.Wind): return Element.Lightning;
                case (Element.Earth, Element.Wind): return Element.Sand;
                default: return Element.None;
            }
        }

        /// <summary>The main elements an element is made of: one for main, two for secondary, none for None.</summary>
        public static Element[] Components(Element e)
        {
            switch (e)
            {
                case Element.Steam: return new[] { Element.Water, Element.Fire };
                case Element.Mud: return new[] { Element.Water, Element.Earth };
                case Element.Ice: return new[] { Element.Water, Element.Wind };
                case Element.Magma: return new[] { Element.Fire, Element.Earth };
                case Element.Lightning: return new[] { Element.Fire, Element.Wind };
                case Element.Sand: return new[] { Element.Earth, Element.Wind };
                case Element.None: return Array.Empty<Element>();
                default: return new[] { e };
            }
        }

        /// <summary>Main cycle Water > Fire > Earth > Wind > Water. Returns the element e beats.</summary>
        public static Element Beats(Element e)
        {
            switch (e)
            {
                case Element.Water: return Element.Fire;
                case Element.Fire: return Element.Earth;
                case Element.Earth: return Element.Wind;
                case Element.Wind: return Element.Water;
                default: return Element.None;
            }
        }

        /// <summary>Main vs main: strong, weak or 1.</summary>
        public static float MainMultiplier(Element attacker, Element defender, float strong = DefaultStrong, float weak = DefaultWeak)
        {
            if (Beats(attacker) == defender) return strong;
            if (Beats(defender) == attacker) return weak;
            return 1f;
        }

        /// <summary>
        /// Damage multiplier for any pair: the product of every attacker component against every
        /// defender component, clamped to [MinMultiplier, MaxMultiplier]. None is always 1.
        /// </summary>
        public static float Multiplier(Element attacker, Element defender, float strong = DefaultStrong, float weak = DefaultWeak)
        {
            float m = 1f;
            foreach (var a in Components(attacker))
                foreach (var d in Components(defender))
                    m *= MainMultiplier(a, d, strong, weak);
            return Math.Max(MinMultiplier, Math.Min(MaxMultiplier, m));
        }
    }
}
