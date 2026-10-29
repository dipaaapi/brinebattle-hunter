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
            // A cell whose module was taken off or swapped (e.g. by loading a save) drops its old slot.
            foreach (var c in slots.Keys.Where(c => !raft.Grid.Has(c) || raft.Grid.Parts[c] != slots[c].Item).ToList())
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

        /// <summary>Live catches waiting on a module's line for the hunter to finish what they're doing.</summary>
        readonly Dictionary<Drift, Slot> held = new Dictionary<Drift, Slot>();

        void StartHeldBattle()
        {
            foreach (var kv in held.ToList())
            {
                if (kv.Key == null) { held.Remove(kv.Key); continue; }
                if (!hook.ModuleBattle(kv.Key, kv.Value)) return;
                held.Remove(kv.Key);
                hook.Log($"The {kv.Value.Item.displayName}'s {kv.Key.Monster.displayName} attacks: auto battle!");
                return;
            }
        }

        void Update()
        {
            if (hook == null || WorldPause.Active) return;
            if (held.Count > 0) StartHeldBattle();
            if (slots.Count == 0) return;
            Drift[] drifts = null;
            foreach (var kv in slots)
            {
                var s = kv.Value;
                if (s.Durability.Broken) continue;
                s.Timer -= Time.deltaTime;
                if (s.Timer > 0f) continue;

                drifts ??= FindObjectsByType<Drift>(FindObjectsSortMode.None);
                if (explained.Count > 64) explained.RemoveWhere(x => x == null);
                Vector3 at = raft.CellToWorld(kv.Key);
                float reach = ModuleRules.Range(s.Level);
                // Measured to the drift's edge, so a big crab counts as touching when it looks like it.
                // Calm weakened drifts are left for the player to tame or finish (idea U-17), never auto-looted.
                var inReach = drifts.Where(x => x != null && !x.Hooked && Flat(x.transform.position - at).magnitude <= reach + 0.5f * x.transform.localScale.x).ToList();
                // Say once per drift why the module leaves it alone, so a skipped drift doesn't look like a bug.
                foreach (var x in inReach)
                    if ((Tameable(x) || x.Spooked) && explained.Add(x))
                        hook.Log(x.Spooked
                            ? $"The {s.Item.displayName} leaves the {x.Monster.displayName} alone: it already bit a module."
                            : $"The {s.Item.displayName} leaves the {x.Monster.displayName} alone: weakened and calm, yours to tame or finish.");
                var d = inReach.Where(x => !x.Spooked && !Tameable(x))
                               .OrderBy(x => Flat(x.transform.position - at).sqrMagnitude).FirstOrDefault();
                if (d == null || !d.TryClaim(s)) continue;   // claim first: no other net, arm or the player can take it
                s.Timer = ModuleRules.Interval(s.Level);
                if (s.Level >= 2) StartCoroutine(HookIn(s, d, at));
                else Grab(s, d, at);
            }
        }

        readonly HashSet<Drift> explained = new HashSet<Drift>();

        public float castTime = 0.4f;
        public float reelTime = 1.1f;

        /// <summary>Hook arm (Lv 2+): the line stretches out to the drift, then reels it in, like the player's hook.</summary>
        System.Collections.IEnumerator HookIn(Slot s, Drift d, Vector3 at)
        {
            var go = new GameObject("Hook arm line");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0.95f, 0.95f, 0.85f);
            lr.startWidth = lr.endWidth = 0.05f;
            lr.positionCount = 2;
            Vector3 origin = at + Vector3.up * 0.6f;

            for (float t = 0f; t < castTime && d != null; )
            {
                if (!WorldPause.Active) t += Time.deltaTime;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, Vector3.Lerp(origin, d.transform.position, t / castTime));
                yield return null;
            }
            Vector3 start = d != null ? d.transform.position : at;
            Vector3 end = at + Flat(start - at).normalized * 0.9f;
            for (float t = 0f; t < reelTime && d != null; )
            {
                if (!WorldPause.Active) t += Time.deltaTime;
                var p = Vector3.Lerp(start, end, t / reelTime);
                p.y = 0.1f + Mathf.Sin(t * 12f) * 0.05f;
                d.transform.position = p;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, p);
                yield return null;
            }
            Destroy(go);
            if (d == null) yield break;
            Grab(s, d, at);
        }

        void Grab(Slot s, Drift d, Vector3 at)
        {
            if (s.Level < 2) Flash(at, d.transform.position, false);
            string name = s.Item.displayName;
            // A live catch can't escape (owner's call): auto battle now, or held on the line until the hunter is free.
            if (d.Actual == DriftState.Alive && hook.CanFight(d))
            {
                if (hook.ModuleBattle(d, s)) hook.Log($"The {name} hooked a live {d.Monster.displayName}! It can't escape: auto battle!");
                else
                {
                    held[d] = s;
                    hook.Log($"The {name} hooked a live {d.Monster.displayName} and holds it. Auto battle as soon as you're free.");
                }
                return;
            }
            if (d.Actual == DriftState.Alive)
            {
                s.Durability.Use(ModuleRules.AliveBite);
                d.Release(s);
                d.SwimOff(Flat(d.transform.position - at));
                hook.Log($"{d.Monster.displayName} was alive! It bit the {name} (−{ModuleRules.AliveBite}) and swam off.");
            }
            else
            {
                s.Durability.Use(1);
                hook.AutoLoot(d, name, s);
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
            HuntStyles.Begin();
            if (Camera.main == null || WorldPause.Active) return;
            foreach (var kv in slots)
            {
                Vector3 sp = HuntStyles.Ui(Camera.main.WorldToScreenPoint(raft.CellToWorld(kv.Key) + Vector3.up * 0.6f));
                if (sp.z < 0) continue;
                var s = kv.Value;
                string text = s.Durability.Broken ? "<color=#f77>BROKEN</color>"
                    : s.Timer > 0f ? $"{s.Durability} · {Mathf.CeilToInt(s.Timer)}s" : $"{s.Durability} · <color=#8f8>ready</color>";
                GUI.Label(new Rect(sp.x - 40, HuntStyles.H - sp.y - 18, 80, 18), text, HuntStyles.WorldLabel);
            }
        }

        /// <summary>A short line (hook arm) or splash (net) so you can see the module grab.</summary>
        void Flash(Vector3 from, Vector3 to, bool hookLine)
        {
            var go = new GameObject("Module grab");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = hookLine ? new Color(1f, 0.95f, 0.6f) : new Color(0.7f, 1f, 0.8f);
            lr.startWidth = lr.endWidth = hookLine ? 0.08f : 0.2f;
            lr.positionCount = 2;
            lr.SetPosition(0, from + Vector3.up * 0.5f);
            lr.SetPosition(1, to + Vector3.up * 0.2f);
            Destroy(go, 0.5f);
        }

        static bool Tameable(Drift d) =>
            d.Actual == DriftState.Weakened && !EncounterRules.IsAggressive(d.Aggression);

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
