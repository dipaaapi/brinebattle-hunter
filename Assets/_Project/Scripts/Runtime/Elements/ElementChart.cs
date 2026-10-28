using UnityEngine;

namespace BrineBattle.Elements
{
    /// <summary>Tunable type-chart multipliers; the rules live in <see cref="ElementRules"/>.</summary>
    [CreateAssetMenu(menuName = "BrineBattle/Element Chart", fileName = "ElementChart")]
    public class ElementChart : ScriptableObject
    {
        [Min(1f)] public float strong = ElementRules.DefaultStrong;
        [Range(0.1f, 1f)] public float weak = ElementRules.DefaultWeak;

        public float Multiplier(Element attacker, Element defender) =>
            ElementRules.Multiplier(attacker, defender, strong, weak);
    }
}
