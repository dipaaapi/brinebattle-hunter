using System.Linq;
using BrineBattle.Items;
using BrineBattle.Rafts;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    /// <summary>Fame and achievements (Ed's ask: the goal is fame and leaderboards; local first version).</summary>
    public class FameTests
    {
        [Test]
        public void FameGrowsWithMonstersBossesAndDifficulty()
        {
            var f = new FameBook();
            Assert.AreEqual(1, f.Kill(Rarity.Common, false, Difficulty.Easy));
            Assert.AreEqual(4, f.Kill(Rarity.Uncommon, false, Difficulty.Hard), "(1 + 1) × 2 on Hard");
            Assert.AreEqual(50, f.Kill(Rarity.Common, true, Difficulty.Hard), "a boss: 25 × 2");
            Assert.AreEqual(3, f.Kills);
            Assert.AreEqual(1, f.Bosses);
            Assert.AreEqual(1f, FameBook.Multiplier(Difficulty.Easy));
            Assert.AreEqual(2f, FameBook.Multiplier(Difficulty.Hard));
        }

        [Test]
        public void NewBestLevelsGiveFameOnlyOnce()
        {
            var f = new FameBook();
            Assert.AreEqual(20, f.Reached(RaftKind.Plain, 5, Difficulty.Easy), "4 new raft levels × 5");
            Assert.AreEqual(0, f.Reached(RaftKind.Plain, 5, Difficulty.Easy));
            Assert.AreEqual(0, f.Reached(RaftKind.Plain, 3, Difficulty.Easy), "going down gives nothing");
            Assert.AreEqual(10, f.Reached(RaftKind.Sea, 1, Difficulty.Easy), "first ship level");
            Assert.AreEqual(EvolutionRules.RaftMax, f.BestRaft);
            Assert.AreEqual(1, f.BestShip);
        }

        [Test]
        public void AchievementsUnlockOnceAndAddFame()
        {
            var f = new FameBook();
            int earned = 0;
            f.Earned += a => earned++;
            f.Kill(Rarity.Common, false, Difficulty.Easy);
            Assert.IsTrue(f.Has("first_blood"));
            Assert.AreEqual(1, earned);
            int points = f.Points;
            f.Check(Difficulty.Easy);
            Assert.AreEqual(points, f.Points, "no second time");
            f.Reached(RaftKind.Plain, 50, Difficulty.Easy);
            Assert.IsTrue(f.Has("raft_10") && f.Has("raft_25") && f.Has("raft_50"));
            Assert.AreEqual(FameBook.All.Length, FameBook.All.Select(a => a.Id).Distinct().Count(), "ids are unique");
        }

        [Test]
        public void RestoreKeepsEverythingAndRankFollowsFame()
        {
            var f = new FameBook();
            f.Restore(1200, 40, 2, 5, 30, 50, 12, new[] { "boss_1", "raft_50" });
            Assert.AreEqual(1200, f.Points);
            Assert.AreEqual("Captain", f.Rank);
            Assert.IsTrue(f.Has("raft_50"));
            Assert.AreEqual(12, f.BestShip);
            Assert.AreEqual("Castaway", new FameBook().Rank);
        }
    }
}
