using System.Collections.Generic;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class RaftTreeTests
    {
        static ItemDefinition Item(RaftKind kind, Rarity r, ItemType t = ItemType.Raft)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{kind} {r}";
            i.type = t;
            i.rarity = r;
            i.raftKind = kind;
            return i;
        }

        static RecipeDefinition Makes(ItemDefinition item)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.outputs = new List<Ingredient> { new Ingredient { item = item, count = 1 } };
            return r;
        }

        static RaftGrid Ready()
        {
            var g = new RaftGrid();
            g.FillSquare(Item(RaftKind.Plain, Rarity.Common), 1);
            g.Kills = 999;
            return g;
        }

        static Inventory Rich()
        {
            var inv = new Inventory();
            inv.Add(Item(RaftKind.Plain, Rarity.Common, ItemType.Material), 99);
            for (var r = Rarity.Common; r <= Rarity.Mythic; r++) inv.AddEssence(new EssenceKey(r, Elements.Element.None), 99);
            return inv;
        }

        [Test]
        public void LevelOneIsOpenForEveryKind()
        {
            var g = Ready();
            foreach (var k in RaftTree.Kinds) Assert.IsTrue(RaftTree.Unlocked(g, k, 1));
            Assert.IsFalse(RaftTree.Unlocked(g, RaftKind.Sky, 2));
        }

        [Test]
        public void PickingATypeAtLevelUpOpensItsBranch()
        {
            var g = Ready();
            Assert.IsTrue(g.TryUpgrade(Rich(), RaftKind.Sky));
            Assert.AreEqual(2, g.PathLevel(RaftKind.Sky));
            Assert.IsTrue(RaftTree.Unlocked(g, RaftKind.Sky, 2));
            Assert.IsFalse(RaftTree.Unlocked(g, RaftKind.Fortress, 2));
            Assert.IsTrue(RaftTree.Knows(g, Makes(Item(RaftKind.Sky, Rarity.Uncommon))));
            Assert.IsFalse(RaftTree.Knows(g, Makes(Item(RaftKind.Beast, Rarity.Uncommon))));
        }

        [Test]
        public void TheTypeIsPickedOnceAndTheOthersStayLocked()
        {
            var g = Ready();
            var inv = Rich();
            g.TryUpgrade(inv, RaftKind.Sky);
            g.Kills = 999;
            g.TryUpgrade(inv, RaftKind.Beast);   // ignored: the raft is already Sky
            Assert.AreEqual(RaftKind.Sky, g.Chosen);
            Assert.IsTrue(RaftTree.Unlocked(g, RaftKind.Sky, 3));
            Assert.IsFalse(RaftTree.Unlocked(g, RaftKind.Beast, 2));
            Assert.IsFalse(RaftTree.Unlocked(g, RaftKind.Fortress, 1), "unpicked types lock, even Lv 1");
            CollectionAssert.AreEqual(new[] { RaftKind.Sky, RaftKind.Sky }, g.Paths);
            Assert.IsFalse(g.Choose(RaftKind.Beast));
            Assert.AreEqual(RaftKind.Sky, g.Type.Main, "the picked type is the raft's type, whatever the parts");
        }

        [Test]
        public void ARaftPastThePickLevelCanChooseWithoutLevelingUp()
        {
            var g = new RaftGrid();
            g.Restore(new[] { new KeyValuePair<Cell, ItemDefinition>(new Cell(0, 0), Item(RaftKind.Plain, Rarity.Rare)) }, Rarity.Rare);
            g.RestorePaths(null);
            Assert.IsTrue(g.CanChoose);
            Assert.IsTrue(g.Choose(RaftKind.Fortress));
            Assert.AreEqual(RaftKind.Fortress, g.Chosen);
            Assert.IsTrue(RaftTree.Unlocked(g, RaftKind.Fortress, 3));
            Assert.IsFalse(g.CanChoose);
        }

        [Test]
        public void PlainItemsAndDismantlesAreAlwaysKnown()
        {
            var g = Ready();
            Assert.IsTrue(RaftTree.Knows(g, Makes(Item(RaftKind.Plain, Rarity.Mythic))));
            var d = Makes(Item(RaftKind.Sky, Rarity.Mythic));
            d.kind = RecipeKind.Dismantle;
            Assert.IsTrue(RaftTree.Knows(g, d));
        }

        [Test]
        public void OlderSavesGetPlainPathsUpToTheirLevel()
        {
            var g = new RaftGrid();
            g.Restore(new[] { new KeyValuePair<Cell, ItemDefinition>(new Cell(0, 0), Item(RaftKind.Plain, Rarity.Rare)) }, Rarity.Rare);
            g.RestorePaths(null);
            Assert.AreEqual(2, g.Paths.Count);
            Assert.AreEqual(1, g.PathLevel(RaftKind.Fortress));
        }

        [Test]
        public void TreeMaterialsAreNotSpentAsMonsterMaterials()
        {
            var inv = new Inventory();
            var raw = Item(RaftKind.Plain, Rarity.Common, ItemType.Material);
            var node = Item(RaftKind.Fortress, Rarity.Uncommon, ItemType.Material);
            inv.Add(raw, 2);
            inv.Add(node, 3);
            Assert.AreEqual(2, inv.MaterialCount());
            Assert.IsTrue(inv.SpendMaterials(2));
            Assert.AreEqual(3, inv.Count(node));
        }

        [Test]
        public void ModulesCatchFlotsamUpToTheirRarity()
        {
            Assert.IsTrue(ModuleRules.CatchesFlotsam(Rarity.Rare, Rarity.Common));
            Assert.IsTrue(ModuleRules.CatchesFlotsam(Rarity.Rare, Rarity.Rare));
            Assert.IsFalse(ModuleRules.CatchesFlotsam(Rarity.Common, Rarity.Uncommon));
        }

        [Test]
        public void PickingATypeSendsOtherTypesPartsBackToTheBag()
        {
            var basic = Item(RaftKind.Plain, Rarity.Common);
            var g = new RaftGrid();
            g.FillSquare(basic, 1);
            g.Kills = 999;
            var fortress = Item(RaftKind.Fortress, Rarity.Rare);
            var sky = Item(RaftKind.Sky, Rarity.Rare);
            g.Place(new Cell(2, 0), fortress);
            g.Place(new Cell(-2, 0), sky);
            var bag = Rich();
            Assert.IsTrue(g.TryUpgrade(bag, RaftKind.Sky));
            var back = g.ResetOffType(basic, bag);
            CollectionAssert.AreEqual(new[] { fortress }, back);
            Assert.AreEqual(basic, g.Parts[new Cell(2, 0)], "a basic plank keeps the raft's shape");
            Assert.AreEqual(sky, g.Parts[new Cell(-2, 0)]);
            Assert.AreEqual(1, bag.Count(fortress));
            Assert.AreEqual(PlaceResult.WrongType, g.CanPlace(new Cell(3, 0), fortress));
            Assert.IsFalse(g.FitsType(fortress));
            Assert.IsTrue(g.FitsType(sky));
        }

        [Test]
        public void ResettingTheRaftLosesAFifthAndKeepsHabitats()
        {
            var basic = Item(RaftKind.Plain, Rarity.Common);
            var g = new RaftGrid();
            g.FillSquare(basic, 1);
            var extra = Item(RaftKind.Plain, Rarity.Uncommon);
            var home = Item(RaftKind.Plain, Rarity.Common, ItemType.Habitat);
            for (int z = -1; z <= 1; z++) { g.Place(new Cell(2, z), extra); g.Place(new Cell(-2, z), extra); }
            g.Place(new Cell(0, 2), extra);
            g.Place(new Cell(0, -2), extra);
            g.Place(new Cell(1, 2), extra);
            g.Place(new Cell(1, -2), extra);   // 10 extra parts
            g.Place(new Cell(-1, 2), home);
            var bag = new Inventory();
            var lost = g.ResetRaft(basic, bag, new System.Random(1));
            Assert.AreEqual(2, lost.Count, "a fifth of 10");
            Assert.AreEqual(8, bag.Count(extra));
            Assert.AreEqual(10, g.Count, "starter square plus the habitat");
            Assert.AreEqual(1, g.HabitatCount);
            Assert.AreEqual(0, bag.Count(basic), "the starter planks don't come back as extras");
        }
    }
}
