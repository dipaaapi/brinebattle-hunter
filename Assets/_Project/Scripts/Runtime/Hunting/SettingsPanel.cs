using System.Linq;
using BrineBattle.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Phase 6 Settings menu (Esc or the HUD button): battle mode and auto-use of consumables,
    /// both saved, plus the controls. Placeholder IMGUI like the other panels.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // before RaftBuilder, so Esc that closes build mode doesn't also open Settings
    public class SettingsPanel : MonoBehaviour
    {
        static bool open => WorldPause.Open == Menu.Settings;
        /// <summary>False in the main menu scene: there is no game to save or load yet.</summary>
        bool inGame;

        void Start() => inGame = FindFirstObjectByType<HookController>() != null;

        void Update()
        {
            AutoSave();
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !BattleController.InBattle
                && (WorldPause.Open == Menu.None || WorldPause.Open == Menu.Settings)) WorldPause.Toggle(Menu.Settings);
        }

        /// <summary>
        /// Every 5 minutes of play, into the existing save file, only in Semi-auto and Auto (owner's call).
        /// Waits while a menu, prompt or battle is open so it never saves mid-action.
        /// </summary>
        void AutoSave()
        {
            if (!inGame || BattleSettings.Mode == BattleMode.Manual || !SaveSystem.Exists) return;
            autoTimer += Time.unscaledDeltaTime;
            if (autoTimer < SaveSystem.AutoSaveSeconds) return;
            if (BattleController.InBattle || WorldPause.Open != Menu.None || WorldPause.Prompt || WorldPause.Asleep) return;
            autoTimer = 0f;
            saveMessage = SaveSystem.Save(auto: true);
            saveInfo = null;
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open) return;
            GUI.depth = -5;
            float w = Mathf.Min(520, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 120);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Settings", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Settings); GUILayout.EndArea(); return; }

            // Owner idea U-25: short pages. FAQ is the game's only guide (Ed's call).
            var labels = inGame ? new[] { "Game", "Save", "Controls", "FAQ" } : new[] { "Game", "Controls", "FAQ" };
            page = Mathf.Min(page, labels.Length - 1);
            page = HuntStyles.Tabs(page, labels);
            string current = labels[page];
            if (current == "Controls") { Controls(); GUILayout.EndArea(); return; }
            if (current == "Save") { SavePage(); GUILayout.EndArea(); return; }
            if (current == "FAQ") { FaqPage(); GUILayout.EndArea(); return; }

            GUILayout.Label("Screen size (text and buttons)", HuntStyles.Heading);
            int size = Mathf.Clamp(Mathf.RoundToInt((HuntStyles.Scale - 1f) / 0.25f), 0, 2);
            int pickedSize = GUILayout.Toolbar(size, new[] { "Small", "Big", "Huge" }, GUILayout.Height(30));
            if (pickedSize != size) HuntStyles.Scale = 1f + 0.25f * pickedSize;

            GUILayout.Space(8);
            GUILayout.Label("Battle mode", HuntStyles.Heading);
            BattleSettings.Mode = (BattleMode)GUILayout.Toolbar((int)BattleSettings.Mode, BattleSettings.Labels, GUILayout.Height(30));

            GUILayout.Space(8);
            GUILayout.Label("Pause when I'm away", HuntStyles.Heading);
            int sleep = System.Array.IndexOf(IdleRules.Choices, IdleRules.Minutes);
            int pickedSleep = GUILayout.Toolbar(Mathf.Max(0, sleep), IdleRules.Labels, GUILayout.Height(28));
            if (pickedSleep != sleep) IdleRules.Minutes = IdleRules.Choices[pickedSleep];

            GUILayout.Space(8);
            BattleSettings.AutoItems = GUILayout.Toggle(BattleSettings.AutoItems,
                $" Auto-use consumables at {Mathf.RoundToInt(BattleAI.LowHp * 100)}% HP or below (Semi-auto and Auto)");

            if (inGame) ResetRaftRow();
            GUILayout.EndArea();
        }

        int page;
        bool confirmReset;

        /// <summary>
        /// Reset raft (Ed's ask): back to the starter square so the raft can be rebuilt, but the sea takes
        /// about a fifth of the parts that come off. Asks twice, since it can't be undone.
        /// </summary>
        void ResetRaftRow()
        {
            var hook = FindFirstObjectByType<HookController>();
            if (hook == null || hook.raft == null || hook.raft.starterPart == null) return;
            GUILayout.Space(10);
            GUILayout.Label("Reset raft", HuntStyles.Heading);
            GUILayout.Label($"<size=11>Takes the raft back to the starter 3x3 so you can build it again. Level and type stay. "
                + $"About {Mathf.RoundToInt(Rafts.RaftGrid.WashAwayShare * 100)}% of the parts that come off float away; the rest go to the Bag. Habitats are kept.</size>", Rich);
            GUI.enabled = !BattleController.InBattle;
            if (!confirmReset)
            {
                if (HuntStyles.ColorButton("Reset raft…", HuntStyles.Risky)) confirmReset = true;
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (HuntStyles.ColorButton("Yes, reset it", HuntStyles.Risky))
                {
                    confirmReset = false;
                    var lost = hook.raft.Grid.ResetRaft(hook.raft.starterPart, hook.Inventory, new System.Random());
                    hook.SyncHabitats();
                    hook.Log(lost.Count == 0 ? "The raft is back to its starter square. Every part is in the Bag."
                        : $"The raft is back to its starter square. The sea took {lost.Count} part(s): "
                          + string.Join(", ", lost.GroupBy(p => p.displayName).Select(x => x.Count() > 1 ? $"{x.Count()}× {x.Key}" : x.Key))
                          + ". The rest are in the Bag.");
                }
                if (GUILayout.Button("Cancel", GUILayout.Height(30))) confirmReset = false;
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;
        }

        static readonly (string key, string what)[] Keys =
        {
            ("Click a drift", "Cast the harpoon at it"),
            ("Space", "Reel in (press in the green zone)"),
            ("B", "Bag"),
            ("P", "Minions"),
            ("C", "Collection Book"),
            ("R", "Build the raft and the Workbench"),
            ("Esc", "Settings, or close a menu"),
            ("Mouse wheel  or  + / -", "Zoom in and out"),
            ("Right-drag  or  Q / E", "Turn the view around the raft"),
            ("F", "Put the view back"),
        };

        void Controls()
        {
            foreach (var (key, what) in Keys)
            {
                GUILayout.BeginHorizontal(HuntStyles.Card);
                GUILayout.Label($"<b>{key}</b>", HuntStyles.Text, GUILayout.Width(120));
                GUILayout.Label(what, HuntStyles.Text);
                GUILayout.EndHorizontal();
            }
        }

        int faqTopic;
        string faqOpen;
        Vector2 faqScroll;

        /// <summary>The full guide: topics on top, questions that open to show their answer.</summary>
        void FaqPage()
        {
            var names = new string[Faq.Topics.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Faq.Topics[i].Name;
            int picked = GUILayout.SelectionGrid(faqTopic, names, 4, GUILayout.Height(52));
            if (picked != faqTopic) { faqTopic = picked; faqOpen = null; faqScroll = Vector2.zero; }
            faqScroll = GUILayout.BeginScrollView(faqScroll);
            foreach (var e in Faq.Topics[faqTopic].Entries)
            {
                bool isOpen = faqOpen == e.Question;
                GUILayout.BeginVertical(HuntStyles.Card);
                if (GUILayout.Button($"{(isOpen ? "▼" : "▶")} <b>{e.Question}</b>", HuntStyles.RichButton)) faqOpen = isOpen ? null : e.Question;
                if (isOpen) GUILayout.Label(e.Answer, HuntStyles.Text);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        void SavePage()
        {
            saveInfo ??= SaveSystem.Describe() ?? "no save yet";
            GUILayout.Label($"Last save: <b>{saveInfo}</b>", HuntStyles.Text);
            GUILayout.BeginHorizontal();
            if (HuntStyles.ColorButton("Save now", HuntStyles.Go)) { saveMessage = SaveSystem.Save(); saveInfo = null; autoTimer = 0f; }
            GUI.enabled = SaveSystem.Exists;
            if (HuntStyles.ColorButton("Load", HuntStyles.Other)) { saveMessage = SaveSystem.Load(); autoTimer = 0f; }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            UploadRow();
            GUILayout.Label(BattleSettings.Mode == BattleMode.Manual
                ? "<size=11>Auto-save: off in Manual</size>"
                : $"<size=11>Auto-save: {(SaveSystem.Exists ? $"next in {Mathf.CeilToInt((SaveSystem.AutoSaveSeconds - autoTimer) / 60f)} min" : "after your first save")}</size>", Rich);
            if (!string.IsNullOrEmpty(saveMessage)) GUILayout.Label($"<size=11>{saveMessage}</size>", Rich);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<size=10>{SaveSystem.Folder}</size>", Rich);
            if (GUILayout.Button("Open folder", GUILayout.Width(90)))
            {
                System.IO.Directory.CreateDirectory(SaveSystem.Folder);
                Application.OpenURL("file:///" + SaveSystem.Folder.Replace('\\', '/'));
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save and go to the main menu", GUILayout.Height(32))) { SaveSystem.Save(); GameStart.ToMenu(); }
        }


        /// <summary>
        /// "Upload save file": in the Editor a file picker; in a build, paste the file's path
        /// (there is no built-in file picker outside the Editor).
        /// </summary>
        void UploadRow()
        {
            GUILayout.BeginHorizontal();
#if UNITY_EDITOR
            if (GUILayout.Button("Upload save file..."))
            {
                string picked = UnityEditor.EditorUtility.OpenFilePanel("Upload a BrineBattle save", "", "json");
                if (!string.IsNullOrEmpty(picked)) Uploaded(SaveSystem.Upload(picked));
            }
#else
            GUILayout.Label("Upload:", GUILayout.Width(50));
            uploadPath = GUILayout.TextField(uploadPath ?? "");
            if (GUILayout.Button("Upload", GUILayout.Width(70))) Uploaded(SaveSystem.Upload(uploadPath));
#endif
            GUILayout.EndHorizontal();
        }

        void Uploaded(string message)
        {
            saveMessage = message;
            saveInfo = null;
            autoTimer = 0f;
        }

        string uploadPath;
        string saveInfo;
        string saveMessage;
        static float autoTimer;

        /// <summary>For the HUD: the save file's name and when the next auto-save happens.</summary>
        public static string AutoSaveStatus
        {
            get
            {
                if (!SaveSystem.Exists) return "No save yet";
                string name = System.IO.Path.GetFileName(SaveSystem.FilePath);
                if (BattleSettings.Mode == BattleMode.Manual) return $"{name} · auto-save off";
                int left = Mathf.Max(0, Mathf.CeilToInt(SaveSystem.AutoSaveSeconds - autoTimer));
                return $"{name} · auto-save in {left / 60}:{left % 60:00}";
            }
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
    }
}
