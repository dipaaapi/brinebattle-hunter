using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using BrineBattle.Saving;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Writes and reads the one save file (JSON), <c>Application.persistentDataPath/Saves/save.json</c>.
    /// "Upload save file" copies a save from anywhere into that slot (the old one is kept as
    /// <c>save.backup.json</c>) and loads it. The rules for what goes in a save
    /// live in <see cref="SaveGame"/>; this only finds the scene objects and touches the disk.
    /// Used from the Settings menu and the auto-save.
    /// </summary>
    public static class SaveSystem
    {
        /// <summary>Auto-save interval: every 5 minutes, in Semi-auto and Auto only (owner's call).</summary>
        public const float AutoSaveSeconds = 300f;

        public static string Folder => Path.Combine(Application.persistentDataPath, "Saves");
        public static string FilePath => Path.Combine(Folder, "save.json");
        public static string BackupPath => Path.Combine(Folder, "save.backup.json");
        public static bool Exists => File.Exists(FilePath);

        /// <summary>"2026-10-29 12:00" when there is a save, null when there is none.</summary>
        public static string Describe()
        {
            try
            {
                if (!Exists) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath))?.savedAt ?? "?";
            }
            catch (Exception) { return "unreadable"; }
        }

        public static string Save(bool auto = false)
        {
            var hook = UnityEngine.Object.FindFirstObjectByType<HookController>();
            if (hook == null || hook.raft == null) return "Nothing to save (no Hook scene).";
            var modules = hook.builder != null ? hook.builder.modules : null;
            var book = UnityEngine.Object.FindFirstObjectByType<CollectionBook>();
            var data = SaveGame.Capture(hook.Inventory, hook.Gear, hook.Harpoon, hook.raft.Grid,
                modules?.Slots.Select(kv => new KeyValuePair<Cell, int>(kv.Key, kv.Value.Durability.Current)),
                hook.Companions, book != null ? book.Log.Serialize() : null, DateTime.Now);
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e) { return $"Save failed: {e.Message}"; }
            hook.Log(auto ? $"Auto-saved ({data.savedAt})." : $"Game saved ({data.savedAt}).");
            return $"Saved ({data.savedAt}).";
        }

        /// <summary>
        /// Makes the save at <paramref name="path"/> the one save file and loads it; auto-save then
        /// writes into it. The current save is kept as save.backup.json first.
        /// </summary>
        public static string Upload(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "Pick a save file first.";
            path = path.Trim().Trim('"');
            if (BattleController.InBattle) return "Can't load during a battle.";
            try
            {
                if (!File.Exists(path)) return $"File not found: {path}";
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || string.IsNullOrEmpty(data.savedAt)) return "That file isn't a BrineBattle save.";
                Directory.CreateDirectory(Folder);
                if (Path.GetFullPath(path) == Path.GetFullPath(FilePath)) return Load();
                if (Exists) File.Copy(FilePath, BackupPath, true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e) { return $"Upload failed: {e.Message}"; }
            return "Uploaded. " + Load();
        }

        public static string Load()
        {
            var hook = UnityEngine.Object.FindFirstObjectByType<HookController>();
            if (hook == null || hook.raft == null) return "Nothing to load into (no Hook scene).";
            if (BattleController.InBattle) return "Can't load during a battle.";
            SaveData data;
            try
            {
                if (!Exists) return "No save file yet.";
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e) { return $"Load failed: {e.Message}"; }
            if (data == null) return "Load failed: the file is empty or broken.";

            var book = UnityEngine.Object.FindFirstObjectByType<CollectionBook>();
            var known = new Dictionary<string, ItemDefinition>();
            void Know(ItemDefinition i) { if (i != null && !string.IsNullOrEmpty(i.id)) known[i.id] = i; }
            if (book != null) foreach (var i in book.items) Know(i);
            if (hook.builder != null)
                foreach (var r in hook.builder.recipes.Where(r => r != null))
                    foreach (var ing in r.inputs.Concat(r.outputs)) Know(ing.item);
            foreach (var kv in hook.raft.Grid.Parts) Know(kv.Value);

            var missing = SaveGame.Apply(data, id => known.TryGetValue(id, out var i) ? i : null, hook.Inventory, hook.Gear,
                hook.Harpoon, hook.raft.Grid, hook.Companions, out var durability);
            var modules = hook.builder != null ? hook.builder.modules : null;
            if (modules != null)
                foreach (var kv in modules.Slots)
                    if (durability.TryGetValue(kv.Key, out var cur)) kv.Value.Durability.Set(cur);
            if (book != null && data.collection != null) book.Restore(data.collection);
            hook.ApplyCompanionBonuses();
            string note = missing.Count > 0 ? $" Unknown items skipped: {string.Join(", ", missing)}." : "";
            hook.Log($"Loaded the save from {data.savedAt}.{note}");
            return $"Loaded ({data.savedAt}).{note}";
        }
    }
}
