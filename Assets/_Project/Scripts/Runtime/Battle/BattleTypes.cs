namespace BrineBattle.Battle
{
    public enum BattleAction
    {
        Attack = 0,
        /// <summary>Heavy hit that costs stamina.</summary>
        Harpoon = 1,
        /// <summary>Halve incoming damage this round and recover extra stamina.</summary>
        Brace = 2,
    }

    /// <summary>GDD §5: set in Settings, applied at battle start, switchable mid-battle.</summary>
    public enum BattleMode
    {
        Manual = 0,
        SemiAuto = 1,
        Auto = 2,
    }

    public enum BattleOutcome
    {
        Ongoing = 0,
        Won = 1,
        /// <summary>Hunter knocked out.</summary>
        LostHunter = 2,
        /// <summary>Raft broken apart.</summary>
        LostRaft = 3,
    }

    /// <summary>Who acts first in round 1. Speed = normal rule; the line-tension check (C-2) can override it.</summary>
    public enum Initiative
    {
        Speed = 0,
        Hunter = 1,
        Monster = 2,
    }
}
