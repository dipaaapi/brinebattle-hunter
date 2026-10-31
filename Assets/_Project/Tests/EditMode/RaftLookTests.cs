using System.Linq;
using BrineBattle.Hunting;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BrineBattle.Tests
{
    /// <summary>
    /// Ed: at raft Lv 33 a dark wedge covered the whole deck and hid the hunter and minions. The real raft
    /// decor is built at every raft level on a small and a large raft, with every open ship piece on and with
    /// the last few missing (so the scaffold shows), and nothing may stand on the deck where people walk: only
    /// flat things (ropes, trim) on it, walls and rails on the edges, the hull under it, sails above head height.
    /// </summary>
    public class RaftLookTests
    {
        const float DeckTop = 0.3f, Flat = 0.08f, HeadHeight = 1.45f, EdgeBand = 0.2f;

        [Test]
        public void NothingStandsOnTheWalkableDeckAtAnyRaftLevel([Values(1, 4)] int half, [Values(0, 4)] int piecesBehind)
        {
            LogAssert.ignoreFailingMessages = true;   // Destroy() in the decor logs in edit mode
            var plank = ScriptableObject.CreateInstance<ItemDefinition>();
            plank.type = ItemType.Raft;
            var go = new GameObject("Raft");
            try
            {
                var raft = go.AddComponent<Raft>();
                raft.Grid.FillSquare(plank, half);
                var decor = go.AddComponent<RaftDecor>();
                decor.raft = raft;
                var rebuild = typeof(RaftDecor).GetMethod("Rebuild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                float s = Raft.CellSize;
                float minX = (-half - 0.5f) * s + EdgeBand, maxX = (half + 0.5f) * s - EdgeBand;
                for (int lv = 1; lv <= EvolutionRules.RaftMax; lv++)
                {
                    raft.Grid.RestoreEvolution(RaftKind.Plain, lv);
                    raft.Grid.RestorePieces(ShipPieceRules.FreeFor(lv - piecesBehind));
                    foreach (Transform c in go.transform) Object.DestroyImmediate(c.gameObject);
                    rebuild.Invoke(decor, null);
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                    {
                        var b = r.bounds;
                        bool overDeck = b.max.x > minX && b.min.x < maxX && b.max.z > minX && b.min.z < maxX;
                        bool inPeoplesSpace = b.max.y > DeckTop + Flat && b.min.y < HeadHeight;
                        Assert.IsFalse(overDeck && inPeoplesSpace,
                            $"raft Lv {lv}, {2 * half + 1}×{2 * half + 1}: '{r.name}' at {b.center} size {b.size} stands on the walkable deck");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void EachInstalledPieceAddsToTheLook()
        {
            LogAssert.ignoreFailingMessages = true;
            var plank = ScriptableObject.CreateInstance<ItemDefinition>();
            plank.type = ItemType.Raft;
            var go = new GameObject("Raft");
            try
            {
                var raft = go.AddComponent<Raft>();
                raft.Grid.FillSquare(plank, 2);
                var decor = go.AddComponent<RaftDecor>();
                decor.raft = raft;
                var rebuild = typeof(RaftDecor).GetMethod("Rebuild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                raft.Grid.RestoreEvolution(RaftKind.Plain, EvolutionRules.RaftMax);
                foreach (var p in ShipPieceRules.All)
                {
                    raft.Grid.RestorePieces(ShipPieceRules.All.Where(x => x.Level <= p.Level).Select(x => x.Id));
                    foreach (Transform c in go.transform) Object.DestroyImmediate(c.gameObject);
                    rebuild.Invoke(decor, null);
                    Assert.IsTrue(go.GetComponentsInChildren<Renderer>().Any(r => r.name == p.Id), $"the {p.Name} shows once it's on");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                LogAssert.ignoreFailingMessages = false;
            }
        }
    }
}
