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
                case 2: return t == ItemType.Raft || t == ItemType.Module || t == ItemType.Habitat;
                case 3: return t == ItemType.Weapon || t == ItemType.Armor;
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
            float w = Mathf.Min(600, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 120);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Bag", HuntStyles.Window);
            if (HuntStyles.CloseX(w)) { WorldPause.Close(Menu.Bag); GUILayout.EndArea(); return; }

            // Owner idea U-25: one short page at a time.
            var picked = (Page)HuntStyles.Tabs((int)page, PageLabels);
            if (picked != page) { page = picked; scroll = Vector2.zero; }
            scroll = GUILayout.BeginScrollView(scroll);
            switch (page)
            {
                case Page.Items: DrawItems(); break;
                case Page.Gear: DrawGear(); break;
                case Page.Essence: DrawEssence(); break;
                default: DrawSalvage(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawItems()
        {
            var inv = hook.Inventory;
            filter = GUILayout.Toolbar(filter, Filters, GUILayout.Height(26));
            var list = inv.Items.Where(kv => InFilter(kv.Key.type, filter)).OrderBy(kv => kv.Key.type).ThenBy(kv => kv.Key.rarity).ToList();
            if (list.Count == 0) { GUILayout.Label(filter == 0 ? "The bag is empty." : "Nothing of this kind yet.", HuntStyles.Text); return; }
            var hookTreats = hook.TreatsFor(null);
            foreach (var kv in list)
            {
                var item = kv.Key;
                bool fits = !SalvageRules.IsMisfit(item.type, item.rarity, TierOf(item.type));
                GUILayout.BeginVertical(HuntStyles.Card);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{kv.Value} ×</b> <color={RarityColor(item.rarity)}>{item.displayName}</color>  <size=11>{TypeName(item.type)} · {item.rarity}</size>"
                    + (fits ? "" : $"  <size=11><color={HuntStyles.BadHex}>below your {item.type} tier</color></size>"), HuntStyles.Text);
                GUILayout.FlexibleSpace();
                if (GearRules.IsGear(item.type))
                {
                    GUI.enabled = hook.Gear.CanEquip(item, inv, TierOf(item.type));
                    if (GUILayout.Button("Equip", GUILayout.Width(60), GUILayout.Height(26)) && hook.Gear.Equip(item, inv, TierOf(item.type)))
                        hook.Log($"Equipped {hook.Gear.Label(GearRules.SlotOf(item.type))}.");
                    GUI.enabled = true;
                }
                // U-32/U-33: drink a potion on the raft (heal or cure the hunter).
                if (item.type == ItemType.Consumable && hookTreats.Contains(item)
                    && GUILayout.Button("Use", GUILayout.Width(50), GUILayout.Height(26))) hook.Treat(item, null);
                int yield = SalvageRules.EssenceYield(item.type, item.rarity);
                if (yield > 0)
                {
                    if (GUILayout.Button(new GUIContent($"Salvage (+{yield})", "Break one into Monster Essence"), GUILayout.Width(110), GUILayout.Height(26))) Salvage(item, 1);
                    if (kv.Value > 1 && GUILayout.Button("All", GUILayout.Width(40), GUILayout.Height(26))) Salvage(item, kv.Value);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
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
            GUILayout.Label(item == null ? "(nothing: equip from the list below)"
                : $"<color={RarityColor(item.rarity)}>{gear.Label(slot)}</color>  <size=10>{stat} +{gear.PowerOf(slot)}</size>", Rich, GUILayout.Width(220));
            if (item != null)
            {
                int cost = gear.RefineCost(slot);
                GUI.enabled = gear.CanRefine(slot, inv);
                string refine = cost > 0 ? $"Refine +1 ({cost} {item.rarity}+ ess.)" : $"Max +{GearRules.MaxRefine}";
                if (GUILayout.Button(refine, GUILayout.Width(190)) && gear.Refine(slot, inv))
                    hook.Log($"Refined to {gear.Label(slot)} ({stat} +{gear.PowerOf(slot)}).");
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
