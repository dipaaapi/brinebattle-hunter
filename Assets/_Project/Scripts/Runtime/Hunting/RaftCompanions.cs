using System.Collections.Generic;
using BrineBattle.Companions;
using BrineBattle.Items;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Companion chores the player can switch off in the Bag (saved in PlayerPrefs).</summary>
    public static class CompanionSettings
    {
        public static bool CrewBuilds
        {
            get => PlayerPrefs.GetInt("brinebattle.crewBuilds", 1) == 1;
            set => PlayerPrefs.SetInt("brinebattle.crewBuilds", value ? 1 : 0);
        }

        /// <summary>Durability share (0-1) at which the Crew repairs a module kind; set per kind in Build (R). Default 50%.</summary>
        public static float CrewRepairAt(string moduleId) => PlayerPrefs.GetFloat($"brinebattle.crewRepair.{moduleId}", 0.5f);
        public static void SetCrewRepairAt(string moduleId, float share) => PlayerPrefs.SetFloat($"brinebattle.crewRepair.{moduleId}", Mathf.Clamp01(share));

        /// <summary>The Mystic crafts a potion or oil when the bag has fewer than this many. Default 2.</summary>
        public static int MysticKeep
        {
            get => PlayerPrefs.GetInt("brinebattle.mysticKeep", 2);
            set => PlayerPrefs.SetInt("brinebattle.mysticKeep", Mathf.Clamp(value, 0, 10));
        }

        public static bool MysticRefines
        {
            get => PlayerPrefs.GetInt("brinebattle.mysticRefines", 1) == 1;
            set => PlayerPrefs.SetInt("brinebattle.mysticRefines", value ? 1 : 0);
        }
    }

    /// <summary>
    /// Shows companions on the raft and runs their chores (owner idea U-23): the Familiar waits
    /// beside the hunter, the Mystic works at a workbench and refines gear, the Crew walks the raft
    /// edge and builds raft parts from the bag. Placeholder spheres; added at runtime by HookController.
    /// </summary>
    public class RaftCompanions : MonoBehaviour
    {
        public HookController hook;

        class Body
        {
            public Companion Pet;
            public GameObject Go;
            public float Phase;
            public float Busy;   // seconds of "working" hop left after a job
            public Vector3 Walk;   // Crew: local spot on the raft it is walking to
            public float NextWalk;
        }

        readonly Dictionary<Companion, Body> bodies = new Dictionary<Companion, Body>();
        GameObject bench;
        float crewTimer, mysticTimer, repairTimer;
        GUIStyle nameStyle;

        Raft Raft => hook.raft;
        Transform HunterTf => Raft.transform.Find("Hunter");

        void Start()
        {
            hook.Companions.Changed += Sync;
            Sync();
        }

        void OnDestroy()
        {
            if (hook != null) hook.Companions.Changed -= Sync;
        }

        void Sync()
        {
            var alive = new HashSet<Companion>(hook.Companions.All);
            foreach (var kv in new List<KeyValuePair<Companion, Body>>(bodies))
                if (!alive.Contains(kv.Key)) { Destroy(kv.Value.Go); bodies.Remove(kv.Key); }
            foreach (var c in hook.Companions.All)
            {
                if (bodies.ContainsKey(c)) continue;
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"{c.Role} ({c.Name})";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(Raft.transform, false);
                go.transform.localScale = Vector3.one * 0.42f;
                Tint.Apply(go.GetComponent<Renderer>(), Tint.Of(c.Element));
                bodies[c] = new Body { Pet = c, Go = go, Phase = Random.value * 10f };
            }
            bool needBench = hook.Companions.Best(CompanionRole.Mystic) != null;
            if (needBench && bench == null)
            {
                bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "Workbench";
                Destroy(bench.GetComponent<Collider>());
                bench.transform.SetParent(Raft.transform, false);
                bench.transform.localScale = new Vector3(0.7f, 0.35f, 0.45f);
                Tint.Apply(bench.GetComponent<Renderer>(), new Color(0.45f, 0.3f, 0.18f));
            }
            else if (!needBench && bench != null) { Destroy(bench); bench = null; }
        }

        void Update()
        {
            if (hook == null || Raft == null) return;
            Place();
            if (WorldPause.Open != Menu.None || BattleController.InBattle) return;
            Chores();
        }

        void Place()
        {
            var hunter = HunterTf;
            var hunterLocal = hunter != null ? hunter.localPosition : Vector3.up * 0.8f;
            if (bench != null) bench.transform.localPosition = new Vector3(-0.75f, 0.45f, 0.75f);
            foreach (var b in bodies.Values)
            {
                b.Phase += Time.deltaTime;
                b.Busy = Mathf.Max(0f, b.Busy - Time.deltaTime);
                // In battle the stage shows the fighting Familiar; hide the idle one.
                b.Go.SetActive(!(BattleController.InBattle && b.Pet == hook.Companions.Active));
                float hop = Mathf.Abs(Mathf.Sin(b.Phase * (b.Busy > 0f ? 12f : 3f))) * (b.Busy > 0f ? 0.35f : 0.08f);
                Vector3 p;
                switch (b.Pet.Role)
                {
                    case CompanionRole.Mystic:
                        p = new Vector3(-0.75f, 0.55f, 0.25f);
                        break;
                    case CompanionRole.Crew:
                        // Walks from tile to tile, always on the raft.
                        if (b.Phase >= b.NextWalk || !OnRaft(b.Walk)) { b.Walk = RandomTile(); b.NextWalk = b.Phase + Random.Range(2f, 4f); }
                        var from = b.Go.transform.localPosition;
                        from.y = 0.5f;
                        p = OnRaft(from) ? Vector3.MoveTowards(from, b.Walk, Time.deltaTime * 1.2f) : b.Walk;
                        break;
                    default:
                        p = hunterLocal + new Vector3(0.6f, -0.3f, -0.2f);
                        break;
                }
                b.Go.transform.localPosition = p + Vector3.up * hop;
            }
        }

        static Vector3 Local(Rafts.Cell c) => new Vector3(c.X * Raft.CellSize, 0.5f, c.Z * Raft.CellSize);

        Vector3 RandomTile()
        {
            int n = Raft.Grid.Count;
            if (n == 0) return Vector3.up * 0.5f;
            int i = Random.Range(0, n);
            foreach (var kv in Raft.Grid.Parts) if (i-- == 0) return Local(kv.Key);
            return Vector3.up * 0.5f;
        }

        /// <summary>True when a local point sits over a raft tile.</summary>
        bool OnRaft(Vector3 local) =>
            Raft.Grid.Has(new Rafts.Cell(Mathf.RoundToInt(local.x / Raft.CellSize), Mathf.RoundToInt(local.z / Raft.CellSize)));

        /// <summary>−1 when drifts come from −X (the left), from the spawner's current.</summary>
        int UpstreamX()
        {
            var spawner = FindFirstObjectByType<DriftSpawner>();
            return spawner != null && spawner.currentDirection.x < 0f ? 1 : -1;
        }

        void Chores()
        {
            var crew = hook.Companions.Best(CompanionRole.Crew);
            if (crew != null && CompanionSettings.CrewBuilds)
            {
                crewTimer += Time.deltaTime;
                if (crewTimer >= CompanionWork.JobSeconds)
                {
                    crewTimer = 0f;
                    if (CompanionWork.CrewBuild(Raft.Grid, hook.Inventory, out var part, UpstreamX()))
                    {
                        Work(crew);
                        hook.Log(part.type == ItemType.Module
                            ? $"{crew.Name} (Crew) installed a {part.displayName} on the upstream side."
                            : $"{crew.Name} (Crew) built a {part.displayName} onto the raft.");
                    }
                }
            }
            // Crew keeps auto-loot modules above the share set per module kind in Build (R), paying the usual cost.
            var modules = hook.builder != null ? hook.builder.modules : null;
            if (crew != null && modules != null)
            {
                repairTimer += Time.deltaTime;
                if (repairTimer >= 5f)
                {
                    repairTimer = 0f;
                    RaftModules.Slot worst = null;
                    foreach (var s in modules.Slots.Values)
                        if (CompanionWork.CrewShouldRepair(s.Durability, CompanionSettings.CrewRepairAt(s.Item.id))
                            && (worst == null || s.Durability.Current / (float)s.Durability.Max < worst.Durability.Current / (float)worst.Durability.Max))
                            worst = s;
                    if (worst != null)
                    {
                        int before = worst.Durability.Current;
                        string result = modules.Repair(worst);
                        if (worst.Durability.Current > before) { Work(crew); hook.Log($"{crew.Name} (Crew): {result}"); }
                    }
                }
            }

            // Mystic: restock potions and oils first, otherwise refine gear.
            var mystic = hook.Companions.Best(CompanionRole.Mystic);
            if (mystic != null)
            {
                mysticTimer += Time.deltaTime;
                if (mysticTimer >= CompanionWork.JobSeconds)
                {
                    mysticTimer = 0f;
                    var recipe = hook.builder != null ? CompanionWork.MysticCraftPick(hook.builder.recipes, hook.Inventory, CompanionSettings.MysticKeep) : null;
                    var slot = recipe == null && CompanionSettings.MysticRefines ? CompanionWork.MysticPick(hook.Gear, hook.Inventory) : null;
                    if (recipe != null && CraftRules.Craft(hook.Inventory, recipe))
                    {
                        Work(mystic);
                        hook.Log($"{mystic.Name} (Mystic) crafted {CraftRules.Describe(recipe.outputs)}.");
                    }
                    else if (slot != null && hook.Gear.Refine(slot.Value, hook.Inventory))
                    {
                        Work(mystic);
                        hook.Log($"{mystic.Name} (Mystic) refined your {hook.Gear.Label(slot.Value)}.");
                    }
                }
            }
        }

        void Work(Companion c)
        {
            if (bodies.TryGetValue(c, out var b)) b.Busy = 1.5f;
        }

        void OnGUI()
        {
            HuntStyles.Begin();
            if (bodies.Count == 0 || BattleController.InBattle) return;
            var cam = Camera.main;
            if (cam == null) return;
            nameStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 11, richText = true };
            foreach (var b in bodies.Values)
            {
                var sp = HuntStyles.Ui(cam.WorldToScreenPoint(b.Go.transform.position + Vector3.up * 0.45f));
                if (sp.z < 0f) continue;
                string tag = b.Busy > 0f ? " <color=#ffd866>working!</color>" : "";
                GUI.Label(new Rect(sp.x - 70, HuntStyles.H - sp.y - 10, 140, 20), $"<b>{b.Pet.Role}</b>{tag}", nameStyle);
            }
        }
    }
}
