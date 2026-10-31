using System;
using System.Collections.Generic;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;

namespace BrineBattle.Companions
{
    /// <summary>
    /// The three roles before minions (U-23). Ed's U-39 merged them into one kind of minion with skills;
    /// kept only to read older saves and memorials.
    /// </summary>
    public enum CompanionRole
    {
        /// <summary>Fights beside the hunter.</summary>
        Familiar = 0,
        /// <summary>Looks after the raft: repairs it in battle, mends the harpoon and modules between fights.</summary>
        Crew = 1,
        /// <summary>Helps with alchemy and crafting (not the raft): extra crafts, cheaper refines.</summary>
        Mystic = 2,
    }

    /// <summary>Taming and companion numbers (GDD §10). Plain C# for EditMode tests.</summary>
    public static class CompanionRules
    {
        /// <summary>A tamed monster keeps this share of its wild stats (it's weakened and still growing).</summary>
        public const float StatShare = 0.6f;
        /// <summary>Chance the monster goes for the companion instead of the hunter or raft.</summary>
        public const float TargetedChance = 0.25f;

        /// <summary>Tame chance by rarity: Common 90% … Mythic 15%.</summary>
        public static float TameChance(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 0.9f;
                case Rarity.Uncommon: return 0.75f;
                case Rarity.Rare: return 0.55f;
                case Rarity.Epic: return 0.4f;
                case Rarity.Legendary: return 0.25f;
                default: return 0.15f;
            }
        }

        /// <summary>Companions the raft can keep: 3 without habitats (room for one of each role), +1 per habitat (later in Phase 7).</summary>
        public static int Capacity(int habitats) => 3 + Math.Max(0, habitats);   // 3 without habitats, +1 per habitat on the raft

        /// <summary>Raft HP a Crewmate patches at the end of each battle round: rank 0 → 2, +2 per rank (rarity step + stage).</summary>
        public static int CrewRaftRepair(Rarity r) => CrewRaftRepair((int)r);
        public static int CrewRaftRepair(int rank) => 2 + 2 * rank;
        /// <summary>Seconds between a Crew companion's mending rounds outside battle (+1 durability to the harpoon and each module).</summary>
        public const float CrewMendSeconds = 15f;

        /// <summary>Chance a Mystic makes one extra of a gear, potion or oil craft: Common 20%, +5% per rarity step.</summary>
        public static float MysticExtraChance(Rarity r) => MysticExtraChance((int)r);
        public static float MysticExtraChance(int rank) => Math.Min(0.6f, 0.2f + 0.05f * rank);
        /// <summary>HP a Mystic heals the most-hurt ally (hunter or Familiar) at the end of each battle round: Common 3, +2 per rarity step.</summary>
        public static int MysticBattleHeal(Rarity r) => MysticBattleHeal((int)r);
        public static int MysticBattleHeal(int rank) => 3 + 2 * rank;

        /// <summary>Seconds between a Familiar's loot fetches (Ed's U-29): Common 20 s, 2 s less per rarity step (at least 8 s).</summary>
        public static float FetchSeconds(Rarity r) => FetchSeconds((int)r);
        public static float FetchSeconds(int rank) => Math.Max(6f, 20f - 2f * rank);
        /// <summary>How far past the raft's edge a Familiar swims to fetch floating loot, in metres.</summary>
        public const float FetchReach = 8f;

        /// <summary>Essence a Mystic saves on each refine (never below 1).</summary>
        public const int MysticRefineDiscount = 1;

        /// <summary>Skills an older save's role turns into (U-39).</summary>
        public static MinionSkill[] SkillsOf(CompanionRole role)
        {
            switch (role)
            {
                case CompanionRole.Crew: return new[] { MinionSkill.Crewmate, MinionSkill.Mender };
                case CompanionRole.Mystic: return new[] { MinionSkill.Alchemist, MinionSkill.Smith };
                default: return new[] { MinionSkill.Fighter, MinionSkill.Looter };
            }
        }

        public static int Share(int wild) => Math.Max(1, (int)Math.Round(wild * StatShare));

        // Old age (U-23) is switched off: Ed's U-32 replaced it with health. The numbers stay for older saves and tests.
        // Growth and old age (owner idea U-23, GDD §10). Age counts battles won until day/night exists (U-10).

        /// <summary>Battles a companion lives through before it dies of old age: Common 40, +20 per rarity step.</summary>
        public static int Lifespan(Rarity r) => 40 + 20 * (int)r;
        /// <summary>Age at which a companion reaches a stage: Adult at a quarter of its life, Elder at three quarters.</summary>
        public static int AgeFor(GrowthStage stage, Rarity r)
        {
            switch (stage)
            {
                case GrowthStage.Adult: return Lifespan(r) / 4;
                case GrowthStage.Elder: return Lifespan(r) * 3 / 4;
                default: return 0;
            }
        }
        /// <summary>Each evolution multiplies HP, attack and defense by this (speed +1).</summary>
        public const float EvolveBoost = 1.25f;
        /// <summary>Essence a companion leaves behind when it dies of old age: 1 + its rarity step, of its own rarity and element.</summary>
        public static int EssenceLeft(Rarity r) => 1 + (int)r;
    }

    /// <summary>Life stage of a companion. Each step up is an evolution with a stat boost.</summary>
    public enum GrowthStage { Young = 0, Adult = 1, Elder = 2 }

    /// <summary>What happened to a companion when it grew one battle older.</summary>
    public enum AgeResult { None, Evolved, Died }

    /// <summary>A companion that died of old age, remembered in the Book.</summary>
    public class Memorial
    {
        public string Id, Name;
        public Rarity Rarity;
        public CompanionRole Role;
        public int Age;
    }

    /// <summary>
    /// A tamed monster. It doesn't age (Ed's U-32): its HP and ailment carry over between battles,
    /// and it heals slowly on the raft (faster with habitats, C-10). At 0 HP it's knocked out until it heals.
    /// </summary>
    public class Companion
    {
        /// <summary>HP now (U-32). 0 = knocked out.</summary>
        public int Hp;
        public Battle.StatusEffect Status;
        public int StatusRounds;
        public bool KnockedOut => Hp <= 0;
        public string Id;
        public string Name;
        public Element Element;
        public Habitat Habitat;
        public Rarity Rarity;
        public int MaxHp, Attack, Defense, Speed;
        /// <summary>Minion level 1–30 (U-39), raised by eating loot.</summary>
        public int Level = 1;
        /// <summary>EXP toward the next level.</summary>
        public int Exp;
        /// <summary>Skills learned, in the order learned.</summary>
        public List<MinionSkill> Skills = new List<MinionSkill>();
        public bool Has(MinionSkill s) => Skills.Contains(s);
        public int Rank => MinionRules.Rank(this);
        /// <summary>Battles won since it was tamed.</summary>
        public int Age;
        public GrowthStage Stage;
        /// <summary>Feeding points toward the next evolution (<see cref="CompanionGrowth.GrowthToEvolve"/>).</summary>
        public int Growth;
        /// <summary>Potions infused so far (each gives a small permanent stat; capped by stage).</summary>
        public int Infusions;
        /// <summary>The passive kept from the last companion fused into this one ("Echo of …"), or null.</summary>
        public string Echo;
        /// <summary>Looks only (U-23 varieties): 0 Plain, 1 Scarf, 2 Bandana, 3 Crown.</summary>
        public int Look;

        public int Lifespan => CompanionRules.Lifespan(Rarity);
        public int AgeLeft => Math.Max(0, Lifespan - Age);
        public string StageName => Stage == GrowthStage.Young ? "Young" : Stage == GrowthStage.Adult ? "Adult" : "Elder";

        /// <summary>Grows one battle older: evolves when it reaches the next stage, dies at the end of its lifespan.</summary>
        public AgeResult GrowOlder()
        {
            Age++;
            if (Age >= Lifespan) return AgeResult.Died;
            if (Stage < GrowthStage.Elder && Age >= CompanionRules.AgeFor(Stage + 1, Rarity))
            {
                Evolve();
                return AgeResult.Evolved;
            }
            return AgeResult.None;
        }

        /// <summary>Moves up one stage with a stat boost (also used later by feeding and fusion).</summary>
        public bool Evolve()
        {
            if (Stage >= GrowthStage.Elder) return false;
            Stage++;
            int before = MaxHp;
            MaxHp = Boost(MaxHp);
            Hp = Math.Min(MaxHp, Hp + (MaxHp - before));
            Attack = Boost(Attack);
            Defense = Boost(Defense);
            Speed += 1;
            return true;
        }

        static int Boost(int v) => Math.Max(v + 1, (int)Math.Round(v * CompanionRules.EvolveBoost));

        /// <summary>
        /// Learns every skill its level and room allow (in <see cref="MinionRules.Order"/>), applying passives.
        /// Returns what it learned.
        /// </summary>
        public List<MinionSkill> CatchUp()
        {
            var learned = new List<MinionSkill>();
            while (Skills.Count < MinionRules.SlotsOpened(Level) && Skills.Count < MinionRules.MaxSkills(Rarity, Stage))
            {
                var next = MinionRules.NextSkill(this);
                if (next == null) break;
                Learn(next.Value);
                learned.Add(next.Value);
            }
            return learned;
        }

        /// <summary>Adds a skill (passives change stats once).</summary>
        public bool Learn(MinionSkill s)
        {
            if (Has(s)) return false;
            Skills.Add(s);
            if (s == MinionSkill.Hardy)
            {
                int before = MaxHp;
                MaxHp = Up(MaxHp);
                Hp = Math.Min(MaxHp, Hp + (MaxHp - before));
                Defense = Up(Defense);
            }
            else if (s == MinionSkill.Ferocious) { Attack = Up(Attack); Speed += 1; }
            return true;
        }

        static int Up(int v) => Math.Max(v + 1, (int)Math.Round(v * 1.2f));

        /// <summary>
        /// Adds EXP and levels up as far as it reaches: +2 HP each level, ATK and DEF in turn, +1 SPD every
        /// 5 levels; evolves at Lv 10 and 20; learns skills as slots open. Returns the levels gained.
        /// </summary>
        public int GainExp(int amount, List<MinionSkill> learned = null)
        {
            if (amount <= 0 || Level >= MinionRules.MaxLevel) return 0;
            Exp += amount;
            int gained = 0;
            while (Level < MinionRules.MaxLevel && Exp >= MinionRules.ExpToNext(Level))
            {
                Exp -= MinionRules.ExpToNext(Level);
                Level++;
                gained++;
                MaxHp += 2;
                Hp = Math.Min(MaxHp, Hp + 2);
                if (Level % 2 == 1) Attack += 1; else Defense += 1;
                if (Level % 5 == 0) Speed += 1;
                while (Stage < MinionRules.StageAt(Level)) Evolve();
                var got = CatchUp();
                learned?.AddRange(got);
            }
            if (Level >= MinionRules.MaxLevel) Exp = 0;
            return gained;
        }

        public static Companion FromMonster(MonsterDefinition m)
        {
            var c = new Companion
            {
            Id = m.id,
            Name = m.displayName,
            Element = m.element,
            Habitat = m.habitat,
            Rarity = m.rarity,
            MaxHp = CompanionRules.Share(m.stats.hp),
            Hp = CompanionRules.Share(m.stats.hp),
            Attack = CompanionRules.Share(m.stats.attack),
            Defense = CompanionRules.Share(m.stats.defense),
            Speed = m.stats.speed,
            };
            c.CatchUp();
            return c;
        }

        public string Stats => $"HP {MaxHp}  ATK {Attack}  DEF {Defense}  SPD {Speed}";
    }

    /// <summary>The hunter's companions. One Familiar is active and joins battles; Crew and Mystics always work.</summary>
    public class CompanionRoster
    {
        readonly List<Companion> all = new List<Companion>();
        readonly List<Memorial> memorials = new List<Memorial>();

        public event Action Changed;
        public int Habitats { get; set; }
        public int Capacity => CompanionRules.Capacity(Habitats);
        public IReadOnlyList<Companion> All => all;
        public Companion Active { get; private set; }
        public bool Full => all.Count >= Capacity;
        /// <summary>Companions that died of old age, oldest first.</summary>
        public IReadOnlyList<Memorial> Memorials => memorials;

        /// <summary>Adds a minion if there is room; the first Fighter becomes the battle minion.</summary>
        public bool Add(Companion c)
        {
            if (c == null || Full) return false;
            all.Add(c);
            if (Active == null && c.Has(MinionSkill.Fighter)) Active = c;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Only a minion with Fighter can be the battle minion.</summary>
        public bool SetActive(Companion c)
        {
            if (c != null && (!all.Contains(c) || !c.Has(MinionSkill.Fighter))) return false;
            Active = c;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Lets a companion go back to the sea.</summary>
        public bool Release(Companion c)
        {
            if (!all.Remove(c)) return false;
            if (Active == c) Active = all.Find(x => x.Has(MinionSkill.Fighter));
            Changed?.Invoke();
            return true;
        }

        /// <summary>Replaces the roster (loading a save). <paramref name="active"/> is an index into the list, -1 for none.</summary>
        public void Restore(IEnumerable<Companion> companions, int active, int habitats)
        {
            all.Clear();
            all.AddRange(companions);
            Habitats = habitats;
            Active = active >= 0 && active < all.Count && all[active].Has(MinionSkill.Fighter) ? all[active] : null;
            Changed?.Invoke();
        }

        /// <summary>
        /// Every companion grows one battle older (call after each battle won). Returns who evolved and who died;
        /// the dead leave the roster and get a memorial.
        /// </summary>
        public List<KeyValuePair<Companion, AgeResult>> GrowOlder()
        {
            var changes = new List<KeyValuePair<Companion, AgeResult>>();
            foreach (var c in all.ToArray())
            {
                var r = c.GrowOlder();
                if (r == AgeResult.None) continue;
                changes.Add(new KeyValuePair<Companion, AgeResult>(c, r));
                if (r == AgeResult.Died)
                {
                    memorials.Add(new Memorial { Id = c.Id, Name = c.Name, Rarity = c.Rarity, Age = c.Age });
                    all.Remove(c);
                    if (Active == c) Active = all.Find(x => x.Has(MinionSkill.Fighter));
                }
            }
            if (changes.Count > 0) Changed?.Invoke();
            return changes;
        }

        /// <summary>Replaces the memorials (loading a save).</summary>
        public void RestoreMemorials(IEnumerable<Memorial> list)
        {
            memorials.Clear();
            if (list != null) memorials.AddRange(list);
        }

        /// <summary>
        /// The minion that does a skill's job (the one whose bonus counts): highest rank, then level.
        /// Knocked-out minions rest and don't work.
        /// </summary>
        public Companion Best(MinionSkill skill)
        {
            Companion best = null;
            foreach (var c in all)
                if (c.Has(skill) && !c.KnockedOut && (best == null || c.Rank > best.Rank || (c.Rank == best.Rank && c.Level > best.Level))) best = c;
            return best;
        }

        /// <summary>After a minion learned a skill: makes the first Fighter the battle minion if there is none.</summary>
        public void Refresh()
        {
            if (Active == null || !all.Contains(Active)) Active = all.Find(x => x.Has(MinionSkill.Fighter));
            Changed?.Invoke();
        }
    }
}
