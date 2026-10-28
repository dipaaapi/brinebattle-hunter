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
        public Sprite icon;
        [TextArea] public string description;
    }
}
