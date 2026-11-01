using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrineBattle.Items;

namespace BrineBattle.Rafts
{
    /// <summary>
    /// A saved raft layout (Ed's ask, 2026-10-31): which part is on which cell, its line and level, who made it
    /// and, for a copy, who it was copied from (so a copy is never passed off as your own). Loading one shows it
    /// as a ghost; applying it builds what the bag has and lists what's missing. Copying a design never gives
    /// parts for free. Plain C# for EditMode tests.
    /// </summary>
    [Serializable]
    public class Blueprint
    {
        public string name = "My raft", author = "Captain", copiedFrom = "";
        public int line, level;
        public List<Slot> cells = new List<Slot>();

        [Serializable]
        public class Slot { public int x, z; public string id; }

        public RaftKind Line => (RaftKind)line;
        public int PartCount => cells.Count;

        public static Blueprint Capture(RaftGrid g, string name, string author) => new Blueprint
        {
            name = Clean(name, "My raft"), author = Clean(author, "Captain"),
            line = (int)g.Chosen, level = g.Level,
            cells = g.Parts.OrderBy(kv => kv.Key.X).ThenBy(kv => kv.Key.Z).Select(kv => new Slot { x = kv.Key.X, z = kv.Key.Z, id = kv.Value.id }).ToList(),
        };

        /// <summary>Names stay short and free of the code's separators.</summary>
        public static string Clean(string s, string fallback)
        {
            s = new string((s ?? "").Where(c => !char.IsControl(c) && c != '|' && c != ';').ToArray()).Trim();
            if (s.Length > 32) s = s.Substring(0, 32);
            return s.Length == 0 ? fallback : s;
        }

        /// <summary>A copy for someone else: their name as author, the original's author kept as the credit.</summary>
        public Blueprint CopyFor(string newAuthor) => new Blueprint
        {
            name = name, author = Clean(newAuthor, "Captain"),
            copiedFrom = string.IsNullOrEmpty(copiedFrom) ? author : copiedFrom,
            line = line, level = level, cells = cells.Select(c => new Slot { x = c.x, z = c.z, id = c.id }).ToList(),
        };

        public string Credit => string.IsNullOrEmpty(copiedFrom) ? $"by {author}" : $"by {author}, copied from {copiedFrom}";

        // ---- Share code (no server, no login: paste it anywhere, e.g. Discord) ----

        const string Prefix = "BBH1:";

        public string ToCode()
        {
            var sb = new StringBuilder();
            sb.Append(name).Append('|').Append(author).Append('|').Append(copiedFrom).Append('|').Append(line).Append('|').Append(level).Append('|');
            sb.Append(string.Join(";", cells.Select(c => $"{c.x},{c.z},{c.id}")));
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
            return Prefix + body + "." + Check(body);
        }

        /// <summary>Reads a share code; null when it's broken or tampered with (the checksum doesn't match).</summary>
        public static Blueprint FromCode(string code)
        {
            try
            {
                code = (code ?? "").Trim();
                if (!code.StartsWith(Prefix)) return null;
                code = code.Substring(Prefix.Length);
                int dot = code.LastIndexOf('.');
                if (dot < 0) return null;
                string body = code.Substring(0, dot);
                if (code.Substring(dot + 1) != Check(body)) return null;
                var parts = Encoding.UTF8.GetString(Convert.FromBase64String(body)).Split('|');
                if (parts.Length != 6) return null;
                var bp = new Blueprint
                {
                    name = Clean(parts[0], "Shared raft"), author = Clean(parts[1], "Someone"), copiedFrom = Clean(parts[2], ""),
                    line = int.Parse(parts[3]), level = int.Parse(parts[4]),
                };
                if (bp.line < 0 || bp.line > 3) return null;
                foreach (var cell in parts[5].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var f = cell.Split(',');
                    if (f.Length != 3) return null;
                    int x = int.Parse(f[0]), z = int.Parse(f[1]);
                    if (Math.Abs(x) > RaftRules.MaxRadius || Math.Abs(z) > RaftRules.MaxRadius) return null;
                    bp.cells.Add(new Slot { x = x, z = z, id = f[2] });
                }
                return bp.cells.Count > 0 ? bp : null;
            }
            catch (Exception) { return null; }
        }

        static string Check(string body)
        {
            uint h = 2166136261;
            foreach (char ch in body) { h ^= ch; h *= 16777619; }
            return (h % 46656).ToString("x4");
        }

        // ---- Building it ----

        /// <summary>Parts the blueprint needs that aren't on those cells now, by item id (what a build would use).</summary>
        public Dictionary<string, int> Needed(RaftGrid g)
        {
            var need = new Dictionary<string, int>();
            foreach (var c in cells)
            {
                var cell = new Cell(c.x, c.z);
                if (g.Parts.TryGetValue(cell, out var on) && on.id == c.id) continue;
                need[c.id] = need.TryGetValue(c.id, out var n) ? n + 1 : 1;
            }
            return need;
        }

        /// <summary>What's still missing after the bag: id → count (the quest for the minions).</summary>
        public Dictionary<string, int> Missing(RaftGrid g, Inventory bag, Func<string, ItemDefinition> find)
        {
            var missing = new Dictionary<string, int>();
            foreach (var kv in Needed(g))
            {
                var item = find(kv.Key);
                int have = item != null ? bag.Count(item) : 0;
                if (have < kv.Value) missing[kv.Key] = kv.Value - have;
            }
            return missing;
        }

        /// <summary>
        /// Builds the blueprint from the bag: empty cells get their part, cells with another part get swapped
        /// (the old part goes to the bag). Every placement follows the normal rules, so cells that don't fit yet
        /// are skipped. Repeats until nothing more goes down. Returns how many cells changed.
        /// </summary>
        public int Apply(RaftGrid g, Inventory bag, Func<string, ItemDefinition> find)
        {
            int changed = 0;
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (var c in cells)
                {
                    var cell = new Cell(c.x, c.z);
                    var item = find(c.id);
                    if (item == null || bag.Count(item) <= 0) continue;
                    if (g.Parts.TryGetValue(cell, out var on))
                    {
                        if (on.id == c.id) continue;
                        if (g.Replace(cell, item, out var old) != PlaceResult.Ok) continue;
                        bag.Remove(item, 1);
                        bag.Add(old, 1);
                    }
                    else
                    {
                        if (g.Place(cell, item) != PlaceResult.Ok) continue;
                        bag.Remove(item, 1);
                    }
                    changed++;
                    progress = true;
                }
            }
            return changed;
        }
    }

    /// <summary>The player's saved blueprints (the Shipyard's Store tab); kept outside the save file.</summary>
    [Serializable]
    public class BlueprintBook
    {
        public string captain = "Captain";
        public List<Blueprint> saved = new List<Blueprint>();

        public void Add(Blueprint bp)
        {
            saved.RemoveAll(b => b.name == bp.name && b.author == bp.author);
            saved.Insert(0, bp);
            if (saved.Count > 50) saved.RemoveAt(saved.Count - 1);
        }
    }
}
