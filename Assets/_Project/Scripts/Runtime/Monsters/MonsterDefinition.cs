using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Loot;
using UnityEngine;

namespace BrineBattle.Monsters
{
    [CreateAssetMenu(menuName = "BrineBattle/Monster", fileName = "Monster")]
    public class MonsterDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves, bestiary and loot. Never rename once in use.")]
        public string id;
        public string displayName;
        public Element element;
        public Habitat habitat;
        public Rarity rarity;
        [Tooltip("Raft size needed to fight it (GDD §4 raft-size gate). 1 = smallest.")]
        [Range(1, 10)] public int sizeClass = 1;
        [Tooltip("Temperament 0 (calm) to 1 (aggressive). Each drift varies a little. A weakened aggressive monster is killed outright; only calm ones can be tamed (idea U-17).")]
        [Range(0f, 1f)] public float aggression = 0.5f;
        public MonsterStats stats = new MonsterStats { hp = 10, attack = 1, defense = 0, speed = 1 };
        public LootTable loot;
        [TextArea] public string description;
    }
}
