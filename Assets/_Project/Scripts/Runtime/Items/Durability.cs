using System;

namespace BrineBattle.Items
{
    /// <summary>
    /// Wear on a tool (idea C-4): the Harpoon and raft modules share this. At 0 the tool is broken
    /// and does nothing until repaired. Plain C# for EditMode tests.
    /// </summary>
    public class Durability
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool Broken => Current <= 0;
        public bool Full => Current >= Max;

        public Durability(int max) : this(max, max) { }
        public Durability(int max, int current)
        {
            Max = Math.Max(1, max);
            Current = Math.Max(0, Math.Min(Max, current));
        }

        /// <summary>Uses <paramref name="amount"/> if not broken. Returns false (and uses nothing) when broken.</summary>
        public bool Use(int amount = 1)
        {
            if (Broken) return false;
            Current = Math.Max(0, Current - amount);
            return true;
        }

        public void Repair(int amount) => Current = Math.Min(Max, Current + Math.Max(0, amount));

        public override string ToString() => $"{Current}/{Max}";
    }
}
