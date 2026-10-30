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
        [Tooltip("Raft parts only: the raft type this part builds toward (Phase 8). Plain counts toward none.")]
        public Rafts.RaftKind raftKind;
        [Tooltip("Gear only: attack (weapon) or defense (armor) added to the hunter. Refine (+) raises it.")]
        [Min(0)] public int gearPower;
        [Tooltip("Gear only: passive skills. Only as many count as the rarity allows (Rare 1, Legendary 2, Mythic 3).")]
        public GearPassive[] passives = new GearPassive[0];
        [Tooltip("Gear only: active skills used in battle. Epic 1, Legendary/Mythic 2.")]
        public GearActive[] actives = new GearActive[0];
        [Tooltip("Consumables only: what using it does (Phase 6).")]
        public ConsumableEffect effect;
        [Tooltip("Consumables only: HP restored to the hunter or the raft, or rounds an elemental oil lasts.")]
        [Min(0)] public int effectAmount;
        public Sprite icon;
        [TextArea] public string description;
    }
}
