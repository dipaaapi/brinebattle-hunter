using System.Collections.Generic;
using BrineBattle.Hunting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrineBattle.Editor
{
    /// <summary>
    /// Builds the MainMenu scene (New game, Continue, Settings, Quit) from code and makes it the first
    /// scene of the build and of Play mode. Menu: BrineBattle > Build Main Menu Scene.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        public const string HookPath = "Assets/_Project/Scenes/Hook.unity";

        // Play always starts at the main menu, whichever scene is open in the Editor.
        static MainMenuSceneBuilder() => EditorApplication.delayCall += SetPlayModeStart;

        [MenuItem("BrineBattle/Build Main Menu Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.06f, 0.2f, 0.32f);

            var menu = new GameObject("Main Menu");
            menu.AddComponent<MainMenu>();
            menu.AddComponent<SettingsPanel>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            OrderBuildScenes();
            SetPlayModeStart();
            Debug.Log($"[MainMenuSceneBuilder] saved {ScenePath}");
        }

        /// <summary>Build order: MainMenu first, then Hook, then anything else.</summary>
        public static void OrderBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath || s.path == HookPath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HookPath) != null) scenes.Insert(0, new EditorBuildSettingsScene(HookPath, true));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void SetPlayModeStart() =>
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
    }
}
