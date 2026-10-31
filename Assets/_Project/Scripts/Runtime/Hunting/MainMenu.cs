using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrineBattle.Hunting
{
    /// <summary>How the game scene was entered from the main menu.</summary>
    public enum StartKind { Direct, NewGame, Continue }

    /// <summary>Scene names and what the main menu asked the game scene to do.</summary>
    public static class GameStart
    {
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Hook";
        public static StartKind Kind = StartKind.Direct;
        /// <summary>Picked on New game (Ed's ask); a loaded save brings its own.</summary>
        public static Rafts.Difficulty Difficulty = Rafts.Difficulty.Normal;

        public static void Play(StartKind kind)
        {
            Kind = kind;
            WorldPause.Close(WorldPause.Open);
            WorldPause.Prompt = false;
            SceneManager.LoadScene(GameScene);
        }

        public static void ToMenu()
        {
            WorldPause.Close(WorldPause.Open);
            WorldPause.Prompt = false;
            SceneManager.LoadScene(MenuScene);
        }

        /// <summary>Game scene side: on Continue, loads the save once every object has woken up.</summary>
        public static IEnumerator Apply(HookController hook)
        {
            var kind = Kind;
            Kind = StartKind.Direct;
            yield return null;
            if (kind == StartKind.Continue) hook.Log(SaveSystem.Load());
            else if (kind == StartKind.NewGame) hook.Log("A new game begins. Click a drift to cast your harpoon.");
        }
    }

    /// <summary>
    /// The MainMenu scene (Ed's ask): the game opens here before the Hook scene. New game, Continue,
    /// Settings (the normal Settings panel) and Quit.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        bool confirmNew, pickDifficulty;
        string message;
        GUIStyle title, big, richButton, rich;

        // The calm day music plays on the main menu too (Ed's ask).
        void Start()
        {
            if (FindFirstObjectByType<MusicPlayer>() == null) gameObject.AddComponent<MusicPlayer>();
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            big ??= new GUIStyle(GUI.skin.button) { fontSize = 20 };
            richButton ??= new GUIStyle(big) { richText = true, wordWrap = true };
            rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            if (WorldPause.Open == Menu.Settings) return;   // the Settings panel is showing

            float w = 320, x = (HuntStyles.W - w) / 2, y = Mathf.Max(30, HuntStyles.H / 2 - 220);
            GUILayout.BeginArea(new Rect(x - 100, y, w + 200, 80));
            GUILayout.Label("BrineBattle Hunter", title, GUILayout.Height(56));
            GUILayout.Label("<size=13>Hunt, tame and build on an endless ocean.</size>", rich);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(x, y + 100, w, 360));
            if (pickDifficulty) PickDifficulty();
            else if (confirmNew) ConfirmNew();
            else Buttons();
            if (!string.IsNullOrEmpty(message)) GUILayout.Label($"<size=12>{message}</size>", rich);
            GUILayout.EndArea();
        }

        void Buttons()
        {
            string saved = SaveSystem.Describe();
            GUI.enabled = SaveSystem.Exists && saved != "unreadable";
            if (GUILayout.Button(saved != null ? $"Continue  <size=12>({saved})</size>" : "Continue", richButton, GUILayout.Height(52)))
                GameStart.Play(StartKind.Continue);
            GUI.enabled = true;
            if (GUILayout.Button("New game", big, GUILayout.Height(52))) pickDifficulty = true;
            if (GUILayout.Button("Settings", big, GUILayout.Height(44))) WorldPause.Toggle(Menu.Settings);
            if (GUILayout.Button("Quit", big, GUILayout.Height(44))) Quit();
            if (saved == "unreadable") message = "The save file can't be read. Start a new game, or upload a save in Settings.";
        }

        /// <summary>
        /// Difficulty (Ed's ask): how many raft types must leave the sea to win, and how tough the sea is.
        /// </summary>
        void PickDifficulty()
        {
            GUILayout.Label("<b>Pick a difficulty</b>", rich);
            for (int i = 0; i < DifficultyLabels.Length; i++)
            {
                var d = (Rafts.Difficulty)i;
                if (GUILayout.Button($"{DifficultyLabels[i]}  <size=12>{Rafts.DifficultyRules.Describe(d)}</size>", richButton, GUILayout.Height(52)))
                {
                    GameStart.Difficulty = d;
                    pickDifficulty = false;
                    if (SaveSystem.Exists) confirmNew = true;
                    else GameStart.Play(StartKind.NewGame);
                }
            }
            if (GUILayout.Button("Back", big, GUILayout.Height(40))) pickDifficulty = false;
        }

        static readonly string[] DifficultyLabels = Rafts.DifficultyRules.Labels;

        void ConfirmNew()
        {
            GUILayout.Label("Start a new game? Your saved game is kept as save.backup.json (Settings → Upload save file brings it back).", rich);
            if (GUILayout.Button("Yes, new game", big, GUILayout.Height(48)))
            {
                string problem = SaveSystem.StartNew();
                if (problem != null) { message = problem; confirmNew = false; return; }
                GameStart.Play(StartKind.NewGame);
            }
            if (GUILayout.Button("Back", big, GUILayout.Height(40))) confirmNew = false;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
