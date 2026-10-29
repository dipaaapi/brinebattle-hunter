using System;
using System.Collections.Generic;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;

namespace BrineBattle.Companions
{
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

        /// <summary>Companions the raft can keep: 1 without habitats, +1 per habitat (habitats come later in Phase 7).</summary>
        public static int Capacity(int habitats) => 1 + Math.Max(0, habitats);

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
        public int MaxHp, Attack, Defense, Speed;

        public static Companion FromMonster(MonsterDefinition m) => new Companion
        {
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

    /// <summary>The hunter's companions. One is active and joins battles.</summary>
    public class CompanionRoster
    {
        readonly List<Companion> all = new List<Companion>();

        public event Action Changed;
        public int Habitats { get; set; }
        public int Capacity => CompanionRules.Capacity(Habitats);
        public IReadOnlyList<Companion> All => all;
        public Companion Active { get; private set; }
        public bool Full => all.Count >= Capacity;

        /// <summary>Adds a companion if there is room; the first one becomes active.</summary>
        public bool Add(Companion c)
        {
            if (c == null || Full) return false;
            all.Add(c);
            if (Active == null) Active = c;
            Changed?.Invoke();
            return true;
        }

        public bool SetActive(Companion c)
        {
            if (c != null && !all.Contains(c)) return false;
            Active = c;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Lets a companion go back to the sea.</summary>
        public bool Release(Companion c)
        {
            if (!all.Remove(c)) return false;
            if (Active == c) Active = all.Count > 0 ? all[0] : null;
            Changed?.Invoke();
            return true;
        }
    }
}
