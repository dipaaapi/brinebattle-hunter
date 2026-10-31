using System.Linq;
using BrineBattle.Battle;
using BrineBattle.Elements;
using BrineBattle.Items;
using BrineBattle.Loot;
using BrineBattle.Monsters;
using BrineBattle.Rafts;
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
            // Phase 7: companion habitats. Each on the raft gives room for one more companion.
            var tidePool = LoadOrCreate<ItemDefinition>($"{Root}/Items/TidePool.asset", i =>
            {
                i.id = "tide_pool"; i.displayName = "Tide Pool"; i.type = ItemType.Habitat;
                i.rarity = Rarity.Common; i.element = Element.Water;
                i.description = "A plank tub of seawater. A sea companion can rest here: room for one more companion.";
            });
            var skyPerch = LoadOrCreate<ItemDefinition>($"{Root}/Items/SkyPerch.asset", i =>
            {
                i.id = "sky_perch"; i.displayName = "Sky Perch"; i.type = ItemType.Habitat;
                i.rarity = Rarity.Common; i.element = Element.Wind;
                i.description = "A tall pole with a feather-lined nest. A sky companion can rest here: room for one more companion.";
            });
            Recipe("craft_tide_pool", "Tide Pool", RecipeKind.Craft, new[] { (plank, 4), (scale, 3) }, new[] { (tidePool, 1) });
            Recipe("craft_sky_perch", "Sky Perch", RecipeKind.Craft, new[] { (plank, 3), (feather, 3) }, new[] { (skyPerch, 1) });
            Recipe("dismantle_tide_pool", "Tide Pool", RecipeKind.Dismantle, new[] { (tidePool, 1) }, new[] { (plank, 2), (scale, 1) });
            Recipe("dismantle_sky_perch", "Sky Perch", RecipeKind.Dismantle, new[] { (skyPerch, 1) }, new[] { (plank, 2), (feather, 1) });
            // Ed's playtest: a leveled-up raft needs parts of its own level. Reinforce recipes turn lower parts into the next level.
            var tarred = RaftPart("TarredPlank", "tarred_plank", "Tarred Plank", Rarity.Uncommon, Element.Water,
                "Two planks bound with eel-skin and tar. Fits a Lv 2 raft.");
            var coral = RaftPart("CoralboundPlank", "coralbound_plank", "Coralbound Plank", Rarity.Epic, Element.Earth,
                "Shell plates grown together with living coral. Fits a Lv 4 raft.");
            var leviathan = RaftPart("LeviathanBeam", "leviathan_beam", "Leviathan Beam", Rarity.Legendary, Element.Water,
                "A beam studded with great fangs. Fits a Lv 5 raft.");
            var abyssal = RaftPart("AbyssalKeel", "abyssal_keel", "Abyssal Keel", Rarity.Mythic, Element.None,
                "Black wood from the deep that never rots. Fits a Lv 6 raft.");
            Recipe("reinforce_tarred_plank", "Tarred Plank", RecipeKind.Upgrade, new[] { (plank, 2), (patch, 1) }, new[] { (tarred, 1) });
            Recipe("reinforce_shellback_plate", "Shellback Plate", RecipeKind.Upgrade, new[] { (tarred, 2), (fang, 2) }, new[] { (shell, 1) });
            Recipe("reinforce_coralbound_plank", "Coralbound Plank", RecipeKind.Upgrade, new[] { (shell, 2), (scale, 2) }, new[] { (coral, 1) });
            Recipe("reinforce_leviathan_beam", "Leviathan Beam", RecipeKind.Upgrade, new[] { (coral, 2), (fang, 3) }, new[] { (leviathan, 1) });
            Recipe("reinforce_abyssal_keel", "Abyssal Keel", RecipeKind.Upgrade, new[] { (leviathan, 2), (shell, 3) }, new[] { (abyssal, 1) });
            // Phase 8: raft types. Typed parts at every raft level; most parts of one kind set the raft's type.
            SetRaftKind(shell, RaftKind.Fortress);
            SetRaftKind(coral, RaftKind.Fortress);
            // Ed's U-31: each level of a type also has its own refined material (the tree's node), opened by
            // picking that type at a raft level-up. Bases per level: Tarred, Shellback, Coralbound, Leviathan, Abyssal.
            var bases = new[] { tarred, shell, coral, leviathan, abyssal };
            var fortressParts = TypedParts(RaftKind.Fortress, "ShellPlating", "shell_plating", "Shell Plating", Element.Earth, plank, scale, bases,
                new[] { "Hardened Scale", "Shell Weave", "Coral Bulwark", "Leviathan Hide", "Abyssal Carapace" },
                "Scales riveted over planks. Fortress part: slow but sturdy.");
            var skyParts = TypedParts(RaftKind.Sky, "WingSail", "wing_sail", "Wing Sail", Element.Wind, plank, feather, bases,
                new[] { "Gale Down", "Storm Silk", "Cloud Bladder", "Tempest Plume", "Aether Membrane" },
                "A feathered sail on a frame. Sky part: quick, with a longer hook reach.");
            var beastParts = TypedParts(RaftKind.Beast, "BeastHarness", "beast_harness", "Beast Harness", Element.Water, plank, fang, bases,
                new[] { "Fang Rope", "Sinew Strap", "Bone Yoke", "Leviathan Tendon", "Abyssal Chain" },
                "Fang-studded straps for a monster to pull the raft. Beast part: hits harder and moves faster.");
            // Ed's ask: a boss per raft level, and the vehicle of each type (five parts, one per level from Lv 2).
            var cores = Bosses();
            VesselParts(RaftKind.Fortress, fortressParts, cores);
            VesselParts(RaftKind.Sky, skyParts, cores);
            VesselParts(RaftKind.Beast, beastParts, cores);
            // Ed's ask: building needs a hammer of the raft's level or better; each hammer upgrades from the last.
            var hammers = new[]
            {
                Hammer("DriftwoodHammer", "driftwood_hammer", "Driftwood Hammer", Rarity.Common, "A fang lashed to a plank. Builds on a Lv 1 raft."),
                Hammer("TarredHammer", "tarred_hammer", "Tarred Hammer", Rarity.Uncommon, "Heavier, tar-bound head. Builds on a raft up to Lv 2."),
                Hammer("ShellHammer", "shell_hammer", "Shell Hammer", Rarity.Rare, "A crab-shell head that doesn't chip. Builds on a raft up to Lv 3."),
                Hammer("CoralHammer", "coral_hammer", "Coral Hammer", Rarity.Epic, "Living coral, hard as stone. Builds on a raft up to Lv 4."),
                Hammer("LeviathanHammer", "leviathan_hammer", "Leviathan Hammer", Rarity.Legendary, "A great fang for a head. Builds on a raft up to Lv 5."),
                Hammer("AbyssalHammer", "abyssal_hammer", "Abyssal Hammer", Rarity.Mythic, "Black deep-wood that never breaks. Builds on any raft."),
            };
            Recipe("craft_driftwood_hammer", "Driftwood Hammer", RecipeKind.Craft, new[] { (plank, 2), (fang, 1) }, new[] { (hammers[0], 1) });
            Recipe("upgrade_tarred_hammer", "Tarred Hammer", RecipeKind.Upgrade, new[] { (hammers[0], 1), (tarred, 1), (fang, 1) }, new[] { (hammers[1], 1) });
            Recipe("upgrade_shell_hammer", "Shell Hammer", RecipeKind.Upgrade, new[] { (hammers[1], 1), (shell, 1), (fang, 2) }, new[] { (hammers[2], 1) });
            Recipe("upgrade_coral_hammer", "Coral Hammer", RecipeKind.Upgrade, new[] { (hammers[2], 1), (coral, 1), (scale, 2) }, new[] { (hammers[3], 1) });
            Recipe("upgrade_leviathan_hammer", "Leviathan Hammer", RecipeKind.Upgrade, new[] { (hammers[3], 1), (leviathan, 1), (fang, 3) }, new[] { (hammers[4], 1) });
            Recipe("upgrade_abyssal_hammer", "Abyssal Hammer", RecipeKind.Upgrade, new[] { (hammers[4], 1), (abyssal, 1), (shell, 2) }, new[] { (hammers[5], 1) });
            // Phase 6: gear power (set every build so older assets get it too) and gear recipes.
            SetGearPower(spear, 4);
            SetGearPower(buckler, 3);
            Recipe("craft_eel_fang_spear", "Eel-Fang Spear", RecipeKind.Craft, new[] { (fang, 2), (plank, 1) }, new[] { (spear, 1) });
            Recipe("craft_gull_bone_buckler", "Gull-Bone Buckler", RecipeKind.Craft, new[] { (feather, 3), (plank, 1) }, new[] { (buckler, 1) });
            // Phase 6: consumables, used in battle (Bag holds them; the battle panel shows a button each).
            var tonic = LoadOrCreate<ItemDefinition>($"{Root}/Items/BrineTonic.asset", i =>
            {
                i.id = "brine_tonic"; i.displayName = "Brine Tonic"; i.type = ItemType.Consumable;
                i.rarity = Rarity.Common; i.element = Element.Water;
                i.effect = ConsumableEffect.HealHunter; i.effectAmount = 25;
                i.description = "Salty, bitter, and it closes cuts. Restores 25 HP.";
            });
            var tarPatch = LoadOrCreate<ItemDefinition>($"{Root}/Items/TarPatch.asset", i =>
            {
                i.id = "tar_patch"; i.displayName = "Tar Patch"; i.type = ItemType.Consumable;
                i.rarity = Rarity.Common; i.element = Element.None;
                i.effect = ConsumableEffect.RepairRaft; i.effectAmount = 30;
                i.description = "An eel-skin patch with a plank backing, slapped on mid-fight. Restores 30 raft HP.";
            });
            // Phase 7: companion treat (heals or wakes the battle companion).
            var biscuit = LoadOrCreate<ItemDefinition>($"{Root}/Items/KelpBiscuit.asset", i =>
            {
                i.id = "kelp_biscuit"; i.displayName = "Kelp Biscuit"; i.type = ItemType.Consumable;
                i.rarity = Rarity.Common; i.element = Element.Water;
                i.effect = ConsumableEffect.HealCompanion; i.effectAmount = 20;
                i.description = "A crunchy treat for companions. Heals your battle companion 20 HP, or wakes it up if it was knocked out.";
            });
            Recipe("craft_kelp_biscuit", "Kelp Biscuit", RecipeKind.Craft, new[] { (scale, 1), (fang, 1) }, new[] { (biscuit, 1) });
            Recipe("craft_brine_tonic", "Brine Tonic", RecipeKind.Craft, new[] { (scale, 2), (feather, 1) }, new[] { (tonic, 1) });
            Recipe("craft_tar_patch", "Tar Patch", RecipeKind.Craft, new[] { (patch, 1), (plank, 1) }, new[] { (tarPatch, 1) });
            EnsureEntry(loot, tonic, 0.2f, 1, 1);

            // Phase 6: gear skills by rarity (GDD §9). Rare = 1 passive, Epic = 1 passive + 1 active.
            var maul = LoadOrCreate<ItemDefinition>($"{Root}/Items/ShellMaul.asset", i =>
            {
                i.id = "shell_maul"; i.displayName = "Shell Maul"; i.type = ItemType.Weapon;
                i.rarity = Rarity.Rare; i.element = Element.Earth; i.maxStack = 1; i.gearPower = 7;
                i.passives = new[] { GearPassive.Thorns };
                i.description = "A crab claw on a club. Whatever bites you gets spikes for its trouble.";
            });
            var lance = LoadOrCreate<ItemDefinition>($"{Root}/Items/TidecallerLance.asset", i =>
            {
                i.id = "tidecaller_lance"; i.displayName = "Tidecaller Lance"; i.type = ItemType.Weapon;
                i.rarity = Rarity.Epic; i.element = Element.Water; i.maxStack = 1; i.gearPower = 9;
                i.passives = new[] { GearPassive.Leech };
                i.actives = new[] { GearActive.TidalStrike };
                i.description = "An eel-fang spear bound in shell. It drinks what it cuts and calls the tide behind a thrust.";
            });
            var carapace = LoadOrCreate<ItemDefinition>($"{Root}/Items/ShellbackCarapace.asset", i =>
            {
                i.id = "shellback_carapace"; i.displayName = "Shellback Carapace"; i.type = ItemType.Armor;
                i.rarity = Rarity.Epic; i.element = Element.Earth; i.maxStack = 1; i.gearPower = 6;
                i.passives = new[] { GearPassive.Bulwark };
                i.actives = new[] { GearActive.ShellGuard };
                i.description = "A whole crab back, worn. Its bulk shields the raft as much as you.";
            });
            Recipe("craft_shell_maul", "Shell Maul", RecipeKind.Craft, new[] { (shell, 3), (fang, 2) }, new[] { (maul, 1) });
            Recipe("craft_tidecaller_lance", "Tidecaller Lance", RecipeKind.Craft, new[] { (spear, 1), (fang, 4), (shell, 3) }, new[] { (lance, 1) });
            Recipe("craft_shellback_carapace", "Shellback Carapace", RecipeKind.Craft, new[] { (shell, 5), (scale, 4) }, new[] { (carapace, 1) });
            // Ed's ask: accessories, a third gear slot. Their worth is their skills (by rarity), element and + refine.
            var charm = Accessory("ShellCharm", "shell_charm", "Shell Charm", Rarity.Uncommon, Element.Earth, 2, null, null,
                "A crab-shell bead on a cord. A little sturdier, a little stronger. Earth gives the hunter no sun or moon.");
            var earring = Accessory("GaleEarring", "gale_earring", "Gale Feather Earring", Rarity.Rare, Element.Wind, 3,
                new[] { GearPassive.Swift }, null, "A feather that never stops fluttering. Swift: +2 SPD in battle.");
            var amulet = Accessory("MoonpearlAmulet", "moonpearl_amulet", "Moonpearl Amulet", Rarity.Epic, Element.Water, 4,
                new[] { GearPassive.SecondWind }, new[] { GearActive.TidalStrike },
                "A pearl that glows at night. Water (Moon): stronger after dark. Second Wind, and Tidal Strike in battle.");
            var sunstone = Accessory("SunstoneBand", "sunstone_band", "Sunstone Band", Rarity.Legendary, Element.Fire, 5,
                new[] { GearPassive.Leech, GearPassive.Swift }, new[] { GearActive.ShellGuard },
                "A ring of tar-baked stone that holds the day's heat. Fire (Sun): stronger by day. Leech, Swift, Shell Guard.");
            Recipe("craft_shell_charm", "Shell Charm", RecipeKind.Craft, new[] { (shell, 1), (fang, 1) }, new[] { (charm, 1) });
            Recipe("craft_gale_earring", "Gale Feather Earring", RecipeKind.Craft, new[] { (feather, 4), (fang, 1) }, new[] { (earring, 1) });
            Recipe("craft_moonpearl_amulet", "Moonpearl Amulet", RecipeKind.Craft, new[] { (scale, 6), (shell, 2), (fang, 2) }, new[] { (amulet, 1) });
            Recipe("craft_sunstone_band", "Sunstone Band", RecipeKind.Craft, new[] { (charm, 1), (tarred, 3), (shell, 4) }, new[] { (sunstone, 1) });
            Recipe("dismantle_shell_charm", "Shell Charm", RecipeKind.Dismantle, new[] { (charm, 1) }, new[] { (shell, 1) });
            // Phase 6: elemental oils coat the weapon for a few rounds (Wind beats the eel, Earth the gull, Fire the crab).
            var galeOil = Oil("GaleOil", "gale_oil", "Gale Oil", Element.Wind, "Feather down steeped in fish oil. Attacks turn Wind for 3 rounds.");
            var gritOil = Oil("GritOil", "grit_oil", "Grit Oil", Element.Earth, "Ground crab shell in a paste. Attacks turn Earth for 3 rounds.");
            var pitchOil = Oil("PitchOil", "pitch_oil", "Burning Pitch", Element.Fire, "Tar and plank shavings, lit before the swing. Attacks turn Fire for 3 rounds.");
            Recipe("craft_gale_oil", "Gale Oil", RecipeKind.Craft, new[] { (feather, 2), (scale, 1) }, new[] { (galeOil, 1) });
            Recipe("craft_grit_oil", "Grit Oil", RecipeKind.Craft, new[] { (shell, 1), (scale, 1) }, new[] { (gritOil, 1) });
            Recipe("craft_burning_pitch", "Burning Pitch", RecipeKind.Craft, new[] { (patch, 1), (plank, 2) }, new[] { (pitchOil, 1) });
            EnsureEntry(gullLoot, plank, 0.5f, 1, 2);
            EnsureEntry(loot, patch, 0.4f, 1, 2);
            EnsureEntry(loot, spear, 0.15f, 1, 1);
            EnsureEntry(gullLoot, buckler, 0.2f, 1, 1);

            // Idea U-17 temperament (set every build so older assets get it too).
            SetAggression("BrinefangEel", 0.8f);
            SetAggression("GaleGull", 0.3f);
            SetAggression("ShellbackCrab", 0.45f);

            // Ed's U-30: a module line by rarity (each upgrades from the last) and floating loot that only
            // modules of its rarity or better catch, so better modules gather more of what the raft needs.
            var nets = new[]
            {
                Module("TarredNet", "tarred_net", "Tarred Net", Rarity.Uncommon, 2, "A tar-soaked net on a pole. Catches floating loot up to Uncommon."),
                Module("ShellTrap", "shell_trap", "Shell Trap", Rarity.Rare, 3, "Crab-shell jaws that snap shut. Catches floating loot up to Rare."),
                Module("CoralSnare", "coral_snare", "Coral Snare", Rarity.Epic, 4, "Living coral loops that grab and hold. Catches floating loot up to Epic."),
                Module("LeviathanGrapple", "leviathan_grapple", "Leviathan Grapple", Rarity.Legendary, 5, "A great fang on a long chain. Catches floating loot up to Legendary."),
                Module("AbyssalMaw", "abyssal_maw", "Abyssal Maw", Rarity.Mythic, 5, "Deep-wood jaws that never let go. Catches any floating loot."),
            };
            var netBases = new[] { tarred, shell, coral, leviathan, abyssal };
            var netExtra = new[] { (fang, 1), (fang, 2), (scale, 2), (fang, 3), (shell, 2) };
            ItemDefinition prevNet = hookArm;
            for (int n = 0; n < nets.Length; n++)
            {
                Recipe($"upgrade_{nets[n].id}", nets[n].displayName, RecipeKind.Upgrade, new[] { (prevNet, 1), (netBases[n], 2), netExtra[n] }, new[] { (nets[n], 1) });
                Recipe($"dismantle_{nets[n].id}", nets[n].displayName, RecipeKind.Dismantle, new[] { (nets[n], 1) }, new[] { (prevNet, 1), (netBases[n], 1) });
                prevNet = nets[n];
            }
            Flotsam("DriftwoodBundle", "driftwood_bundle", "Driftwood Bundle", Rarity.Common, (plank, 1f, 2, 3), (patch, 0.4f, 1, 1));
            Flotsam("TarBarrel", "tar_barrel", "Tar Barrel", Rarity.Uncommon, (tarred, 1f, 1, 2), (patch, 0.6f, 1, 2));
            Flotsam("ShellWreck", "shell_wreck", "Shell Wreck", Rarity.Rare, (shell, 1f, 1, 2), (scale, 0.6f, 1, 2));
            Flotsam("CoralChunk", "coral_chunk", "Coral Chunk", Rarity.Epic, (coral, 1f, 1, 1), (scale, 0.5f, 1, 2));
            Flotsam("LeviathanBones", "leviathan_bones", "Leviathan Bones", Rarity.Legendary, (leviathan, 1f, 1, 1), (fang, 0.8f, 2, 3));
            Flotsam("AbyssalDriftwood", "abyssal_driftwood", "Abyssal Driftwood", Rarity.Mythic, (abyssal, 1f, 1, 1));

            // Ed's U-33: ailments from monster hits (shown over drifts, C-11), and potions that cure them.
            SetInflicts("BrinefangEel", StatusEffect.Poison, 0.25f);
            SetInflicts("GaleGull", StatusEffect.Blind, 0.25f);
            SetInflicts("ShellbackCrab", StatusEffect.Bleed, 0.3f);
            var antidote = Cure("ToxicRelief", "toxic_relief", "Toxic Relief", StatusEffect.Poison,
                "Eel venom, boiled down and turned against itself. Cures poison.");
            var calm = Cure("ConfusionRelief", "confusion_relief", "Confusion Relief", StatusEffect.Confusion,
                "Sharp-smelling feather salts. Clears a dazed head.");
            var clot = Cure("ClottingPatch", "clotting_patch", "Clotting Patch", StatusEffect.Bleed,
                "An eel-skin patch packed with crushed scale. Stops bleeding.");
            var eyes = Cure("EyeCleanser", "eye_cleanser", "Eye Cleanser", StatusEffect.Blind,
                "Fresh water filtered through planks. Clears blinded eyes, so skills work again.");
            Recipe("craft_toxic_relief", "Toxic Relief", RecipeKind.Craft, new[] { (fang, 1), (feather, 1) }, new[] { (antidote, 1) });
            Recipe("craft_confusion_relief", "Confusion Relief", RecipeKind.Craft, new[] { (feather, 2), (fang, 1) }, new[] { (calm, 1) });
            Recipe("craft_clotting_patch", "Clotting Patch", RecipeKind.Craft, new[] { (patch, 1), (scale, 1) }, new[] { (clot, 1) });
            Recipe("craft_eye_cleanser", "Eye Cleanser", RecipeKind.Craft, new[] { (scale, 1), (plank, 1) }, new[] { (eyes, 1) });
            var bigTonic = LoadOrCreate<ItemDefinition>($"{Root}/Items/BigBrineTonic.asset", i =>
            {
                i.id = "big_brine_tonic"; i.displayName = "Big Brine Tonic"; i.type = ItemType.Consumable;
                i.rarity = Rarity.Uncommon; i.element = Element.Water;
                i.effect = ConsumableEffect.HealHunter; i.effectAmount = 60;
                i.description = "Two tonics and a fang, steeped overnight. Restores 60 HP.";
            });
            Recipe("craft_big_brine_tonic", "Big Brine Tonic", RecipeKind.Craft, new[] { (tonic, 2), (fang, 1) }, new[] { (bigTonic, 1) });
            EnsureEntry(gullLoot, antidote, 0.1f, 1, 1);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SampleDataBuilder] done, chart strong={chart.strong}");
        }

        static ItemDefinition Hammer(string file, string id, string name, Rarity rarity, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Tool;
                i.rarity = rarity; i.element = Element.None; i.maxStack = 1;
                i.description = description;
            });

        static ItemDefinition RaftPart(string file, string id, string name, Rarity rarity, Element element, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Raft;
                i.rarity = rarity; i.element = element;
                i.description = description;
            });

        static void SetRaftKind(ItemDefinition part, RaftKind kind)
        {
            if (part.raftKind == kind) return;
            part.raftKind = kind;
            EditorUtility.SetDirty(part);
        }

        /// <summary>
        /// One typed part per raft level (Phase 8) and the material tree behind it (U-31). Lv 1: 1 plank +
        /// 2 of the kind's raw material. From Lv 2 the level's refined material (node) = the level's base part
        /// + level × raw material, and the part = the last level's part + that node. Names say the level.
        /// </summary>
        static ItemDefinition[] TypedParts(RaftKind kind, string file, string id, string name, Element element,
            ItemDefinition plank, ItemDefinition material, ItemDefinition[] bases, string[] nodeNames, string description)
        {
            var made = new ItemDefinition[6];
            ItemDefinition last = null;
            for (var r = Rarity.Common; r <= Rarity.Mythic; r++)
            {
                int lv = (int)r + 1;
                string suffix = lv == 1 ? "" : $" Lv {lv}";
                var part = RaftPart($"{file}{(lv == 1 ? "" : lv.ToString())}", $"{id}{(lv == 1 ? "" : "_" + lv)}", name + suffix, r, element,
                    $"{description} Fits a raft up to Lv {lv}.");
                SetRaftKind(part, kind);
                if (last == null)
                    Recipe($"craft_{id}", name, RecipeKind.Craft, new[] { (plank, 1), (material, 2) }, new[] { (part, 1) });
                else
                {
                    string nodeName = nodeNames[lv - 2];
                    string nodeFile = nodeName.Replace(" ", "");
                    var node = LoadOrCreate<ItemDefinition>($"{Root}/Items/{nodeFile}.asset", i =>
                    {
                        i.id = nodeName.ToLowerInvariant().Replace(" ", "_"); i.displayName = nodeName; i.type = ItemType.Material;
                        i.rarity = r; i.element = element;
                        i.description = $"{kind} material for a Lv {lv} raft (raft tree).";
                    });
                    SetRaftKind(node, kind);
                    Recipe($"refine_{node.id}", nodeName, RecipeKind.Craft, new[] { (bases[lv - 2], 1), (material, lv) }, new[] { (node, 1) });
                    Recipe($"reinforce_{id}_{lv}", name + suffix, RecipeKind.Upgrade, new[] { (last, 1), (node, 1) }, new[] { (part, 1) });
                }
                last = part;
                made[(int)r] = part;
            }
            return made;
        }

        /// <summary>
        /// One boss per raft level (Ed's ask, numbers tunable): beat the boss of the raft's level to level
        /// up; the Lv 6 boss guards the way out of the sea. Each drops a core used for that level's vehicle part.
        /// Returns the cores by level (index 0 = Lv 1 boss).
        /// </summary>
        static ItemDefinition[] Bosses()
        {
            var specs = new (string file, string id, string name, Element el, Habitat hab, int hp, int atk, int def, int spd, StatusEffect inflicts, string core, string about)[]
            {
                ("ReefjawBrute", "reefjaw_brute", "Reefjaw Brute", Element.Earth, Habitat.Sea, 60, 9, 5, 4, StatusEffect.Bleed, "Reefjaw Tusk",
                    "A coral-crusted brute that rams small rafts. The Lv 1 boss."),
                ("TidecallerEel", "tidecaller_eel", "Tidecaller Eel", Element.Water, Habitat.Sea, 110, 14, 7, 8, StatusEffect.Poison, "Tidecaller Pearl",
                    "An eel that calls the tide to swamp the raft. The Lv 2 boss."),
                ("StormwingRoc", "stormwing_roc", "Stormwing Roc", Element.Lightning, Habitat.Sky, 170, 20, 10, 11, StatusEffect.Blind, "Storm Heart",
                    "A giant bird wreathed in lightning. The Lv 3 boss."),
                ("MagmaKraken", "magma_kraken", "Magma Kraken", Element.Magma, Habitat.Sea, 260, 28, 15, 7, StatusEffect.Bleed, "Kraken Ember",
                    "Glowing tentacles that boil the sea around them. The Lv 4 boss."),
                ("FrostLeviathan", "frost_leviathan", "Frost Leviathan", Element.Ice, Habitat.Sea, 380, 38, 22, 9, StatusEffect.Confusion, "Leviathan Frost-Eye",
                    "A sea serpent that freezes the waves it swims through. The Lv 5 boss."),
                ("AbyssKing", "abyss_king", "The Abyss King", Element.Steam, Habitat.Sea, 560, 52, 30, 10, StatusEffect.Poison, "Abyss Crown",
                    "The ruler of this sea. Beat it to leave. The Lv 6 boss."),
            };
            var cores = new ItemDefinition[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var sp = specs[i];
                var r = (Rarity)i;
                var core = LoadOrCreate<ItemDefinition>($"{Root}/Items/{sp.core.Replace(" ", "").Replace("-", "")}.asset", it =>
                {
                    it.id = sp.core.ToLowerInvariant().Replace(" ", "_").Replace("-", "_"); it.displayName = sp.core;
                    it.type = ItemType.Material; it.rarity = r; it.element = sp.el;
                    it.description = $"Dropped by {sp.name}. Used for a vehicle part.";
                });
                var loot = LoadOrCreate<LootTable>($"{Root}/Loot/{sp.file}_Loot.asset", t =>
                    t.entries.Add(new LootEntry { item = core, chance = 1f, minQuantity = 1, maxQuantity = 1 }));
                var boss = LoadOrCreate<MonsterDefinition>($"{Root}/Monsters/{sp.file}.asset", m =>
                {
                    m.id = sp.id; m.displayName = sp.name; m.element = sp.el; m.habitat = sp.hab; m.rarity = r;
                    m.sizeClass = 1; m.aggression = 1f; m.isBoss = true;
                    m.stats = new MonsterStats { hp = sp.hp, attack = sp.atk, defense = sp.def, speed = sp.spd };
                    m.inflicts = sp.inflicts; m.inflictChance = 0.3f;
                    m.loot = loot;
                    m.description = sp.about;
                });
                if (!boss.isBoss) { boss.isBoss = true; EditorUtility.SetDirty(boss); }
                cores[i] = core;
            }
            return cores;
        }

        /// <summary>
        /// The vehicle of a raft type (Ed's ask): five parts, slot n opening at raft Lv n+1. Each takes two
        /// typed parts of its level and the core of the boss before it.
        /// </summary>
        static void VesselParts(RaftKind kind, ItemDefinition[] typed, ItemDefinition[] cores)
        {
            string vehicle = VesselRules.Vehicle(kind);
            for (int slot = 1; slot <= VesselRules.Parts; slot++)
            {
                string name = VesselRules.PartName(kind, slot);
                string id = $"{kind.ToString().ToLowerInvariant()}_{name.ToLowerInvariant().Replace(" & ", "_").Replace(" ", "_")}";
                string file = $"Vessel{kind}{name.Replace(" & ", "").Replace(" ", "")}";
                int lv = VesselRules.UnlockLevel(slot);
                var part = LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", it =>
                {
                    it.id = id; it.displayName = name; it.type = ItemType.Vessel; it.maxStack = 1;
                    it.rarity = VesselRules.RarityOf(slot); it.element = typed[slot].element;
                    it.raftKind = kind; it.vesselSlot = slot;
                    it.description = $"Part {slot} of 5 of the {vehicle}. Opens at raft Lv {lv}. Put it on in Build → Raft tree.";
                });
                Recipe($"craft_{id}", name, RecipeKind.Craft, new[] { (typed[slot], 2), (cores[slot - 1], 1) }, new[] { (part, 1) });
            }
        }

        static ItemDefinition Oil(string file, string id, string name, Element element, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Consumable;
                i.rarity = Rarity.Common; i.element = element;
                i.effect = ConsumableEffect.CoatWeapon; i.effectAmount = 3;
                i.description = description;
            });

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

        static ItemDefinition Module(string file, string id, string name, Rarity rarity, int level, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Module;
                i.rarity = rarity; i.element = Element.None; i.moduleLevel = level;
                i.description = description;
            });

        /// <summary>Floating loot (U-30): a "monster" that is always dead and only carries loot.</summary>
        static void Flotsam(string file, string id, string name, Rarity rarity, params (ItemDefinition item, float chance, int min, int max)[] drops)
        {
            EnsureFolder(Root, "Flotsam");
            var table = LoadOrCreate<LootTable>($"{Root}/Flotsam/{file}_Loot.asset", t =>
            {
                foreach (var d in drops) t.entries.Add(new LootEntry { item = d.item, chance = d.chance, minQuantity = d.min, maxQuantity = d.max });
            });
            LoadOrCreate<MonsterDefinition>($"{Root}/Flotsam/{file}.asset", m =>
            {
                m.id = id; m.displayName = name; m.isFlotsam = true; m.element = Element.None;
                m.habitat = Habitat.Sea; m.rarity = rarity; m.sizeClass = 1; m.aggression = 0f;
                m.stats = new MonsterStats { hp = 1, attack = 0, defense = 0, speed = 0 };
                m.loot = table;
                m.description = "Floating loot. Hook it, or let a module of its rarity or better catch it.";
            });
        }

        static void SetInflicts(string asset, StatusEffect status, float chance)
        {
            var m = AssetDatabase.LoadAssetAtPath<MonsterDefinition>($"{Root}/Monsters/{asset}.asset");
            if (m == null || (m.inflicts == status && Mathf.Approximately(m.inflictChance, chance))) return;
            m.inflicts = status;
            m.inflictChance = chance;
            EditorUtility.SetDirty(m);
        }

        static ItemDefinition Cure(string file, string id, string name, StatusEffect status, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Consumable;
                i.rarity = Rarity.Common; i.element = Element.None;
                i.effect = ConsumableEffect.CureStatus; i.cures = status; i.effectAmount = 1;
                i.description = description;
            });

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

        static ItemDefinition Accessory(string file, string id, string name, Rarity rarity, Element element, int power,
            GearPassive[] passives, GearActive[] actives, string description) =>
            LoadOrCreate<ItemDefinition>($"{Root}/Items/{file}.asset", i =>
            {
                i.id = id; i.displayName = name; i.type = ItemType.Accessory;
                i.rarity = rarity; i.element = element; i.maxStack = 1; i.gearPower = power;
                i.passives = passives ?? new GearPassive[0];
                i.actives = actives ?? new GearActive[0];
                i.description = description;
            });

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
