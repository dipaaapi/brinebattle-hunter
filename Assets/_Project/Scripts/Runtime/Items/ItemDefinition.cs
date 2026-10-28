using BrineBattle.Elements;
using UnityEngine;

namespace BrineBattle.Items
{
    [CreateAssetMenu(menuName = "BrineBattle/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves and loot tables. Never rename once in use.")]
        public string id;
        public string displayName;
        public ItemType type;
        public Rarity rarity;
        public Element element;
        [Min(1)] public int maxStack = 99;
        [Tooltip("Modules only: level 1-5 (idea U-13).")]
        [Range(1, 5)] public int moduleLevel = 1;
        public Sprite icon;
        [TextArea] public string description;
    }
}
