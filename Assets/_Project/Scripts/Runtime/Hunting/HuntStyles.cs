using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Shared IMGUI styles for the Phase 2 placeholder UI.</summary>
    public static class HuntStyles
    {
        static GUIStyle worldLabel;

        public static GUIStyle WorldLabel
        {
            get
            {
                if (worldLabel == null)
                {
                    worldLabel = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.LowerCenter,
                        richText = true,
                        fontSize = 12,
                    };
                    worldLabel.normal.textColor = Color.white;
                }
                return worldLabel;
            }
        }
    }
}
