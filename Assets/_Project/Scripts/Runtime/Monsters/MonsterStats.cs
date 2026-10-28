using System;
using UnityEngine;

namespace BrineBattle.Monsters
{
    [Serializable]
    public struct MonsterStats
    {
        [Min(1)] public int hp;
        [Min(0)] public int attack;
        [Min(0)] public int defense;
        [Min(1)] public int speed;
    }
}
