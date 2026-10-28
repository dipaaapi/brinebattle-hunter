using System.Collections.Generic;
using System.Linq;
using BrineBattle.Items;
using UnityEngine;

namespace BrineBattle.Loot
{
    [CreateAssetMenu(menuName = "BrineBattle/Loot Table", fileName = "LootTable")]
    public class LootTable : ScriptableObject
    {
        public List<LootEntry> entries = new List<LootEntry>();

        public List<Drop<ItemDefinition>> Roll(System.Random rng) =>
            LootRoller.Roll(entries.Select(e => (e.item, e.chance, e.minQuantity, e.maxQuantity)), rng);
    }
}
