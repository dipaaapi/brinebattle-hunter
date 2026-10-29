using BrineBattle.Companions;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Monsters;

namespace BrineBattle.Battle
{
    /// <summary>One side of a battle. Plain C# so battles can run in EditMode tests.</summary>
    public class Combatant
    {
        public string Name;
        public Element Element;
        public Habitat Habitat;
        public int MaxHp;
        public int Hp;
        public int Attack;
        public int Defense;
        public int Speed;
        public bool Bracing;

        public bool Alive => Hp > 0;

        public static Combatant FromMonster(MonsterDefinition m) => new Combatant
        {
            Name = m.displayName,
            Element = m.element,
            Habitat = m.habitat,
            MaxHp = m.stats.hp,
            Hp = m.stats.hp,
            Attack = m.stats.attack,
            Defense = m.stats.defense,
            Speed = m.stats.speed,
        };

        /// <summary>A companion at full HP (it rests between battles).</summary>
        public static Combatant FromCompanion(Companion c) => new Combatant
        {
            Name = c.Name,
            Element = c.Element,
            Habitat = c.Habitat,
            MaxHp = c.MaxHp,
            Hp = c.MaxHp,
            Attack = c.Attack,
            Defense = c.Defense,
            Speed = c.Speed,
        };

        /// <summary>No class, no magic: power comes from gear (GDD §7, §9). Levels come later.</summary>
        public static Combatant Hunter(Equipment gear = null) => new Combatant
        {
            Name = "Hunter",
            Element = gear?.Element ?? Element.None,
            MaxHp = 60,
            Hp = 60,
            Attack = 9 + (gear?.Attack ?? 0),
            Defense = 3 + (gear?.Defense ?? 0),
            Speed = 6,
        };
    }
}
