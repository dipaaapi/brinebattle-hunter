using System.Collections.Generic;
using System.Linq;

namespace BrineBattle.World
{
    /// <summary>
    /// The sea journal of "firsts" (Claude's C-15): small one-time goals beside the quests, each with a
    /// small reward (neutral essence of the raft's level and Alchemy Mastery points). Plain C#; saved by id.
    /// </summary>
    public class Firsts
    {
        public readonly struct Entry
        {
            public readonly string Id, Title, How;
            public readonly int Essence, Mastery;
            public Entry(string id, string title, string how, int essence, int mastery) { Id = id; Title = title; How = how; Essence = essence; Mastery = mastery; }
        }

        public static readonly Entry[] All =
        {
            new Entry("clean_reel", "First clean reel", "Catch something while barely leaving the green zone.", 3, 5),
            new Entry("tame", "First minion", "Tame a weakened, calm monster.", 3, 5),
            new Entry("favourite", "A happy minion", "Feed a minion its favourite food.", 2, 5),
            new Entry("lost_part", "Nothing lost at sea", "Catch back a raft part that floated away.", 2, 5),
            new Entry("level", "Growing raft", "Level the raft up for the first time.", 5, 10),
            new Entry("storm", "Weathered the storm", "Live through a storm until the weather changes.", 5, 10),
            new Entry("rescue", "Lifeguard", "Rescue a survivor drifting on a plank.", 5, 10),
            new Entry("vessel", "Shipwright", "Put the first vehicle part on the raft.", 8, 15),
            new Entry("boss", "Boss hunter", "Beat a raft-level boss.", 10, 20),
        };

        readonly HashSet<string> done = new HashSet<string>();

        public IEnumerable<string> Done => done;
        public bool Has(string id) => done.Contains(id);
        public int Count => done.Count;

        /// <summary>Marks a first. True only the first time (and only for a known id): then give its reward.</summary>
        public bool Mark(string id) => All.Any(e => e.Id == id) && done.Add(id);

        public static Entry Get(string id) => All.First(e => e.Id == id);

        public void Restore(IEnumerable<string> ids)
        {
            done.Clear();
            if (ids == null) return;
            foreach (var id in ids) if (All.Any(e => e.Id == id)) done.Add(id);
        }
    }
}
