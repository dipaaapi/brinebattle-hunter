namespace BrineBattle.Encounters
{
    /// <summary>What a hooked creature turns out to be (GDD §4).</summary>
    public enum DriftState
    {
        Dead = 0,
        Weakened = 1,
        Alive = 2,
    }

    /// <summary>What the player can do when the raft is too small to fight (GDD §4 raft-size gate).</summary>
    public enum GateChoice
    {
        CutLine = 0,
        Flee = 1,
        Sedate = 2,
    }
}
