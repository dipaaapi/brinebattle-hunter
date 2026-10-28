namespace BrineBattle.Battle
{
    /// <summary>Picks the hunter's action for Semi-auto and Auto. Null means "wait for the player".</summary>
    public static class BattleAI
    {
        public const float LowHp = 0.35f;

        public static BattleAction? Choose(BattleState s, BattleMode mode)
        {
            if (mode == BattleMode.Manual) return null;
            bool low = s.Hunter.Hp <= s.Hunter.MaxHp * LowHp;
            bool harpoonReady = s.CanUse(BattleAction.Harpoon);

            if (mode == BattleMode.SemiAuto)
                // Auto basic attacks; the player decides the important turns.
                return low || harpoonReady ? (BattleAction?)null : BattleAction.Attack;

            if (low && !harpoonReady) return BattleAction.Brace;
            if (harpoonReady) return BattleAction.Harpoon;
            return BattleAction.Attack;
        }
    }
}
