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

        void Start()
        {
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

        void Spawn(float alongCurrent)
        {
            if (monsters.Count == 0 || raft == null) return;
            var monster = monsters[rng.Next(monsters.Count)];
            if (monster == null) return;

            var dir = currentDirection.normalized;
            var side = Vector3.Cross(Vector3.up, dir);
            var pos = raft.transform.position + dir * alongCurrent + side * Random.Range(-9f, 9f);
            pos.y = 0.1f;

            var state = EncounterRules.RollState(deadWeight, weakenedWeight, aliveWeight, rng);
            var hint = EncounterRules.Hint(state, Accuracy, rng);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            var drift = go.AddComponent<Drift>();
            drift.Init(monster, state, hint, dir * driftSpeed * Random.Range(0.7f, 1.3f));
            drift.Aggression = Encounters.EncounterRules.RollAggression(monster.aggression, new System.Random(Random.Range(0, int.MaxValue)));
            live.Add(drift);
        }
    }
}
