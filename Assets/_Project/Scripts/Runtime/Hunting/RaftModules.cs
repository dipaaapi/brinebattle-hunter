using System.Collections.Generic;
using System.Linq;
using BrineBattle.Encounters;
using BrineBattle.Items;
using BrineBattle.Rafts;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Runs the auto-loot modules placed on the raft (idea U-13). Lv 1 basic net: every
    /// <see cref="ModuleRules.Interval"/> seconds it grabs one drift touching it. Dead or weakened
    /// drifts are looted (−1 durability); alive ones bite it (−5) and swim off.
    /// </summary>
    public class RaftModules : MonoBehaviour
    {
        public Raft raft;
        public HookController hook;

        public class Slot
        {
            public ItemDefinition Item;
            public Durability Durability;
            public float Timer;
            public int Level => Mathf.Max(1, Item.moduleLevel);
        }

        readonly Dictionary<Cell, Slot> slots = new Dictionary<Cell, Slot>();
        // Durability of modules taken off the raft, so removing and re-placing doesn't repair them.
        readonly Dictionary<ItemDefinition, List<int>> stored = new Dictionary<ItemDefinition, List<int>>();

        public IReadOnlyDictionary<Cell, Slot> Slots => slots;

        void Start()
        {
            raft.Grid.Changed += Sync;
            Sync();
        }

        void OnDestroy()
        {
            if (raft != null) raft.Grid.Changed -= Sync;
        }

        void Sync()
        {
            foreach (var c in slots.Keys.Where(c => !raft.Grid.Has(c) || raft.Grid.Parts[c].type != ItemType.Module).ToList())
            {
                var s = slots[c];
                if (!stored.TryGetValue(s.Item, out var list)) stored[s.Item] = list = new List<int>();
                list.Add(s.Durability.Current);
                slots.Remove(c);
            }
            foreach (var kv in raft.Grid.Parts)
            {
                if (kv.Value.type != ItemType.Module || slots.ContainsKey(kv.Key)) continue;
                var item = kv.Value;
                int max = ModuleRules.MaxDurability(Mathf.Max(1, item.moduleLevel), item.rarity);
                int cur = max;
                if (stored.TryGetValue(item, out var list) && list.Count > 0)
                {
                    cur = list.Min();
                    list.Remove(cur);
                }
                slots[kv.Key] = new Slot { Item = item, Durability = new Durability(max, cur), Timer = 2f };
            }
        }

        void Update()
        {
            if (hook == null || slots.Count == 0) return;
            Drift[] drifts = null;
            foreach (var kv in slots)
            {
                var s = kv.Value;
                if (s.Durability.Broken) continue;
                s.Timer -= Time.deltaTime;
                if (s.Timer > 0f) continue;

                drifts ??= FindObjectsByType<Drift>(FindObjectsSortMode.None);
                Vector3 at = raft.CellToWorld(kv.Key);
                float reach = ModuleRules.Range(s.Level);
                var d = drifts.Where(x => x != null && !x.Hooked && Flat(x.transform.position - at).magnitude <= reach)
                              .OrderBy(x => Flat(x.transform.position - at).sqrMagnitude).FirstOrDefault();
                if (d == null) continue;
                s.Timer = ModuleRules.Interval(s.Level);
                Grab(s, d, at);
            }
        }

        void Grab(Slot s, Drift d, Vector3 at)
        {
            string name = s.Item.displayName;
            if (d.Actual == DriftState.Alive)
            {
                s.Durability.Use(ModuleRules.AliveBite);
                d.SwimOff(Flat(d.transform.position - at));
                hook.Log($"{d.Monster.displayName} was alive! It bit the {name} (−{ModuleRules.AliveBite}) and swam off.");
            }
            else
            {
                s.Durability.Use(1);
                hook.AutoLoot(d, name);
            }
            if (s.Durability.Broken) hook.Log($"The {name} is broken. Repair it in build mode (R).");
        }

        /// <summary>Repairs one chunk if the bag can pay. Returns a message for the build panel.</summary>
        public string Repair(Slot s)
        {
            if (s.Durability.Full) return $"{s.Item.displayName} is already at full durability.";
            int cost = ModuleRules.RepairCost(s.Level, s.Item.rarity);
            var inv = hook.Inventory;
            bool mats = ModuleRules.RepairUsesMaterials(s.Level), ess = ModuleRules.RepairUsesEssence(s.Level);
            if (mats && inv.MaterialCount() < cost) return $"Need {cost} materials to repair.";
            if (ess && inv.EssenceAtLeast(s.Item.rarity) < cost) return $"Need {cost} {s.Item.rarity}+ essence to repair.";
            if (mats) inv.SpendMaterials(cost);
            if (ess) inv.SpendEssence(s.Item.rarity, cost);
            s.Durability.Repair(ModuleRules.RepairChunk);
            return $"Repaired {s.Item.displayName} to {s.Durability}.";
        }

        public static string CostText(Slot s)
        {
            int cost = ModuleRules.RepairCost(s.Level, s.Item.rarity);
            bool mats = ModuleRules.RepairUsesMaterials(s.Level), ess = ModuleRules.RepairUsesEssence(s.Level);
            return mats && ess ? $"{cost} materials + {cost} essence" : mats ? $"{cost} materials" : $"{cost} essence";
        }

        void OnGUI()
        {
            if (Camera.main == null) return;
            foreach (var kv in slots)
            {
                Vector3 sp = Camera.main.WorldToScreenPoint(raft.CellToWorld(kv.Key) + Vector3.up * 0.6f);
                if (sp.z < 0) continue;
                var s = kv.Value;
                string text = s.Durability.Broken ? "<color=#f77>BROKEN</color>" : $"{s.Durability}";
                GUI.Label(new Rect(sp.x - 40, Screen.height - sp.y - 18, 80, 18), text, HuntStyles.WorldLabel);
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
