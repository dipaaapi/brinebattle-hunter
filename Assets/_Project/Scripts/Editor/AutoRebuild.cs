using UnityEditor;
using UnityEngine;

namespace BrineBattle.Editor
{
    /// <summary>
    /// After a recompile, rebuilds the sample data and the Hook scene once whenever
    /// <see cref="DataVersion"/> goes up, so new items, recipes and scene wiring show up without
    /// remembering BrineBattle → Build Hook Scene. Bump DataVersion when SampleDataBuilder or
    /// HookSceneBuilder changes. Skipped in play mode and batch mode (unity-verify).
    /// </summary>
    [InitializeOnLoad]
    static class AutoRebuild
    {
        public const int DataVersion = 6;   // 1 gear, 2 consumables, 3 gear skills, 4 elemental oils, 5 habitats, 6 reinforced raft parts
        static string Key => $"brinebattle.dataVersion.{Application.dataPath.GetHashCode()}";

        static AutoRebuild()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorPrefs.GetInt(Key, 0) >= DataVersion) return;
            // The Hook scene is generated, but give the user a chance to keep unsaved edits first.
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorPrefs.SetInt(Key, DataVersion);
            Debug.Log($"[AutoRebuild] data version {DataVersion}: rebuilding sample data and the Hook scene.");
            HookSceneBuilder.Build();
        }
    }
}
