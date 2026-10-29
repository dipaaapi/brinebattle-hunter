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
    }
}
