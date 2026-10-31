# Ideas — Claude (suggestions)

*Claude's suggestions, kept separate from the owner's ideas in [USER_IDEAS.md](USER_IDEAS.md). Nothing here is in the design until it is accepted in [COMPARISON.md](COMPARISON.md).*

Format: `### C-<n> Title` · date · status (💡 open / ✅ accepted / ❌ dropped / 🔀 merged) · related GDD section · cost (S/M/L) · earliest phase.

### C-1 Drift "reading" as a skill check — 2026-10-28 · ✅ Idea Round 3 · §4 · S · Phase 2
Alchemy Mastery raises the accuracy of the Dead/Weakened/Alive hint (e.g. 50% → 90%). Gives Mastery a use from the very first loop, and makes "hook the unknown" a choice instead of pure luck.

### C-2 Line tension mini-step on reel-in — 2026-10-28 · ✅ Idea Round 1 · §4 · S · Phase 2
Reeling an Alive creature is one short timing input; success = you act first in battle, fail = it does. Keeps the encounter launch from feeling random without adding real-time combat.

### C-3 Weather and currents decide what drifts — 2026-10-28 · ✅ Idea Round 4 · §2, §4 · M · Phase 2–5
Storm = bigger/Alive drifts, calm = more Dead loot, fog = hints unreliable. One system that gives the endless ocean variety and a reason to wait or move.

### C-4 Action resource = weapon durability + stamina (answer to U-7) — 2026-10-28 · ✅ Idea Round 2 · §5 · S · Phase 3
Stamina per turn for basic actions, weapon durability for skills. Both are fed by alchemy (potions refill stamina, oils repair weapons), which ties battle back to the core pillar.

### C-5 Fusion keeps one "echo" (answer to U-8) — 2026-10-28 · ✅ Phase 7 · §10 · M · Phase 7
Sources are consumed, but the result inherits one passive from the consumed one. Makes fusion a real trade-off without feeling like pure loss.

### C-6 Bestiary / Hunter's Journal — 2026-10-28 · 🔀 merged into U-11 · §4, §11 · S · Phase 4
Each monster entry fills in as you harvest/fight it: weakness, loot table, tame chance. Cheap to build on the Phase 1 data, rewards repeat hunts.

### C-7 Absorbed parts are visible on the monster — 2026-10-28 · ✅ Phase 8 (eaten parts stick out of the gorged drift) · §6 · M · Phase 8
A monster that ate your Fortress shell shows it and gains armor; you see what you'll get back. Makes U-3 readable and personal ("that one has my mast").

### C-8 Asynchronous hunters before real multiplayer — 2026-10-28 · 💡 · §12 · M · Phase 10
Other hunters first appear as NPC/ghost rafts (trade, message-in-a-bottle) before live party play. Delivers "rare human contact" early and lets multiplayer stay a later, separate risk.

### C-9 Type chart: RO-style cycle, secondaries multiply their parts — 2026-10-28 · 💡 · §8 · S · Phase 1
Main cycle Water > Fire > Earth > Wind > Water: strong ×1.5, weak ×0.75, otherwise ×1. A secondary element counts as both of its parts and the multipliers are multiplied (Steam vs Earth = 1 × 1.5 = 1.5), clamped to 0.5–2. None is neutral. Implemented in Phase 1 as a **tunable default** (`ElementChart` asset), so changing the cycle or numbers later costs nothing.

### C-10 Habitats are where pets heal — 2026-10-30 · ✅ Phase 8 · §10 · S · Phase 8
With pets on health instead of age (U-32), habitats do more than add room: each Tide Pool or Sky Perch on the raft makes pets heal 50% faster. Gives a reason to build habitats even with free room.

### C-11 The drift says what ailment it carries — 2026-10-30 · ✅ Phase 8 · §4, §5 · S · Phase 8
Next to the Dead/Weakened/Alive hint, a drift shows the ailment its hits give (☠ Poison, ♦ Bleed, ◉ Blind), so the player can make or bring the right cure (U-33) before hooking it.

### C-12 Weather forecast — 2026-10-30 · ✅ Idea Round 4 · §2 · S · Idea Round 4
The HUD shows the next weather and when it comes ("Storm in 40 s"), and storms only grow out of rain or a rough sea. Weather becomes something to plan for (heal, get a Crew pet, reel in fast) instead of a random punishment.

### C-13 Wreck field after a storm — 2026-10-30 · ✅ Idea Round 4 · §2, §11 · S · Idea Round 4
When a storm clears, the next 3 drifts are floating loot one rarity higher. A reward for riding out the storm, and an answer to U-15's "do storms bring rarer drifts?".

### C-14 Reel "fight moves" by monster kind — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §4 · S · after U-42
Each element moves differently on the reel bar: eels zig-zag, crabs sit still then jump, gulls dart up, heavy monsters sink. Dead drifts barely move, alive ones thrash. The reel becomes a second hint ("it's fighting hard, careful") without new UI.

### C-15 Sea journal of "firsts" with small rewards — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §11 · S · Phase 9
First clean reel, first boss, first storm survived, first vehicle part, first tame of each element: each gives a one-time reward (essence, a recipe hint, a cosmetic for the raft). Gives new players a trail of small goals beside the quest tab.

### C-16 Minion moods and favourite food — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §10 · S · Idea Round 5
Each minion has a favourite loot type (rolled when tamed). Feeding it that gives double EXP and a happy hop; ignoring it for long makes it sulk (works 20% slower). Makes the Glutton and the junk pile a small choice, and minions feel like individuals.

### C-17 Distress flares and rescue drifts — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §2, §4 · M · Idea Round 5
Now and then a flare goes up far away: a shipwrecked survivor on a plank drifts by. Hook them before the current takes them and they trade one rare recipe or an item for supplies. A breather from monsters and a reason to scan the horizon.

### C-18 Raft crafting combos ("set bonuses") — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §6 · S · Idea Round 5
Three of the same module in a row, or a full ring of one raft kind around a habitat, gives a small bonus (+range, +pet heal). Rewards planning the layout instead of filling cells anywhere.

### C-19 Night lanterns and bioluminescent drifts — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §2 · S · Phase 9
At night, a craftable lantern on the raft pushes sight back out a few metres; some Moon creatures glow, so they can be read through the dark. Makes night look great in the art pass and gives a reason to craft light.

### C-20 Audio that follows the world — 2026-10-31 · 🟨 Phase 9 (Ed\'s call) · U-20 · M · Phase 9
Music layers instead of tracks: a calm ocean bed by day, softer at night, drums added when an alive drift is near, full battle theme in fights, a big brass layer for bosses, rain and thunder layers by weather. One small set of loops covers every state.

### C-21 Juice for the reel and the loot — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §4 · S · Phase 9
Line creak that rises with the catch meter, a splash and a "pop" when it lands, loot icons that fly into the Bag button, a soft screen shake on a clean reel. Cheap, and it is most of what makes catching feel good.

### C-22 Daily current (seeded sea of the day) — 2026-10-31 · 💡 Phase 10 · §2 · S · Phase 10
One sea a day with a fixed seed: same weather, same drifts for everyone. A score for "most loot by sunset" that can later feed the asynchronous hunters (C-8) before real multiplayer.

### C-23 Danger forecast on the minimap edge — 2026-10-31 · ✅ Idea Round 4 (Ed: "i-apply na natin ngayon") · §4 · S · Idea Round 5
Small arrows at the screen edge for things coming from off-screen: a boss, a gorged monster, a lost raft part floating away. Helps you not miss what matters, and the lost-part arrow fits Ed's Reset raft chase.
