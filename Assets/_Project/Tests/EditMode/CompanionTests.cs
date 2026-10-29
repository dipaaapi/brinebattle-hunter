using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class CompanionTests
    {
        static MonsterDefinition Monster(string id = "eel", Rarity rarity = Rarity.Common)
        {
            var m = ScriptableObject.CreateInstance<MonsterDefinition>();
            m.id = id;
            m.displayName = id;
            m.rarity = rarity;
            m.stats = new MonsterStats { hp = 30, attack = 6, defense = 2, speed = 5 };
            return m;
        }

        static Combatant Target(int hp = 999, int atk = 0) =>
            new Combatant { Name = "Mon", MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1, Habitat = Habitat.Sea };

        [Test]
        public void TamedMonsterKeepsAShareOfItsStats()
        {
            var c = Companion.FromMonster(Monster());
            Assert.AreEqual(18, c.MaxHp);
            Assert.AreEqual(4, c.Attack);
            Assert.AreEqual(1, c.Defense);
            Assert.AreEqual(5, c.Speed, "speed is kept");
        }

        [Test]
        public void TameChanceDropsWithRarity()
        {
            for (var r = Rarity.Common; r < Rarity.Mythic; r++)
                Assert.Greater(CompanionRules.TameChance(r), CompanionRules.TameChance(r + 1));
        }

        [Test]
        public void RosterHasRoomForThreePlusHabitats()
        {
            var roster = new CompanionRoster();
            for (int i = 0; i < 3; i++) Assert.IsTrue(roster.Add(Companion.FromMonster(Monster($"m{i}"))));
            Assert.IsTrue(roster.Full);
            Assert.IsFalse(roster.Add(Companion.FromMonster(Monster("extra"))), "no habitat yet");
            roster.Habitats = 1;
            Assert.IsTrue(roster.Add(Companion.FromMonster(Monster("extra"))));
        }

        [Test]
        public void OnlyAFamiliarCanBeTheBattleCompanion()
        {
            var roster = new CompanionRoster();
            var crew = Companion.FromMonster(Monster("crab"), CompanionRole.Crew);
            var fam = Companion.FromMonster(Monster("gull"), CompanionRole.Familiar);
            roster.Add(crew);
            Assert.IsNull(roster.Active, "crew doesn't fight");
            Assert.IsFalse(roster.SetActive(crew));
            roster.Add(fam);
            Assert.AreEqual(fam, roster.Active);
            Assert.IsTrue(roster.Release(fam));
            Assert.IsNull(roster.Active, "no Familiar left");
            Assert.AreEqual(crew, roster.Best(CompanionRole.Crew));
            Assert.IsNull(roster.Best(CompanionRole.Mystic));
        }

        [Test]
        public void BestPicksTheRarestInARole()
        {
            var roster = new CompanionRoster();
            var common = Companion.FromMonster(Monster("a"), CompanionRole.Mystic);
            var rare = Companion.FromMonster(Monster("b", Rarity.Rare), CompanionRole.Mystic);
            roster.Add(common);
            roster.Add(rare);
            Assert.AreEqual(rare, roster.Best(CompanionRole.Mystic));
            Assert.Greater(CompanionRules.MysticExtraChance(Rarity.Rare), CompanionRules.MysticExtraChance(Rarity.Common));
        }

        [Test]
        public void CrewPatchesTheRaftEachRound()
        {
            var s = new BattleState(Combatant.Hunter(), Target(atk: 20), 100, new System.Random(1));
            s.CrewRepair = CompanionRules.CrewRaftRepair(Rarity.Common);
            for (int i = 0; i < 10 && s.Outcome == BattleOutcome.Ongoing; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsTrue(s.Log.Any(l => l.StartsWith("Your crew patches the raft")));
            Assert.IsTrue(s.Events.Any(e => e.Kind == BattleEventKind.Heal && e.To == BattleActor.Raft));
        }

        [Test]
        public void MysticMakesRefinesCheaperButNeverFree()
        {
            var gear = new Equipment();
            var inv = new Inventory();
            var spear = ScriptableObject.CreateInstance<ItemDefinition>();
            spear.type = ItemType.Weapon;
            spear.gearPower = 4;
            inv.Add(spear, 1);
            gear.Equip(spear, inv, Rarity.Common);
            Assert.AreEqual(2, gear.RefineCost(GearSlot.Weapon));
            gear.RefineDiscount = CompanionRules.MysticRefineDiscount;
            Assert.AreEqual(1, gear.RefineCost(GearSlot.Weapon));
            gear.RefineDiscount = 5;
            Assert.AreEqual(1, gear.RefineCost(GearSlot.Weapon), "never below 1");
        }

        [Test]
        public void CompanionAttacksEachRound()
        {
            var alone = new BattleState(Combatant.Hunter(), Target(), 90, new System.Random(1));
            var pair = new BattleState(Combatant.Hunter(), Target(), 90, new System.Random(1));
            pair.Companion = Combatant.FromCompanion(Companion.FromMonster(Monster()));
            for (int i = 0; i < 3; i++) { alone.PlayRound(BattleAction.Attack); pair.PlayRound(BattleAction.Attack); }
            Assert.Less(pair.Monster.Hp, alone.Monster.Hp);
            Assert.IsTrue(pair.Log.Any(l => l.StartsWith("eel attacks:")));
        }

        [Test]
        public void CompanionCanBeKnockedOutAndStopsAttacking()
        {
            var s = new BattleState(Combatant.Hunter(), Target(atk: 200), 9999, new System.Random(3));
            s.Hunter.MaxHp = s.Hunter.Hp = 99999;
            s.Companion = Combatant.FromCompanion(Companion.FromMonster(Monster()));
            for (int i = 0; i < 40 && s.Companion.Alive; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsFalse(s.Companion.Alive, "the monster went for it at some point");
            int before = s.Log.Count(l => l.StartsWith("eel attacks:"));
            s.PlayRound(BattleAction.Brace);
            Assert.AreEqual(before, s.Log.Count(l => l.StartsWith("eel attacks:")));
        }
    
        [Test]
        public void CompanionEvolvesAsItAgesThenDiesOfOldAge()
        {
            var c = Companion.FromMonster(Monster());
            int life = CompanionRules.Lifespan(Rarity.Common);
            int hp = c.MaxHp;
            var results = Enumerable.Range(0, life).Select(_ => c.GrowOlder()).ToList();
            Assert.AreEqual(2, results.Count(r => r == AgeResult.Evolved), "Young → Adult → Elder");
            Assert.AreEqual(GrowthStage.Elder, c.Stage);
            Assert.Greater(c.MaxHp, hp);
            Assert.AreEqual(AgeResult.Died, results.Last());
            Assert.AreEqual(1, results.Count(r => r == AgeResult.Died));
        }

        [Test]
        public void RarerCompanionsLiveLonger()
        {
            for (var r = Rarity.Common; r < Rarity.Mythic; r++)
                Assert.Less(CompanionRules.Lifespan(r), CompanionRules.Lifespan(r + 1));
        }

        [Test]
        public void DeadCompanionLeavesTheRosterAndIsRemembered()
        {
            var roster = new CompanionRoster();
            var old = Companion.FromMonster(Monster("old"));
            var young = Companion.FromMonster(Monster("young"));
            roster.Add(old);
            roster.Add(young);
            old.Age = old.Lifespan - 1;
            var changes = roster.GrowOlder();
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(AgeResult.Died, changes[0].Value);
            CollectionAssert.DoesNotContain(roster.All.ToList(), old);
            Assert.AreEqual(young, roster.Active, "the next Familiar takes over");
            Assert.AreEqual("old", roster.Memorials.Single().Id);
        }
    }
}
