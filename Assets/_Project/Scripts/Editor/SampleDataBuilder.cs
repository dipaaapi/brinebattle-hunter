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
