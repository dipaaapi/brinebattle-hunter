using System.Collections.Generic;
using BrineBattle.Hunting;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class FaqTests
    {
        [Test]
        public void EveryTopicHasAnsweredQuestions()
        {
            Assert.IsNotEmpty(Faq.Topics);
            var seen = new HashSet<string>();
            foreach (var t in Faq.Topics)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(t.Name));
                Assert.IsNotEmpty(t.Entries, t.Name);
                foreach (var e in t.Entries)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(e.Question), t.Name);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(e.Answer), e.Question);
                    Assert.IsTrue(seen.Add(e.Question), $"Duplicate question: {e.Question}");
                }
            }
        }
    }
}
