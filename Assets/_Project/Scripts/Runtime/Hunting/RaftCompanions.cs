using System.Linq;
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
    /// beside the hunter, the Mystic works at the storage box (crafting table) and refines gear, the Crew walks the raft
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
            public int Look = -1;   // the look its attire was built for
            public GameObject Attire;
        }

        readonly Dictionary<Companion, Body> bodies = new Dictionary<Companion, Body>();
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
        }

        /// <summary>After a lost battle the pets hide in the storage box until the hunter is up again (Ed's ask).</summary>
        bool Scared => hook.RestLeft > 0f && !BattleController.InBattle;

        void Update()
        {
            if (hook == null || Raft == null) return;
            Place();
            if (StorageBox.Instance != null) StorageBox.Instance.Scared = Scared && bodies.Count > 0;
            if (WorldPause.Open != Menu.None || BattleController.InBattle || Scared) return;
            Chores();
        }

        void Place()
        {
            var hunter = HunterTf;
            var hunterLocal = hunter != null ? hunter.localPosition : Vector3.up * 0.8f;
            foreach (var b in bodies.Values)
            {
                b.Phase += Time.deltaTime;
                b.Busy = Mathf.Max(0f, b.Busy - Time.deltaTime);
                // In battle the stage shows the fighting Familiar; hide the idle one.
                if (Scared)
                {
                    // Scurry into the box in a little arc, then vanish inside it.
                    if (!b.Go.activeSelf) continue;
                    var inBox = Box + Vector3.up * 0.3f;
                    var cur = b.Go.transform.localPosition;
                    var next = Vector3.MoveTowards(cur, inBox, Time.deltaTime * 3f);
                    float left = Vector3.Distance(next, inBox);
                    b.Go.transform.localPosition = next + Vector3.up * Mathf.Min(0.4f, left * 0.5f);
                    if (left < 0.08f) b.Go.SetActive(false);
                    continue;
                }
                bool show = !(BattleController.InBattle && b.Pet == hook.Companions.Active);
                // Back out of the box after the rest: pop out of the lid.
                if (show && !b.Go.activeSelf && !BattleController.InBattle) b.Go.transform.localPosition = Box + Vector3.up * 0.8f;
                b.Go.SetActive(show);
                if (b.Look != b.Pet.Look) Dress(b);
                float hop = Mathf.Abs(Mathf.Sin(b.Phase * (b.Busy > 0f ? 12f : 3f))) * (b.Busy > 0f ? 0.35f : 0.08f);
                Vector3 p;
                switch (b.Pet.Role)
                {
                    case CompanionRole.Mystic:
                        // Works beside the storage box (the crafting table), never inside it.
                        p = Box + new Vector3(0.8f, 0.2f, 0f);
                        break;
                    case CompanionRole.Crew:
                        // Walks from tile to tile, always on the raft.
                        var from = b.Go.transform.localPosition;
                        from.y = 0.5f;
                        if (b.Phase >= b.NextWalk || !OnRaft(b.Walk)) { b.Walk = RandomTile(from, hunterLocal); b.NextWalk = b.Phase + Random.Range(2f, 4f); }
                        p = OnRaft(from) ? Vector3.MoveTowards(from, b.Walk, Time.deltaTime * 1.2f) : b.Walk;
                        break;
                    default:
                        p = hunterLocal + new Vector3(0.6f, -0.3f, -0.2f);
                        break;
                }
                b.Go.transform.localPosition = p + Vector3.up * hop;
            }
            KeepApart(hunterLocal);
        }

        /// <summary>Looks only (U-23 varieties): a scarf, a bandana or a little crown on the pet.</summary>
        static void Dress(Body b)
        {
            b.Look = b.Pet.Look;
            if (b.Attire != null) Destroy(b.Attire);
            b.Attire = null;
            var look = CompanionGrowth.LookName(b.Pet.Look);
            if (look == "Plain") return;
            var go = GameObject.CreatePrimitive(look == "Scarf" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = look;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(b.Go.transform, false);
            // Sizes are in the pet's own space (the pet sphere is 1 unit across).
            if (look == "Scarf") { go.transform.localPosition = new Vector3(0f, -0.15f, 0f); go.transform.localScale = new Vector3(1.05f, 0.08f, 1.05f); }
            else if (look == "Bandana") { go.transform.localPosition = new Vector3(0f, 0.32f, 0f); go.transform.localScale = new Vector3(0.95f, 0.14f, 0.95f); }
            else { go.transform.localPosition = new Vector3(0f, 0.55f, 0f); go.transform.localScale = new Vector3(0.45f, 0.25f, 0.45f); }
            Tint.Apply(go.GetComponent<Renderer>(), look == "Scarf" ? new Color(0.9f, 0.25f, 0.3f) : look == "Bandana" ? new Color(0.25f, 0.4f, 0.9f) : new Color(1f, 0.82f, 0.2f));
            b.Attire = go;
        }

        const float PetRadius = 0.21f, HunterRadius = 0.3f, BoxRadius = 0.55f;

        /// <summary>Pets never stand inside the hunter, the storage box or each other (owner's ask).</summary>
        void KeepApart(Vector3 hunter)
        {
            var shown = bodies.Values.Where(b => b.Go.activeSelf).ToList();
            foreach (var b in shown)
            {
                var p = b.Go.transform.localPosition;
                p = PushOut(p, hunter, PetRadius + HunterRadius, 1f);
                p = PushOut(p, Box, PetRadius + BoxRadius, 1f);
                foreach (var other in shown)
                    if (other != b) p = PushOut(p, other.Go.transform.localPosition, PetRadius * 2f, 0.5f);
                b.Go.transform.localPosition = p;
            }
        }

        static Vector3 PushOut(Vector3 p, Vector3 from, float gap, float share)
        {
            var d = new Vector3(p.x - from.x, 0f, p.z - from.z);
            float dist = d.magnitude;
            if (dist >= gap) return p;
            var away = dist > 0.001f ? d / dist : Vector3.right;
            return p + away * (gap - dist) * share;
        }

        static Vector3 Local(Rafts.Cell c) => new Vector3(c.X * Raft.CellSize, 0.5f, c.Z * Raft.CellSize);

        /// <summary>The storage box's spot on the raft (local), or the old workbench spot before it exists.</summary>
        Vector3 Box => StorageBox.Instance != null ? StorageBox.Instance.LocalPosition : new Vector3(-1.05f, 0.3f, 1.05f);

        /// <summary>
        /// A tile to walk to whose straight path keeps clear of the storage box and the hunter
        /// (owner's ask: nothing goes through the box). Falls back to staying put.
        /// </summary>
        Vector3 RandomTile(Vector3 from, Vector3 hunter)
        {
            var tiles = Raft.Grid.Parts.Keys.Select(Local).ToList();
            if (tiles.Count == 0) return Vector3.up * 0.5f;
            for (int tries = 0; tries < 8; tries++)
            {
                var to = tiles[Random.Range(0, tiles.Count)];
                if (Clear(from, to, Box, 0.75f) && Clear(from, to, hunter, 0.55f)) return to;
            }
            return from;
        }

        /// <summary>True when the flat segment a→b stays at least <paramref name="gap"/> from <paramref name="obstacle"/>.</summary>
        static bool Clear(Vector3 a, Vector3 b, Vector3 obstacle, float gap)
        {
            Vector2 p = new Vector2(a.x, a.z), q = new Vector2(b.x, b.z), o = new Vector2(obstacle.x, obstacle.z);
            var d = q - p;
            float t = d.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(o - p, d) / d.sqrMagnitude);
            return (p + d * t - o).magnitude >= gap;
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
                    // The hammer comes first when the raft has outgrown it (Ed's ask), then potions and oils.
                    // Only recipes the hunter's Alchemy Mastery knows (Idea Round 3).
                    var known = hook.builder != null ? hook.builder.recipes.Where(r => hook.Mastery.Knows(r)).ToList() : null;
                    var recipe = known != null
                        ? CompanionWork.MysticHammerPick(known, hook.Inventory, Raft.Tier)
                          ?? CompanionWork.MysticCraftPick(known, hook.Inventory, CompanionSettings.MysticKeep)
                        : null;
                    var slot = recipe == null && CompanionSettings.MysticRefines ? CompanionWork.MysticPick(hook.Gear, hook.Inventory) : null;
                    if (recipe != null && CraftRules.Craft(hook.Inventory, recipe))
                    {
                        Work(mystic);
                        hook.Log($"{mystic.Name} (Mystic) crafted {CraftRules.Describe(recipe.outputs)}.");
                        StorageBox.Instance?.Crafted($"{CraftRules.Describe(recipe.outputs)} ({mystic.Name})");
                    }
                    else if (slot != null && hook.Gear.Refine(slot.Value, hook.Inventory))
                    {
                        Work(mystic);
                        hook.Log($"{mystic.Name} (Mystic) refined your {hook.Gear.Label(slot.Value)}.");
                        StorageBox.Instance?.Crafted($"refined {hook.Gear.Label(slot.Value)} ({mystic.Name})");
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
