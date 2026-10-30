namespace BrineBattle.Hunting
{
    public enum Menu { None, Bag, Book, Build, Settings, Pets, Bin }

    /// <summary>
    /// Holds the world around the raft still (drifts, spawner, current, modules) while the player
    /// is in a battle, answering a prompt, or in a menu. Only one menu is open at a time:
    /// opening one closes the other.
    /// </summary>
    public static class WorldPause
    {
        public static bool Prompt;
        /// <summary>The player fell asleep (no input for a while, Ed's scenario): nothing happens until they're back.</summary>
        public static bool Asleep;
        public static Menu Open { get; private set; }
        public static bool Active => BattleController.InBattle || Prompt || Asleep || Open != Menu.None;

        public static void Toggle(Menu m) => Open = Open == m ? Menu.None : m;
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() { Prompt = false; Asleep = false; Open = Menu.None; }

        public static void Close(Menu m) { if (Open == m) Open = Menu.None; }
    }
}
