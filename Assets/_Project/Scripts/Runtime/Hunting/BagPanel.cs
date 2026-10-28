using System.Linq;
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

        bool open;
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

        void Toggle() => open = !open;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.bKey.wasPressedThisFrame) Toggle();
        }

        public Rarity TierOf(ItemType t)
        {
            switch (t)
            {
                case ItemType.Raft: return raftTier;
                case ItemType.Weapon: return weaponTier;
                case ItemType.Armor: return armorTier;
                case ItemType.Companion: return companionTier;
                default: return Rarity.Common;
            }
        }

        void OnLoot()
        {
            if (!autoSalvage) return;
            int gained = hook.Inventory.SalvageIncompatible(TierOf);
            if (gained > 0) hook.Log($"Auto-salvaged misfits into {gained} essence.");
        }

        void OnGUI()
        {
            if (!open || hook == null) return;
            var inv = hook.Inventory;
            float w = 460, h = Mathf.Min(560, Screen.height - 40);
            GUILayout.BeginArea(new Rect(Screen.width - w - 12, 20, w, h), "Bag", GUI.skin.window);

            GUILayout.Label("<b>Gear tiers</b> (an item fits if its rarity ≥ the tier)", Rich);
            raftTier = TierRow("Raft", raftTier);
            weaponTier = TierRow("Weapon", weaponTier);
            armorTier = TierRow("Armor", armorTier);
            companionTier = TierRow("Companion", companionTier);
            autoSalvage = GUILayout.Toggle(autoSalvage, " Auto-salvage misfits on pickup");

            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("<b>Items</b>", Rich);
            if (!inv.Items.Any()) GUILayout.Label("  (empty)");
            foreach (var kv in inv.Items.ToList())
            {
                var item = kv.Key;
                bool fits = SalvageRules.IsCompatible(item.type, item.rarity, TierOf(item.type));
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{kv.Value} × <color={RarityColor(item.rarity)}>{item.displayName}</color>  <size=10>{item.type} · {item.rarity}</size>", Rich, GUILayout.Width(250));
                GUILayout.Label(fits ? "<color=#7f7>fits</color>" : "<color=#f77>misfit</color>", Rich, GUILayout.Width(50));
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
                int gained = inv.SalvageIncompatible(TierOf);
                hook.Log(gained > 0 ? $"Salvaged misfits into {gained} essence." : "No misfits to salvage.");
            }
            if (GUILayout.Button("Close (B)")) open = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
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
