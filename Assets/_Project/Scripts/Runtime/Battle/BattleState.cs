using System;
using System.Collections.Generic;
using BrineBattle.Monsters;

namespace BrineBattle.Battle
{
    /// <summary>A 1v1 battle: hunter on the raft against one monster (GDD §5). Plain C#.</summary>
    public class BattleState
    {
        public readonly Combatant Hunter;
        public readonly Combatant Monster;
        public int RaftMaxHp { get; }
        public int RaftHp { get; private set; }
        public int Stamina { get; private set; } = BattleRules.StartStamina;
        public int Round { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public readonly List<string> Log = new List<string>();

        readonly Random rng;

        public BattleState(Combatant hunter, Combatant monster, int raftHp, Random rng)
        {
            Hunter = hunter;
            Monster = monster;
            RaftMaxHp = RaftHp = Math.Max(1, raftHp);
            this.rng = rng;
        }

        public bool CanUse(BattleAction a) => a != BattleAction.Harpoon || Stamina >= BattleRules.HarpoonCost;

        /// <summary>Plays one round: both sides act in speed order. Returns false if the battle was already over.</summary>
        public bool PlayRound(BattleAction hunterAction)
        {
            if (Outcome != BattleOutcome.Ongoing) return false;
            if (!CanUse(hunterAction)) hunterAction = BattleAction.Attack;
            Round++;
            Hunter.Bracing = hunterAction == BattleAction.Brace;

            if (BattleRules.HunterFirst(Hunter, Monster))
            {
                HunterTurn(hunterAction);
                if (Outcome == BattleOutcome.Ongoing) MonsterTurn();
            }
            else
            {
                MonsterTurn();
                if (Outcome == BattleOutcome.Ongoing) HunterTurn(hunterAction);
            }

            Hunter.Bracing = false;
            Stamina = Math.Min(BattleRules.MaxStamina, Stamina + BattleRules.StaminaPerRound);
            return true;
        }

        void HunterTurn(BattleAction action)
        {
            switch (action)
            {
                case BattleAction.Brace:
                    Stamina = Math.Min(BattleRules.MaxStamina, Stamina + BattleRules.BraceStaminaBonus);
                    Add($"You brace yourself.");
                    return;
                case BattleAction.Harpoon:
                    Stamina -= BattleRules.HarpoonCost;
                    Hit(BattleRules.HarpoonPower, "Harpoon");
                    return;
                default:
                    Hit(1f, "Attack");
                    return;
            }
        }

        void Hit(float power, string label)
        {
            int dmg = BattleRules.Damage(Hunter.Attack, Monster.Defense, power, Hunter.Element, Monster.Element, false, rng);
            Monster.Hp = Math.Max(0, Monster.Hp - dmg);
            Add($"{label}: {dmg} damage to {Monster.Name}.");
            if (!Monster.Alive)
            {
                Outcome = BattleOutcome.Won;
                Add($"{Monster.Name} is defeated!");
            }
        }

        void MonsterTurn()
        {
            float raftChance = Monster.Habitat == Habitat.Sea ? BattleRules.SeaHitsRaftChance : 1f - BattleRules.SeaHitsRaftChance;
            bool hitsRaft = rng.NextDouble() < raftChance;
            int dmg = BattleRules.Damage(Monster.Attack, hitsRaft ? 0 : Hunter.Defense, 1f, Monster.Element, Hunter.Element, Hunter.Bracing, rng);
            string from = Monster.Habitat == Habitat.Sea ? "from below" : "from above";
            if (hitsRaft)
            {
                RaftHp = Math.Max(0, RaftHp - dmg);
                Add($"{Monster.Name} strikes the raft {from}: {dmg} damage.");
                if (RaftHp == 0) { Outcome = BattleOutcome.LostRaft; Add("The raft breaks apart!"); }
            }
            else
            {
                Hunter.Hp = Math.Max(0, Hunter.Hp - dmg);
                Add($"{Monster.Name} attacks you {from}: {dmg} damage.");
                if (!Hunter.Alive) { Outcome = BattleOutcome.LostHunter; Add("You are knocked out!"); }
            }
        }

        void Add(string line) => Log.Add(line);
    }
}
