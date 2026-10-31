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
        public void DevSetLevelCoversRaftFiftyAndShipHundred()
        {
            var g = Raft();
            g.Kills = 3;
            g.DevSetLevel(RaftKind.Plain, 50);
            Assert.AreEqual(50, g.Level);
            Assert.AreEqual(0, g.Kills);
            Assert.IsTrue(g.CanChoose, "a Lv 50 boat with no line picked");
            g.DevSetLevel(RaftKind.Plain, 999);
            Assert.AreEqual(50, g.Level, "a raft stops at 50");
            g.DevSetLevel(RaftKind.Undersea, 100);
            Assert.AreEqual(100, g.Level);
            Assert.AreEqual(Rarity.Mythic, g.Tier);
            Assert.IsTrue(g.AtMax);
            g.DevSetLevel(RaftKind.Plain, 1);
            Assert.AreEqual(RaftKind.Plain, g.Chosen);
            Assert.AreEqual(Rarity.Common, g.Tier);
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
