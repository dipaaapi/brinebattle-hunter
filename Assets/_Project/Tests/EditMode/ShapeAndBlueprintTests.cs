using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's bigger module shapes and blueprints (save, share code, build from the bag).</summary>
    public class ShapeAndBlueprintTests
    {
        static ItemDefinition Item(string id, ItemType t, Rarity r = Rarity.Common)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = id;
            i.displayName = id;
            i.type = t;
            i.rarity = r;
            return i;
        }

        static ItemDefinition Kit(ItemDefinition module, ModuleShape shape)
        {
            var k = Item($"{module.id}_{shape}", ItemType.Module);
            k.shape = shape;
            k.segment = module;
            return k;
        }

        static RaftGrid Raft(ItemDefinition plank)
        {
            var g = new RaftGrid();
            g.FillSquare(plank, 1);
            return g;
        }

        [Test]
        public void FootprintsHaveTheirSizeAndTurn()
        {
            Assert.AreEqual(1, ShapeRules.Size(ModuleShape.Single));
            Assert.AreEqual(4, ShapeRules.Size(ModuleShape.Square));
            Assert.AreEqual(4, ShapeRules.Size(ModuleShape.T));
            CollectionAssert.AreEquivalent(new[] { new Cell(5, 5), new Cell(6, 5) }, ShapeRules.Footprint(ModuleShape.Bar2, 0, new Cell(5, 5)));
            CollectionAssert.AreEquivalent(new[] { new Cell(5, 5), new Cell(5, 6) }, ShapeRules.Footprint(ModuleShape.Bar2, 1, new Cell(5, 5)), "a quarter turn stands it up");
            CollectionAssert.AreEquivalent(ShapeRules.Footprint(ModuleShape.L, 0, new Cell(0, 0)), ShapeRules.Footprint(ModuleShape.L, 4, new Cell(0, 0)), "four turns come back");
            foreach (ModuleShape s in System.Enum.GetValues(typeof(ModuleShape)))
                for (int r = 0; r < 4; r++)
                    Assert.AreEqual(ShapeRules.Size(s), ShapeRules.Footprint(s, r, new Cell(0, 0)).Distinct().Count(), "no cell twice");
        }

        [Test]
        public void ShapesOpenWithTheRaftsGrowth()
        {
            var g = Raft(Item("plank", ItemType.Raft));
            Assert.IsTrue(ShapeRules.Unlocked(g, ModuleShape.Single));
            Assert.IsFalse(ShapeRules.Unlocked(g, ModuleShape.Bar2));
            g.RestoreEvolution(RaftKind.Plain, 20);
            Assert.IsTrue(ShapeRules.Unlocked(g, ModuleShape.Square));
            Assert.IsFalse(ShapeRules.Unlocked(g, ModuleShape.L));
            Assert.IsFalse(ShapeRules.Unlocked(g, ModuleShape.Bar3), "1×3 is for ships");
            g.RestoreEvolution(RaftKind.Sea, 1);
            Assert.IsTrue(ShapeRules.Unlocked(g, ModuleShape.Bar3));
        }

        [Test]
        public void AKitNeedsRoomToTouchTheRaftAndNoOverlap()
        {
            var g = Raft(Item("plank", ItemType.Raft));
            var net = Item("net", ItemType.Module);
            var bar = Kit(net, ModuleShape.Bar2);
            Assert.AreEqual(PlaceResult.Occupied, ShapeRules.CanPlace(g, bar, 0, new Cell(1, 0), out _), "overlaps the raft");
            Assert.AreEqual(PlaceResult.NotAdjacent, ShapeRules.CanPlace(g, bar, 0, new Cell(3, 3), out _));
            Assert.AreEqual(PlaceResult.OutOfBounds, ShapeRules.CanPlace(g, bar, 0, new Cell(4, 0), out _));
            Assert.AreEqual(PlaceResult.Ok, ShapeRules.CanPlace(g, bar, 0, new Cell(2, 0), out var order));
            Assert.AreEqual(2, order.Count);
            Assert.AreEqual(new Cell(2, 0), order[0], "the cell touching the raft goes first");
            Assert.AreEqual(2, ShapeRules.Place(g, bar, 0, new Cell(2, 0)));
            Assert.AreEqual(net, g.Parts[new Cell(3, 0)], "each cell is a plain module");
            Assert.AreEqual(11, g.Count);
            Assert.AreEqual(0, ShapeRules.Place(g, bar, 0, new Cell(2, 0)), "can't go twice on the same cells");
        }

        [Test]
        public void ShareCodesRoundTripAndRejectTampering()
        {
            var g = Raft(Item("plank", ItemType.Raft));
            g.RestoreEvolution(RaftKind.Aerial, 12);
            var bp = Blueprint.Capture(g, "Swift|One", "Ed");
            Assert.AreEqual("SwiftOne", bp.name, "separators are removed");
            var code = bp.ToCode();
            StringAssert.StartsWith("BBH1:", code);
            var back = Blueprint.FromCode(code);
            Assert.IsNotNull(back);
            Assert.AreEqual("SwiftOne", back.name);
            Assert.AreEqual("Ed", back.author);
            Assert.AreEqual(RaftKind.Aerial, back.Line);
            Assert.AreEqual(12, back.level);
            Assert.AreEqual(9, back.PartCount);
            string bad = code.Substring(0, 8) + (code[8] == 'A' ? 'B' : 'A') + code.Substring(9);
            Assert.IsNull(Blueprint.FromCode(bad), "a changed code is refused");
            Assert.IsNull(Blueprint.FromCode("hello"));
            var copy = back.CopyFor("Mia");
            Assert.AreEqual("Mia", copy.author);
            Assert.AreEqual("Ed", copy.copiedFrom, "the original author keeps the credit");
            Assert.AreEqual("Ed", copy.CopyFor("Zed").copiedFrom, "even after another copy");
            StringAssert.Contains("copied from Ed", copy.Credit);
        }

        [Test]
        public void BuildingABlueprintUsesTheBagAndListsWhatsMissing()
        {
            var plank = Item("plank", ItemType.Raft);
            var tarred = Item("tarred", ItemType.Raft, Rarity.Uncommon);
            var design = Raft(plank);
            design.Place(new Cell(2, 0), tarred);
            design.Place(new Cell(3, 0), tarred);
            var bp = Blueprint.Capture(design, "Long", "Ed");

            var mine = Raft(plank);
            var bag = new Inventory();
            bag.Add(tarred, 1);
            ItemDefinition Find(string id) => id == "plank" ? plank : id == "tarred" ? tarred : null;
            Assert.AreEqual(2, bp.Needed(mine)["tarred"]);
            Assert.AreEqual(1, bp.Missing(mine, bag, Find)["tarred"], "one in the bag, one missing");
            Assert.AreEqual(1, bp.Apply(mine, bag, Find), "only what the bag has");
            Assert.AreEqual(tarred, mine.Parts[new Cell(2, 0)]);
            Assert.AreEqual(0, bag.Count(tarred));
            Assert.IsFalse(mine.Has(new Cell(3, 0)), "copying a design gives nothing for free");
            var quest = QuestRules.BlueprintQuest(bp, mine, bag, Find);
            Assert.AreEqual(1, quest.Lines.Single().Need);
        }
    }
}
