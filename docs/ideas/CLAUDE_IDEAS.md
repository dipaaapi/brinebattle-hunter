# Ideas — Claude (suggestions)

*Claude's suggestions, kept separate from the owner's ideas in [USER_IDEAS.md](USER_IDEAS.md). Nothing here is in the design until it is accepted in [COMPARISON.md](COMPARISON.md).*

Format: `### C-<n> Title` · date · status (💡 open / ✅ accepted / ❌ dropped / 🔀 merged) · related GDD section · cost (S/M/L) · earliest phase.

### C-1 Drift "reading" as a skill check — 2026-10-28 · ✅ Idea Round 3 · §4 · S · Phase 2
Alchemy Mastery raises the accuracy of the Dead/Weakened/Alive hint (e.g. 50% → 90%). Gives Mastery a use from the very first loop, and makes "hook the unknown" a choice instead of pure luck.

### C-2 Line tension mini-step on reel-in — 2026-10-28 · ✅ Idea Round 1 · §4 · S · Phase 2
Reeling an Alive creature is one short timing input; success = you act first in battle, fail = it does. Keeps the encounter launch from feeling random without adding real-time combat.

### C-3 Weather and currents decide what drifts — 2026-10-28 · 💡 · §2, §4 · M · Phase 2–5
Storm = bigger/Alive drifts, calm = more Dead loot, fog = hints unreliable. One system that gives the endless ocean variety and a reason to wait or move.

### C-4 Action resource = weapon durability + stamina (answer to U-7) — 2026-10-28 · ✅ Idea Round 2 · §5 · S · Phase 3
Stamina per turn for basic actions, weapon durability for skills. Both are fed by alchemy (potions refill stamina, oils repair weapons), which ties battle back to the core pillar.

### C-5 Fusion keeps one "echo" (answer to U-8) — 2026-10-28 · ✅ Phase 7 · §10 · M · Phase 7
Sources are consumed, but the result inherits one passive from the consumed one. Makes fusion a real trade-off without feeling like pure loss.

### C-6 Bestiary / Hunter's Journal — 2026-10-28 · 🔀 merged into U-11 · §4, §11 · S · Phase 4
Each monster entry fills in as you harvest/fight it: weakness, loot table, tame chance. Cheap to build on the Phase 1 data, rewards repeat hunts.

### C-7 Absorbed parts are visible on the monster — 2026-10-28 · 🟨 built in Phase 8 (eaten parts stick out of the gorged drift) · §6 · M · Phase 8
A monster that ate your Fortress shell shows it and gains armor; you see what you'll get back. Makes U-3 readable and personal ("that one has my mast").

### C-8 Asynchronous hunters before real multiplayer — 2026-10-28 · 💡 · §12 · M · Phase 10
Other hunters first appear as NPC/ghost rafts (trade, message-in-a-bottle) before live party play. Delivers "rare human contact" early and lets multiplayer stay a later, separate risk.

### C-9 Type chart: RO-style cycle, secondaries multiply their parts — 2026-10-28 · 💡 · §8 · S · Phase 1
Main cycle Water > Fire > Earth > Wind > Water: strong ×1.5, weak ×0.75, otherwise ×1. A secondary element counts as both of its parts and the multipliers are multiplied (Steam vs Earth = 1 × 1.5 = 1.5), clamped to 0.5–2. None is neutral. Implemented in Phase 1 as a **tunable default** (`ElementChart` asset), so changing the cycle or numbers later costs nothing.
