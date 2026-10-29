using System;
using System.Collections.Generic;
using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
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
        public Initiative FirstRound { get; set; } = Initiative.Speed;
        /// <summary>Harpoon durability (idea C-4). Null = no wear.</summary>
        public Durability Weapon { get; set; }
        /// <summary>The hunter's bag, for consumables. Null = no items in this battle.</summary>
        public Inventory Bag { get; set; }
        /// <summary>Gear passives and actives in this battle (GDD §9). Empty = none.</summary>
        public HashSet<GearPassive> Passives { get; } = new HashSet<GearPassive>();
        public List<GearActive> Actives { get; } = new List<GearActive>();
        public readonly List<string> Log = new List<string>();

        /// <summary>The active companion (Phase 7). Null = fighting alone. It attacks after the hunter each round.</summary>
        public Combatant Companion { get; set; }

        /// <summary>Element of an elemental oil on the weapon (None = no coating) and the rounds it has left.</summary>
        public Element Coating { get; private set; }
        public int CoatingRounds { get; private set; }

        /// <summary>The element the hunter's attacks use: the oil while it lasts, otherwise the weapon's.</summary>
        public Element AttackElement => CoatingRounds > 0 ? Coating : Hunter.Element;

        readonly Random rng;
        ItemDefinition pendingItem;
        GearActive pendingSkill;
        bool shellGuard;

        public BattleState(Combatant hunter, Combatant monster, int raftHp, Random rng)
        {
            Hunter = hunter;
            Monster = monster;
            RaftMaxHp = RaftHp = Math.Max(1, raftHp);
            this.rng = rng;
        }

        public bool CanUse(BattleAction a) =>
            a == BattleAction.Item ? CanUseItem(pendingItem)
            : a == BattleAction.Skill ? CanUseSkill(pendingSkill)
            : a != BattleAction.Harpoon || (Stamina >= BattleRules.HarpoonCost && (Weapon == null || !Weapon.Broken));

        /// <summary>A consumable in the bag whose effect would do something right now (no potions at full HP).</summary>
        public bool CanUseItem(ItemDefinition item)
        {
            if (item == null || Bag == null || Bag.Count(item) <= 0 || item.effectAmount <= 0) return false;
            switch (item.effect)
            {
                case ConsumableEffect.HealHunter: return Hunter.Hp < Hunter.MaxHp;
                case ConsumableEffect.RepairRaft: return RaftHp < RaftMaxHp;
                case ConsumableEffect.CoatWeapon: return item.element != Element.None && AttackElement != item.element;
                default: return false;
            }
        }

        /// <summary>Plays one round where the hunter uses <paramref name="item"/>. Returns false (and plays nothing) if it can't be used.</summary>
        public bool PlayItem(ItemDefinition item)
        {
            if (Outcome != BattleOutcome.Ongoing || !CanUseItem(item)) return false;
            pendingItem = item;
            PlayRound(BattleAction.Item);
            pendingItem = null;
            return true;
        }

        public bool CanUseSkill(GearActive skill) =>
            skill != GearActive.None && Actives.Contains(skill) && Stamina >= GearSkillRules.Cost(skill);

        /// <summary>Plays one round where the hunter uses an active gear skill. Returns false (and plays nothing) if it can't.</summary>
        public bool PlaySkill(GearActive skill)
        {
            if (Outcome != BattleOutcome.Ongoing || !CanUseSkill(skill)) return false;
            pendingSkill = skill;
            PlayRound(BattleAction.Skill);
            pendingSkill = GearActive.None;
            return true;
        }

        /// <summary>Plays one round: both sides act in speed order. Returns false if the battle was already over.</summary>
        public bool PlayRound(BattleAction hunterAction)
        {
            if (Outcome != BattleOutcome.Ongoing) return false;
            if (!CanUse(hunterAction)) hunterAction = BattleAction.Attack;
            Round++;
            Hunter.Bracing = hunterAction == BattleAction.Brace;

            bool hunterFirst = Round == 1 && FirstRound != Initiative.Speed
                ? FirstRound == Initiative.Hunter
                : BattleRules.HunterFirst(Hunter, Monster);
            if (hunterFirst)
            {
                HunterTurn(hunterAction);
                if (Outcome == BattleOutcome.Ongoing) CompanionTurn();
                if (Outcome == BattleOutcome.Ongoing) MonsterTurn();
            }
            else
            {
                MonsterTurn();
                if (Outcome == BattleOutcome.Ongoing) HunterTurn(hunterAction);
                if (Outcome == BattleOutcome.Ongoing) CompanionTurn();
            }

            Hunter.Bracing = false;
            shellGuard = false;
            if (CoatingRounds > 0 && --CoatingRounds == 0) Add($"The {Coating} oil wears off.");
            int regen = BattleRules.StaminaPerRound + (Passives.Contains(GearPassive.SecondWind) ? GearSkillRules.SecondWindStamina : 0);
            Stamina = Math.Min(BattleRules.MaxStamina, Stamina + regen);
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
                case BattleAction.Item:
                    UseItem(pendingItem);
                    return;
                case BattleAction.Skill:
                    Stamina -= GearSkillRules.Cost(pendingSkill);
                    if (pendingSkill == GearActive.TidalStrike) Hit(GearSkillRules.TidalStrikePower, "Tidal Strike");
                    else { shellGuard = true; Add("Shell Guard: the raft is shielded this round."); }
                    return;
                case BattleAction.Harpoon:
                    Stamina -= BattleRules.HarpoonCost;
                    Weapon?.Use(BattleRules.HarpoonWear);
                    Hit(BattleRules.HarpoonPower, "Harpoon");
                    return;
                default:
                    Hit(1f, "Attack");
                    return;
            }
        }

        void CompanionTurn()
        {
            if (Companion == null || !Companion.Alive) return;
            int dmg = BattleRules.Damage(Companion.Attack, Monster.Defense, 1f, Companion.Element, Monster.Element, false, rng);
            Monster.Hp = Math.Max(0, Monster.Hp - dmg);
            Add($"{Companion.Name} attacks: {dmg} damage to {Monster.Name}.");
            if (!Monster.Alive)
            {
                Outcome = BattleOutcome.Won;
                Add($"{Monster.Name} is defeated!");
            }
        }

        void Hit(float power, string label)
        {
            int dmg = BattleRules.Damage(Hunter.Attack, Monster.Defense, power, AttackElement, Monster.Element, false, rng);
            Monster.Hp = Math.Max(0, Monster.Hp - dmg);
            Add($"{label}: {dmg} damage to {Monster.Name}.");
            if (Passives.Contains(GearPassive.Leech) && Hunter.Hp < Hunter.MaxHp)
            {
                int heal = Math.Min(Hunter.MaxHp - Hunter.Hp, Math.Max(1, (int)Math.Round(dmg * GearSkillRules.LeechShare)));
                Hunter.Hp += heal;
                Add($"Leech: you recover {heal} HP.");
            }
            if (!Monster.Alive)
            {
                Outcome = BattleOutcome.Won;
                Add($"{Monster.Name} is defeated!");
            }
        }

        void UseItem(ItemDefinition item)
        {
            if (!CanUseItem(item)) { Add("Nothing happens."); return; }
            Bag.Remove(item, 1);
            if (item.effect == ConsumableEffect.HealHunter)
            {
                int gained = Math.Min(item.effectAmount, Hunter.MaxHp - Hunter.Hp);
                Hunter.Hp += gained;
                Add($"{item.displayName}: you recover {gained} HP.");
            }
            else if (item.effect == ConsumableEffect.CoatWeapon)
            {
                Coating = item.element;
                // +1 because the round it is applied in counts down too.
                CoatingRounds = item.effectAmount + 1;
                Add($"{item.displayName}: your attacks are {item.element} for {item.effectAmount} rounds.");
            }
            else
            {
                int gained = Math.Min(item.effectAmount, RaftMaxHp - RaftHp);
                RaftHp += gained;
                Add($"{item.displayName}: the raft recovers {gained} HP.");
            }
        }

        void MonsterTurn()
        {
            if (Companion != null && Companion.Alive && rng.NextDouble() < CompanionRules.TargetedChance)
            {
                int hurt = BattleRules.Damage(Monster.Attack, Companion.Defense, 1f, Monster.Element, Companion.Element, false, rng);
                Companion.Hp = Math.Max(0, Companion.Hp - hurt);
                Add($"{Monster.Name} attacks {Companion.Name}: {hurt} damage.");
                if (!Companion.Alive) Add($"{Companion.Name} is knocked out and swims back to the raft.");
                return;
            }
            float raftChance = Monster.Habitat == Habitat.Sea ? BattleRules.SeaHitsRaftChance : 1f - BattleRules.SeaHitsRaftChance;
            bool hitsRaft = rng.NextDouble() < raftChance;
            int dmg = BattleRules.Damage(Monster.Attack, hitsRaft ? 0 : Hunter.Defense, 1f, Monster.Element, Hunter.Element, Hunter.Bracing, rng);
            string from = Monster.Habitat == Habitat.Sea ? "from below" : "from above";
            if (hitsRaft)
            {
                if (shellGuard) { Add($"{Monster.Name} strikes the raft {from}, but Shell Guard blocks it."); return; }
                if (Passives.Contains(GearPassive.Bulwark)) dmg = Math.Max(1, (int)Math.Round(dmg * GearSkillRules.BulwarkFactor));
                RaftHp = Math.Max(0, RaftHp - dmg);
                Add($"{Monster.Name} strikes the raft {from}: {dmg} damage.");
                if (RaftHp == 0) { Outcome = BattleOutcome.LostRaft; Add("The raft breaks apart!"); }
            }
            else
            {
                Hunter.Hp = Math.Max(0, Hunter.Hp - dmg);
                Add($"{Monster.Name} attacks you {from}: {dmg} damage.");
                if (!Hunter.Alive) { Outcome = BattleOutcome.LostHunter; Add("You are knocked out!"); return; }
                if (Passives.Contains(GearPassive.Thorns))
                {
                    int back = Math.Max(1, (int)Math.Round(dmg * GearSkillRules.ThornsShare));
                    Monster.Hp = Math.Max(0, Monster.Hp - back);
                    Add($"Thorns: {back} damage back to {Monster.Name}.");
                    if (!Monster.Alive) { Outcome = BattleOutcome.Won; Add($"{Monster.Name} is defeated!"); }
                }
            }
        }

        void Add(string line) => Log.Add(line);
    }
}
