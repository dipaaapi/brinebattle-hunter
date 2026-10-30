namespace BrineBattle.Items
{
    /// <summary>What a consumable does when used (GDD §11 Consumables). Battle-only for now.</summary>
    public enum ConsumableEffect
    {
        None = 0,
        /// <summary>Restores hunter HP.</summary>
        HealHunter = 1,
        /// <summary>Restores raft HP.</summary>
        RepairRaft = 2,
        /// <summary>Elemental oil: the hunter's attacks take the item's element for effectAmount rounds.</summary>
        CoatWeapon = 3,
        /// <summary>Companion treat: heals the battle companion, or wakes it up if it was knocked out.</summary>
        HealCompanion = 4,
        /// <summary>Cures the item's ailment (U-33): the hunter first, otherwise the battle companion.</summary>
        CureStatus = 5,
    }
}
