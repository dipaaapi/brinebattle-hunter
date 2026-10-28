using BrineBattle.Elements;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class ElementRulesTests
    {
        [TestCase(Element.Water, Element.Fire, Element.Steam)]
        [TestCase(Element.Water, Element.Earth, Element.Mud)]
        [TestCase(Element.Water, Element.Wind, Element.Ice)]
        [TestCase(Element.Fire, Element.Earth, Element.Magma)]
        [TestCase(Element.Fire, Element.Wind, Element.Lightning)]
        [TestCase(Element.Earth, Element.Wind, Element.Sand)]
        public void CombineMakesSecondary_EitherOrder(Element a, Element b, Element expected)
        {
            Assert.AreEqual(expected, ElementRules.Combine(a, b));
            Assert.AreEqual(expected, ElementRules.Combine(b, a));
        }

        [TestCase(Element.Water, Element.Water)]
        [TestCase(Element.None, Element.Fire)]
        [TestCase(Element.Steam, Element.Fire)]
        public void CombineInvalidReturnsNone(Element a, Element b)
        {
            Assert.AreEqual(Element.None, ElementRules.Combine(a, b));
        }

        [Test]
        public void ComponentsRoundTripThroughCombine()
        {
            foreach (Element e in System.Enum.GetValues(typeof(Element)))
            {
                if (!ElementRules.IsSecondary(e)) continue;
                var c = ElementRules.Components(e);
                Assert.AreEqual(2, c.Length, e.ToString());
                Assert.AreEqual(e, ElementRules.Combine(c[0], c[1]));
            }
        }

        [Test]
        public void MainCycleIsStrongOneWayWeakTheOther()
        {
            Assert.AreEqual(ElementRules.DefaultStrong, ElementRules.Multiplier(Element.Water, Element.Fire));
            Assert.AreEqual(ElementRules.DefaultWeak, ElementRules.Multiplier(Element.Fire, Element.Water));
            Assert.AreEqual(1f, ElementRules.Multiplier(Element.Water, Element.Earth));
            Assert.AreEqual(1f, ElementRules.Multiplier(Element.Fire, Element.Fire));
        }

        [Test]
        public void EveryMainBeatsExactlyOneMain()
        {
            foreach (var e in new[] { Element.Water, Element.Fire, Element.Earth, Element.Wind })
                Assert.IsTrue(ElementRules.IsMain(ElementRules.Beats(e)), e.ToString());
        }

        [Test]
        public void NoneIsNeutral()
        {
            Assert.AreEqual(1f, ElementRules.Multiplier(Element.None, Element.Magma));
            Assert.AreEqual(1f, ElementRules.Multiplier(Element.Ice, Element.None));
        }

        [Test]
        public void SecondaryMultipliesComponents()
        {
            // Steam (Water+Fire) vs Earth: Water 1 * Fire 1.5
            Assert.AreEqual(1.5f, ElementRules.Multiplier(Element.Steam, Element.Earth), 1e-5f);
            // Mud (Water+Earth) vs Fire: Water 1.5 * Earth 0.75
            Assert.AreEqual(1.125f, ElementRules.Multiplier(Element.Mud, Element.Fire), 1e-5f);
        }

        [Test]
        public void MultiplierIsAlwaysClamped()
        {
            foreach (Element a in System.Enum.GetValues(typeof(Element)))
                foreach (Element d in System.Enum.GetValues(typeof(Element)))
                {
                    float m = ElementRules.Multiplier(a, d, 3f, 0.1f);
                    Assert.That(m, Is.InRange(ElementRules.MinMultiplier, ElementRules.MaxMultiplier), $"{a} vs {d}");
                }
        }
    }
}
