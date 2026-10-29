using System.Linq;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Loot;
using BrineBattle.Monsters;
using UnityEditor;
using UnityEngine;

namespace BrineBattle.Editor
{
    /// <summary>
    /// Creates the Phase 1 sample data (one monster, its loot table and items) if missing.
    /// Menu: BrineBattle > Create Sample Data. Headless: -executeMethod BrineBattle.Editor.SampleDataBuilder.Build
    /// </summary>
    public static class SampleDataBuilder
    {
        const string Root = "Assets/_Project/Data";

        [MenuItem("BrineBattle/Create Sample Data")]
        public static void Build()
        {
            EnsureFolder(Root, "Items");
            EnsureFolder(Root, "Loot");
            EnsureFolder(Root, "Monsters");

            var chart = LoadOrCreate<ElementChart>($"{Root}/ElementChart.asset");

            var scale = LoadOrCreate<ItemDefinition>($"{Root}/Items/EelScale.asset", i =>
            {
                i.id = "eel_scale"; i.displayName = "Eel Scale"; i.type = ItemType.Material;
                i.rarity = Rarity.Common; i.element = Element.Water;
                i.description = "Slick, overlapping scales. Good raft plating.";
            });
            var fang = LoadOrCreate<ItemDefinition>($"{Root}/Items/EelFang.asset", i =>
            {
                i.id = "eel_fang"; i.displayName = "Eel Fang"; i.type = ItemType.Material;
                i.rarity = Rarity.Uncommon; i.element = Element.Water;
                i.description = "Hooked and needle-sharp. Weapon material.";
            });

            var loot = LoadOrCreate<LootTable>($"{Root}/Loot/BrinefangEel_Loot.asset", t =>
            {
                t.entries.Add(new LootEntry { item = scale, chance = 1f, minQuantity = 2, maxQuantity = 4 });
                t.entries.Add(new LootEntry { item = fang, chance = 0.3f, minQuantity = 1, maxQuantity = 1 });
            });

            LoadOrCreate<MonsterDefinition>($"{Root}/Monsters/BrinefangEel.asset", m =>
            {
                m.id = "brinefang_eel"; m.displayName = "Brinefang Eel"; m.element = Element.Water;
                m.habitat = Habitat.Sea; m.rarity = Rarity.Common; m.sizeClass = 1;
                m.stats = new MonsterStats { hp = 30, attack = 6, defense = 2, speed = 5 };
                m.loot = loot;
                m.description = "A small eel that drifts belly-up to lure hunters close.";
            });

            var feather = LoadOrCreate<ItemDefinition>($"{Root}/Items/GaleFeather.asset", i =>
            {
                i.id = "gale_feather"; i.displayName = "Gale Feather"; i.type = ItemType.Material;
                i.rarity = Rarity.Common; i.element = Element.Wind;
                i.description = "Light as foam. Sky-raft material.";
            });
            var gullLoot = LoadOrCreate<LootTable>($"{Root}/Loot/GaleGull_Loot.asset", t =>
            {
                t.entries.Add(new LootEntry { item = feather, chance = 1f, minQuantity = 1, maxQuantity = 3 });
            });
            LoadOrCreate<MonsterDefinition>($"{Root}/Monsters/GaleGull.asset", m =>
            {
                m.id = "gale_gull"; m.displayName = "Gale Gull"; m.element = Element.Wind;
                m.habitat = Habitat.Sky; m.rarity = Rarity.Common; m.sizeClass = 1;
                m.stats = new MonsterStats { hp = 20, attack = 5, defense = 1, speed = 9 };
                m.loot = gullLoot;
                m.description = "A storm gull that dives at anything shiny on a raft.";
            });

            var shell = LoadOrCreate<ItemDefinition>($"{Root}/Items/ShellbackPlate.asset", i =>
            {
                i.id = "shellback_plate"; i.displayName = "Shellback Plate"; i.type = ItemType.Raft;
                i.rarity = Rarity.Rare; i.element = Element.Earth;
                i.description = "A slab of crab shell. Fortress-raft part.";
            });
            var crabLoot = LoadOrCreate<LootTable>($"{Root}/Loot/ShellbackCrab_Loot.asset", t =>
            {
                t.entries.Add(new LootEntry { item = shell, chance = 1f, minQuantity = 1, maxQuantity = 2 });
                t.entries.Add(new LootEntry { item = scale, chance = 0.5f, minQuantity = 1, maxQuantity = 2 });
            });
            LoadOrCreate<MonsterDefinition>($"{Root}/Monsters/ShellbackCrab.asset", m =>
            {
                m.id = "shellback_crab"; m.displayName = "Shellback Crab"; m.element = Element.Earth;
                m.habitat = Habitat.Sea; m.rarity = Rarity.Rare; m.sizeClass = 3;
                m.stats = new MonsterStats { hp = 120, attack = 14, defense = 12, speed = 2 };
                m.loot = crabLoot;
                m.description = "Too big for a starter raft. Tests the raft-size gate.";
            });

            // Phase 4: gear so the bag has fits and misfits to salvage.
            var patch = LoadOrCreate<ItemDefinition>($"{Root}/Items/EelSkinPatch.asset", i =>
            {
                i.id = "eel_skin_patch"; i.displayName = "Eel-Skin Patch"; i.type = ItemType.Raft;
                i.rarity = Rarity.Common; i.element = Element.Water;
                i.description = "A cheap waterproof patch. Too weak for a sturdy raft.";
            });
            var spear = LoadOrCreate<ItemDefinition>($"{Root}/Items/EelFangSpear.asset", i =>
            {
                i.id = "eel_fang_spear"; i.displayName = "Eel-Fang Spear"; i.type = ItemType.Weapon;
                i.rarity = Rarity.Uncommon; i.element = Element.Water; i.maxStack = 1;
                i.description = "A fang lashed to a pole.";
            });
            var buckler = LoadOrCreate<ItemDefinition>($"{Root}/Items/GullBoneBuckler.asset", i =>
            {
                i.id = "gull_bone_buckler"; i.displayName = "Gull-Bone Buckler"; i.type = ItemType.Armor;
                i.rarity = Rarity.Common; i.element = Element.Wind; i.maxStack = 1;
                i.description = "Hollow bones woven into a light shield.";
            });
            // Phase 5: the starter raft is made of this; it also drifts in as loot.
            var plank = LoadOrCreate<ItemDefinition>($"{Root}/Items/DriftwoodPlank.asset", i =>
            {
                i.id = "driftwood_plank"; i.displayName = "Driftwood Plank"; i.type = ItemType.Raft;
                i.rarity = Rarity.Common; i.element = Element.None;
                i.description = "Grey, salt-bleached wood. What every raft starts as.";
            });
            // Idea Round 2 (U-13): Lv 1 auto-loot module.
            var net = LoadOrCreate<ItemDefinition>($"{Root}/Items/DriftNet.asset", i =>
            {
                i.id = "drift_net"; i.displayName = "Drift Net"; i.type = ItemType.Module;
                i.rarity = Rarity.Common; i.element = Element.None; i.moduleLevel = 1;
                i.description = "A basic net for the raft's edge. Catches whatever floats into it, a little at a time.";
            });
            var hookArm = LoadOrCreate<ItemDefinition>($"{Root}/Items/HookArm.asset", i =>
            {
                i.id = "hook_arm"; i.displayName = "Hook Arm"; i.type = ItemType.Module;
                i.rarity = Rarity.Common; i.element = Element.None; i.moduleLevel = 2;
                i.description = "A Drift Net with a fang hook on a pole. Reaches drifts that pass nearby.";
            });
            // Idea U-16: planks and patches become modules, modules upgrade, and come apart again.
            Recipe("craft_drift_net", "Drift Net", RecipeKind.Craft, new[] { (plank, 3), (patch, 2) }, new[] { (net, 1) });
            Recipe("upgrade_hook_arm", "Hook Arm", RecipeKind.Upgrade, new[] { (net, 1), (fang, 2), (plank, 2) }, new[] { (hookArm, 1) });
            Recipe("dismantle_drift_net", "Drift Net", RecipeKind.Dismantle, new[] { (net, 1) }, new[] { (plank, 2), (patch, 1) });
            Recipe("dismantle_hook_arm", "Hook Arm", RecipeKind.Dismantle, new[] { (hookArm, 1) }, new[] { (net, 1), (fang, 1) });
            // Phase 6: gear power (set every build so older assets get it too) and gear recipes.
            SetGearPower(spear, 4);
            SetGearPower(buckler, 3);
            Recipe("craft_eel_fang_spear", "Eel-Fang Spear", RecipeKind.Craft, new[] { (fang, 2), (plank, 1) }, new[] { (spear, 1) });
            Recipe("craft_gull_bone_buckler", "Gull-Bone Buckler", RecipeKind.Craft, new[] { (feather, 3), (plank, 1) }, new[] { (buckler, 1) });
            EnsureEntry(gullLoot, plank, 0.5f, 1, 2);
            EnsureEntry(loot, patch, 0.4f, 1, 2);
            EnsureEntry(loot, spear, 0.15f, 1, 1);
            EnsureEntry(gullLoot, buckler, 0.2f, 1, 1);

            // Idea U-17 temperament (set every build so older assets get it too).
            SetAggression("BrinefangEel", 0.8f);
            SetAggression("GaleGull", 0.3f);
            SetAggression("ShellbackCrab", 0.45f);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SampleDataBuilder] done, chart strong={chart.strong}");
        }

        static void Recipe(string id, string name, RecipeKind kind, (ItemDefinition, int)[] inputs, (ItemDefinition, int)[] outputs)
        {
            if (!AssetDatabase.IsValidFolder($"{Root}/Recipes")) AssetDatabase.CreateFolder(Root, "Recipes");
            var r = LoadOrCreate<RecipeDefinition>($"{Root}/Recipes/{id}.asset");
            r.id = id; r.displayName = name; r.kind = kind;
            r.inputs = inputs.Select(x => new Ingredient { item = x.Item1, count = x.Item2 }).ToList();
            r.outputs = outputs.Select(x => new Ingredient { item = x.Item1, count = x.Item2 }).ToList();
            EditorUtility.SetDirty(r);
        }

        static void SetGearPower(ItemDefinition item, int power)
        {
            if (item == null || item.gearPower == power) return;
            item.gearPower = power;
            EditorUtility.SetDirty(item);
        }

        static void SetAggression(string asset, float value)
        {
            var m = AssetDatabase.LoadAssetAtPath<MonsterDefinition>($"{Root}/Monsters/{asset}.asset");
            if (m == null || Mathf.Approximately(m.aggression, value)) return;
            m.aggression = value;
            EditorUtility.SetDirty(m);
        }

        static void EnsureEntry(LootTable table, ItemDefinition item, float chance, int min, int max)
        {
            if (table.entries.Exists(e => e.item == item)) return;
            table.entries.Add(new LootEntry { item = item, chance = chance, minQuantity = min, maxQuantity = max });
            EditorUtility.SetDirty(table);
        }

        static T LoadOrCreate<T>(string path, System.Action<T> init = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
