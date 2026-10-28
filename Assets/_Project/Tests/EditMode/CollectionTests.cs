using BrineBattle.Collection;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class CollectionTests
    {
        [Test]
        public void RecordOnlyFirstTime()
        {
            var log = new CollectionLog();
            int events = 0;
            log.Unlocked += (p, id) => events++;
            Assert.IsTrue(log.Record(CollectionPage.Monsters, "eel"));
            Assert.IsFalse(log.Record(CollectionPage.Monsters, "eel"));
            Assert.IsFalse(log.Record(CollectionPage.Monsters, ""));
            Assert.AreEqual(1, events);
            Assert.IsTrue(log.Has(CollectionPage.Monsters, "eel"));
            Assert.IsFalse(log.Has(CollectionPage.Items, "eel"));
        }

        [Test]
        public void CompletionIgnoresUnknownAndDuplicateIds()
        {
            var log = new CollectionLog();
            log.Record(CollectionPage.Items, "a");
            log.Record(CollectionPage.Items, "zzz");
            Assert.AreEqual(0.5f, log.Completion(CollectionPage.Items, new[] { "a", "b", "b", null }), 1e-5f);
            Assert.AreEqual(0f, log.Completion(CollectionPage.Pets, new string[0]));
        }

        [Test]
        public void SerializeRoundTrip()
        {
            var log = new CollectionLog();
            log.Record(CollectionPage.Monsters, "eel");
            log.Record(CollectionPage.Monsters, "gull");
            log.Record(CollectionPage.Items, "scale");
            var back = CollectionLog.Deserialize(log.Serialize());
            Assert.IsTrue(back.Has(CollectionPage.Monsters, "eel"));
            Assert.IsTrue(back.Has(CollectionPage.Monsters, "gull"));
            Assert.IsTrue(back.Has(CollectionPage.Items, "scale"));
            Assert.AreEqual(0, back.Count(CollectionPage.Pets));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("garbage|Nope:x|Items")]
        public void DeserializeBadDataIsEmpty(string data)
        {
            var log = CollectionLog.Deserialize(data);
            Assert.AreEqual(0, log.Count(CollectionPage.Monsters) + log.Count(CollectionPage.Items) + log.Count(CollectionPage.Pets));
        }
    }
}
