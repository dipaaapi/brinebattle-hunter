using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Companions;
using BrineBattle.Monsters;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class BattleEventTests
    {
        static Combatant Mon(int hp = 999, int atk = 5) =>
            new Combatant { Name = "Mon", MaxHp = hp, Hp = hp, Attack = atk, Defense = 0, Speed = 1, Habitat = Habitat.Sea };

        [Test]
        public void EveryDamageLineHasAnEvent()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(), 90, new System.Random(2));
            s.Companion = new Combatant { Name = "Pet", MaxHp = 10, Hp = 10, Attack = 4, Speed = 3 };
            for (int i = 0; i < 6; i++) s.PlayRound(i % 2 == 0 ? BattleAction.Attack : BattleAction.Brace);
            int damageLines = s.Log.Count(l => l.Contains(" damage"));
            int damageEvents = s.Events.Count(e => e.Amount > 0 && e.Kind != BattleEventKind.Heal);
            Assert.AreEqual(damageLines, damageEvents);
            Assert.IsTrue(s.Events.Any(e => e.From == BattleActor.Companion && e.To == BattleActor.Monster));
            Assert.IsTrue(s.Events.Any(e => e.From == BattleActor.Monster));
            Assert.IsTrue(s.Events.Any(e => e.Kind == BattleEventKind.Buff && e.Text == "Brace"));
        }

        [Test]
        public void HarpoonIsAShotAndAKillIsMarked()
        {
            var s = new BattleState(Combatant.Hunter(), Mon(hp: 5, atk: 0), 90, new System.Random(1)) { FirstRound = Initiative.Hunter };
            s.PlayRound(BattleAction.Brace);
            s.PlayRound(BattleAction.Harpoon);
            Assert.AreEqual(BattleOutcome.Won, s.Outcome);
            Assert.IsTrue(s.Events.Any(e => e.Kind == BattleEventKind.Shot && e.To == BattleActor.Monster));
            Assert.AreEqual(BattleEventKind.KnockedOut, s.Events.Last().Kind);
            Assert.AreEqual(BattleActor.Monster, s.Events.Last().To);
        }
    }
}
