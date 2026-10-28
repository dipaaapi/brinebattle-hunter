namespace BrineBattle.Elements
{
    /// <summary>Four main elements plus the six combinations (GDD §8).</summary>
    public enum Element
    {
        None = 0,
        // Main
        Water = 1,
        Fire = 2,
        Earth = 3,
        Wind = 4,
        // Secondary
        Steam = 10,     // Water + Fire
        Mud = 11,       // Water + Earth
        Ice = 12,       // Water + Wind
        Magma = 13,     // Fire + Earth
        Lightning = 14, // Fire + Wind
        Sand = 15,      // Earth + Wind
    }
}
