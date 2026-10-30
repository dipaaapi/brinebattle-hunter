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
            CaptureSettings(data, hook, book);
            data.hasMastery = true;
            data.mastery = hook.Mastery.Points;
            foreach (var e in hook.Gorged.Entries)
                data.gorged.Add(new SaveData.Gorge { monster = e.MonsterId, parts = e.Parts.Select(p => p.id).ToList() });
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
        /// New game (main menu): the current save moves to save.backup.json so auto-save can't write
        /// the new game over it. Saving the new game makes a fresh save.json. Returns null, or what went wrong.
        /// </summary>
        public static string StartNew()
        {
            try
            {
                if (!Exists) return null;
                File.Copy(FilePath, BackupPath, true);
                File.Delete(FilePath);
                return null;
            }
            catch (Exception e) { return $"Couldn't put the old save aside: {e.Message}"; }
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

        /// <summary>Menu choices: battle mode and auto-items (Settings), auto-salvage, tiers and companion chores (Bag), Crew repair shares (Build).</summary>
        static void CaptureSettings(SaveData data, HookController hook, CollectionBook book)
        {
            var bag = UnityEngine.Object.FindFirstObjectByType<BagPanel>();
            var s = data.settings;
            data.hasSettings = true;
            s.battleMode = (int)Battle.BattleSettings.Mode;
            s.autoItems = Battle.BattleSettings.AutoItems;
            if (bag != null)
            {
                s.autoSalvage = bag.autoSalvage;
                s.weaponTier = (int)bag.weaponTier;
                s.armorTier = (int)bag.armorTier;
                s.companionTier = (int)bag.companionTier;
            }
            s.crewBuilds = CompanionSettings.CrewBuilds;
            s.mysticRefines = CompanionSettings.MysticRefines;
            s.mysticKeep = CompanionSettings.MysticKeep;
            s.uiScale = HuntStyles.Scale;
            var modules = new HashSet<string>();
            if (book != null) foreach (var i in book.items) if (i != null && i.type == ItemType.Module) modules.Add(i.id);
            foreach (var kv in hook.raft.Grid.Parts) if (kv.Value.type == ItemType.Module) modules.Add(kv.Value.id);
            foreach (var id in modules.Where(id => !string.IsNullOrEmpty(id)))
                s.crewRepairAt.Add(new SaveData.Share { id = id, share = CompanionSettings.CrewRepairAt(id) });
        }

        static void ApplySettings(SaveData.Settings s)
        {
            Battle.BattleSettings.Mode = (Battle.BattleMode)s.battleMode;
            Battle.BattleSettings.AutoItems = s.autoItems;
            var bag = UnityEngine.Object.FindFirstObjectByType<BagPanel>();
            if (bag != null)
            {
                bag.autoSalvage = s.autoSalvage;
                bag.weaponTier = (Rarity)s.weaponTier;
                bag.armorTier = (Rarity)s.armorTier;
                bag.companionTier = (Rarity)s.companionTier;
            }
            CompanionSettings.CrewBuilds = s.crewBuilds;
            CompanionSettings.MysticRefines = s.mysticRefines;
            CompanionSettings.MysticKeep = s.mysticKeep;
            if (s.uiScale > 0f) HuntStyles.Scale = s.uiScale;
            foreach (var r in s.crewRepairAt) if (!string.IsNullOrEmpty(r.id)) CompanionSettings.SetCrewRepairAt(r.id, r.share);
            PlayerPrefs.Save();
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
            if (data.hasSettings) ApplySettings(data.settings);
            hook.Gorged.Clear();
            foreach (var g in data.gorged ?? new List<SaveData.Gorge>())
                foreach (var id in g.parts)
                    if (known.TryGetValue(id ?? "", out var part)) hook.Gorged.Add(g.monster, part);
            hook.Mastery.Restore(data.hasMastery ? data.mastery : AlchemyMastery.StartingPoints(hook.raft.Tier));
            // Saves from before hammers existed get the starter hammer, so building isn't locked out.
            if (RaftRules.BestHammer(hook.Inventory) == null && hook.starterHammer != null && !data.items.Any(s => s.id != null && s.id.EndsWith("_hammer")))
                hook.Inventory.Add(hook.starterHammer, 1);
            hook.SyncHabitats();
            hook.ApplyCompanionBonuses();
            string note = missing.Count > 0 ? $" Unknown items skipped: {string.Join(", ", missing)}." : "";
            hook.Log($"Loaded the save from {data.savedAt}.{note}");
            return $"Loaded ({data.savedAt}).{note}";
        }
    }
}
