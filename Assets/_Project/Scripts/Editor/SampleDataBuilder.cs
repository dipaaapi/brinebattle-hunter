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

            AssetDatabase.SaveAssets();
            Debug.Log($"[SampleDataBuilder] done, chart strong={chart.strong}");
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
