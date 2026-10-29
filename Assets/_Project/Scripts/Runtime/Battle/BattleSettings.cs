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

        const string AutoItemsKey = "brinebattle.autoItems";

        /// <summary>Semi-auto and Auto use consumables by themselves when low (default on).</summary>
        public static bool AutoItems
        {
            get => PlayerPrefs.GetInt(AutoItemsKey, 1) == 1;
            set { PlayerPrefs.SetInt(AutoItemsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static readonly string[] Labels = { "Manual", "Semi-auto", "Auto" };
    }
}
