using BrineBattle.Hunting;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class IdleTests
    {
        [Test]
        public void FallsAsleepOnceAfterTheLimit()
        {
            var clock = new IdleClock();
            Assert.IsFalse(clock.Tick(9 * 60f, false, 10));
            Assert.IsTrue(clock.Tick(60f, false, 10));
            Assert.IsFalse(clock.Tick(60f, false, 10), "only once");
        }

        [Test]
        public void InputResetsTheClock()
        {
            var clock = new IdleClock();
            clock.Tick(9 * 60f, false, 10);
            Assert.IsFalse(clock.Tick(0.1f, true, 10));
            Assert.AreEqual(0f, clock.Idle);
            Assert.IsFalse(clock.Tick(9 * 60f, false, 10));
        }

        [Test]
        public void OffNeverSleeps()
        {
            var clock = new IdleClock();
            Assert.IsFalse(clock.Tick(10000f, false, 0));
        }
    }
}
