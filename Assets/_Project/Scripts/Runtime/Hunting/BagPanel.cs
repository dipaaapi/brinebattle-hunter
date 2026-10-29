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

        void OnGUI()
        {
            HuntStyles.Begin();
            if (!open || hook == null) return;
            var inv = hook.Inventory;
            GUI.depth = -5;
            float w = Mathf.Min(600, HuntStyles.W - HookController.HudRight - 22), h = Mathf.Min(560, HuntStyles.H - 40);
            GUILayout.BeginArea(new Rect(HuntStyles.W - w - 12, 20, w, h), "Bag", HuntStyles.Window);

            GUILayout.Label("<b>Gear tiers</b> (an item fits if its rarity ≥ the tier)", Rich);
            if (hook.raft != null) raftTier = hook.raft.Tier;
            GUILayout.Label($"Raft        <color={RarityColor(raftTier)}>{raftTier}</color>  (upgrade it in Build mode)", Rich);
            weaponTier = TierRow("Weapon", weaponTier);
            armorTier = TierRow("Armor", armorTier);
            companionTier = TierRow("Companion", companionTier);
            autoSalvage = GUILayout.Toggle(autoSalvage, " Auto-salvage misfits on pickup");

            var hunter = Battle.Combatant.Hunter(hook.Gear);
            string el = hunter.Element == Elements.Element.None ? "Neutral" : hunter.Element.ToString();
            GUILayout.Label($"<b>Equipment</b>  (hunter: ATK {hunter.Attack} · DEF {hunter.Defense} · {el})", Rich);
            GearRow(GearSlot.Weapon);
            GearRow(GearSlot.Armor);

            scroll = GUILayout.BeginScrollView(scroll);
            CompanionRows();
            GUILayout.Label("<b>Items</b>", Rich);
            if (!inv.Items.Any()) GUILayout.Label("  (empty)");
            foreach (var kv in inv.Items.ToList())
            {
                var item = kv.Key;
                bool fits = SalvageRules.IsCompatible(item.type, item.rarity, TierOf(item.type));
                GUILayout.BeginHorizontal();
                string skillTip = GearRules.IsGear(item.type) ? SkillText(item) : "";
                GUILayout.Label(new GUIContent($"{kv.Value} × <color={RarityColor(item.rarity)}>{item.displayName}</color>  <size=10>{item.type} · {item.rarity}{(skillTip.Length > 0 ? " · skills" : "")}</size>", skillTip), Rich, GUILayout.Width(320));
                GUILayout.Label(fits ? "<color=#7f7>fits</color>" : IsIngredient(item) ? "<color=#ccf>crafting</color>" : "<color=#f77>misfit</color>", Rich, GUILayout.Width(50));
                if (GearRules.IsGear(item.type))
                {
                    GUI.enabled = hook.Gear.CanEquip(item, inv, TierOf(item.type));
                    if (GUILayout.Button("Equip", GUILayout.Width(55)) && hook.Gear.Equip(item, inv, TierOf(item.type)))
                        hook.Log($"Equipped {hook.Gear.Label(GearRules.SlotOf(item.type))}.");
                    GUI.enabled = true;
                }
                int yield = SalvageRules.EssenceYield(item.type, item.rarity);
                if (GUILayout.Button($"Salvage 1 (+{yield})")) Salvage(item, 1);
                if (GUILayout.Button("All")) Salvage(item, kv.Value);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("<b>Monster Essence</b>", Rich);
            if (!inv.Essence.Any()) GUILayout.Label("  (none)");
            foreach (var kv in inv.Essence)
                GUILayout.Label($"  {kv.Value} × <color={RarityColor(kv.Key.Rarity)}>{kv.Key}</color>", Rich);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Salvage all misfits"))
            {
                int gained = inv.SalvageIncompatible(TierOf, IsIngredient);
                hook.Log(gained > 0 ? $"Salvaged misfits into {gained} essence." : "No misfits to salvage.");
            }
            if (GUILayout.Button("Close (B)")) WorldPause.Close(Menu.Bag);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        Companion releasing;

        /// <summary>Phase 7: tamed companions; the active one joins battles. Release asks twice.</summary>
        void CompanionRows()
        {
            var roster = hook.Companions;
            GUILayout.Label($"<b>Companions</b>  ({roster.All.Count}/{roster.Capacity}; habitats on the raft add room later)", Rich);
            GUILayout.Label("<size=10>Familiar fights with you · Crew repairs the raft, harpoon and modules · Mystic helps craft gear, potions and oils</size>", Rich);
            if (roster.Best(CompanionRole.Crew) != null)
                CompanionSettings.CrewBuilds = GUILayout.Toggle(CompanionSettings.CrewBuilds, " Crew builds raft parts from the bag onto the raft");
            if (roster.Best(CompanionRole.Mystic) != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mystic keeps at least {CompanionSettings.MysticKeep} of each potion and oil", GUILayout.Width(300));
                CompanionSettings.MysticKeep = Mathf.RoundToInt(GUILayout.HorizontalSlider(CompanionSettings.MysticKeep, 0, 5));
                GUILayout.EndHorizontal();
                CompanionSettings.MysticRefines = GUILayout.Toggle(CompanionSettings.MysticRefines, " Mystic refines your equipped gear with essence when stocked");
            }
            if (roster.All.Count == 0) GUILayout.Label("  (none — tame a weakened, calm drift)");
            foreach (var c in roster.All.ToList())
            {
                bool active = roster.Active == c;
                GUILayout.BeginHorizontal();
                string tip = $"{c.Role}: {CompanionRules.Describe(c.Role)}";
                GUILayout.Label(new GUIContent($"<color={RarityColor(c.Rarity)}>{c.Name}</color> <b>{c.Role}</b>{(active ? " (in battle)" : "")}  <size=10>{c.Element} · {c.StageName}, {c.AgeLeft} battles left · {c.Stats}</size>", tip), Rich, GUILayout.Width(360));
                if (c.Role == CompanionRole.Familiar)
                {
                    GUI.enabled = !active;
                    if (GUILayout.Button("Set active", GUILayout.Width(80))) roster.SetActive(c);
                    GUI.enabled = true;
                }
                else GUILayout.Label("", GUILayout.Width(80));
                if (releasing == c)
                {
                    if (GUILayout.Button("Sure?", GUILayout.Width(55))) { roster.Release(c); releasing = null; hook.ApplyCompanionBonuses(); hook.Log($"Released {c.Name} back to the sea."); }
                }
                else if (GUILayout.Button("Release", GUILayout.Width(65))) releasing = c;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4);
        }

        void GearRow(GearSlot slot)
        {
            var gear = hook.Gear;
            var inv = hook.Inventory;
            var item = gear.Get(slot);
            GUILayout.BeginHorizontal();
            GUILayout.Label(slot.ToString(), GUILayout.Width(80));
            string stat = slot == GearSlot.Weapon ? "ATK" : "DEF";
            GUILayout.Label(item == null ? "(none — equip from the list below)"
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
            if (skills.Length > 0) GUILayout.Label($"<size=10>      {skills}</size>", Rich);
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
