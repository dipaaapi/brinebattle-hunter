using System;
using System.Collections.Generic;
using System.Linq;

namespace BrineBattle.Collection
{
    public enum CollectionPage
    {
        Monsters = 0,
        Items = 1,
        Pets = 2,
    }

    /// <summary>
    /// Which ids the player has acquired, per page (idea U-11). Entries not in here show as
    /// silhouettes. Plain C#; saves to a single string so any storage can hold it.
    /// </summary>
    public class CollectionLog
    {
        readonly Dictionary<CollectionPage, HashSet<string>> acquired = new Dictionary<CollectionPage, HashSet<string>>
        {
            { CollectionPage.Monsters, new HashSet<string>() },
            { CollectionPage.Items, new HashSet<string>() },
            { CollectionPage.Pets, new HashSet<string>() },
        };

        public event Action<CollectionPage, string> Unlocked;

        public bool Has(CollectionPage page, string id) => !string.IsNullOrEmpty(id) && acquired[page].Contains(id);

        public int Count(CollectionPage page) => acquired[page].Count;

        /// <summary>Returns true the first time an id is recorded.</summary>
        public bool Record(CollectionPage page, string id)
        {
            if (string.IsNullOrEmpty(id) || !acquired[page].Add(id)) return false;
            Unlocked?.Invoke(page, id);
            return true;
        }

        /// <summary>Share of <paramref name="allIds"/> acquired, 0-1. Unknown ids in the log don't count.</summary>
        public float Completion(CollectionPage page, IEnumerable<string> allIds)
        {
            var ids = allIds.Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
            if (ids.Count == 0) return 0f;
            return ids.Count(i => acquired[page].Contains(i)) / (float)ids.Count;
        }

        /// <summary>Format: "Monsters:a,b|Items:c|Pets:". Ids must not contain ',' '|' or ':'.</summary>
        public string Serialize() =>
            string.Join("|", acquired.Select(kv => $"{kv.Key}:{string.Join(",", kv.Value.OrderBy(x => x))}"));

        public static CollectionLog Deserialize(string data)
        {
            var log = new CollectionLog();
            if (string.IsNullOrEmpty(data)) return log;
            foreach (var part in data.Split('|'))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0 || !Enum.TryParse(part.Substring(0, colon), out CollectionPage page)) continue;
                foreach (var id in part.Substring(colon + 1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    log.acquired[page].Add(id);
            }
            return log;
        }
    }
}
