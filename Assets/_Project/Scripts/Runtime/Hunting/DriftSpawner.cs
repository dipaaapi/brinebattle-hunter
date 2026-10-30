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
            monsters.RemoveAll(m => m == null || m.isFlotsam);
            hook = FindFirstObjectByType<HookController>();
            // A few drifts already in reach so there is something to hook right away.
            for (int i = 0; i < 3; i++) Spawn(Random.Range(-8f, 4f));
        }

        void Update()
        {
            live.RemoveAll(d => d == null);
            if (WorldPause.Active) return;
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
            if (flotsam.Count == 0 || rng.NextDouble() >= flotsamShare) return null;
            var tier = raft != null ? raft.Tier : Items.Rarity.Common;
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

        static float Weight(Items.Rarity r, Items.Rarity tier) => r == tier ? 3f : r > tier ? 0.5f : 1f;

        void Spawn(float alongCurrent)
        {
            if (monsters.Count == 0 || raft == null) return;
            var monster = monsters[rng.Next(monsters.Count)];
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

            var state = monster.isFlotsam ? DriftState.Dead : EncounterRules.RollState(deadWeight, weakenedWeight, aliveWeight, rng);
            var hint = monster.isFlotsam ? DriftState.Dead : EncounterRules.Hint(state, Accuracy, rng);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            var drift = go.AddComponent<Drift>();
            drift.Init(monster, state, hint, dir * driftSpeed * Random.Range(0.7f, 1.3f));
            drift.Aggression = Encounters.EncounterRules.RollAggression(monster.aggression, new System.Random(Random.Range(0, int.MaxValue)));
            if (back != null && monster.id == back.MonsterId) drift.MakeGorged(back.Parts);
            live.Add(drift);
        }
    }
}
