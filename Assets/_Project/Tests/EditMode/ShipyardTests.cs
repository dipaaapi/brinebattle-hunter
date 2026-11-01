using BrineBattle.Hunting;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's Shipyard: a separate view to customise the raft, with the game fully halted.</summary>
    public class ShipyardTests
    {
        static ItemDefinition Part(Rarity r = Rarity.Common, ItemType t = ItemType.Raft)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{r} {t}";
            i.type = t;
            i.rarity = r;
            return i;
        }

        static RaftGrid Raft(ItemDefinition plank)
        {
            var g = new RaftGrid();
            g.FillSquare(plank, 1);
            return g;
        }

        [Test]
        public void TheViewTurnsAndZoomsWithinLimits()
        {
            Assert.AreEqual(ShipyardRules.MaxPitch, ShipyardRules.ClampPitch(200f));
            Assert.AreEqual(ShipyardRules.MinPitch, ShipyardRules.ClampPitch(-50f));
            Assert.AreEqual(ShipyardRules.MinZoom, ShipyardRules.ClampZoom(0f));
            Assert.AreEqual(ShipyardRules.MaxZoom, ShipyardRules.ClampZoom(999f));
            Assert.AreEqual(10f, ShipyardRules.WrapYaw(370f), 1e-4f);
            Assert.AreEqual(350f, ShipyardRules.WrapYaw(-10f), 1e-4f);
            Assert.IsTrue(ShipyardRules.IsClick(2f));
            Assert.IsFalse(ShipyardRules.IsClick(20f), "a drag turns the raft instead");
        }

        [Test]
        public void ThePointUnderTheMouseGivesTheRightCell()
        {
            const float s = 1.05f;
            // The model is centred on the raft's middle (here the 3×3 square around 0,0).
            Assert.AreEqual(new Cell(0, 0), ShipyardRules.CellAt(0.1f, -0.2f, 0f, 0f, s));
            Assert.AreEqual(new Cell(2, 0), ShipyardRules.CellAt(2.1f, 0.3f, 0f, 0f, s), "an empty cell beside the raft");
            Assert.AreEqual(new Cell(-1, 1), ShipyardRules.CellAt(-1.0f, 1.0f, 0f, 0f, s));
            // A raft whose middle is at (1, 0.5): the model was shifted by that, so add it back.
            Assert.AreEqual(new Cell(1, 1), ShipyardRules.CellAt(0f, 0.3f, 1f, 0.5f, s));
            Assert.IsTrue(ShipyardRules.OnGrid(new Cell(4, -4)));
            Assert.IsFalse(ShipyardRules.OnGrid(new Cell(5, 0)), "off the 9×9 grid");
        }

        [Test]
        public void TheWholeGameIsHaltedWhileItIsOpen()
        {
            Assert.AreEqual(0f, ShipyardRules.WorldTimeScale(true, 1f));
            Assert.AreEqual(3f, ShipyardRules.WorldTimeScale(false, 3f), "the old speed comes back on close");
            Assert.IsFalse(ShipyardRules.MinionsMayWork(true), "minions don't help in the Shipyard");
            Assert.IsTrue(ShipyardRules.MinionsMayWork(false));
            Assert.IsFalse(ShipyardRules.TimersRun(true), "no auto-save or dozing off");
        }

        [Test]
        public void QuestsPointAtTheRightEdges()
        {
            Assert.AreEqual(new[] { RaftEdge.Stern }, ShipyardRules.EdgesFor(new Quest { Id = "modules" }), "modules go on the left (upstream) edge");
            Assert.AreEqual(new[] { RaftEdge.Bow }, ShipyardRules.EdgesFor(new Quest { Id = "piece", Piece = ShipPieceRules.Find("piece_pointed_bow") }));
            Assert.AreEqual(new[] { RaftEdge.Stern }, ShipyardRules.EdgesFor(new Quest { Id = "piece", Piece = ShipPieceRules.Find("piece_rudder") }));
            CollectionAssert.AreEquivalent(new[] { RaftEdge.Port, RaftEdge.Starboard }, ShipyardRules.EdgesFor(new Quest { Id = "piece", Piece = ShipPieceRules.Find("piece_keel") }));
            Assert.AreEqual(4, ShipyardRules.EdgesFor(new Quest { Id = "piece", Piece = ShipPieceRules.Find("piece_deck_trim") }).Length, "the rim goes all round");
            Assert.AreEqual(0, ShipyardRules.EdgesFor(new Quest { Id = "level" }).Length);
            Assert.AreEqual(0, ShipyardRules.EdgesFor(null).Length);
            Assert.AreEqual((1, 0), ShipyardRules.Direction(RaftEdge.Bow));
            Assert.AreEqual((-1, 0), ShipyardRules.Direction(RaftEdge.Stern));
            Assert.IsTrue(ShipyardRules.IsShipyardQuest(new Quest { Id = "piece" }));
            Assert.IsFalse(ShipyardRules.IsShipyardQuest(new Quest { Id = "boss" }), "the Shipyard shows only raft and ship quests");
        }

        [Test]
        public void EdgeMarkersSitOnTheBorderOfTheView()
        {
            // From the centre (100, 50) of a 0..200 × 0..100 view.
            Assert.AreEqual((200f, 50f), ShipyardRules.ToBorder(100, 50, 1, 0, 0, 0, 200, 100));
            Assert.AreEqual((0f, 50f), ShipyardRules.ToBorder(100, 50, -1, 0, 0, 0, 200, 100));
            Assert.AreEqual((100f, 0f), ShipyardRules.ToBorder(100, 50, 0, -1, 0, 0, 200, 100));
            var (x, y) = ShipyardRules.ToBorder(100, 50, 0.7071f, 0.7071f, 0, 0, 200, 100);
            Assert.AreEqual(100f, y, 1e-3f, "a diagonal stops at the nearer border");
            Assert.AreEqual(150f, x, 1e-2f);
        }

        [Test]
        public void HoverPreviewUsesTheSameRulesAsBuilding()
        {
            var plank = Part();
            var g = Raft(plank);
            var tarred = Part(Rarity.Uncommon);
            Assert.AreEqual(ShipyardRules.Action.Place, ShipyardRules.Preview(g, new Cell(2, 0), tarred, false, out _));
            Assert.AreEqual(ShipyardRules.Action.None, ShipyardRules.Preview(g, new Cell(4, 4), tarred, false, out var far));
            Assert.AreEqual(ShipyardRules.PlaceText(PlaceResult.NotAdjacent), far);
            Assert.AreEqual(ShipyardRules.Action.Replace, ShipyardRules.Preview(g, new Cell(0, 0), tarred, false, out _));
            Assert.AreEqual(ShipyardRules.Action.None, ShipyardRules.Preview(g, new Cell(0, 0), plank, false, out var same));
            StringAssert.Contains("already", same);
            Assert.AreEqual(ShipyardRules.Action.Remove, ShipyardRules.Preview(g, new Cell(1, 1), null, true, out _));
            Assert.AreEqual(ShipyardRules.Action.None, ShipyardRules.Preview(g, new Cell(3, 3), null, true, out var none));
            Assert.AreEqual("No part there.", none);
            Assert.AreEqual(ShipyardRules.Action.None, ShipyardRules.Preview(g, new Cell(2, 0), null, false, out var pick));
            StringAssert.Contains("Pick a part", pick);
        }
    }
}
