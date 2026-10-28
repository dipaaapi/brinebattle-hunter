namespace BrineBattle.Hunting
{
    /// <summary>
    /// Holds the world around the raft still (drifts, spawner, current, modules) while the player
    /// is in a battle or has to answer a prompt.
    /// </summary>
    public static class WorldPause
    {
        public static bool Prompt;
        public static bool Active => BattleController.InBattle || Prompt;
    }
}
