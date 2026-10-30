using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class StatusTests
    {
        static Combatant Mon(int hp = 500, int atk = 6, StatusEffect inflicts = StatusEffect.None, float chance = 1f) => new Combatant
        {
            Name = "Eel", Element = Element.None, Habitat = Habitat.Sea, MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1,
            Inflicts = inflicts, InflictChance = chance,
        };

        static ItemDefinition Cure(StatusEffect s)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{s} cure";
            i.type = ItemType.Consumable;
            i.effect = ConsumableEffect.CureStatus;
            i.cures = s;
            return i;
        }

        [Test]
        public void PoisonHurtsEachRoundThenWearsOff()
        {
            var c = new Combatant { MaxHp = 60, Hp = 60 };
            StatusRules.Inflict(c, StatusEffect.Poison);
            int rounds = StatusRules.Rounds(StatusEffect.Poison);
            for (int i = 0; i < rounds; i++) StatusRules.Tick(c);
            Assert.AreEqual(60 - rounds * 3, c.Hp);
            Assert.AreEqual(StatusEffect.None, c.Status);
        }

        [Test]
        public void BleedHurtsMoreThanPoison()
        {
            Assert.Greater(StatusRules.TickDamage(StatusEffect.Bleed, 60), StatusRules.TickDamage(StatusEffect.Poison, 60));
            Assert.AreEqual(0, StatusRules.TickDamage(StatusEffect.Confusion, 60));
        }

        [Test]
        public void OutsideBattleTickNeverTakesTheLastHp()
        {
            var c = new Combatant { MaxHp = 60, Hp = 2 };
            StatusRules.Inflict(c, StatusEffect.Bleed);
            StatusRules.Tick(c, 1);
            StatusRules.Tick(c, 1);
            Assert.AreEqual(1, c.Hp);
        }

        [Test]
        public void MonsterHitGivesItsAilment()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(inflicts: StatusEffect.Poison), 9999, new System.Random(2));
            s.Hunter.MaxHp = s.Hunter.Hp = 9999;
            for (int i = 0; i < 10 && s.Hunter.Status == StatusEffect.None; i++) s.PlayRound(BattleAction.Brace);
            Assert.AreEqual(StatusEffect.Poison, s.Hunter.Status);
            Assert.IsTrue(s.Log.Any(l => l.Contains("poisoned")));
        }

        [Test]
        public void BlindBlocksSkills()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(atk: 0), 90, new System.Random(1));
            s.Actives.Add(GearActive.TidalStrike);
            for (int i = 0; i < 5; i++) s.PlayRound(BattleAction.Brace);   // build stamina
            Assert.IsTrue(s.CanUseSkill(GearActive.TidalStrike));
            StatusRules.Inflict(s.Hunter, StatusEffect.Blind);
            Assert.IsFalse(s.CanUseSkill(GearActive.TidalStrike));
        }

        [Test]
        public void ConfusedHunterSometimesHitsThemself()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(atk: 0), 9999, new System.Random(5));
            for (int i = 0; i < 20; i++) { StatusRules.Inflict(s.Hunter, StatusEffect.Confusion); s.PlayRound(BattleAction.Attack); }
            Assert.IsTrue(s.Log.Any(l => l.Contains("confused and hit yourself")));
        }

        [Test]
        public void CurePotionWorksOnlyForItsAilment()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(atk: 0), 90, new System.Random(1)) { Bag = new Inventory() };
            var antidote = Cure(StatusEffect.Poison);
            var cleanser = Cure(StatusEffect.Blind);
            s.Bag.Add(antidote, 1);
            s.Bag.Add(cleanser, 1);
            Assert.IsFalse(s.CanUseItem(antidote), "nothing to cure yet");
            StatusRules.Inflict(s.Hunter, StatusEffect.Poison);
            Assert.IsFalse(s.CanUseItem(cleanser));
            Assert.IsTrue(s.PlayItem(antidote));
            Assert.AreEqual(StatusEffect.None, s.Hunter.Status);
            Assert.AreEqual(0, s.Bag.Count(antidote));
        }

        [Test]
        public void AutoUsesTheRightCure()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(atk: 0), 90, new System.Random(1)) { Bag = new Inventory() };
            var antidote = Cure(StatusEffect.Poison);
            s.Bag.Add(antidote, 1);
            StatusRules.Inflict(s.Hunter, StatusEffect.Poison);
            Assert.AreEqual(antidote, BattleAI.ChooseItem(s, BattleMode.Auto));
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Manual));
        }

        [Test]
        public void PetKeepsItsHpBetweenBattles()
        {
            var m = ScriptableObject.CreateInstance<MonsterDefinition>();
            m.displayName = "eel";
            m.stats = new MonsterStats { hp = 30, attack = 6, defense = 2, speed = 5 };
            var pet = Companion.FromMonster(m);
            Assert.AreEqual(pet.MaxHp, pet.Hp);
            pet.Hp = 3;
            pet.Status = StatusEffect.Bleed;
            var body = Combatant.FromCompanion(pet);
            Assert.AreEqual(3, body.Hp);
            Assert.AreEqual(StatusEffect.Bleed, body.Status);
            pet.Hp = 0;
            Assert.IsTrue(pet.KnockedOut);
        }

        [Test]
        public void TagsNameEveryAilment()
        {
            foreach (StatusEffect s in System.Enum.GetValues(typeof(StatusEffect)))
                if (s != StatusEffect.None) Assert.IsNotEmpty(StatusRules.Tag(s), s.ToString());
        }
    }
}
