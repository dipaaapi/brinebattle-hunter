namespace BrineBattle.Battle
{
    public enum BattleAction
    {
        Attack = 0,
        /// <summary>Heavy hit that costs stamina.</summary>
        Harpoon = 1,
        /// <summary>Halve incoming damage this round and recover extra stamina.</summary>
        Brace = 2,
        /// <summary>Use a consumable from the bag; takes the hunter's turn.</summary>
        Item = 3,
        /// <summary>Use an active gear skill; costs stamina and takes the turn.</summary>
        Skill = 4,
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

    /// <summary>Who is on screen in a battle, for <see cref="BattleEvent"/>.</summary>
    public enum BattleActor { Hunter = 0, Companion = 1, Monster = 2, Raft = 3 }

    public enum BattleEventKind
    {
        /// <summary>Close hit: the attacker lunges.</summary>
        Strike = 0,
        /// <summary>Thrown or fired: something flies to the target.</summary>
        Shot = 1,
        /// <summary>A heavy skill shot (Tidal Strike).</summary>
        BigShot = 2,
        /// <summary>Damage bounced back (Thorns).</summary>
        Recoil = 3,
        Heal = 4,
        /// <summary>The hit was stopped (Shell Guard).</summary>
        Blocked = 5,
        /// <summary>A non-damage action worth a label (brace, guard, oil).</summary>
        Buff = 6,
        KnockedOut = 7,
    }

    /// <summary>
    /// One thing that happened in a round, in order, for the battle visuals (the Log is the text
    /// version). Amount is damage or healing; Text is a short label for buffs.
    /// </summary>
    public struct BattleEvent
    {
        public BattleEventKind Kind;
        public BattleActor From;
        public BattleActor To;
        public int Amount;
        public BrineBattle.Elements.Element Element;
        public string Text;

        public override string ToString() => $"{Kind} {From}->{To} {Amount} {Text}";
    }
}
