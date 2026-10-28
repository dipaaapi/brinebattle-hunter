namespace BrineBattle.Items
{
    /// <summary>Loot types (GDD §11). Material and Essence cover harvest drops and salvage output.</summary>
    public enum ItemType
    {
        Material = 0,
        Raft = 1,
        Weapon = 2,
        Armor = 3,
        Companion = 4,
        Consumable = 5,
        Essence = 6,
        /// <summary>Raft module (idea U-13): placed on the raft grid like a part.</summary>
        Module = 7,
    }
}
