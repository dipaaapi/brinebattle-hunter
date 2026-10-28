using BrineBattle.Elements;
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

        /// <summary>No class, no magic: base hunter until levels and gear arrive (GDD §7).</summary>
        public static Combatant Hunter() => new Combatant
        {
            Name = "Hunter",
            Element = Element.None,
            MaxHp = 60,
            Hp = 60,
            Attack = 9,
            Defense = 3,
            Speed = 6,
        };
    }
}
