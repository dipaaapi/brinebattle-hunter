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
        /// <summary>Companion habitat (Phase 7): placed on the raft grid; each one gives room for one more companion.</summary>
        Habitat = 8,
        /// <summary>Building tool (Ed's ask): a hammer of the raft's level or better is needed to build on it.</summary>
        Tool = 9,
        /// <summary>Accessory (Ed's ask): a third gear slot whose worth is its skills, element and + refine.</summary>
        Accessory = 10,
        /// <summary>Vehicle part (Ed's ask): one of the five pieces that finish the plane, sea train or pirate ship.</summary>
        Vessel = 11,
    }
}
