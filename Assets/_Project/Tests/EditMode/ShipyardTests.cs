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
        public void PanningStaysNearTheRaftAndDragsDropOnlyOnACell()
        {
            Assert.AreEqual(ShipyardRules.PanLimit, ShipyardRules.ClampPan(50f));
            Assert.AreEqual(-ShipyardRules.PanLimit, ShipyardRules.ClampPan(-50f));
            Assert.AreEqual(2f, ShipyardRules.ClampPan(2f));
            Assert.IsTrue(ShipyardRules.Drops(false, true), "released over a cell of the raft");
            Assert.IsFalse(ShipyardRules.Drops(true, true), "released over the list: cancelled");
            Assert.IsFalse(ShipyardRules.Drops(false, false), "released off the raft: cancelled");
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
