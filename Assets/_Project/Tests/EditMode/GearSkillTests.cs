using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class GearSkillTests
    {
        static ItemDefinition Gear(ItemType type, Rarity rarity, GearPassive[] passives = null, GearActive[] actives = null)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = $"{rarity} {type}";
            i.type = type;
            i.rarity = rarity;
            i.passives = passives ?? new GearPassive[0];
            i.actives = actives ?? new GearActive[0];
            return i;
        }

        static Combatant Mon(int hp = 999, int atk = 0, Habitat hab = Habitat.Sea) =>
            new Combatant { Name = "Mon", MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1, Habitat = hab };

        [TestCase(Rarity.Common, 0, 0)]
        [TestCase(Rarity.Uncommon, 0, 0)]
        [TestCase(Rarity.Rare, 1, 0)]
        [TestCase(Rarity.Epic, 1, 1)]
        [TestCase(Rarity.Legendary, 2, 2)]
        [TestCase(Rarity.Mythic, 3, 2)]
        public void SlotsFollowRarity(Rarity r, int passives, int actives)
        {
            Assert.AreEqual(passives, GearSkillRules.PassiveSlots(r));
            Assert.AreEqual(actives, GearSkillRules.ActiveSlots(r));
        }

        [Test]
        public void SkillsAreCappedByRarityAndDeduplicated()
        {
            var all = new[] { GearPassive.Leech, GearPassive.Leech, GearPassive.None, GearPassive.Thorns };
            var acts = new[] { GearActive.TidalStrike, GearActive.ShellGuard };
            CollectionAssert.IsEmpty(GearSkillRules.Passives(Gear(ItemType.Weapon, Rarity.Common, all, acts)));
            CollectionAssert.AreEqual(new[] { GearPassive.Leech }, GearSkillRules.Passives(Gear(ItemType.Weapon, Rarity.Rare, all, acts)).ToArray());
            CollectionAssert.IsEmpty(GearSkillRules.Actives(Gear(ItemType.Weapon, Rarity.Rare, all, acts)));
            CollectionAssert.AreEqual(new[] { GearActive.TidalStrike }, GearSkillRules.Actives(Gear(ItemType.Weapon, Rarity.Epic, all, acts)).ToArray());
            CollectionAssert.AreEqual(new[] { GearPassive.Leech, GearPassive.Thorns }, GearSkillRules.Passives(Gear(ItemType.Weapon, Rarity.Legendary, all, acts)).ToArray());
        }

        [Test]
        public void EquipmentCollectsSkillsFromBothSlots()
        {
            var inv = new Inventory();
            var gear = new Equipment();
            var weapon = Gear(ItemType.Weapon, Rarity.Epic, new[] { GearPassive.Leech }, new[] { GearActive.TidalStrike });
            var armor = Gear(ItemType.Armor, Rarity.Rare, new[] { GearPassive.Leech });
            inv.Add(weapon, 1);
            inv.Add(armor, 1);
            gear.Equip(weapon, inv, Rarity.Common);
            gear.Equip(armor, inv, Rarity.Common);
            CollectionAssert.AreEqual(new[] { GearPassive.Leech }, gear.Passives.ToArray(), "same passive twice doesn't stack");
            CollectionAssert.AreEqual(new[] { GearActive.TidalStrike }, gear.Actives.ToArray());
        }

        [Test]
        public void LeechHealsOnHit()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            s.Passives.Add(GearPassive.Leech);
            s.Hunter.Hp = 30;
            s.PlayRound(BattleAction.Attack);
            Assert.Greater(s.Hunter.Hp, 29, "healed more than the monster's 1-damage hit");
        }

        [Test]
        public void ThornsHurtsTheMonsterWhenItHitsYou()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 999, atk: 40, hab: Habitat.Sky), 90, new System.Random(5));
            s.Passives.Add(GearPassive.Thorns);
            for (int i = 0; i < 5; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsTrue(s.Log.Any(l => l.StartsWith("Thorns:")));
        }

        [Test]
        public void SecondWindAddsStamina()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            s.Passives.Add(GearPassive.SecondWind);
            s.PlayRound(BattleAction.Attack);
            Assert.AreEqual(BattleRules.StartStamina + BattleRules.StaminaPerRound + GearSkillRules.SecondWindStamina, s.Stamina);
        }

        [Test]
        public void ActivesCostStaminaAndNeedToBeEquipped()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            Assert.IsFalse(s.CanUseSkill(GearActive.ShellGuard), "not equipped");
            s.Actives.Add(GearActive.ShellGuard);
            s.Actives.Add(GearActive.TidalStrike);
            Assert.IsTrue(s.CanUseSkill(GearActive.ShellGuard));
            Assert.IsFalse(s.CanUseSkill(GearActive.TidalStrike), "6 stamina, starts with 4");
            Assert.IsTrue(s.PlaySkill(GearActive.ShellGuard));
            Assert.AreEqual(BattleRules.StartStamina - GearSkillRules.ShellGuardCost + BattleRules.StaminaPerRound, s.Stamina);
            Assert.AreEqual(s.RaftMaxHp, s.RaftHp, "the raft was shielded");
        }

        [Test]
        public void TidalStrikeHitsHarderThanAttack()
        {
            var a = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            var b = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            b.Actives.Add(GearActive.TidalStrike);
            for (int i = 0; i < 2; i++) { a.PlayRound(BattleAction.Brace); b.PlayRound(BattleAction.Brace); }
            a.PlayRound(BattleAction.Attack);
            Assert.IsTrue(b.PlaySkill(GearActive.TidalStrike));
            Assert.Less(b.Monster.Hp, a.Monster.Hp);
        }

        [Test]
        public void AutoPicksShellGuardWhenTheRaftIsLow()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(atk: 20), 100, new System.Random(1));
            s.Actives.Add(GearActive.ShellGuard);
            Assert.AreEqual(GearActive.None, BattleAI.ChooseSkill(s, BattleMode.Auto), "raft is fine");
            while (s.RaftHp > 35 && s.Outcome == BattleOutcome.Ongoing && s.Round < 100) s.PlayRound(BattleAction.Brace);
            Assume.That(s.Outcome, Is.EqualTo(BattleOutcome.Ongoing));
            if (s.Stamina >= GearSkillRules.ShellGuardCost)
                Assert.AreEqual(GearActive.ShellGuard, BattleAI.ChooseSkill(s, BattleMode.Auto));
            Assert.AreEqual(GearActive.None, BattleAI.ChooseSkill(s, BattleMode.SemiAuto), "Semi-auto leaves skills to the player");
        }
    }
}
