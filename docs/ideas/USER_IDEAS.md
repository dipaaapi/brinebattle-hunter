# Ideas — Ed (owner)

*The owner's ideas and suggestions. Compare against [CLAUDE_IDEAS.md](CLAUDE_IDEAS.md) in [COMPARISON.md](COMPARISON.md); accepted ideas move into `ROADMAP.md` and `GAME_DESIGN.md`.*

Format: `### U-<n> Title` · date · status (💡 open / ✅ accepted / ❌ dropped / 🔀 merged) · related GDD section.

## Already in the design (v0.1)
These are Ed's original pillars; listed so they can be compared with alternatives.

### U-1 Hook the unknown — 2026-10-28 · ✅ · GDD §4
Every drift is Dead / Weakened / Alive; hints are unreliable.

### U-2 Raft from monster parts, three vessel types — 2026-10-28 · ✅ · GDD §6
Fortress / Sky / Beast-Driven, hybrids; raft size gates fights.

### U-3 Absorption / downgrade — 2026-10-28 · ✅ · GDD §6
Monsters eat raft parts when you lose or flee; beat them later to recover parts + bonus.

### U-4 Companion growth by fusion, not EXP — 2026-10-28 · ✅ · GDD §10

### U-5 RO-style party share with tied rafts — 2026-10-28 · ✅ · GDD §12

## Open (proposed in the GDD, not yet decided)

### U-6 Combine low-rarity essence into higher-rarity essence — 2026-10-28 · 💡 · GDD §11

### U-7 Battle action resource: stamina vs weapon charge — 2026-10-28 · 💡 · GDD §5, §14

### U-8 Are fused companions consumed? — 2026-10-28 · 💡 · GDD §10, §14

## New ideas

### U-9 Harvest mode toggle: Auto / Manual — 2026-10-28 · 🟨 partly in (Auto reel = 50% clean) · GDD §4, §11
A toggle when harvesting a monster.
- **Auto:** 50% chance to catch / get the harvest.
- **Manual:** the player does it themselves and gets it, but with a 30% chance to miss (so 70% success).

Open: what Manual asks of the player (timing input? see C-2), and what a miss costs (lose the loot, or just less?).

### U-10 Day/night cycle (sun & moon) that boosts or weakens — 2026-10-28 · 💡 · new system (GDD §4, §5, §10, §12)
Add a day/night cycle as its own system. Some monsters, dead or alive, are tied to the sun or the moon, and the time of day boosts (or weakens) them. The same system also affects the hunter, the party, and companions/pets.

Open: which stats it changes (attack, catch/tame chance, hint accuracy, drift spawns?), whether gear or alchemy can grant sun/moon affinity, and how long a full day lasts.

### U-11 Collection book (Pokédex-style) — 2026-10-28 · ✅ Idea Round 1 · GDD §4, §10, §11
A collection book with pages for **monsters, items, and pets/companions**. Entries you haven't acquired show as a **silhouette**; once acquired, the full entry becomes visible.

Related: C-6 (Hunter's Journal) is similar but only for monsters; U-11 covers all three and adds the silhouette reveal.

### U-12 Skill slots limited by rarity (weapons, gear, accessories) — 2026-10-28 · 💡 · GDD §9
The number of skill slots (passive and active) on weapons, gear **and accessories** depends on rarity. Example: a **Common** weapon literally can't hold any passive or active skill; rarer items get more slots.

Builds on the GDD §9 rarity table (Common = element only, Rare = 1 passive, Epic = 1 active + 1 passive, Legendary/Mythic = multiple slots). New here: **accessories** as a gear type, and slots as a hard limit.
Open: exact slot count per rarity, and whether slots can be added later (refine +, alchemy).

### U-13 Auto-loot raft module — 2026-10-28 · 💡 · GDD §3, §6
A raft part built on the **edge of the raft** that automatically loots monsters floating past (no manual hook).

**Upgrades (added 2026-10-28):**
- The module is **upgradable**, and each level/version has different capabilities.
- Some versions have their own **hooks**, so the raft harvests by itself, and some hook **multiple** drifts at once.
- Some versions act as a **weapon**: a drift that looked dead but is actually alive gets **killed automatically** by the module.
- **Durability limit:** the hooks wear out and break after use, so auto-loot is never unlimited. It must not replace hunting, which would hurt the game.

**Owner answers (2026-10-28):**
- **Basic version:** takes whatever comes, any state (random), but only what drifts close enough to touch it, since it has no hook, and only a little at a time.
- **Range, speed, capacity:** grow with upgrades. Claude sets the scaling.
- **Repair:** essence or materials depending on the module's level and rarity. Claude sets the scaling.

**Scaling (proposed by Claude at the owner's request; numbers tunable):**

| Lv | Version | Hooks | Range | Every | Durability | Alive drifts |
|---|---|---|---|---|---|---|
| 1 | Basic net (no hook) | 0 (touch only) | 1.5 m | 20 s | 30 | Bites the module (−5 durability) and swims off |
| 2 | Hook arm | 1 | 4 m | 12 s | 40 | Breaks free (−5) |
| 3 | Twin hooks | 2 | 6 m | 10 s | 50 | Breaks free (−5) |
| 4 | Harpoon arm (weapon) | 2 | 8 m | 8 s | 60 | Killed if size ≤ raft size (−3 per kill), else breaks free |
| 5 | Multi-harpoon | 3 | 10 m | 6 s | 80 | Same as Lv 4 |

- Each loot uses 1 durability. At 0 the module stops until repaired.
- **Rarity** of the module multiplies durability: Common ×1, +20% per rarity step (Mythic ×2).
- **Repair** (per 10 durability): Lv 1–2 = materials only; Lv 3 = materials + essence of the module's rarity; Lv 4–5 = essence only. Cost = level × (rarity + 1). Same-element essence repairs 25% more.
- Fits Phase 5 (raft builder) as a buildable part; durability shares a system with C-4 weapon durability.

### U-14 Crafting knowledge unlocks the book — 2026-10-28 · 💡 · GDD §7, §9, §11, U-11
The book should also show **how to craft** things, but you only know a recipe once you have the **skill/knowledge** for it. That is the point of harvesting: harvesting builds your skills, skills let you craft, and crafting unlocks more.
- Right now the book only lists what you've looted.
- As skills grow, **new "???" entries appear** (craftable items, gear, potions, raft parts) that you can now learn to make.
- Each revealed craftable shows its recipe (ingredients + required skill level).

Open: is the skill the same as Alchemy Mastery (GDD §7) or separate per craft (smithing, raft-building, potions)? Do harvests of a monster type give knowledge of that monster's recipes?

### U-15 Rough seas and thunderstrikes — 2026-10-28 · 💡 · GDD §2, §4, §6
The ocean isn't always calm. Sometimes the sea gets rough, which makes hunting and looting harder. Sometimes the enemy is the weather itself: **thunderstrikes** hit the raft, damaging it or stopping the hunter from looting further until it's dealt with.

Related: C-3 (weather and currents decide what drifts) and U-10 (day/night). These three could become one **World conditions** system.
Open: how rough seas hurt hunting (smaller sweet spot, shorter hook range, drifts move faster?), whether a lightning rod / Fortress parts protect the raft, and whether storms also bring rarer drifts as a reward.

