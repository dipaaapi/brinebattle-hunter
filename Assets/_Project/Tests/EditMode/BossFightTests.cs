using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Items;
using BrineBattle.Monsters;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    /// <summary>Ed's grand boss fights: enrage, tidal waves, minions helping, lucky treasure.</summary>
    public class BossFightTests
    {
        static Combatant Boss(int hp = 100, int atk = 10) =>
            new Combatant { Name = "Boss", MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1, Habitat = Habitat.Sea };

        static Companion Minion(string name, params MinionSkill[] skills)
        {
            var c = new Companion { Id = name, Name = name, MaxHp = 20, Hp = 20, Attack = 9 };
            c.Skills.AddRange(skills);
            return c;
        }

        [Test]
        public void EnragesOnceAtHalfHp()
        {
            var b = Boss();
            Assert.IsFalse(BossFightRules.ShouldEnrage(b, false));
            b.Hp = 50;
            Assert.IsTrue(BossFightRules.ShouldEnrage(b, false));
            Assert.IsFalse(BossFightRules.ShouldEnrage(b, true), "only once");
        }

        [Test]
        public void TidalWavesComeEveryThreeRoundsAfterTheEnrage()
        {
            Assert.IsFalse(BossFightRules.TidalRound(5, 0), "not enraged");
            Assert.IsFalse(BossFightRules.TidalRound(4, 4));
            Assert.IsTrue(BossFightRules.TidalRound(7, 4));
            Assert.IsTrue(BossFightRules.TidalRound(10, 4));
            Assert.AreEqual(12, BossFightRules.TidalDamage(100));
        }

        [Test]
        public void OtherFightersChipInAndLootersBringLuck()
        {
            var active = Minion("a", MinionSkill.Fighter);
            var other = Minion("b", MinionSkill.Fighter, MinionSkill.Looter);
            var crew = Minion("c", MinionSkill.Crewmate, MinionSkill.Looter);
            var all = new[] { active, other, crew };
            Assert.AreEqual(3, BossFightRules.AllyStrike(all, active), "only the other Fighter: 9 / 3");
            Assert.AreEqual(2, BossFightRules.LuckyRolls(all));
            other.Hp = 0;
            Assert.AreEqual(0, BossFightRules.AllyStrike(all, active), "knocked-out minions don't help");
            Assert.AreEqual(1, BossFightRules.LuckyRolls(all));
            Assert.Greater(BossFightRules.TreasureEssence(Rarity.Epic, 1), BossFightRules.TreasureEssence(Rarity.Common, 0));
            var lines = BossFightRules.Contributions(all, active);
            Assert.IsTrue(lines.Any(l => l.StartsWith("c:") && l.Contains("patches the raft")));
        }

        [Test]
        public void ABossBattleEnragesAndSendsATidalWave()
        {
            var hunter = Combatant.Hunter();
            hunter.MaxHp = hunter.Hp = 999;
            hunter.Attack = 30;
            var s = new BattleState(hunter, Boss(hp: 200, atk: 1), 500, new System.Random(3)) { IsBoss = true, AllyAttack = 5 };
            int atk = s.Monster.Attack;
            for (int i = 0; i < 40 && s.Outcome == BattleOutcome.Ongoing && !s.Log.Any(l => l.Contains("TIDAL WAVE")); i++) s.PlayRound(BattleAction.Brace);
            Assert.IsTrue(s.Log.Any(l => l.StartsWith("Your minions strike Boss together")));
            Assert.IsTrue(s.Enraged);
            Assert.Greater(s.Monster.Attack, atk);
            Assert.IsTrue(s.Log.Any(l => l.Contains("TIDAL WAVE")), "a wave hits after enraging");
            Assert.IsTrue(s.Events.Any(e => e.Kind == BattleEventKind.BigShot && e.Text == "Tidal Wave"));
        }

        [Test]
        public void NormalBattlesHaveNoBossEffects()
        {
            var s = new BattleState(Combatant.Hunter(), Boss(hp: 60, atk: 1), 500, new System.Random(3)) { AllyAttack = 5 };
            for (int i = 0; i < 10 && s.Outcome == BattleOutcome.Ongoing; i++) s.PlayRound(BattleAction.Brace);
            Assert.IsFalse(s.Enraged);
            Assert.IsFalse(s.Log.Any(l => l.Contains("minions strike")));
        }
    }
}
