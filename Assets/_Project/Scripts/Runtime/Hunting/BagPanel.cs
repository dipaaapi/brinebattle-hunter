using System.Collections.Generic;
using System.Linq;
using BrineBattle.Companions;
using BrineBattle.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Phase 4 bag: items by type and rarity, compatibility against gear tiers, salvage into
    /// Monster Essence (GDD §11). Placeholder IMGUI; press B or the HUD button.
    /// </summary>
    public class BagPanel : MonoBehaviour
    {
        public HookController hook;

        [Header("Target tiers (placeholder until Phases 5-7 own them)")]
        public Rarity raftTier = Rarity.Uncommon;
        public Rarity weaponTier = Rarity.Common;
        public Rarity armorTier = Rarity.Common;
        public Rarity companionTier = Rarity.Common;
        [Tooltip("Salvage misfits as soon as they are picked up.")]
        public bool autoSalvage;

        static bool open => WorldPause.Open == Menu.Bag;
        Vector2 scroll;

        void OnEnable()
        {
            if (hook == null) return;
            hook.BagToggleRequested += Toggle;
            hook.LootAdded += OnLoot;
        }

        void OnDisable()
        {
            if (hook == null) return;
            hook.BagToggleRequested -= Toggle;
            hook.LootAdded -= OnLoot;
        }

        void Toggle() => WorldPause.Toggle(Menu.Bag);

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) Toggle();
        }

        public Rarity TierOf(ItemType t)
        {
            switch (t)
            {
                case ItemType.Raft: return hook != null && hook.raft != null ? hook.raft.Tier : raftTier;
                case ItemType.Weapon: return weaponTier;
                case ItemType.Armor: return armorTier;
                case ItemType.Companion: return companionTier;
                default: return Rarity.Common;
            }
        }

        /// <summary>
        /// Items a workbench recipe uses are never salvaged as misfits: a Common plank is below a Lv 2 raft
        /// but still needed for modules, habitats, gear and potions (Ed's playtest).
        /// </summary>
        bool IsIngredient(ItemDefinition item) =>
            hook.builder != null && hook.builder.recipes.Any(r => r != null && r.kind != RecipeKind.Dismantle && r.inputs.Any(x => x.item == item));

        void OnLoot()
        {
            if (!autoSalvage) return;
            int gained = hook.Inventory.SalvageIncompatible(TierOf, IsIngredient);
            if (gained > 0) hook.Log($"Auto-salvaged misfits into {gained} essence.");
        }

        enum Page { Items, Gear, Essence, Salvage }
        static readonly string[] PageLabels = { "Items", "Equipment", "Essence", "Auto-salvage" };
        static readonly string[] Filters = { "All", "Materials", "Raft", "Gear", "Potions", "Tools" };
        Page page;
        int filter;

        static bool InFilter(ItemType t, int f)
        {
            switch (f)
            {
                case 1: return t == ItemType.Material || t == ItemType.Essence;
                case 2: return t == ItemType.Raft || t == ItemType.Module || t == ItemType.Habitat || t == ItemType.Vessel;
                case 3: return GearRules.IsGear(t);
                case 4: return t == ItemType.Consumable;
                case 5: return t == ItemType.Tool;
                default: return true;
            }
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open || hook == null) return;
            GUI.depth = -5;
            float w = Mathf.Min(700, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(600, HuntStyles.H - 100);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Bag", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Bag); GUILayout.EndArea(); return; }

            // Owner idea U-25: one short page at a time.
            var picked = (Page)HuntStyles.Tabs((int)page, PageLabels);
            if (picked != page) { page = picked; scroll = Vector2.zero; }
            if (page == Page.Items) { DrawItems(w); GUILayout.EndArea(); return; }
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            switch (page)
            {
                case Page.Gear: DrawGear(); break;
                case Page.Essence: DrawEssence(); break;
                default: DrawSalvage(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        readonly List<HuntStyles.GridTile> tiles = new List<HuntStyles.GridTile>();
        readonly List<ItemDefinition> tileItems = new List<ItemDefinition>();
        ItemDefinition picked;

        /// <summary>
        /// Items as a tile grid with counts (Ed: the Bag looked outdated); tap one and its card at the bottom
        /// says what it is and offers Equip, Use and Salvage.
        /// </summary>
        void DrawItems(float w)
        {
            var inv = hook.Inventory;
            filter = GUILayout.Toolbar(filter, Filters, GUILayout.Height(26));
            var list = inv.Items.Where(kv => InFilter(kv.Key.type, filter)).OrderBy(kv => kv.Key.type).ThenBy(kv => kv.Key.rarity).ToList();
            if (list.Count == 0) { GUILayout.Label(filter == 0 ? "The bag is empty." : "Nothing of this kind yet.", HuntStyles.Text); return; }
            if (picked != null && inv.Count(picked) <= 0) picked = null;
            tiles.Clear();
            tileItems.Clear();
            foreach (var kv in list)
            {
                var item = kv.Key;
                bool fits = !SalvageRules.IsMisfit(item.type, item.rarity, TierOf(item.type));
                tiles.Add(new HuntStyles.GridTile
                {
                    Title = item.displayName, Badge = $"×{kv.Value}", Swatch = ItemTint(item),
                    Rim = fits ? HuntStyles.RarityTint(item.rarity) : new Color(0.9f, 0.4f, 0.35f),
                });
                tileItems.Add(item);
            }
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandHeight(true));
            int clicked = HuntStyles.TileGrid(tiles, w - 48, tileItems.IndexOf(picked), 84f);
            if (clicked >= 0) picked = tileItems[clicked];
            GUILayout.EndScrollView();
            ItemCard(picked);
        }

        /// <summary>The picked item: what it is and what you can do with it.</summary>
        void ItemCard(ItemDefinition item)
        {
            var inv = hook.Inventory;
            GUILayout.BeginVertical(HuntStyles.Card, GUILayout.MinHeight(86));
            if (item == null)
            {
                GUILayout.Label("Tap an item to see what it is and what you can do with it. A red rim means it's below your tier.", HuntStyles.Hint);
                GUILayout.EndVertical();
                return;
            }
            bool fits = !SalvageRules.IsMisfit(item.type, item.rarity, TierOf(item.type));
            GUILayout.Label($"<color={RarityColor(item.rarity)}>{item.displayName}</color>  <size=12>× {inv.Count(item)}</size>", HuntStyles.Heading);
            GUILayout.BeginHorizontal();
            HuntStyles.Chip(TypeName(item.type), HuntStyles.TealDeep);
            HuntStyles.Chip(item.rarity.ToString(), HuntStyles.RarityTint(item.rarity) * 0.55f);
            if (item.element != Elements.Element.None) HuntStyles.Chip(item.element.ToString(), Tint.Of(item.element) * 0.55f);
            if (!fits) HuntStyles.Chip($"below your {item.type} tier", new Color(0.55f, 0.2f, 0.18f));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(item.description)) GUILayout.Label(item.description, HuntStyles.Hint);
            string skills = SkillText(item);
            if (skills.Length > 0) GUILayout.Label(skills, HuntStyles.Hint);
            GUILayout.BeginHorizontal();
            if (GearRules.IsGear(item.type))
            {
                GUI.enabled = hook.Gear.CanEquip(item, inv, TierOf(item.type));
                if (HuntStyles.ColorButton("Equip", HuntStyles.Go, GUILayout.Width(110)) && hook.Gear.Equip(item, inv, TierOf(item.type)))
                    hook.Log($"Equipped {hook.Gear.Label(GearRules.SlotOf(item.type))}.");
                GUI.enabled = true;
            }
            // U-32/U-33: drink a potion on the raft (heal or cure the hunter).
            if (item.type == ItemType.Consumable && hook.TreatsFor(null).Contains(item)
                && HuntStyles.ColorButton("Use", HuntStyles.Go, GUILayout.Width(90))) hook.Treat(item, null);
            int yield = SalvageRules.EssenceYield(item.type, item.rarity);
            if (yield > 0)
            {
                if (HuntStyles.ColorButton($"Salvage 1 (+{yield} essence)", HuntStyles.Other, GUILayout.Width(200))) Salvage(item, 1);
                int n = inv.Count(item);
                if (n > 1 && HuntStyles.ColorButton($"Salvage all {n}", HuntStyles.Risky, GUILayout.Width(150))) Salvage(item, n);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        /// <summary>Items without an element get a colour by kind, so the grid isn't all grey.</summary>
        static Color ItemTint(ItemDefinition i)
        {
            if (i.element != Elements.Element.None) return Tint.Of(i.element);
            switch (i.type)
            {
                case ItemType.Raft: return Raft.PartColor(i);
                case ItemType.Consumable: return new Color(0.85f, 0.45f, 0.75f);
                case ItemType.Tool: return new Color(0.75f, 0.6f, 0.4f);
                case ItemType.Module: return new Color(0.3f, 0.65f, 0.55f);
                case ItemType.Vessel: return new Color(0.55f, 0.5f, 0.75f);
                default: return HuntStyles.RarityTint(i.rarity);
            }
        }

        static string TypeName(ItemType t) => t == ItemType.Consumable ? "Potion/Oil" : t == ItemType.Raft ? "Raft part" : t.ToString();

        void DrawGear()
        {
            var hunter = Battle.Combatant.Hunter(hook.Gear);
            string el = hunter.Element == Elements.Element.None ? "Neutral" : hunter.Element.ToString();
            GUILayout.Label($"Hunter: <b>ATK {hunter.Attack} · DEF {hunter.Defense}</b> · {el}", HuntStyles.Heading);
            GearRow(GearSlot.Weapon);
            GearRow(GearSlot.Armor);
            GearRow(GearSlot.Accessory);
            GUILayout.Space(6);
            GUILayout.Label("<b>Gear in the bag</b>", HuntStyles.Text);
            var gear = hook.Inventory.Items.Where(kv => GearRules.IsGear(kv.Key.type)).ToList();
            if (gear.Count == 0) GUILayout.Label("No spare gear.", HuntStyles.Text);
            foreach (var kv in gear)
            {
                var item = kv.Key;
                GUILayout.BeginHorizontal(HuntStyles.Card);
                GUILayout.Label($"{kv.Value} × <color={RarityColor(item.rarity)}>{item.displayName}</color>  <size=11>{item.type}</size>", HuntStyles.Text);
                GUI.enabled = hook.Gear.CanEquip(item, hook.Inventory, TierOf(item.type));
                if (GUILayout.Button("Equip", GUILayout.Width(70), GUILayout.Height(26)) && hook.Gear.Equip(item, hook.Inventory, TierOf(item.type)))
                    hook.Log($"Equipped {hook.Gear.Label(GearRules.SlotOf(item.type))}.");
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        void DrawEssence()
        {
            var inv = hook.Inventory;
            if (GUILayout.Button("Open the Salvage Bin (dismantle)", GUILayout.Height(28))) SalvageBin.Toggle();
            if (!inv.Essence.Any()) { GUILayout.Label("No essence yet.", HuntStyles.Text); return; }
            var mastery = hook.Mastery;
            bool canCombine = mastery.Level >= AlchemyMastery.CombineLevel;
            foreach (var kv in inv.Essence.ToList())
            {
                GUILayout.BeginHorizontal(HuntStyles.Card);
                GUILayout.Label($"<b>{kv.Value}</b> × <color={RarityColor(kv.Key.Rarity)}>{kv.Key}</color>", HuntStyles.Text);
                // Idea Round 3 (U-6): 5 essence → 1 of the next rarity, from Alchemy Mastery Lv 3.
                if (kv.Key.Rarity < Rarity.Mythic)
                {
                    GUI.enabled = canCombine && kv.Value >= AlchemyMastery.CombineCost;
                    string label = canCombine ? $"Combine {AlchemyMastery.CombineCost} → 1 {kv.Key.Rarity + 1}" : $"Combine (Mastery Lv {AlchemyMastery.CombineLevel})";
                    if (GUILayout.Button(label, GUILayout.Width(190), GUILayout.Height(26)) && mastery.Combine(inv, kv.Key))
                        hook.Log($"Combined {AlchemyMastery.CombineCost} {kv.Key} into 1 {new EssenceKey(kv.Key.Rarity + 1, kv.Key.Element)}.");
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
        }

        void DrawSalvage()
        {
            var inv = hook.Inventory;
            GUILayout.Label("Misfit tiers", HuntStyles.Heading);
            if (hook.raft != null) raftTier = hook.raft.Tier;
            weaponTier = TierRow("Weapon", weaponTier);
            armorTier = TierRow("Armor", armorTier);
            companionTier = TierRow("Companion", companionTier);
            GUILayout.Space(6);
            autoSalvage = GUILayout.Toggle(autoSalvage, " Auto-salvage misfit weapons and armor when I pick them up");
            GUILayout.Space(6);
            int misfits = inv.Items.Where(kv => SalvageRules.IsMisfit(kv.Key.type, kv.Key.rarity, TierOf(kv.Key.type)) && !IsIngredient(kv.Key)).Sum(kv => kv.Value);
            GUI.enabled = misfits > 0;
            if (GUILayout.Button(misfits > 0 ? $"Salvage {misfits} misfit(s) now" : "No misfits to salvage", GUILayout.Height(30)))
            {
                int gained = inv.SalvageIncompatible(TierOf, IsIngredient);
                hook.Log(gained > 0 ? $"Salvaged misfits into {gained} essence." : "No misfits to salvage.");
            }
            GUI.enabled = true;
        }

        void GearRow(GearSlot slot)
        {
            var gear = hook.Gear;
            var inv = hook.Inventory;
            var item = gear.Get(slot);
            GUILayout.BeginVertical(HuntStyles.Card);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{slot}</b>", HuntStyles.Text, GUILayout.Width(70));
            string stat = slot == GearSlot.Weapon ? "ATK" : "DEF";
            string power = slot == GearSlot.Accessory
                ? $"ATK +{GearRules.AccessoryAttack(gear.PowerOf(slot))} DEF +{GearRules.AccessoryDefense(gear.PowerOf(slot))}{(item != null && item.element != Elements.Element.None ? $" · {item.element}" : "")}"
                : $"{stat} +{gear.PowerOf(slot)}";
            GUILayout.Label(item == null ? "(nothing: equip from the list below)"
                : $"<color={RarityColor(item.rarity)}>{gear.Label(slot)}</color>  <size=10>{power}</size>", Rich, GUILayout.Width(220));
            if (item != null)
            {
                int cost = gear.RefineCost(slot);
                GUI.enabled = gear.CanRefine(slot, inv);
                string refine = cost > 0 ? $"Refine +1 ({cost} {item.rarity}+ ess.)" : $"Max +{GearRules.MaxRefine}";
                if (GUILayout.Button(refine, GUILayout.Width(190)) && gear.Refine(slot, inv))
                    hook.Log($"Refined to {gear.Label(slot)}.");
                GUI.enabled = true;
                if (GUILayout.Button("Take off", GUILayout.Width(70))) gear.Unequip(slot, inv);
            }
            GUILayout.EndHorizontal();
            string skills = SkillText(item);
            if (skills.Length > 0) GUILayout.Label(skills, HuntStyles.Hint);
            GUILayout.EndVertical();
        }

        /// <summary>"Passive: Thorns (...) · Active: Shell Guard" for gear with skills (GDD §9), else "".</summary>
        static string SkillText(ItemDefinition item)
        {
            var parts = GearSkillRules.Passives(item).Select(p => $"Passive: {GearSkillRules.Label(p)}")
                .Concat(GearSkillRules.Actives(item).Select(a => $"Active: {GearSkillRules.Label(a)} ({GearSkillRules.Describe(a)})"));
            return string.Join(" · ", parts);
        }

        void Salvage(ItemDefinition item, int qty)
        {
            int gained = hook.Inventory.Salvage(item, qty);
            if (gained > 0) hook.Log($"Salvaged {qty} {item.displayName} into {gained} essence.");
        }

        static Rarity TierRow(string label, Rarity tier)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(80));
            GUILayout.Label($"<color={RarityColor(tier)}>{tier}</color>", Rich, GUILayout.Width(90));
            if (GUILayout.Button("-", GUILayout.Width(24)) && tier > Rarity.Common) tier--;
            if (GUILayout.Button("+", GUILayout.Width(24)) && tier < Rarity.Mythic) tier++;
            GUILayout.EndHorizontal();
            return tier;
        }

        public static string RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return "#7fdc7f";
                case Rarity.Rare: return "#6fa8ff";
                case Rarity.Epic: return "#c47fff";
                case Rarity.Legendary: return "#ffb347";
                case Rarity.Mythic: return "#ff6f8f";
                default: return "#e6e6e6";
            }
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true };
    }
}
