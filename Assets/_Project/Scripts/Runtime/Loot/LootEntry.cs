using System;
using BrineBattle.Items;
using UnityEngine;

namespace BrineBattle.Loot
{
    [Serializable]
    public struct LootEntry
    {
        public ItemDefinition item;
        [Tooltip("Chance this entry drops at all, 0 to 1.")]
        [Range(0f, 1f)] public float chance;
        [Min(1)] public int minQuantity;
        [Min(1)] public int maxQuantity;
    }
}
