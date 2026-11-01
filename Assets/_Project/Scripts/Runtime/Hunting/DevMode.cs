using System.Collections.Generic;
using UnityEngine;

namespace BrineBattle.Hunting
{
    /// <summary>
    /// Developer mode (Ed's ask, 2026-10-31): fast QA of the real game. It only exists in the Unity Editor and in
    /// Development Builds (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`); a release build for Steam has none of it, so
    /// nothing needs removing later. No login: who can open it is decided by which build you run.
    /// </summary>
    public static class DevMode
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const bool Available = true;
#else
        public const bool Available = false;
#endif

        /// <summary>True once developer mode changed this game; its saves are marked DEV and never count as real progress.</summary>
        public static bool Used { get; set; }
        /// <summary>God mode: the hunter, the battle minion and the raft are healed every frame in battle.</summary>
        public static bool God { get; set; }

        static readonly List<string> log = new List<string>();
        public static IReadOnlyList<string> Log => log;

        /// <summary>Records a developer action (shown in the panel's Log tab and the game log) and marks the game as DEV.</summary>
        public static void Did(string what, HookController hook = null)
        {
            Used = true;
            log.Add($"{Time.realtimeSinceStartup:0}s  {what}");
            if (log.Count > 200) log.RemoveAt(0);
            hook?.Log($"[DEV] {what}");
        }
    }
}
