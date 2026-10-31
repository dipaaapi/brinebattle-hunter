using System.Collections.Generic;
using BrineBattle.Rafts;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    /// <summary>
    /// Ed: no part of the ship may overlap the hunter, a minion or a monster, and nobody may look like they float
    /// half off the raft. Bodies on the deck stay on walkable planks; bodies in the water stay out of the decor.
    /// </summary>
    public class DeckRulesTests
    {
        const float S = 1.05f;

        static HashSet<Cell> Square(int half)
        {
            var set = new HashSet<Cell>();
            for (int x = -half; x <= half; x++) for (int z = -half; z <= half; z++) set.Add(new Cell(x, z));
            return set;
        }

        [Test]
        public void ABodyNeverHangsOverTheEdge()
        {
            var deck = Square(1);   // 3×3: the deck spans -1.575 .. 1.575
            var (x, z) = DeckRules.Clamp(1.5f, 0f, 0.3f, S, deck);
            Assert.LessOrEqual(x + 0.3f, 1.5f * S, "the whole body stays on the planks");
            Assert.AreEqual(0f, z);
            (x, z) = DeckRules.Clamp(-2f, -2f, 0.21f, S, deck);
            Assert.GreaterOrEqual(x - 0.21f, -1.5f * S);
            Assert.GreaterOrEqual(z - 0.21f, -1.5f * S);
            (x, z) = DeckRules.Clamp(0.5f, 0.2f, 0.3f, S, deck);
            Assert.AreEqual((0.5f, 0.2f), (x, z), "inside the raft, crossing a seam between planks is fine");
        }

        [Test]
        public void AMissingPlankIsAnEdgeToo()
        {
            var deck = Square(1);
            deck.Remove(new Cell(1, 0));   // a module or a hole: not walkable
            var (x, _) = DeckRules.Clamp(0.5f, 0f, 0.3f, S, deck);
            Assert.LessOrEqual(x + 0.3f, 0.5f * S, "stops at the edge of the walkable plank");
            var (bx, bz) = DeckRules.Clamp(1.05f, 0f, 0.3f, S, deck);
            Assert.IsTrue(deck.Contains(new Cell((int)System.Math.Round(bx / S), (int)System.Math.Round(bz / S))), "brought back onto a walkable plank");
        }

        [Test]
        public void MonstersArePushedOutOfTheDecor()
        {
            var outrigger = new DeckBox(-1.5f, 1.5f, 2.5f, 2.8f);
            var (x, z) = DeckRules.PushOut(outrigger, 0f, 2.6f, 0.4f);
            Assert.IsFalse(outrigger.Overlaps(x, z, 0.4f), "no longer overlaps");
            Assert.AreEqual(0f, x, "pushed the short way (out sideways)");
            var (fx, fz) = DeckRules.PushOut(outrigger, 0f, 5f, 0.4f);
            Assert.AreEqual((0f, 5f), (fx, fz), "far away: untouched");
        }

        [Test]
        public void TheShipReachesFurtherWhereItsBowIs()
        {
            var boxes = new[] { new DeckBox(-1.6f, 1.6f, -1.6f, 1.6f), new DeckBox(1.6f, 4f, -0.3f, 0.3f) };   // deck + bowsprit
            Assert.AreEqual(4f, DeckRules.Reach(boxes, 1f, 0f), 1e-4f, "toward the bow: past the bowsprit");
            Assert.AreEqual(1.6f, DeckRules.Reach(boxes, -1f, 0f), 1e-4f, "toward the stern: just the deck");
            Assert.AreEqual(1.6f, DeckRules.Reach(boxes, 0f, 1f), 1e-4f, "to the side the bowsprit isn't in the way");
        }
    }
}
