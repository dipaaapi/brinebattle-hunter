using UnityEngine;

namespace BrineBattle.Battle
{
    /// <summary>Player setting for battle mode (GDD §5), saved between sessions.</summary>
    public static class BattleSettings
    {
        const string Key = "brinebattle.battleMode";

        public static BattleMode Mode
        {
            get => (BattleMode)PlayerPrefs.GetInt(Key, (int)BattleMode.Manual);
            set { PlayerPrefs.SetInt(Key, (int)value); PlayerPrefs.Save(); }
        }

        public static readonly string[] Labels = { "Manual", "Semi-auto", "Auto" };
    }
}
