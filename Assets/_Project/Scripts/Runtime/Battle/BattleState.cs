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
        /// <summary>Structured version of the log for the battle visuals, in the order things happened.</summary>
        public readonly List<BattleEvent> Events = new List<BattleEvent>();

        /// <summary>The active companion (Phase 7). Null = fighting alone. It attacks after the hunter each round.</summary>
        public Combatant Companion { get; set; }
        /// <summary>Raft HP a Crew companion patches at the end of each round (Phase 7). 0 = no crew.</summary>
        public int CrewRepair { get; set; }
        /// <summary>HP a Mystic heals the most-hurt ally at the end of each round (Phase 7). 0 = no mystic.</summary>
        public int SupportHeal { get; set; }
        /// <summary>A boss fight (Ed's ask): it enrages at half HP and sends tidal waves (<see cref="BossFightRules"/>).</summary>
        public bool IsBoss { get; set; }
        /// <summary>Boss fights: damage the other Fighter minions add each round.</summary>
        public int AllyAttack { get; set; }
        public bool Enraged { get; private set; }
        int enragedRound;

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
            if (item == null || Bag == null || Bag.Count(item) <= 0) return false;
            if (item.effect == ConsumableEffect.CureStatus)
                return item.cures != StatusEffect.None && (Hunter.Status == item.cures || (Companion != null && Companion.Status == item.cures));
            if (item.effectAmount <= 0) return false;
            switch (item.effect)
            {
                case ConsumableEffect.HealHunter: return Hunter.Hp < Hunter.MaxHp;
                case ConsumableEffect.RepairRaft: return RaftHp < RaftMaxHp;
                case ConsumableEffect.CoatWeapon: return item.element != Element.None && AttackElement != item.element;
                case ConsumableEffect.HealCompanion: return Companion != null && Companion.Hp < Companion.MaxHp;
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
            skill != GearActive.None && Actives.Contains(skill) && Stamina >= GearSkillRules.Cost(skill) && Hunter.Status != StatusEffect.Blind;

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

            BossEffects();
            Hunter.Bracing = false;
            shellGuard = false;
            if (SupportHeal > 0 && Outcome == BattleOutcome.Ongoing) Support();
            if (CrewRepair > 0 && Outcome == BattleOutcome.Ongoing && RaftHp < RaftMaxHp)
            {
                int fixedHp = Math.Min(CrewRepair, RaftMaxHp - RaftHp);
                RaftHp += fixedHp;
                Add($"Your crewmate patches the raft: +{fixedHp} HP.");
                Emit(BattleEventKind.Heal, BattleActor.Raft, BattleActor.Raft, fixedHp, "Crewmate");
            }
            if (Outcome == BattleOutcome.Ongoing) TickAilments();
            if (CoatingRounds > 0 && --CoatingRounds == 0) Add($"The {Coating} oil wears off.");
            int regen = BattleRules.StaminaPerRound + (Passives.Contains(GearPassive.SecondWind) ? GearSkillRules.SecondWindStamina : 0);
            Stamina = Math.Min(BattleRules.MaxStamina, Stamina + regen);
            return true;
        }

        void HunterTurn(BattleAction action)
        {
            bool attacking = action == BattleAction.Attack || action == BattleAction.Harpoon || action == BattleAction.Skill;
            if (attacking && Hunter.Status == StatusEffect.Confusion && rng.NextDouble() < StatusRules.ConfusionSelfHit)
            {
                int self = HurtSelf(Hunter);
                Add($"You're confused and hit yourself: {self} damage.");
                Emit(BattleEventKind.Ailment, BattleActor.Hunter, BattleActor.Hunter, self, "Confused!");
                if (!Hunter.Alive) HunterDown();
                return;
            }
            switch (action)
            {
                case BattleAction.Brace:
                    Stamina = Math.Min(BattleRules.MaxStamina, Stamina + BattleRules.BraceStaminaBonus);
                    Add($"You brace yourself.");
                    Emit(BattleEventKind.Buff, BattleActor.Hunter, BattleActor.Hunter, 0, "Brace");
                    return;
                case BattleAction.Item:
                    UseItem(pendingItem);
                    return;
                case BattleAction.Skill:
                    Stamina -= GearSkillRules.Cost(pendingSkill);
                    if (pendingSkill == GearActive.TidalStrike) Hit(GearSkillRules.TidalStrikePower, "Tidal Strike", BattleEventKind.BigShot);
                    else
                    {
                        shellGuard = true;
                        Add("Shell Guard: the raft is shielded this round.");
                        Emit(BattleEventKind.Buff, BattleActor.Hunter, BattleActor.Raft, 0, "Shell Guard");
                    }
                    return;
                case BattleAction.Harpoon:
                    Stamina -= BattleRules.HarpoonCost;
                    Weapon?.Use(BattleRules.HarpoonWear);
                    Hit(BattleRules.HarpoonPower, "Harpoon", BattleEventKind.Shot);
                    return;
                default:
                    Hit(1f, "Attack", BattleEventKind.Strike);
                    return;
            }
        }

        /// <summary>Boss fights: the other minions strike, the boss enrages at half HP, then tidal waves hit the raft.</summary>
        void BossEffects()
        {
            if (!IsBoss || Outcome != BattleOutcome.Ongoing) return;
            if (AllyAttack > 0)
            {
                int dmg = Math.Min(AllyAttack, Monster.Hp);
                Monster.Hp -= dmg;
                Add($"Your minions strike {Monster.Name} together: {dmg} damage.");
                Emit(BattleEventKind.Shot, BattleActor.Raft, BattleActor.Monster, dmg, "Minions");
                if (!Monster.Alive)
                {
                    Outcome = BattleOutcome.Won;
                    Add($"{Monster.Name} is defeated!");
                    Emit(BattleEventKind.KnockedOut, BattleActor.Raft, BattleActor.Monster, 0);
                    return;
                }
            }
            if (BossFightRules.ShouldEnrage(Monster, Enraged))
            {
                Enraged = true;
                enragedRound = Round;
                Monster.Attack = Math.Max(Monster.Attack + 1, (int)Math.Round(Monster.Attack * BossFightRules.EnragedAttack));
                Add($"{Monster.Name} is ENRAGED! Its attacks hit harder.");
                Emit(BattleEventKind.Buff, BattleActor.Monster, BattleActor.Monster, 0, "ENRAGED!");
            }
            if (Enraged && BossFightRules.TidalRound(Round, enragedRound))
            {
                if (shellGuard)
                {
                    Add($"{Monster.Name} calls a tidal wave, but Shell Guard holds it off.");
                    Emit(BattleEventKind.Blocked, BattleActor.Monster, BattleActor.Raft, 0, "Blocked");
                    return;
                }
                int dmg = BossFightRules.TidalDamage(RaftMaxHp);
                if (Passives.Contains(GearPassive.Bulwark)) dmg = Math.Max(1, (int)Math.Round(dmg * GearSkillRules.BulwarkFactor));
                RaftHp = Math.Max(0, RaftHp - dmg);
                Add($"{Monster.Name} calls a TIDAL WAVE onto the raft: {dmg} damage.");
                Emit(BattleEventKind.BigShot, BattleActor.Monster, BattleActor.Raft, dmg, "Tidal Wave", Monster.Element);
                if (RaftHp == 0)
                {
                    Outcome = BattleOutcome.LostRaft;
                    Add("The raft breaks apart!");
                    Emit(BattleEventKind.KnockedOut, BattleActor.Monster, BattleActor.Raft, 0);
                }
            }
        }

        /// <summary>The Mystic heals whichever ally (hunter or Familiar) is lower on HP, if either is hurt.</summary>
        void Support()
        {
            float hunter = Hunter.Hp / (float)Hunter.MaxHp;
            bool pet = Companion != null && Companion.Alive && Companion.Hp < Companion.MaxHp
                && Companion.Hp / (float)Companion.MaxHp < hunter;
            var target = pet ? Companion : Hunter;
            if (target.Hp >= target.MaxHp) return;
            int heal = Math.Min(SupportHeal, target.MaxHp - target.Hp);
            target.Hp += heal;
            Add(pet ? $"Your alchemist heals {Companion.Name}: +{heal} HP." : $"Your alchemist heals you: +{heal} HP.");
            Emit(BattleEventKind.Heal, BattleActor.Raft, pet ? BattleActor.Companion : BattleActor.Hunter, heal, "Alchemist");
        }

        void CompanionTurn()
        {
            if (Companion == null || !Companion.Alive) return;
            if (Companion.Status == StatusEffect.Confusion && rng.NextDouble() < StatusRules.ConfusionSelfHit)
            {
                int self = HurtSelf(Companion);
                Add($"{Companion.Name} is confused and hurts itself: {self} damage.");
                Emit(BattleEventKind.Ailment, BattleActor.Companion, BattleActor.Companion, self, "Confused!");
                if (!Companion.Alive) PetDown();
                return;
            }
            if (Companion.Status == StatusEffect.Blind && rng.NextDouble() < StatusRules.BlindMiss)
            {
                Add($"{Companion.Name} can't see and misses.");
                Emit(BattleEventKind.Ailment, BattleActor.Companion, BattleActor.Monster, 0, "Miss!");
                return;
            }
            int dmg = BattleRules.Damage(Companion.Attack, Monster.Defense, 1f, Companion.Element, Monster.Element, false, rng);
            Monster.Hp = Math.Max(0, Monster.Hp - dmg);
            Add($"{Companion.Name} attacks: {dmg} damage to {Monster.Name}.");
            Emit(BattleEventKind.Strike, BattleActor.Companion, BattleActor.Monster, dmg, element: Companion.Element);
            if (!Monster.Alive)
            {
                Outcome = BattleOutcome.Won;
                Add($"{Monster.Name} is defeated!");
                Emit(BattleEventKind.KnockedOut, BattleActor.Companion, BattleActor.Monster, 0);
            }
        }

        void Hit(float power, string label, BattleEventKind kind)
        {
            if (Hunter.Status == StatusEffect.Blind && rng.NextDouble() < StatusRules.BlindMiss)
            {
                Add($"{label}: you can't see and miss.");
                Emit(BattleEventKind.Ailment, BattleActor.Hunter, BattleActor.Monster, 0, "Miss!");
                return;
            }
            int dmg = BattleRules.Damage(Hunter.Attack, Monster.Defense, power, AttackElement, Monster.Element, false, rng);
            Monster.Hp = Math.Max(0, Monster.Hp - dmg);
            Add($"{label}: {dmg} damage to {Monster.Name}.");
            Emit(kind, BattleActor.Hunter, BattleActor.Monster, dmg, element: AttackElement);
            if (Passives.Contains(GearPassive.Leech) && Hunter.Hp < Hunter.MaxHp)
            {
                int heal = Math.Min(Hunter.MaxHp - Hunter.Hp, Math.Max(1, (int)Math.Round(dmg * GearSkillRules.LeechShare)));
                Hunter.Hp += heal;
                Add($"Leech: you recover {heal} HP.");
                Emit(BattleEventKind.Heal, BattleActor.Hunter, BattleActor.Hunter, heal, "Leech");
            }
            if (!Monster.Alive)
            {
                Outcome = BattleOutcome.Won;
                Add($"{Monster.Name} is defeated!");
                Emit(BattleEventKind.KnockedOut, BattleActor.Hunter, BattleActor.Monster, 0);
            }
        }

        void UseItem(ItemDefinition item)
        {
            if (!CanUseItem(item)) { Add("Nothing happens."); return; }
            Bag.Remove(item, 1);
            if (item.effect == ConsumableEffect.CureStatus)
            {
                bool hunter = StatusRules.Cure(Hunter, item.cures);
                if (!hunter) StatusRules.Cure(Companion, item.cures);
                Add($"{item.displayName}: {(hunter ? "you are" : Companion.Name + " is")} no longer {StatusRules.Name(item.cures).ToLowerInvariant()}.");
                Emit(BattleEventKind.Heal, BattleActor.Hunter, hunter ? BattleActor.Hunter : BattleActor.Companion, 0, item.displayName);
            }
            else if (item.effect == ConsumableEffect.HealHunter)
            {
                int gained = Math.Min(item.effectAmount, Hunter.MaxHp - Hunter.Hp);
                Hunter.Hp += gained;
                Add($"{item.displayName}: you recover {gained} HP.");
                Emit(BattleEventKind.Heal, BattleActor.Hunter, BattleActor.Hunter, gained, item.displayName);
            }
            else if (item.effect == ConsumableEffect.HealCompanion)
            {
                bool woke = !Companion.Alive;
                int gained = Math.Min(item.effectAmount, Companion.MaxHp - Companion.Hp);
                Companion.Hp += gained;
                Add(woke ? $"{item.displayName}: {Companion.Name} wakes up with {Companion.Hp} HP!" : $"{item.displayName}: {Companion.Name} recovers {gained} HP.");
                Emit(BattleEventKind.Heal, BattleActor.Hunter, BattleActor.Companion, gained, item.displayName);
            }
            else if (item.effect == ConsumableEffect.CoatWeapon)
            {
                Coating = item.element;
                // +1 because the round it is applied in counts down too.
                CoatingRounds = item.effectAmount + 1;
                Add($"{item.displayName}: your attacks are {item.element} for {item.effectAmount} rounds.");
                Emit(BattleEventKind.Buff, BattleActor.Hunter, BattleActor.Hunter, 0, $"{item.element} oil", item.element);
            }
            else
            {
                int gained = Math.Min(item.effectAmount, RaftMaxHp - RaftHp);
                RaftHp += gained;
                Add($"{item.displayName}: the raft recovers {gained} HP.");
                Emit(BattleEventKind.Heal, BattleActor.Hunter, BattleActor.Raft, gained, item.displayName);
            }
        }

        void MonsterTurn()
        {
            if (Companion != null && Companion.Alive && rng.NextDouble() < CompanionRules.TargetedChance)
            {
                int hurt = BattleRules.Damage(Monster.Attack, Companion.Defense, 1f, Monster.Element, Companion.Element, false, rng);
                Companion.Hp = Math.Max(0, Companion.Hp - hurt);
                Add($"{Monster.Name} attacks {Companion.Name}: {hurt} damage.");
                Emit(BattleEventKind.Strike, BattleActor.Monster, BattleActor.Companion, hurt, element: Monster.Element);
                if (!Companion.Alive) PetDown();
                else TryInflict(Companion, BattleActor.Companion);
                return;
            }
            float raftChance = Monster.Habitat == Habitat.Sea ? BattleRules.SeaHitsRaftChance : 1f - BattleRules.SeaHitsRaftChance;
            bool hitsRaft = rng.NextDouble() < raftChance;
            int dmg = BattleRules.Damage(Monster.Attack, hitsRaft ? 0 : Hunter.Defense, 1f, Monster.Element, Hunter.Element, Hunter.Bracing, rng);
            string from = Monster.Habitat == Habitat.Sea ? "from below" : "from above";
            if (hitsRaft)
            {
                if (shellGuard)
                {
                    Add($"{Monster.Name} strikes the raft {from}, but Shell Guard blocks it.");
                    Emit(BattleEventKind.Blocked, BattleActor.Monster, BattleActor.Raft, 0, "Blocked");
                    return;
                }
                if (Passives.Contains(GearPassive.Bulwark)) dmg = Math.Max(1, (int)Math.Round(dmg * GearSkillRules.BulwarkFactor));
                RaftHp = Math.Max(0, RaftHp - dmg);
                Add($"{Monster.Name} strikes the raft {from}: {dmg} damage.");
                Emit(BattleEventKind.Strike, BattleActor.Monster, BattleActor.Raft, dmg, element: Monster.Element);
                if (RaftHp == 0)
                {
                    Outcome = BattleOutcome.LostRaft;
                    Add("The raft breaks apart!");
                    Emit(BattleEventKind.KnockedOut, BattleActor.Monster, BattleActor.Raft, 0);
                }
            }
            else
            {
                Hunter.Hp = Math.Max(0, Hunter.Hp - dmg);
                Add($"{Monster.Name} attacks you {from}: {dmg} damage.");
                Emit(BattleEventKind.Strike, BattleActor.Monster, BattleActor.Hunter, dmg, element: Monster.Element);
                if (!Hunter.Alive)
                {
                    HunterDown();
                    return;
                }
                TryInflict(Hunter, BattleActor.Hunter);
                if (Passives.Contains(GearPassive.Thorns))
                {
                    int back = Math.Max(1, (int)Math.Round(dmg * GearSkillRules.ThornsShare));
                    Monster.Hp = Math.Max(0, Monster.Hp - back);
                    Add($"Thorns: {back} damage back to {Monster.Name}.");
                    Emit(BattleEventKind.Recoil, BattleActor.Hunter, BattleActor.Monster, back, "Thorns");
                    if (!Monster.Alive)
                    {
                        Outcome = BattleOutcome.Won;
                        Add($"{Monster.Name} is defeated!");
                        Emit(BattleEventKind.KnockedOut, BattleActor.Hunter, BattleActor.Monster, 0);
                    }
                }
            }
        }

        /// <summary>The monster's hit may give its ailment (U-33).</summary>
        void TryInflict(Combatant target, BattleActor who)
        {
            if (Monster.Inflicts == StatusEffect.None || rng.NextDouble() >= Monster.InflictChance) return;
            StatusRules.Inflict(target, Monster.Inflicts);
            Add($"{(who == BattleActor.Hunter ? "You are" : target.Name + " is")} {StatusRules.Name(Monster.Inflicts).ToLowerInvariant()}!");
            Emit(BattleEventKind.Ailment, BattleActor.Monster, who, 0, StatusRules.Name(Monster.Inflicts) + "!");
        }

        /// <summary>End of a round: poison and bleed hurt the hunter and the companion, then count down.</summary>
        void TickAilments()
        {
            foreach (var (c, who) in new[] { (Hunter, BattleActor.Hunter), (Companion, BattleActor.Companion) })
            {
                if (c == null || !c.Alive || c.Status == StatusEffect.None) continue;
                var status = c.Status;
                int dmg = StatusRules.Tick(c);
                if (dmg > 0)
                {
                    Add($"{(who == BattleActor.Hunter ? "You lose" : c.Name + " loses")} {dmg} HP ({StatusRules.Name(status).ToLowerInvariant()}).");
                    Emit(BattleEventKind.Ailment, BattleActor.Monster, who, dmg, StatusRules.Name(status));
                }
                if (c.Status == StatusEffect.None) Add($"{(who == BattleActor.Hunter ? "You are" : c.Name + " is")} no longer {StatusRules.Name(status).ToLowerInvariant()}.");
                if (!c.Alive)
                {
                    if (who == BattleActor.Hunter) { HunterDown(); return; }
                    PetDown();
                }
            }
        }

        /// <summary>A confused hit on oneself: half a normal hit, no element.</summary>
        int HurtSelf(Combatant c)
        {
            int dmg = Math.Max(1, BattleRules.Damage(c.Attack, c.Defense, 0.5f, Element.None, Element.None, false, rng));
            c.Hp = Math.Max(0, c.Hp - dmg);
            return dmg;
        }

        void HunterDown()
        {
            Outcome = BattleOutcome.LostHunter;
            Add("You are knocked out!");
            Emit(BattleEventKind.KnockedOut, BattleActor.Monster, BattleActor.Hunter, 0);
        }

        void PetDown()
        {
            Add($"{Companion.Name} is knocked out and swims back to the raft.");
            Emit(BattleEventKind.KnockedOut, BattleActor.Monster, BattleActor.Companion, 0);
        }

        void Add(string line) => Log.Add(line);

        void Emit(BattleEventKind kind, BattleActor from, BattleActor to, int amount, string text = null, Element element = Element.None) =>
            Events.Add(new BattleEvent { Kind = kind, From = from, To = to, Amount = amount, Text = text, Element = element });
    }
}
