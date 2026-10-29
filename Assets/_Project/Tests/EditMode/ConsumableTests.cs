using System;
using BrineBattle.Battle;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    public class ConsumableTests
    {
        static ItemDefinition Item(ConsumableEffect effect, int amount)
        {
            var i = ScriptableObject.CreateInstance<ItemDefinition>();
            i.displayName = effect.ToString();
            i.type = ItemType.Consumable;
            i.effect = effect;
            i.effectAmount = amount;
            return i;
        }

        static Combatant Mon(int atk = 0, Habitat hab = Habitat.Sea) =>
            new Combatant { Name = "Mon", MaxHp = 999, Hp = 999, Attack = atk, Defense = 0, Speed = 1, Habitat = hab };

        [Test]
        public void TonicHealsUpToMaxAndIsUsedUp()
        {
            var tonic = Item(ConsumableEffect.HealHunter, 25);
            var bag = new Inventory();
            bag.Add(tonic, 2);
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1)) { Bag = bag };
            Assert.IsFalse(s.CanUseItem(tonic), "no use at full HP");

            s.Hunter.Hp = 50;
            Assert.IsTrue(s.PlayItem(tonic));
            Assert.GreaterOrEqual(s.Hunter.Hp, s.Hunter.MaxHp - 1, "capped at max, then at most the monster's 1-damage hit");
            Assert.AreEqual(1, bag.Count(tonic));
            Assert.AreEqual(1, s.Round, "using an item takes the turn");
        }

        [Test]
        public void PatchRepairsTheRaft()
        {
            var patch = Item(ConsumableEffect.RepairRaft, 30);
            var bag = new Inventory();
            bag.Add(patch, 1);
            // A 0-attack sea monster chips the raft for 1 most rounds.
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1)) { Bag = bag };
            Assert.IsFalse(s.CanUseItem(patch), "raft is at full HP");
            while (s.RaftHp > 80 && s.Round < 200) s.PlayRound(BattleAction.Brace);
            Assert.LessOrEqual(s.RaftHp, 80);
            Assert.IsTrue(s.PlayItem(patch));
            Assert.GreaterOrEqual(s.RaftHp, s.RaftMaxHp - 1, "back to full, minus at most this round's hit");
            Assert.AreEqual(0, bag.Count(patch));
            Assert.IsFalse(s.PlayItem(patch), "none left");
        }

        [Test]
        public void ItemsNeedABagAndAnEffect()
        {
            var tonic = Item(ConsumableEffect.HealHunter, 25);
            var dud = Item(ConsumableEffect.None, 25);
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(1));
            s.Hunter.Hp = 10;
            Assert.IsFalse(s.CanUseItem(tonic), "no bag");
            s.Bag = new Inventory();
            s.Bag.Add(dud, 1);
            Assert.IsFalse(s.CanUseItem(dud), "no effect");
            Assert.IsFalse(s.CanUseItem(null));
        }

        [Test]
        public void AutoUsesTheItemForWhicheverSideIsLow()
        {
            var tonic = Item(ConsumableEffect.HealHunter, 25);
            var patch = Item(ConsumableEffect.RepairRaft, 30);
            var bag = new Inventory();
            bag.Add(tonic, 1);
            bag.Add(patch, 1);
            var s = new BattleState(Combatant.Hunter(), Mon(), 100, new System.Random(1)) { Bag = bag };

            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Auto), "nobody is low");
            s.Hunter.Hp = 10;
            Assert.AreEqual(tonic, BattleAI.ChooseItem(s, BattleMode.Auto));
            Assert.AreEqual(tonic, BattleAI.ChooseItem(s, BattleMode.SemiAuto));
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Manual), "Manual leaves it to the player");
            bag.Remove(tonic, 1);
            Assert.IsNull(BattleAI.ChooseItem(s, BattleMode.Auto), "no tonic left; raft is fine");
        }
    }
}
