using System;
using System.Collections.Generic;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;

namespace BrineBattle.Companions
{
    /// <summary>What a companion does (owner idea U-23, GDD §10). Picked by the player when taming.</summary>
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
        public static int Capacity(int habitats) => 3 + Math.Max(0, habitats);

        /// <summary>Raft HP a Crew companion patches at the end of each battle round: Common 2, +2 per rarity step.</summary>
        public static int CrewRaftRepair(Rarity r) => 2 + 2 * (int)r;
        /// <summary>Seconds between a Crew companion's mending rounds outside battle (+1 durability to the harpoon and each module).</summary>
        public const float CrewMendSeconds = 15f;

        /// <summary>Chance a Mystic makes one extra of a gear, potion or oil craft: Common 20%, +5% per rarity step.</summary>
        public static float MysticExtraChance(Rarity r) => 0.2f + 0.05f * (int)r;
        /// <summary>Essence a Mystic saves on each refine (never below 1).</summary>
        public const int MysticRefineDiscount = 1;

        public static string Describe(CompanionRole role)
        {
            switch (role)
            {
                case CompanionRole.Crew: return "walks the raft: patches it in battle, mends the harpoon and modules, and builds raft parts from your bag onto it";
                case CompanionRole.Mystic: return "works the workbench: refines your gear with essence, may craft an extra gear, potion or oil, and refines cost less";
                default: return "fights beside you";
            }
        }

        public static int Share(int wild) => Math.Max(1, (int)Math.Round(wild * StatShare));
    }

    /// <summary>A tamed monster. HP is refilled before each battle (it rests on the raft).</summary>
    public class Companion
    {
        public string Id;
        public string Name;
        public Element Element;
        public Habitat Habitat;
        public Rarity Rarity;
        public CompanionRole Role;
        public int MaxHp, Attack, Defense, Speed;

        public static Companion FromMonster(MonsterDefinition m, CompanionRole role = CompanionRole.Familiar) => new Companion
        {
            Role = role,
            Id = m.id,
            Name = m.displayName,
            Element = m.element,
            Habitat = m.habitat,
            Rarity = m.rarity,
            MaxHp = CompanionRules.Share(m.stats.hp),
            Attack = CompanionRules.Share(m.stats.attack),
            Defense = CompanionRules.Share(m.stats.defense),
            Speed = m.stats.speed,
        };

        public string Stats => $"HP {MaxHp}  ATK {Attack}  DEF {Defense}  SPD {Speed}";
    }

    /// <summary>The hunter's companions. One Familiar is active and joins battles; Crew and Mystics always work.</summary>
    public class CompanionRoster
    {
        readonly List<Companion> all = new List<Companion>();

        public event Action Changed;
        public int Habitats { get; set; }
        public int Capacity => CompanionRules.Capacity(Habitats);
        public IReadOnlyList<Companion> All => all;
        public Companion Active { get; private set; }
        public bool Full => all.Count >= Capacity;

        /// <summary>Adds a companion if there is room; the first Familiar becomes active.</summary>
        public bool Add(Companion c)
        {
            if (c == null || Full) return false;
            all.Add(c);
            if (Active == null && c.Role == CompanionRole.Familiar) Active = c;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Only a Familiar can be the battle companion.</summary>
        public bool SetActive(Companion c)
        {
            if (c != null && (!all.Contains(c) || c.Role != CompanionRole.Familiar)) return false;
            Active = c;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Lets a companion go back to the sea.</summary>
        public bool Release(Companion c)
        {
            if (!all.Remove(c)) return false;
            if (Active == c) Active = all.Find(x => x.Role == CompanionRole.Familiar);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Replaces the roster (loading a save). <paramref name="active"/> is an index into the list, -1 for none.</summary>
        public void Restore(IEnumerable<Companion> companions, int active, int habitats)
        {
            all.Clear();
            all.AddRange(companions);
            Habitats = habitats;
            Active = active >= 0 && active < all.Count && all[active].Role == CompanionRole.Familiar ? all[active] : null;
            Changed?.Invoke();
        }

        /// <summary>The highest-rarity companion in a role (the one whose bonus counts), or null.</summary>
        public Companion Best(CompanionRole role)
        {
            Companion best = null;
            foreach (var c in all)
                if (c.Role == role && (best == null || c.Rarity > best.Rarity)) best = c;
            return best;
        }
    }
}
