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
    /// Writes and reads save files (JSON) in <c>Application.persistentDataPath/Saves</c>, one per slot.
    /// The rules for what goes in a save live in <see cref="SaveGame"/>; this only finds the scene
    /// objects and touches the disk. Used from the Settings menu.
    /// </summary>
    public static class SaveSystem
    {
        public const int Slots = 3;

        public static string Folder => Path.Combine(Application.persistentDataPath, "Saves");
        public static string PathOf(int slot) => Path.Combine(Folder, $"save{slot}.json");

        /// <summary>"2026-10-29 12:00" for a used slot, null for an empty one.</summary>
        public static string Describe(int slot)
        {
            try
            {
                if (!File.Exists(PathOf(slot))) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)))?.savedAt ?? "?";
            }
            catch (Exception) { return "unreadable"; }
        }

        public static string Save(int slot)
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
                File.WriteAllText(PathOf(slot), JsonUtility.ToJson(data, true));
            }
            catch (Exception e) { return $"Save failed: {e.Message}"; }
            hook.Log($"Saved to slot {slot}.");
            return $"Saved slot {slot} ({data.savedAt}).";
        }

        public static string Load(int slot)
        {
            var hook = UnityEngine.Object.FindFirstObjectByType<HookController>();
            if (hook == null || hook.raft == null) return "Nothing to load into (no Hook scene).";
            if (BattleController.InBattle) return "Can't load during a battle.";
            SaveData data;
            try
            {
                if (!File.Exists(PathOf(slot))) return $"Slot {slot} is empty.";
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
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
            hook.Log($"Loaded slot {slot} (saved {data.savedAt}).{note}");
            return $"Loaded slot {slot} ({data.savedAt}).{note}";
        }
    }
}
