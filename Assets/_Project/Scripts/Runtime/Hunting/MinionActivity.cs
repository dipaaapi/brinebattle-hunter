using System.Collections.Generic;
using BrineBattle.Companions;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// What each minion did last, for the HUD (Ed's U-39: show the job each one is doing). A job shows for a
    /// while after it happened; otherwise the minion's main skill says what it is waiting to do.
    /// </summary>
    public static class MinionActivity
    {
        const float ShowSeconds = 12f;
        static readonly Dictionary<Companion, (string what, float at)> last = new Dictionary<Companion, (string, float)>();

        public static void Did(Companion c, string what)
        {
            if (c != null) last[c] = (what, Time.time);
        }

        public static string Now(Companion c)
        {
            if (c != null && last.TryGetValue(c, out var l) && Time.time - l.at < ShowSeconds) return l.what;
            if (c == null || c.Skills.Count == 0) return "idle";
            return Idle(c.Skills[c.Skills.Count > 1 ? 1 : 0]);
        }

        static string Idle(MinionSkill s)
        {
            switch (s)
            {
                case MinionSkill.Fighter: return "on guard";
                case MinionSkill.Looter: return "watching the water";
                case MinionSkill.Crewmate: case MinionSkill.Builder: return "walking the raft";
                case MinionSkill.Mender: return "checking the gear";
                case MinionSkill.Alchemist: case MinionSkill.Smith: case MinionSkill.Tinker: return "at the workbench";
                case MinionSkill.Glutton: return "looking for snacks";
                case MinionSkill.Salvager: return "sorting the bag";
                default: return "resting";
            }
        }
    }
}
