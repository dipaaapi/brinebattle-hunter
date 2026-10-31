using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Loot;
using UnityEngine;

namespace BrineBattle.Monsters
{
    [CreateAssetMenu(menuName = "BrineBattle/Monster", fileName = "Monster")]
    public class MonsterDefinition : ScriptableObject
    {
        [Tooltip("Floating loot, not a creature (Ed's U-30): always harvestable, never fights, not in the Book's monster pages.")]
        public bool isFlotsam;
        [Tooltip("A raft-level boss (Ed's ask): comes once enough monsters are beaten; beat the boss of the raft's level (its rarity) to level up. Never spawns as a normal drift, always fights.")]
        public bool isBoss;
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
        [Tooltip("The ailment its hits can give the hunter or a pet (U-33). Shown over the drift (C-11).")]
        public Battle.StatusEffect inflicts;
        [Range(0f, 1f)] public float inflictChance = 0.25f;
        public LootTable loot;
        [TextArea] public string description;
    }
}
