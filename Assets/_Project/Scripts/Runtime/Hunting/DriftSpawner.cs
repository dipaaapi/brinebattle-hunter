using System.Collections.Generic;
using BrineBattle.Encounters;
using BrineBattle.Monsters;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>Spawns drifts upstream; the current carries them past the raft (GDD §3-4).</summary>
    public class DriftSpawner : MonoBehaviour
    {
        public Raft raft;
        public List<MonsterDefinition> monsters = new List<MonsterDefinition>();

        [Header("State roll weights")]
        public float deadWeight = 5f;
        public float weakenedWeight = 3f;
        public float aliveWeight = 2f;
        [Tooltip("Chance the hint shows the real state. Alchemy Mastery raises this later (idea C-1).")]
        [Range(0f, 1f)] public float hintAccuracy = 0.7f;

        [Header("Current")]
        public Vector3 currentDirection = new Vector3(1f, 0f, -0.3f);
        public float driftSpeed = 1.6f;
        public float spawnDistance = 22f;
        public float spawnInterval = 3f;
        public int maxDrifts = 6;

        readonly List<Drift> live = new List<Drift>();
        System.Random rng;
        float timer;

        HookController hook;

        void Awake() => rng = new System.Random();

        /// <summary>Idea Round 3 (C-1): Alchemy Mastery makes the hints more accurate.</summary>
        float Accuracy => hook != null ? Items.AlchemyMastery.HintAccuracy(hook.Mastery.Level) : hintAccuracy;

        /// <summary>Floating loot (Ed's U-30), split from the creature list at start.</summary>
        readonly List<MonsterDefinition> flotsam = new List<MonsterDefinition>();
        [Tooltip("Share of new drifts that are floating loot instead of creatures.")]
        [Range(0f, 1f)] public float flotsamShare = 0.3f;

        void Start()
        {
            flotsam.AddRange(monsters.FindAll(m => m != null && m.isFlotsam));
            bosses.AddRange(monsters.FindAll(m => m != null && m.isBoss));
            monsters.RemoveAll(m => m == null || m.isFlotsam || m.isBoss);
            hook = FindFirstObjectByType<HookController>();
            // A few drifts already in reach so there is something to hook right away.
            for (int i = 0; i < 3; i++) Spawn(Random.Range(-8f, 4f));
        }

        /// <summary>Raft-level bosses (Ed's ask), split from the creature list at start.</summary>
        readonly List<MonsterDefinition> bosses = new List<MonsterDefinition>();
        float bossTimer = Rafts.BossRules.SpawnSeconds - 5f;

        /// <summary>The boss of a raft level (by its rarity), or null.</summary>
        public MonsterDefinition BossFor(Items.Rarity level) => bosses.Find(b => b.rarity == level);

        /// <summary>The boss of the raft's level, once it's due (one at a time).</summary>
        void SpawnBoss()
        {
            if (raft == null || !Rafts.BossRules.Due(raft.Grid) || live.Exists(d => d != null && d.Monster.isBoss)) { bossTimer = 0f; return; }
            bossTimer += Time.deltaTime;
            if (bossTimer < Rafts.BossRules.SpawnSeconds) return;
            bossTimer = 0f;
            var boss = bosses.Find(b => b.rarity == raft.Tier);
            if (boss == null) return;
            var dir = currentDirection.normalized;
            var side = Vector3.Cross(Vector3.up, dir);
            var pos = raft.transform.position + dir * -spawnDistance + side * Random.Range(-4f, 4f);
            pos.y = 0.1f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            var drift = go.AddComponent<Drift>();
            // Slow, so there's time to hook it; always alive and hostile, and the hint never lies.
            drift.Init(boss, DriftState.Alive, DriftState.Alive, dir * driftSpeed * 0.6f);
            drift.Aggression = 1f;
            live.Add(drift);
            if (hook != null) hook.Log($"BOSS! {boss.displayName} rises from the deep. Beat it to level up the raft.");
        }

        void Update()
        {
            live.RemoveAll(d => d == null);
            if (WorldPause.Active) return;
            SpawnBoss();
            timer += Time.deltaTime;
            if (timer >= spawnInterval && live.Count < maxDrifts)
            {
                timer = 0f;
                Spawn(-spawnDistance);
            }
        }

        /// <summary>
        /// Now and then floating loot instead of a creature (U-30): rarities up to one above the raft's level,
        /// the raft's own level most often.
        /// </summary>
        MonsterDefinition PickFlotsam()
        {
            if (flotsam.Count == 0) return null;
            // Idea C-13: after a storm, wreckage one rarity higher drifts by.
            bool wreck = WorldClock.Now != null && WorldClock.Now.TakeWreck();
            if (!wreck && rng.NextDouble() >= flotsamShare + World.WorldRules.FlotsamBonus(WorldClock.Weather)) return null;
            var tier = raft != null ? raft.Tier : Items.Rarity.Common;
            if (wreck && tier < Items.Rarity.Mythic) tier++;
            var fits = flotsam.FindAll(f => f.rarity <= tier + 1);
            if (fits.Count == 0) return null;
            float total = 0f;
            foreach (var f in fits) total += Weight(f.rarity, tier);
            float roll = (float)rng.NextDouble() * total;
            foreach (var f in fits)
            {
                roll -= Weight(f.rarity, tier);
                if (roll <= 0f) return f;
            }
            return fits[fits.Count - 1];
        }

        /// <summary>Sun creatures turn up more by day, Moon creatures by night (U-10).</summary>
        MonsterDefinition PickCreature()
        {
            float total = 0f;
            foreach (var m in monsters) total += World.WorldRules.SpawnWeight(World.WorldRules.AffinityOf(m.element), WorldClock.Time);
            float roll = (float)rng.NextDouble() * total;
            foreach (var m in monsters)
            {
                roll -= World.WorldRules.SpawnWeight(World.WorldRules.AffinityOf(m.element), WorldClock.Time);
                if (roll <= 0f) return m;
            }
            return monsters[monsters.Count - 1];
        }

        static float Weight(Items.Rarity r, Items.Rarity tier) => r == tier ? 3f : r > tier ? 0.5f : 1f;

        /// <summary>
        /// Reset raft (Ed's ask): the parts the sea takes don't vanish. Each floats off the raft's edge as
        /// slow floating loot, so the hunter, a module or a Looter minion can still catch it back.
        /// </summary>
        public void WashOff(IReadOnlyList<Items.ItemDefinition> parts)
        {
            if (raft == null || parts == null) return;
            var dir = currentDirection.normalized;
            foreach (var part in parts)
            {
                var basis = flotsam.Find(f => f.rarity == part.rarity) ?? (flotsam.Count > 0 ? flotsam[0] : null);
                if (basis == null) return;
                // Off the downstream half of the raft's edge, so it drifts away from the raft, not into it.
                var pos = raft.transform.position + Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * dir * (raft.EdgeDistance + 0.8f);
                pos.y = 0.1f;
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(transform, true);
                go.transform.position = pos;
                var drift = go.AddComponent<Drift>();
                drift.Init(basis, DriftState.Dead, DriftState.Dead, dir * driftSpeed * 0.45f);
                drift.CarryPart(part);
                live.Add(drift);
            }
        }

        void Spawn(float alongCurrent)
        {
            if (monsters.Count == 0 || raft == null) return;
            var monster = PickCreature();
            var loot = PickFlotsam();
            if (loot != null) monster = loot;
            // Phase 8: a monster that ate raft parts comes back now and then (one at a time).
            var back = hook != null && !live.Exists(d => d != null && d.GorgedParts > 0) ? hook.Gorged.PickReturning(rng) : null;
            if (back != null) monster = monsters.Find(m => m != null && m.id == back.MonsterId) ?? monster;
            if (monster == null) return;

            var dir = currentDirection.normalized;
            var side = Vector3.Cross(Vector3.up, dir);
            var pos = raft.transform.position + dir * alongCurrent + side * Random.Range(-9f, 9f);
            pos.y = 0.1f;

            // Idea Round 4: night and storms stir creatures up, fog and night spoil hints.
            var w = WorldClock.Weather;
            var t = WorldClock.Time;
            var state = monster.isFlotsam ? DriftState.Dead : EncounterRules.RollState(deadWeight * World.WorldRules.DeadScale(w, t), weakenedWeight, aliveWeight * World.WorldRules.AliveScale(w, t), rng);
            var hint = monster.isFlotsam ? DriftState.Dead : EncounterRules.Hint(state, World.WorldRules.HintAccuracy(Accuracy, w, t), rng);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            var drift = go.AddComponent<Drift>();
            drift.Init(monster, state, hint, dir * driftSpeed * World.WorldRules.DriftSpeedScale(w) * Random.Range(0.7f, 1.3f));
            drift.Aggression = Encounters.EncounterRules.RollAggression(monster.aggression, new System.Random(Random.Range(0, int.MaxValue)));
            if (back != null && monster.id == back.MonsterId) drift.MakeGorged(back.Parts);
            live.Add(drift);
        }
    }
}
