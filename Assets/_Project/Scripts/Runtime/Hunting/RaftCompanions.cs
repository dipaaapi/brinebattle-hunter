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

        /// <summary>Familiars swim out for dead drifts and floating loot (Ed's U-29). Default on.</summary>
        public static bool FamiliarLoots
        {
            get => PlayerPrefs.GetInt("brinebattle.familiarLoots", 1) == 1;
            set => PlayerPrefs.SetInt("brinebattle.familiarLoots", value ? 1 : 0);
        }

        public static bool MysticRefines
        {
            get => PlayerPrefs.GetInt("brinebattle.mysticRefines", 1) == 1;
            set => PlayerPrefs.SetInt("brinebattle.mysticRefines", value ? 1 : 0);
        }

        // Minion jobs added with U-39 (all on by default; Ed likes it automatic).
        static bool Flag(string key) => PlayerPrefs.GetInt($"brinebattle.minion.{key}", 1) == 1;
        static void Flag(string key, bool on) => PlayerPrefs.SetInt($"brinebattle.minion.{key}", on ? 1 : 0);
        public static bool Mends { get => Flag("mends"); set => Flag("mends", value); }
        public static bool AlchemistCrafts { get => Flag("alchemy"); set => Flag("alchemy", value); }
        public static bool BuilderCrafts { get => Flag("builderCrafts"); set => Flag("builderCrafts", value); }
        public static bool BuilderLevelsUp { get => Flag("builderLevels"); set => Flag("builderLevels", value); }
        public static bool TinkerCrafts { get => Flag("tinker"); set => Flag("tinker", value); }
        public static bool SalvagerSorts { get => Flag("salvager"); set => Flag("salvager", value); }
        public static bool GluttonEats { get => Flag("glutton"); set => Flag("glutton", value); }

        /// <summary>Auto-feed (Ed's ask): in Auto battle mode minions snack from the Bag on their own. Default on.</summary>
        public static bool AutoFeed { get => Flag("autoFeed"); set => Flag("autoFeed", value); }

        /// <summary>Which rarities auto-feed may eat. Default Common and Uncommon.</summary>
        public static bool FeedTier(Rarity r) => PlayerPrefs.GetInt($"brinebattle.minion.feedTier.{(int)r}", r <= Rarity.Uncommon ? 1 : 0) == 1;
        public static void SetFeedTier(Rarity r, bool on) => PlayerPrefs.SetInt($"brinebattle.minion.feedTier.{(int)r}", on ? 1 : 0);

        /// <summary>The most of one stack auto-feed may eat, 0-1. Default half.</summary>
        public static float FeedShare
        {
            get => PlayerPrefs.GetFloat("brinebattle.minion.feedShare", 0.5f);
            set => PlayerPrefs.SetFloat("brinebattle.minion.feedShare", Mathf.Clamp01(value));
        }

        /// <summary>Junk at or under this rarity is taken apart (dismantle or salvage); rarer junk is eaten. Default Uncommon.</summary>
        public static Rarity TakeApartUpTo
        {
            get => (Rarity)PlayerPrefs.GetInt("brinebattle.minion.takeApartUpTo", (int)Rarity.Uncommon);
            set => PlayerPrefs.SetInt("brinebattle.minion.takeApartUpTo", (int)value);
        }
    }

    /// <summary>
    /// Shows minions on the raft and runs their jobs by skill (Ed's U-39): Fighters and Looters stay near the
    /// hunter, Alchemists, Smiths and Tinkers work at the storage box, Crewmates and Builders walk the raft.
    /// Each job is done by the best minion with that skill. Placeholder spheres; added at runtime by HookController.
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
            public int HideIn = -1;   // after a lost battle: 0 storage box, 1 Salvage Bin
            public float HopT = -1f;   // the hop into the lid: -1 still walking over
            public Vector3 HopFrom;
            public GameObject Attire;
        }

        readonly Dictionary<Companion, Body> bodies = new Dictionary<Companion, Body>();
        float crewTimer, repairTimer;
        readonly Dictionary<MinionSkill, float> timers = new Dictionary<MinionSkill, float>();
        BagPanel bagPanel;
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
                go.name = $"Minion ({c.Name})";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(Raft.transform, false);
                go.transform.localScale = Vector3.one * 0.42f;
                Tint.Apply(go.GetComponent<Renderer>(), Tint.Of(c.Element));
                bodies[c] = new Body { Pet = c, Go = go, Phase = Random.value * 10f };
            }
        }

        /// <summary>C-16: a happy hop and a heart over a minion that just ate its favourite food.</summary>
        public void Yum(Companion c, ItemDefinition item)
        {
            if (!MinionRules.IsFavourite(item, c) || !bodies.TryGetValue(c, out var b)) return;
            b.Busy = 1.2f;
            hook.First("favourite");
            Juice.Pop("<3 Yum!", b.Go.transform.position + Vector3.up * 0.3f, new Color(1f, 0.55f, 0.7f));
        }

        /// <summary>A minion's body on the raft, or null when it has none (or is hidden).</summary>
        public Transform BodyOf(Companion c) => c != null && bodies.TryGetValue(c, out var b) && b.Go.activeInHierarchy ? b.Go.transform : null;

        /// <summary>After a lost battle the pets hide in the storage box until the hunter is up again (Ed's ask).</summary>
        bool Scared => hook.RestLeft > 0f && !BattleController.InBattle;

        void Update()
        {
            if (hook == null || Raft == null) return;
            Place();
            foreach (var c in hook.Companions.All) c.SinceFavourite += Time.deltaTime;   // C-16 mood clock
            // Each container rattles while minions are inside it (Ed's ask: the bin too, not just the box).
            if (StorageBox.Instance != null) StorageBox.Instance.Scared = Scared && bodies.Values.Any(b => b.HideIn == 0 && !b.Go.activeSelf);
            if (SalvageBin.Instance != null) SalvageBin.Instance.Scared = Scared && bodies.Values.Any(b => b.HideIn == 1 && !b.Go.activeSelf);
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
                    // Ed's ask: run along the planks to the box or the Salvage Bin (they share), then one clean hop
                    // into the lid, and vanish inside.
                    if (!b.Go.activeSelf) continue;
                    if (b.HideIn < 0) b.HideIn = SalvageBin.Instance != null && SalvageBin.Instance.Placed && HiddenCount(0) > HiddenCount(1) ? 1 : 0;
                    var home = HideSpot(b.HideIn);
                    var cur = b.Go.transform.localPosition;
                    if (b.HopT < 0f)
                    {
                        var ground = new Vector3(cur.x, 0.5f, cur.z);
                        var toward = new Vector3(home.x, 0.5f, home.z);
                        var next = Vector3.MoveTowards(ground, toward, Time.deltaTime * 3.2f);
                        b.Go.transform.localPosition = next + Vector3.up * Mathf.Abs(Mathf.Sin(b.Phase * 16f)) * 0.06f;   // scurrying
                        if (Vector3.Distance(next, toward) < 0.75f) { b.HopT = 0f; b.HopFrom = next; }
                        continue;
                    }
                    b.HopT += Time.deltaTime / 0.45f;
                    float k = Mathf.Clamp01(b.HopT);
                    var top = home + Vector3.up * 0.35f;
                    b.Go.transform.localPosition = Vector3.Lerp(b.HopFrom, top, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.7f;
                    b.Go.transform.localScale = Vector3.one * 0.42f * Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((k - 0.7f) / 0.3f));
                    if (k >= 1f) b.Go.SetActive(false);
                    continue;
                }
                bool show = !(BattleController.InBattle && b.Pet == hook.Companions.Active);
                // Back out after the rest: pop out of the lid it hid in.
                if (show && !b.Go.activeSelf && !BattleController.InBattle)
                {
                    b.Go.transform.localPosition = HideSpot(Mathf.Max(0, b.HideIn)) + Vector3.up * 0.8f;
                    b.Go.transform.localScale = Vector3.one * 0.42f;
                }
                b.HideIn = -1;
                b.HopT = -1f;
                b.Go.SetActive(show);
                if (b.Look != b.Pet.Look) Dress(b);
                float hop = Mathf.Abs(Mathf.Sin(b.Phase * (b.Busy > 0f ? 12f : 3f))) * (b.Busy > 0f ? 0.35f : 0.08f);
                Vector3 p;
                switch (Where(b.Pet))
                {
                    case Spot.Box:
                        // Works beside the storage box (the crafting table), never inside it, and on a plank.
                        p = MysticSpot() + Vector3.up * 0.2f;
                        break;
                    case Spot.Walk:
                        // Walks from plank to plank, always on the raft; with a job it goes to the spot and hammers.
                        var from = b.Go.transform.localPosition;
                        from.y = 0.5f;
                        if (job != null && job.Crew == b.Pet) b.Walk = job.Stand;
                        else if (b.Phase >= b.NextWalk || !OnRaft(b.Walk)) { b.Walk = RandomTile(from, hunterLocal); b.NextWalk = b.Phase + Random.Range(2f, 4f); }
                        p = OnRaft(from) ? Vector3.MoveTowards(from, b.Walk, Time.deltaTime * 1.2f) : b.Walk;
                        if (job != null && job.Crew == b.Pet && Flat(p - job.Stand) < 0.1f) { job.Working = true; hop = Mathf.Abs(Mathf.Sin(b.Phase * 10f)) * 0.12f; }
                        break;
                    default:
                        // Walks after the hunter at its own pace. While the hunter hooks and reels at
                        // the edge it stays where it is and cheers (Ed: it was glued to the hunter).
                        var at = b.Go.transform.localPosition;
                        at.y = 0.5f;
                        var side = new Vector3(hunterLocal.x + 0.6f, 0.5f, hunterLocal.z - 0.2f);
                        if (!OnRaft(side)) side = new Vector3(hunterLocal.x - 0.6f, 0.5f, hunterLocal.z - 0.2f);
                        if (!OnRaft(at)) p = side;
                        else if (hook.HookTarget != null) { p = at; hop = Mathf.Abs(Mathf.Sin(b.Phase * 7f)) * 0.15f; }
                        else p = Vector3.MoveTowards(at, side, Time.deltaTime * 1.4f);
                        break;
                }
                b.Go.transform.localPosition = p + Vector3.up * hop;
            }
            KeepApart(hunterLocal);
        }

        enum Spot { Follow, Box, Walk }

        /// <summary>Where a minion hangs out, by its skills: building jobs walk the raft, crafting jobs stay at the box.</summary>
        Spot Where(Companion c)
        {
            if ((job != null && job.Crew == c) || c.Has(MinionSkill.Crewmate) || c.Has(MinionSkill.Builder)) return Spot.Walk;
            if (c.Has(MinionSkill.Alchemist) || c.Has(MinionSkill.Smith) || c.Has(MinionSkill.Tinker)) return Spot.Box;
            return Spot.Follow;
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

        // Ed: a minion pressed against the box looked stuck to it, so the box and bin keep a wider berth.
        const float PetRadius = 0.21f, HunterRadius = 0.3f, BoxRadius = 0.72f;

        int HiddenCount(int where) => bodies.Values.Count(x => x.HideIn == where);

        /// <summary>Where a minion hides: 0 the storage box, 1 the Salvage Bin.</summary>
        Vector3 HideSpot(int where) =>
            where == 1 && SalvageBin.Instance != null && SalvageBin.Instance.Placed ? SalvageBin.Instance.LocalPosition : Box;

        /// <summary>Pets never stand inside the hunter, the storage box or each other (owner's ask).</summary>
        void KeepApart(Vector3 hunter)
        {
            var shown = bodies.Values.Where(b => b.Go.activeSelf && b.HopT < 0f).ToList();   // not mid-hop into a hiding place
            foreach (var b in shown)
            {
                var p = b.Go.transform.localPosition;
                p = PushOut(p, hunter, PetRadius + HunterRadius, 1f);
                p = PushOut(p, Box, PetRadius + BoxRadius, 1f);
                if (SalvageBin.Instance != null && SalvageBin.Instance.Placed) p = PushOut(p, SalvageBin.Instance.LocalPosition, PetRadius + BoxRadius, 1f);
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
            // Planks only (Ed's ask): never onto an auto-loot module or a habitat.
            var tiles = Raft.Grid.Parts.Keys.Where(Raft.Grid.Walkable).Select(Local).ToList();
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
        bool OnRaft(Vector3 local) => Raft.WalkableAt(local);

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>A plank beside the storage box for the Mystic: right of it if that's a plank, else the nearest plank next to it.</summary>
        Vector3 MysticSpot()
        {
            var want = Box + new Vector3(1f, 0f, 0f);
            want.y = 0.5f;
            if (OnRaft(want)) return want;
            var box = Raft.LocalToCell(Box);
            foreach (var n in box.Neighbours())
                if (Raft.Grid.Walkable(n) && !(SalvageBin.Instance != null && SalvageBin.Instance.Cell.Equals(n))) return Local(n);   // the next plank's middle, clear of the box
            return want;
        }

        /// <summary>−1 when drifts come from −X (the left), from the spawner's current.</summary>
        int UpstreamX()
        {
            var spawner = FindFirstObjectByType<DriftSpawner>();
            return spawner != null && spawner.currentDirection.x < 0f ? 1 : -1;
        }

        void Chores()
        {
            var crew = hook.Companions.Best(MinionSkill.Crewmate);
            if (crew != null && CompanionSettings.CrewBuilds)
            {
                crewTimer += Time.deltaTime;
                if (job == null && crewTimer >= CompanionWork.JobSeconds)
                {
                    crewTimer = 0f;
                    if (CompanionWork.CrewPick(Raft.Grid, hook.Inventory, out var cell, out var part, UpstreamX()))
                        StartJob(crew, cell, part);
                }
            }
            Install();
            // Crew keeps auto-loot modules above the share set per module kind in Build (R), paying the usual cost.
            var modules = hook.builder != null ? hook.builder.modules : null;
            var mender = hook.Companions.Best(MinionSkill.Mender);
            if (mender != null && CompanionSettings.Mends && modules != null)
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
                        if (worst.Durability.Current > before) { Work(mender, "repaired a module"); hook.Log($"{mender.Name} (Mender): {result}"); }
                    }
                }
            }

            // Only recipes the hunter's Alchemy Mastery knows (Idea Round 3).
            var known = hook.builder != null ? hook.builder.recipes.Where(r => hook.Knows(r)).ToList() : new List<RecipeDefinition>();

            // Smith: the hammer first when the raft has outgrown it (Ed's ask), then refine gear (auto-upgrade).
            if (Due(MinionSkill.Smith, out var smith))
            {
                var hammer = CompanionWork.MysticHammerPick(known, hook.Inventory, Raft.Tier);
                var slot = hammer == null && CompanionSettings.MysticRefines ? CompanionWork.MysticPick(hook.Gear, hook.Inventory) : null;
                if (hammer != null) Crafted(smith, "Smith", hammer);
                else if (slot != null && hook.Gear.Refine(slot.Value, hook.Inventory))
                {
                    Work(smith, $"refined the {hook.Gear.Label(slot.Value)}");
                    Incantation(smith);
                    hook.Log($"{smith.Name} (Smith) refined your {hook.Gear.Label(slot.Value)}.");
                    StorageBox.Instance?.Crafted($"refined {hook.Gear.Label(slot.Value)} ({smith.Name})");
                }
            }

            // Alchemist: keeps potions, oils and treats stocked (auto-craft).
            if (CompanionSettings.AlchemistCrafts && Due(MinionSkill.Alchemist, out var alchemist))
            {
                var recipe = CompanionWork.MysticCraftPick(known, hook.Inventory, CompanionSettings.MysticKeep);
                if (recipe != null) Crafted(alchemist, "Alchemist", recipe);
            }

            // Tinker: crafts better weapons, armor and accessories and equips them (auto-upgrade).
            if (CompanionSettings.TinkerCrafts && Due(MinionSkill.Tinker, out var tinker))
            {
                var recipe = CompanionWork.TinkerPick(known, hook.Inventory, hook.Gear, TierOf);
                if (recipe != null && Crafted(tinker, "Tinker", recipe))
                {
                    var made = recipe.outputs[0].item;
                    if (hook.Gear.Equip(made, hook.Inventory, TierOf(made.type))) hook.Log($"{tinker.Name} (Tinker) equipped the {made.displayName} on you.");
                }
            }

            // Builder: grows the raft toward the next level and the vehicle (Ed's ask: the auto-craft ladder).
            if (Due(MinionSkill.Builder, out var builderPet)) BuilderJob(builderPet, known);

            // Salvager and Glutton share the junk (Ed's ask): rarity at or under the Chores setting is taken apart, the rest is eaten.
            if (CompanionSettings.SalvagerSorts && Due(MinionSkill.Salvager, out var salvager)) SalvageJob(salvager);
            if (CompanionSettings.GluttonEats && Due(MinionSkill.Glutton, out var glutton)) EatJob(glutton);
            AutoFeed();
        }

        /// <summary>True every <see cref="CompanionWork.JobSeconds"/> while some minion has the skill; gives that minion.</summary>
        bool Due(MinionSkill skill, out Companion who)
        {
            who = hook.Companions.Best(skill);
            if (who == null) return false;
            timers.TryGetValue(skill, out var t);
            t += Time.deltaTime * MinionRules.WorkSpeed(who);   // a sulking minion (C-16) works slower
            if (t < CompanionWork.JobSeconds) { timers[skill] = t; return false; }
            timers[skill] = 0f;
            return true;
        }

        bool Crafted(Companion who, string skill, RecipeDefinition recipe)
        {
            if (recipe == null || !CraftRules.Craft(hook.Inventory, recipe)) return false;
            string what = CraftRules.Describe(recipe.outputs);
            Work(who, $"crafted {what}");
            Incantation(who);
            hook.Log($"{who.Name} ({skill}) crafted {what}.");
            StorageBox.Instance?.Crafted($"{what} ({who.Name})");
            return true;
        }

        Rarity TierOf(ItemType t)
        {
            if (bagPanel == null) bagPanel = FindFirstObjectByType<BagPanel>();
            return bagPanel != null ? bagPanel.TierOf(t) : t == ItemType.Raft ? Raft.Tier : Rarity.Common;
        }

        bool IsIngredient(ItemDefinition item) =>
            hook.builder != null && hook.builder.recipes.Any(r => r != null && r.kind != RecipeKind.Dismantle && r.inputs.Any(x => x.item == item));

        bool IsJunk(ItemDefinition item) =>
            MinionRules.IsJunk(item, TierOf, Raft.Tier, Rafts.RaftRules.BestHammer(hook.Inventory)?.rarity, IsIngredient);

        RecipeDefinition DismantleFor(ItemDefinition item) =>
            hook.builder?.recipes.FirstOrDefault(r => r != null && r.kind == RecipeKind.Dismantle && r.inputs.Any(x => x.item == item) && CraftRules.CanCraft(hook.Inventory, r));

        MinionRules.JunkAction ActionFor(ItemDefinition item) =>
            MinionRules.ForJunk(item, CompanionSettings.TakeApartUpTo, DismantleFor(item) != null);

        void BuilderJob(Companion who, List<RecipeDefinition> known)
        {
            var g = Raft.Grid;
            var vessel = CompanionWork.BuilderVesselPick(g, hook.Inventory);
            if (vessel != null && g.InstallVessel(vessel, hook.Inventory))
            {
                Work(who, $"put on the {vessel.displayName}");
                hook.Log($"{who.Name} (Builder) put the {vessel.displayName} on the raft.");
                return;
            }
            if (CompanionSettings.BuilderLevelsUp)
            {
                var path = CompanionWork.BuilderLevelUp(g, hook.Inventory);
                if (path != null && g.TryUpgrade(hook.Inventory, path.Value))
                {
                    Work(who, $"leveled the raft to Lv {(int)g.Tier + 1}");
                    hook.Log($"{who.Name} (Builder) leveled the raft up to Lv {(int)g.Tier + 1}!");
                    return;
                }
            }
            if (CompanionSettings.BuilderCrafts) Crafted(who, "Builder", CompanionWork.BuilderCraftPick(known, hook.Inventory, g));
        }

        void SalvageJob(Companion who)
        {
            var item = CompanionWork.JunkPick(hook.Inventory, IsJunk);
            if (item == null) return;
            var action = ActionFor(item);
            if (action == MinionRules.JunkAction.Eat) return;   // rarer junk is the Glutton's
            if (action == MinionRules.JunkAction.Dismantle)
            {
                var r = DismantleFor(item);
                if (r == null || !CraftRules.Craft(hook.Inventory, r)) return;
                Work(who, $"dismantled a {item.displayName}");
                hook.Log($"{who.Name} (Salvager) dismantled a {item.displayName}: {CraftRules.Describe(r.outputs)}.");
                SalvageBin.Instance?.Took($"{item.displayName} → {CraftRules.Describe(r.outputs)}", 3, new Color(0.8f, 0.45f, 1f));
            }
            else
            {
                int essence = hook.Inventory.Salvage(item, 1);
                if (essence <= 0) return;
                Work(who, $"salvaged a {item.displayName}");
                hook.Log($"{who.Name} (Salvager) salvaged a {item.displayName} into {essence} essence.");
                SalvageBin.Instance?.Took($"{item.displayName} → {essence} essence", essence, new Color(0.6f, 0.85f, 1f));
            }
        }

        readonly Dictionary<Companion, float> snackTimers = new Dictionary<Companion, float>();

        /// <summary>
        /// Ed's ask: in Auto, each minion snacks on its own every <see cref="MinionRules.AutoFeedSeconds"/>, only
        /// from the ticked rarities and never more than the set share of a stack.
        /// </summary>
        void AutoFeed()
        {
            if (Battle.BattleSettings.Mode != Battle.BattleMode.Auto || !CompanionSettings.AutoFeed) return;
            foreach (var c in hook.Companions.All)
            {
                if (c.KnockedOut) continue;
                snackTimers.TryGetValue(c, out var t);
                t += Time.deltaTime;
                if (t < MinionRules.AutoFeedSeconds) { snackTimers[c] = t; continue; }
                snackTimers[c] = 0f;
                var item = MinionRules.AutoFeedPick(hook.Inventory.Items, c, CompanionSettings.FeedTier, CompanionSettings.FeedShare);
                if (item == null) continue;
                var learned = new List<MinionSkill>();
                int before = c.Level;
                var r = CompanionGrowth.Feed(c, item, learned);
                if (r == FeedResult.CantEat || r == FeedResult.Full) continue;
                hook.Inventory.Remove(item, 1);
                Work(c, $"snacked on a {item.displayName}");
                hook.Log(FeedLine(c, item, r, before, learned) + " (auto-feed)");
                Yum(c, item);
                if (learned.Count > 0) hook.Companions.Refresh();
                return;   // one snack a frame
            }
        }

        void EatJob(Companion who)
        {
            bool salvagerOn = CompanionSettings.SalvagerSorts && hook.Companions.Best(MinionSkill.Salvager) != null;
            // With a Salvager at work, the Glutton only eats what's above the take-apart rarity.
            var item = CompanionWork.JunkPick(hook.Inventory, i => IsJunk(i) && (!salvagerOn || ActionFor(i) == MinionRules.JunkAction.Eat));
            var eater = CompanionWork.Hungriest(hook.Companions.All);
            if (item == null || eater == null) return;
            var learned = new List<MinionSkill>();
            int before = eater.Level;
            var r = CompanionGrowth.Feed(eater, item, learned);
            if (r == FeedResult.CantEat || r == FeedResult.Full) return;
            hook.Inventory.Remove(item, 1);
            Work(who, eater == who ? $"ate a {item.displayName}" : $"fed {eater.Name}");
            hook.Log(FeedLine(eater, item, r, before, learned) + (eater != who ? $" ({who.Name} shared it)" : ""));
            Yum(eater, item);
            if (learned.Count > 0) hook.Companions.Refresh();
        }

        /// <summary>One log line for a meal: EXP, level-ups, evolutions and new skills.</summary>
        public static string FeedLine(Companion c, ItemDefinition item, FeedResult r, int levelBefore, List<MinionSkill> learned)
        {
            string line = $"{c.Name} ate the {item.displayName} (+{CompanionGrowth.FeedPoints(item, c)} EXP){(MinionRules.IsFavourite(item, c) ? ", its favourite!" : ".")}";
            if (c.Level > levelBefore) line += $" Lv {c.Level}!";
            if (r == FeedResult.Evolved) line += $" Evolved into an {c.StageName}!";
            if (learned != null && learned.Count > 0) line += $" Learned {string.Join(", ", learned)}.";
            return line;
        }

        void Work(Companion c, string what = null)
        {
            if (bodies.TryGetValue(c, out var b)) b.Busy = 1.5f;
            if (what != null) MinionActivity.Did(c, what);
        }

        // ---- Crew installs take time (Ed's ask): walk over, hammer with a progress bar, then the part is on ----

        class Job
        {
            public Companion Crew;
            public Rafts.Cell Cell;
            public ItemDefinition Part;
            public Vector3 Stand;      // plank next to the spot, raft-local
            public bool Working;       // standing there and hammering
            public float Progress;     // 0..1
            public GameObject Ghost, Hammer;
        }

        Job job;

        void StartJob(Companion crew, Rafts.Cell cell, ItemDefinition part)
        {
            // Stand on the plank next to the spot that's nearest the raft's middle.
            var stand = cell.Neighbours().Where(Raft.Grid.Walkable).OrderBy(n => Mathf.Abs(n.X) + Mathf.Abs(n.Z)).Select(Local).FirstOrDefault();
            if (stand == default) stand = Local(new Rafts.Cell(0, 0));
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = $"Installing {part.displayName}";
            Destroy(ghost.GetComponent<Collider>());
            ghost.transform.SetParent(Raft.transform, false);
            ghost.transform.localPosition = new Vector3(cell.X * Raft.CellSize, 0.05f, cell.Z * Raft.CellSize);
            ghost.transform.localScale = new Vector3(0.95f, 0.06f, 0.95f);
            Tint.Apply(ghost.GetComponent<Renderer>(), Raft.PartColor(part) * 0.6f);
            job = new Job { Crew = crew, Cell = cell, Part = part, Stand = stand, Ghost = ghost };
            hook.Log($"{crew.Name} (Crewmate) is going to {(part.type == ItemType.Module ? "install" : "build")} a {part.displayName}.");
            MinionActivity.Did(crew, $"{(part.type == ItemType.Module ? "installing" : "building")} a {part.displayName}");
        }

        void Install()
        {
            if (job == null) return;
            if (!bodies.TryGetValue(job.Crew, out var b) || !hook.Companions.All.Contains(job.Crew) || !CompanionSettings.CrewBuilds) { EndJob(); return; }
            if (!job.Working) return;
            if (job.Hammer == null)
            {
                job.Hammer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                job.Hammer.name = "Crew hammer";
                Destroy(job.Hammer.GetComponent<Collider>());
                job.Hammer.transform.SetParent(b.Go.transform, false);
                var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(head.GetComponent<Collider>());
                head.transform.SetParent(job.Hammer.transform, false);
                head.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                head.transform.localScale = new Vector3(2.2f, 0.45f, 1.2f);
                Tint.Apply(head.GetComponent<Renderer>(), new Color(0.45f, 0.45f, 0.5f));
                job.Hammer.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
                Tint.Apply(job.Hammer.GetComponent<Renderer>(), new Color(0.55f, 0.36f, 0.2f));
            }
            // Swing: up slowly, down fast, toward the spot.
            float swing = Mathf.Repeat(Time.time * 2.5f, 1f);
            float angle = swing < 0.7f ? Mathf.Lerp(-20f, 70f, swing / 0.7f) : Mathf.Lerp(70f, -20f, (swing - 0.7f) / 0.3f);
            var toSpot = new Vector3(job.Cell.X * Raft.CellSize, 0f, job.Cell.Z * Raft.CellSize) - job.Stand;
            float yaw = Mathf.Atan2(toSpot.x, toSpot.z) * Mathf.Rad2Deg;
            job.Hammer.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-angle, 0f, 0f);
            job.Hammer.transform.localPosition = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.6f, 0.6f);
            b.Busy = 0.3f;
            job.Progress += Time.deltaTime / CompanionWork.InstallSeconds;
            job.Ghost.transform.localScale = new Vector3(0.95f, Mathf.Lerp(0.06f, 0.3f, job.Progress), 0.95f);
            if (job.Progress < 1f) return;
            var part = job.Part;
            bool ok = CompanionWork.CrewInstall(Raft.Grid, hook.Inventory, job.Cell, part);
            var crew = job.Crew;
            EndJob();
            if (ok) hook.Log(part.type == ItemType.Module
                ? $"{crew.Name} (Crewmate) installed a {part.displayName} on the upstream side."
                : $"{crew.Name} (Crewmate) built a {part.displayName} onto the raft.");
        }

        void EndJob()
        {
            if (job == null) return;
            if (job.Ghost != null) Destroy(job.Ghost);
            if (job.Hammer != null) Destroy(job.Hammer);
            job = null;
        }

        // ---- Mystic crafting shows an incantation circle (Ed's ask) ----

        /// <summary>A glowing magic circle with spinning runes opens under the Mystic for a moment while it crafts.</summary>
        public void Incantation(Companion mystic)
        {
            if (mystic == null || !bodies.TryGetValue(mystic, out var b)) return;
            StartCoroutine(Circle(b.Go.transform));
        }

        System.Collections.IEnumerator Circle(Transform pet)
        {
            var root = new GameObject("Incantation").transform;
            root.SetParent(Raft.transform, false);
            var at = pet.localPosition;
            // Just above the deck (its top is at 0.3): lower, the planks hid the circle (Ed: "wala din yun").
            root.localPosition = new Vector3(at.x, 0.33f, at.z);
            var glow = new Color(0.75f, 0.45f, 1f);
            var ring = Disc(root, 1.4f, 0.02f, glow);
            var inner = Disc(root, 1.0f, 0.025f, new Color(0.25f, 0.1f, 0.4f));
            var core = Disc(root, 0.55f, 0.03f, new Color(1f, 0.85f, 1f));
            var runes = new Transform[6];
            for (int i = 0; i < runes.Length; i++)
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(r.GetComponent<Collider>());
                r.transform.SetParent(root, false);
                r.transform.localScale = new Vector3(0.12f, 0.04f, 0.22f);
                Tint.Apply(r.GetComponent<Renderer>(), new Color(1f, 0.9f, 0.5f));
                runes[i] = r.transform;
            }
            const float life = 2.4f;
            // Waits while a menu is open, so a craft made at the Workbench still shows its circle after.
            for (float t = 0f; t < life; t += WorldPause.Active ? 0f : Time.deltaTime)
            {
                if (root == null || pet == null) break;
                float open = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((life - t) / 0.35f);
                root.localScale = new Vector3(open, 1f, open);
                root.localRotation = Quaternion.Euler(0f, t * 120f, 0f);
                core.localRotation = Quaternion.Euler(0f, -t * 300f, 0f);
                for (int i = 0; i < runes.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / runes.Length;
                    runes[i].localPosition = new Vector3(Mathf.Cos(a) * 0.6f, 0.06f + Mathf.Sin(t * 8f + i) * 0.05f, Mathf.Sin(a) * 0.6f);
                    runes[i].localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                }
                yield return null;
            }
            if (root != null) Destroy(root.gameObject);
        }

        static Transform Disc(Transform parent, float size, float height, Color color)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(parent, false);
            d.transform.localScale = new Vector3(size, height, size);
            d.transform.localPosition = Vector3.up * height;
            Tint.Apply(d.GetComponent<Renderer>(), color);
            return d.transform;
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
                string tag = b.Busy > 0f ? " <color=#ffd866>working!</color>" : b.Pet.Sulking ? " <color=#9bb0c8>sulking</color>" : "";
                GUI.Label(new Rect(sp.x - 70, HuntStyles.H - sp.y - 10, 140, 20), $"<b>Lv {b.Pet.Level}</b>{tag}", nameStyle);
            }
            // The Crew's install progress over the spot (Ed's ask: a loading bar, not instant).
            if (job != null && job.Ghost != null)
            {
                var sp = HuntStyles.Ui(cam.WorldToScreenPoint(job.Ghost.transform.position + Vector3.up * 0.6f));
                if (sp.z < 0f) return;
                var r = new Rect(sp.x - 60, HuntStyles.H - sp.y - 6, 120, 12);
                GUI.Label(new Rect(r.x - 30, r.y - 18, 180, 18), $"<color=#ffd866>{(job.Working ? "Hammering" : "On the way")}: {job.Part.displayName}</color>", nameStyle);
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0f, 0f, 0f, 0.6f), 0, 3);
                GUI.DrawTexture(new Rect(r.x + 1, r.y + 1, (r.width - 2) * Mathf.Clamp01(job.Progress), r.height - 2), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(1f, 0.8f, 0.3f), 0, 3);
            }
        }
    }
}
