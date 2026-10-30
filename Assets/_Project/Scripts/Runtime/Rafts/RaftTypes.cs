using System;
using System.Collections.Generic;
using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>What kind of raft a part builds (GDD §6). Plain parts (planks) count toward none.</summary>
    public enum RaftKind
    {
        Plain = 0,
        /// <summary>Shells and scales: slow but sturdy.</summary>
        Fortress = 1,
        /// <summary>Wings and sails: quick, with a longer reach.</summary>
        Sky = 2,
        /// <summary>Harnessed monster parts: hits harder and moves faster.</summary>
        Beast = 3,
    }

    /// <summary>What a raft type adds in battle and when hooking (Phase 8, tunable).</summary>
    public struct RaftBonus
    {
        /// <summary>Raft battle HP, in percent (+40 = 40% more).</summary>
        public int RaftHpPercent;
        public int Attack, Defense, Speed;
        /// <summary>Extra harpoon reach, in metres.</summary>
        public float Reach;

        public static RaftBonus operator +(RaftBonus a, RaftBonus b) => new RaftBonus
        {
            RaftHpPercent = a.RaftHpPercent + b.RaftHpPercent,
            Attack = a.Attack + b.Attack,
            Defense = a.Defense + b.Defense,
            Speed = a.Speed + b.Speed,
            Reach = a.Reach + b.Reach,
        };

        /// <summary>Half of every bonus, rounded toward zero (hybrids).</summary>
        public RaftBonus Half() => new RaftBonus
        {
            RaftHpPercent = RaftHpPercent / 2,
            Attack = Attack / 2,
            Defense = Defense / 2,
            Speed = Speed / 2,
            Reach = Reach / 2f,
        };

        public bool IsNone => RaftHpPercent == 0 && Attack == 0 && Defense == 0 && Speed == 0 && Reach == 0f;

        /// <summary>"+40% raft HP, +3 DEF, −1 SPD" for the Build menu and the HUD.</summary>
        public override string ToString()
        {
            var bits = new List<string>();
            if (RaftHpPercent != 0) bits.Add($"{Signed(RaftHpPercent)}% raft HP");
            if (Attack != 0) bits.Add($"{Signed(Attack)} ATK");
            if (Defense != 0) bits.Add($"{Signed(Defense)} DEF");
            if (Speed != 0) bits.Add($"{Signed(Speed)} SPD");
            if (Reach != 0f) bits.Add($"{(Reach > 0 ? "+" : "−")}{Math.Abs(Reach):0.#} m hook reach");
            return bits.Count == 0 ? "no bonus" : string.Join(", ", bits);
        }

        static string Signed(int v) => v > 0 ? $"+{v}" : $"−{-v}";
    }

    /// <summary>The raft's type, from its parts.</summary>
    public readonly struct RaftType
    {
        public readonly RaftKind Main, Second;
        public RaftType(RaftKind main, RaftKind second = RaftKind.Plain) { Main = main; Second = second; }
        public bool IsHybrid => Main != RaftKind.Plain && Second != RaftKind.Plain;
        public RaftBonus Bonus => IsHybrid
            ? RaftTypeRules.BonusOf(Main).Half() + RaftTypeRules.BonusOf(Second).Half()
            : RaftTypeRules.BonusOf(Main);
        public string Name => IsHybrid ? $"Hybrid ({Main}–{Second})" : Main == RaftKind.Plain ? "Plain" : Main.ToString();
        public override string ToString() => Name;
    }

    /// <summary>
    /// Raft types (Phase 8, GDD §6; numbers tunable). Only raft parts count (modules and habitats don't).
    /// A kind with at least <see cref="MinParts"/> parts and half of all raft parts sets the type. Two kinds
    /// that each have <see cref="MinParts"/> parts and <see cref="HybridShare"/> of the parts make a
    /// hybrid with half of each bonus. Otherwise the raft is Plain.
    /// Sky rafts don't fly yet (Claude's default until Ed decides): the type is a battle and reach bonus.
    /// Plain C# for EditMode tests.
    /// </summary>
    public static class RaftTypeRules
    {
        public const int MinParts = 3;
        public const float MainShare = 0.5f;
        public const float HybridShare = 0.3f;

        public static RaftBonus BonusOf(RaftKind kind)
        {
            switch (kind)
            {
                case RaftKind.Fortress: return new RaftBonus { RaftHpPercent = 40, Defense = 3, Speed = -1 };
                case RaftKind.Sky: return new RaftBonus { Speed = 3, Reach = 4f, RaftHpPercent = -10 };
                case RaftKind.Beast: return new RaftBonus { Attack = 4, Speed = 1 };
                default: return default;
            }
        }

        public static RaftType Classify(IEnumerable<ItemDefinition> parts)
        {
            var raftParts = parts.Where(p => p != null && p.type == ItemType.Raft).ToList();
            int total = raftParts.Count;
            if (total == 0) return new RaftType(RaftKind.Plain);
            var counts = raftParts.Where(p => p.raftKind != RaftKind.Plain)
                .GroupBy(p => p.raftKind)
                .Select(g => (kind: g.Key, count: g.Count()))
                .OrderByDescending(k => k.count).ThenBy(k => k.kind)
                .ToList();
            if (counts.Count == 0 || counts[0].count < MinParts) return new RaftType(RaftKind.Plain);
            var main = counts[0];
            bool mainOk = main.count >= total * MainShare - 1e-4;
            bool hasSecond = counts.Count > 1 && counts[1].count >= MinParts && counts[1].count >= total * HybridShare - 1e-4;
            if (hasSecond && main.count >= total * HybridShare - 1e-4) return new RaftType(main.kind, counts[1].kind);
            return mainOk ? new RaftType(main.kind) : new RaftType(RaftKind.Plain);
        }

        /// <summary>The hunter with the raft type's ATK, DEF and SPD bonus (speed never below 1).</summary>
        public static Combatant Apply(Combatant hunter, RaftBonus bonus)
        {
            hunter.Attack = Math.Max(1, hunter.Attack + bonus.Attack);
            hunter.Defense = Math.Max(0, hunter.Defense + bonus.Defense);
            hunter.Speed = Math.Max(1, hunter.Speed + bonus.Speed);
            return hunter;
        }

        /// <summary>Battle HP with the type's bonus; never below 1.</summary>
        public static int WithHpBonus(int hp, RaftBonus bonus) =>
            Math.Max(1, (int)Math.Round(hp * (1f + bonus.RaftHpPercent / 100f)));
    }
}
