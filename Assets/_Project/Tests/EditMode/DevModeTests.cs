using BrineBattle.Battle;
using BrineBattle.Hunting;
using BrineBattle.Items;
using BrineBattle.Monsters;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;

namespace BrineBattle.Tests
{
    /// <summary>Developer mode hooks (Ed's ask): only in the Editor and Development Builds.</summary>
    public class DevModeTests
    {
        static RaftGrid Raft()
        {
            var plank = ScriptableObject.CreateInstance<ItemDefinition>();
            plank.type = ItemType.Raft;
            var g = new RaftGrid();
            g.FillSquare(plank, 1);
            return g;
        }

        [Test]
        public void DevModeExistsInTheEditor() => Assert.IsTrue(DevMode.Available);

        [Test]
        public void DevSetTierJumpsBothWaysAndResetsKills()
        {
            var g = Raft();
            g.Kills = 3;
            g.DevSetTier(Rarity.Epic);
            Assert.AreEqual(Rarity.Epic, g.Tier);
            Assert.AreEqual(0, g.Kills);
            Assert.IsTrue(g.CanChoose, "past the pick level with no type picked");
            g.DevSetTier(Rarity.Common);
            Assert.AreEqual(Rarity.Common, g.Tier);
            Assert.IsFalse(g.CanChoose);
        }

        [Test]
        public void DevFinishEndsTheBattleAndDevHealRefills()
        {
            var foe = new Combatant { Name = "Mon", MaxHp = 50, Hp = 50, Attack = 30, Speed = 1, Habitat = Habitat.Sea };
            var s = new BattleState(Combatant.Hunter(), foe, 100, new System.Random(1));
            s.PlayRound(BattleAction.Attack);
            s.DevHeal();
            Assert.AreEqual(s.Hunter.MaxHp, s.Hunter.Hp);
            Assert.AreEqual(s.RaftMaxHp, s.RaftHp);
            s.DevFinish(BattleOutcome.Won);
            Assert.AreEqual(BattleOutcome.Won, s.Outcome);
            Assert.AreEqual(0, s.Monster.Hp);
        }

        [Test]
        public void DevActionsMarkTheGame()
        {
            DevMode.Used = false;
            DevMode.Did("test");
            Assert.IsTrue(DevMode.Used);
            StringAssert.EndsWith("test", DevMode.Log[DevMode.Log.Count - 1]);
            DevMode.Used = false;
        }
    }
}
