using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's Quest tab: quests come from game state and list what's missing.</summary>
    public class QuestTests
    {
        static ItemDefinition Item(string id, ItemType type, Rarity rarity = Rarity.Common)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.id = id;
            i.displayName = id;
            i.type = type;
            i.rarity = rarity;
            return i;
        }

        static RecipeDefinition Recipe(ItemDefinition output, params (ItemDefinition item, int count)[] inputs)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.kind = RecipeKind.Craft;
            r.outputs = new List<Ingredient> { new Ingredient { item = output, count = 1 } };
            r.inputs = inputs.Select(x => new Ingredient { item = x.item, count = x.count }).ToList();
            return r;
        }

        static RaftGrid Raft()
        {
            var g = new RaftGrid();
            g.FillSquare(Item("plank", ItemType.Raft), 1);
            return g;
        }

        [Test]
        public void NextLevelListsEveryRequirementWithHaveAndNeed()
        {
            var raft = Raft();
            raft.Kills = 2;
            var bag = new Inventory();
            var quests = QuestRules.Build(raft, bag, null);
            var level = quests.Single(q => q.Id == "level");
            Assert.AreEqual("Raft Lv 2", level.Title);
            Assert.AreEqual(QuestTarget.RaftTree, level.Target);
            var kills = level.Lines.Single(l => l.Text == "Monsters beaten");
            Assert.AreEqual(2, kills.Have);
            Assert.AreEqual(EvolutionRules.Kills(Rarity.Common), kills.Need);
            Assert.IsFalse(level.Done);
            Assert.IsFalse(level.Lines.Single(l => l.Text.Contains("hammer")).Done, "no hammer in the bag");
        }

        [Test]
        public void MissingHammerPointsToItsRecipeAndIngredients()
        {
            var raft = Raft();
            var bag = new Inventory();
            var wood = Item("wood", ItemType.Material);
            var fang = Item("fang", ItemType.Material);
            bag.Add(wood, 1);
            var hammer = Recipe(Item("hammer", ItemType.Tool), (wood, 2), (fang, 1));
            var q = QuestRules.Build(raft, bag, new[] { hammer }).Single(x => x.Id == "hammer");
            Assert.AreEqual(QuestTarget.Workbench, q.Target);
            Assert.AreEqual(RecipeSub.Hammers, q.Sub);
            var line = q.Lines.Single(l => l.Text == "wood");
            Assert.AreEqual(1, line.Have);
            Assert.AreEqual(2, line.Need);
            bag.Add(hammer.outputs[0].item, 1);
            Assert.IsFalse(QuestRules.Build(raft, bag, new[] { hammer }).Any(x => x.Id == "hammer"), "gone once the hammer is in the bag");
        }

        [Test]
        public void BossAndVehicleQuestsAppearOnlyWhenTheyMatter()
        {
            var raft = Raft();
            var bag = new Inventory();
            Assert.IsFalse(QuestRules.Build(raft, bag, null).Any(q => q.Id == "boss"), "bosses off");
            raft.BossesNeeded = true;
            Assert.IsFalse(QuestRules.Build(raft, bag, null).Any(q => q.Id == "boss"), "no boss in the middle of a tier");
            raft.RestoreEvolution(RaftKind.Plain, 16);
            Assert.IsTrue(QuestRules.Build(raft, bag, null).Any(q => q.Id == "boss"), "Lv 16 ends the Common tier");
            Assert.AreEqual(QuestTarget.Boss, QuestRules.Build(raft, bag, null).First(q => q.Id == "boss").Target, "summon it from the Quest tab");
            Assert.IsFalse(QuestRules.Build(raft, bag, null).Any(q => q.Id == "vessel" || q.Id == "leave"), "no type picked yet");
        }

        [Test]
        public void ADoneQuestHasAllLinesMet()
        {
            var q = new Quest { Lines = { new QuestLine("a", 3, 3), new QuestLine("b", 5, 2) } };
            Assert.IsTrue(q.Done);
            Assert.AreEqual(0, q.Missing);
            q.Lines.Add(new QuestLine("c", 0, 1));
            Assert.IsFalse(q.Done);
            Assert.AreEqual(1, q.Missing);
        }
    }
}
