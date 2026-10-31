using System.Collections.Generic;
using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Saving;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Ed's U-39: one kind of minion that levels up by eating loot and learns skills.</summary>
    public class MinionTests
    {
        static Companion Minion(string id = "eel", Rarity rarity = Rarity.Common, Element element = Element.Water)
        {
            var c = new Companion { Id = id, Name = id, Element = element, Rarity = rarity, MaxHp = 20, Hp = 20, Attack = 4, Defense = 2, Speed = 5 };
            c.CatchUp();
            return c;
        }

        static ItemDefinition Item(ItemType type, Rarity rarity = Rarity.Common, Element element = Element.None)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{rarity} {type}";
            i.type = type;
            i.rarity = rarity;
            i.element = element;
            return i;
        }

        [Test]
        public void ExpCurveGrowsWithLevelAndStopsAtMax()
        {
            Assert.AreEqual(10, MinionRules.ExpToNext(1));
            Assert.AreEqual(55, MinionRules.ExpToNext(10));
            Assert.AreEqual(0, MinionRules.ExpToNext(MinionRules.MaxLevel));
            var c = Minion();
            c.GainExp(100000);
            Assert.AreEqual(MinionRules.MaxLevel, c.Level);
            Assert.AreEqual(GrowthStage.Elder, c.Stage, "evolves at 10 and 20");
            Assert.AreEqual(0, c.Exp);
        }

        [Test]
        public void EveryMinionStartsAsAFighterThenFollowsItsElement()
        {
            var water = Minion("eel", Rarity.Common, Element.Water);
            CollectionAssert.AreEqual(new[] { MinionSkill.Fighter }, water.Skills);
            water.GainExp(MinionRules.ExpToNext(1) + MinionRules.ExpToNext(2));
            Assert.AreEqual(3, water.Level);
            CollectionAssert.AreEqual(new[] { MinionSkill.Fighter, MinionSkill.Looter }, water.Skills, "Water leans to Looter");
            var mud = Minion("crab", Rarity.Common, Element.Mud);
            mud.GainExp(MinionRules.ExpToNext(1) + MinionRules.ExpToNext(2));
            Assert.AreEqual(MinionSkill.Crewmate, mud.Skills[1], "Mud leans to Crewmate");
        }

        [Test]
        public void TwoKindsOfTheSameElementGrowDifferentlyButEachTheSameEveryTime()
        {
            var a = MinionRules.Order(Minion("eel")).ToList();
            var b = MinionRules.Order(Minion("ray")).ToList();
            CollectionAssert.AreEqual(a, MinionRules.Order(Minion("eel")).ToList(), "stable for one kind");
            CollectionAssert.AreNotEqual(a, b, "other kinds learn in another order");
            Assert.AreEqual(MinionRules.All.Length, a.Distinct().Count(), "every skill appears once");
        }

        [Test]
        public void RarityAndStageGateSkillsAndRoom()
        {
            var common = Minion("a", Rarity.Common);
            Assert.IsFalse(MinionRules.CanLearn(common, MinionSkill.Tinker), "Tinker needs Rare+");
            Assert.IsFalse(MinionRules.CanLearn(common, MinionSkill.Builder), "Builder needs Uncommon+");
            Assert.IsFalse(MinionRules.CanLearn(common, MinionSkill.Hardy), "Hardy needs Adult");
            Assert.IsTrue(MinionRules.CanLearn(Minion("b", Rarity.Rare), MinionSkill.Tinker));
            Assert.AreEqual(2, MinionRules.MaxSkills(Rarity.Common, GrowthStage.Young));
            Assert.AreEqual(9, MinionRules.MaxSkills(Rarity.Mythic, GrowthStage.Elder));
            common.GainExp(100000);
            Assert.AreEqual(MinionRules.MaxSkills(Rarity.Common, GrowthStage.Elder), common.Skills.Count, "a Common Elder holds 4 skills");
            Assert.IsFalse(common.Has(MinionSkill.Tinker));
            var mythic = Minion("m", Rarity.Mythic);
            mythic.GainExp(100000);
            Assert.AreEqual(9, mythic.Skills.Count);
            Assert.AreEqual(MinionSkill.Fighter, mythic.Skills[0]);
        }

        [Test]
        public void HardyAndFerociousRaiseStatsOnce()
        {
            var c = Minion();
            int hp = c.MaxHp, def = c.Defense, atk = c.Attack, spd = c.Speed;
            c.Learn(MinionSkill.Hardy);
            Assert.AreEqual(24, c.MaxHp);
            Assert.Greater(c.Defense, def);
            Assert.IsFalse(c.Learn(MinionSkill.Hardy), "only once");
            c.Learn(MinionSkill.Ferocious);
            Assert.Greater(c.Attack, atk);
            Assert.AreEqual(spd + 1, c.Speed);
            Assert.Greater(c.MaxHp, hp);
        }

        [Test]
        public void ExpDependsOnKindRarityAndElement()
        {
            var c = Minion(element: Element.Fire);
            Assert.AreEqual(1, MinionRules.ExpFor(Item(ItemType.Material), c));
            Assert.AreEqual(4, MinionRules.ExpFor(Item(ItemType.Raft, Rarity.Uncommon), c));
            Assert.AreEqual(9, MinionRules.ExpFor(Item(ItemType.Weapon, Rarity.Rare), c));
            Assert.AreEqual(18, MinionRules.ExpFor(Item(ItemType.Weapon, Rarity.Rare, Element.Fire), c), "favourite element doubles");
            Assert.AreEqual(0, MinionRules.ExpFor(Item(ItemType.Consumable), c), "potions infuse instead");
        }

        [Test]
        public void JunkIsLowGearPartsModulesAndOldHammersButNeverMaterials()
        {
            Rarity Tier(ItemType t) => t == ItemType.Weapon ? Rarity.Rare : Rarity.Common;
            var raftTier = Rarity.Uncommon;
            Assert.IsTrue(MinionRules.IsJunk(Item(ItemType.Weapon, Rarity.Uncommon), Tier, raftTier, null));
            Assert.IsFalse(MinionRules.IsJunk(Item(ItemType.Weapon, Rarity.Rare), Tier, raftTier, null));
            Assert.IsTrue(MinionRules.IsJunk(Item(ItemType.Raft, Rarity.Common), Tier, raftTier, null));
            Assert.IsFalse(MinionRules.IsJunk(Item(ItemType.Raft, Rarity.Common), Tier, raftTier, null, i => true), "a recipe still uses it");
            Assert.IsTrue(MinionRules.IsJunk(Item(ItemType.Module, Rarity.Common), Tier, raftTier, null));
            Assert.IsTrue(MinionRules.IsJunk(Item(ItemType.Tool, Rarity.Common), Tier, raftTier, Rarity.Uncommon), "an older hammer");
            Assert.IsFalse(MinionRules.IsJunk(Item(ItemType.Tool, Rarity.Uncommon), Tier, raftTier, Rarity.Uncommon), "the best hammer");
            Assert.IsFalse(MinionRules.IsJunk(Item(ItemType.Material, Rarity.Common), Tier, raftTier, null), "raft level-ups need materials");
            Assert.IsFalse(MinionRules.IsJunk(Item(ItemType.Consumable, Rarity.Common), Tier, raftTier, null));
        }

        [Test]
        public void JunkUpToTheSettingIsTakenApartTheRestIsEaten()
        {
            var common = Item(ItemType.Weapon, Rarity.Common);
            var rare = Item(ItemType.Weapon, Rarity.Rare);
            Assert.AreEqual(MinionRules.JunkAction.Dismantle, MinionRules.ForJunk(common, Rarity.Uncommon, true));
            Assert.AreEqual(MinionRules.JunkAction.Salvage, MinionRules.ForJunk(common, Rarity.Uncommon, false));
            Assert.AreEqual(MinionRules.JunkAction.Eat, MinionRules.ForJunk(rare, Rarity.Uncommon, true));
        }

        [Test]
        public void HungriestIsTheLowestLevel()
        {
            var a = Minion("a");
            var b = Minion("b");
            a.GainExp(50);
            Assert.AreEqual(b, CompanionWork.Hungriest(new[] { a, b }));
        }

        [Test]
        public void FusionPassesOneSkillAndTheHigherLevel()
        {
            var keep = Minion("eel");
            var source = Minion("eel");
            source.Learn(MinionSkill.Smith);
            source.Level = 8;
            Assert.AreEqual(MinionSkill.Smith, CompanionGrowth.EchoSkill(keep, source));
            Assert.AreEqual(FuseResult.Upgraded, CompanionGrowth.Fuse(keep, source, false, 0.0));
            Assert.IsTrue(keep.Has(MinionSkill.Smith));
            Assert.AreEqual(8, keep.Level);
        }

        [Test]
        public void OldSavesTurnRolesIntoSkills()
        {
            var crew = SaveGame.LoadPet(new SaveData.Pet { id = "crab", name = "Crab", role = (int)CompanionRole.Crew, maxHp = 10 });
            Assert.AreEqual(1, crew.Level);
            CollectionAssert.AreEqual(new[] { MinionSkill.Crewmate, MinionSkill.Mender }, crew.Skills);
            var mystic = SaveGame.LoadPet(new SaveData.Pet { id = "ray", name = "Ray", role = (int)CompanionRole.Mystic, stage = (int)GrowthStage.Adult, maxHp = 10 });
            Assert.AreEqual(10, mystic.Level);
            Assert.IsTrue(mystic.Has(MinionSkill.Alchemist) && mystic.Has(MinionSkill.Smith));
            Assert.Greater(mystic.Skills.Count, 2, "an Adult has room for more and learns it");
            var fam = SaveGame.LoadPet(new SaveData.Pet { id = "eel", name = "Eel", role = (int)CompanionRole.Familiar, maxHp = 10 });
            Assert.IsTrue(fam.Has(MinionSkill.Fighter));
            var minion = SaveGame.LoadPet(new SaveData.Pet { id = "eel", name = "Eel", level = 5, exp = 4, skills = new List<int> { (int)MinionSkill.Glutton }, maxHp = 10 });
            Assert.AreEqual(5, minion.Level);
            CollectionAssert.AreEqual(new[] { MinionSkill.Glutton }, minion.Skills, "a new save is loaded as it was");
        }

        [Test]
        public void SkillTreeShowsLearnedNextLaterAndLocked()
        {
            var c = Minion("eel", Rarity.Common, Element.Water);
            Assert.AreEqual(MinionRules.NodeState.Learned, MinionRules.StateOf(c, MinionSkill.Fighter, out _));
            Assert.AreEqual(MinionRules.NodeState.Next, MinionRules.StateOf(c, MinionSkill.Looter, out int at));
            Assert.AreEqual(3, at);
            Assert.AreEqual(MinionRules.NodeState.Locked, MinionRules.StateOf(c, MinionSkill.Tinker, out _), "Rare+ only");
            Assert.AreEqual(MinionRules.NodeState.Later, MinionRules.StateOf(c, MinionSkill.Hardy, out _), "opens at Adult");
            Assert.AreEqual(4, MinionRules.Families.Length);
            Assert.AreEqual(MinionRules.All.Length, MinionRules.Families.Sum(f => MinionRules.In(f).Count()));
        }
    }
}
