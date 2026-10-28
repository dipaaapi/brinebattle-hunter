using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class SmokeTests
    {
        [Test]
        public void RuntimeAssemblyIsReferenced()
        {
            Assert.AreEqual("BrineBattle Hunter", GameInfo.Title);
        }
    }
}
