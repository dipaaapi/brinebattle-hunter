# BrineBattle Hunter — Roadmap

*Living document. Update it whenever a task is finished, a phase changes, or a new idea comes up.*

## ▶ Where we are

| | |
|---|---|
| **Current phase** | Phase 9 — Art pass ⬜ (Idea Round 4 merged into `main`, 2026-10-31) |
| **Next step** | Start Phase 9 on `phase/9-art`: Blender pipeline and a first model (pirate ship suggested), toon shader + outline, then BGM/SFX (U-20, C-20 layered audio, C-21 sounds). Open: module battles eat parts? (default: no) |
| **Unity version** | 6000.3.11f1 (URP 17.3, template Universal 3D) |
| **Project folder** | `D:\Repository\brinebattle-hunter` (office desktop); `C:\Users\USER\brinebattle-hunter` (laptop) |
| **Last updated** | 2026-10-31 |

> Start every session by reading this box. Finish every session by updating it.

## Rules

- One phase at a time. Each phase is its own branch (`phase/<n>-<name>`) and PR into `main`.
- A phase is **done** only when its *Done when* line is true in the Unity Editor (playable / testable), not just when code exists.
- Placeholder art (primitives, flat colours) until Phase 5. Blender pipeline and toon shader come after the vertical slice.
- New ideas go to `docs/ideas/` first (owner's and Claude's in separate files). They move into a phase only when scheduled; then `GAME_DESIGN.md` is updated too.
- **Idea Rounds:** after each core phase, add one Idea Round with exactly **two** ideas: the top-priority open owner idea (U-n) and the top-priority open Claude idea (C-n). Priority = fits the next core phase, cheapest first, unblocks other ideas. Claude picks and logs the reason in `COMPARISON.md`; the owner can swap either before the round starts.
- Every finished task gets a line in [DEVLOG.md](DEVLOG.md).

## Phases

Status: ⬜ not started · 🟨 in progress · ✅ done

### ✅ Phase 0 — Setup
- [x] Unity project created (URP template)
- [x] Repo connected; `.gitignore` + `.gitattributes` (Git LFS for `.blend`, `.fbx`, `.png`, `.psd`, audio)
- [x] Folder layout: `Assets/_Project/{Scripts,Data,Art,Prefabs,Scenes,Tests}`
- [x] Assembly definitions (`BrineBattle.Runtime`, `BrineBattle.Editor`, `BrineBattle.Tests`)
- [x] Headless check: compile + EditMode tests from the command line (`unity-verify` skill)
- [x] First commit pushed to `main`

**Done when:** the project opens with no errors, `git status` is clean, and the headless check passes.

### 🟨 Phase 1 — Data foundation
- [x] `Element` (4 main + 6 secondary, combination table, type-chart multipliers)
- [x] `Rarity` (Common → Mythic) and `ItemType` (Raft, Weapon, Armor, Companion, Consumable)
- [x] `MonsterDefinition` (stats, element, habitat sea/sky, size class, loot table)
- [x] `LootTable` + `ItemDefinition`
- [x] EditMode tests for element combination, type chart and loot rolls (21)
- [x] Sample data: Brinefang Eel + loot table + 2 items (menu BrineBattle → Create Sample Data)

**Done when:** a monster and its loot can be created in the Inspector and tests pass.

### ✅ Phase 2 — Hook & encounter
- [x] Drift spawner (objects float toward the raft)
- [x] Hook action (click to cast, reel)
- [x] State roll: Dead / Weakened / Alive, with unreliable hints
- [x] Raft-size gate: cut line / flee / sedate when too small
- [x] Placeholder scene `Hook.unity` built by `HookSceneBuilder` (menu BrineBattle → Build Hook Scene), 3 sample monsters

**Done when:** you can hook a drift and see the correct outcome for each state.

### ✅ Phase 3 — Turn-based battle
- [x] Battle view (camera moves in) + turn order by speed
- [x] 1v1; sea monsters mostly hit the raft, sky monsters mostly hit the hunter
- [x] Battle modes Manual / Semi-auto / Auto (HUD setting, saved; switchable mid-battle)
- [x] Raft parts take damage (raft HP = parts × 10)
- [ ] Decide the action resource (stamina vs weapon charge) — **stamina used as placeholder**; see U-7 / C-4

**Done when:** a full battle can be won or lost in all three modes.

### ✅ Phase 4 — Loot & salvage
- [x] Harvest on Dead / victory
- [x] Inventory with Type + Rarity (bag panel, B key)
- [x] Compatibility check (item rarity ≥ target tier) → salvage to Monster Essence (rarity + element; yield 1–6); manual, all-misfits, or auto on pickup

**Done when:** a kill produces loot, and incompatible loot becomes essence.

### ✅ Idea Round 1 (after Phase 4)
- [x] **U-11 Collection book** — monsters, items, pets; silhouette until acquired (reads the Phase 4 inventory). Absorbs C-6.
- [x] **C-2 Line tension** — one timing input while reeling; success = you act first in battle, fail = monster does.

**Done when:** the book opens from the HUD with silhouettes and reveals, and reeling has a timing check that changes who acts first.

### 🟨 Phase 5 — Raft builder (vertical slice)
- [x] Grid-snapped modular parts (9x9 max, must touch the raft, can't split it; Build mode R: place / remove)
- [x] Raft size from part count (9 = size 1, +1 per 4 parts) feeds the gate; battle HP = sum of part HP
- [x] Upgrade raft tier with essence (10 × next tier, rarity ≥ current tier); higher tier = +20% part HP but parts below the tier can't be added

**Done when:** the loop hook → battle → salvage → upgrade works end to end. **Vertical slice complete.**

### ✅ Idea Round 2 (after Phase 5)
- [x] **U-13 Auto-loot raft module** — Lv 1 basic net first (touch-only, random state, durability), upgrade path per the U-13 scaling table.
- [x] **C-4 Stamina + durability** — one durability system shared by the module's hooks and the Harpoon (answers U-7; Ed can swap it).

**Done when:** a module can be built on the raft edge and loots passing drifts until its durability runs out; Harpoon uses weapon durability.

### ✅ Phase 6 — Alchemy & gear
- [x] Gear slots (weapon, armor) in the bag: equip / take off, tier check; gear power feeds hunter ATK/DEF, weapon element = hunter element
- [x] Craft gear at the workbench (Eel-Fang Spear, Gull-Bone Buckler)
- [x] Refine (+1…+10) with essence of the item's rarity or higher
- [x] Gear rarity skills: passives (Leech, Thorns, Second Wind, Bulwark) and actives (Tidal Strike, Shell Guard), slots by rarity; Shell Maul (Rare), Tidecaller Lance and Shellback Carapace (Epic)
- [x] Consumables: Brine Tonic (HP) and Tar Patch (raft) from the workbench, used in battle (takes the turn); Semi-auto/Auto use them at ≤35% HP
- [x] Elemental oils: Gale Oil (Wind), Grit Oil (Earth), Burning Pitch (Fire) turn attacks to that element for 3 rounds; Auto uses one that beats the monster
- Companion consumables moved to Phase 7 (no companions yet)
- [x] Workbench tabs per category (Gear, Potions, Oils, Raft, Dismantle)
- [x] Real **Settings menu** (Esc / HUD button): battle mode, auto-use consumables toggle, controls

**Done when:** gear crafted from monster parts can be equipped and refined and changes battle numbers, a consumable can be used, and settings live in their own menu.

### ✅ Phase 7 — Companions
- [x] Tame a weakened, calm drift (chance by rarity) → companion with 60% of its wild stats; roster with one active companion (Bag: set active / release); Collection book Pets page fills in
- [x] Active companion fights beside the hunter: attacks after the hunter each round, can be targeted (25%) and knocked out; rests to full HP between battles
- [x] Battle visuals first pass (owner idea U-19, asked for now): fighters lunge, harpoon and skills fly, hits flash and shake, damage numbers pop, KO sinks; battle panel moved bottom-right
- [x] Roles (owner idea U-23), picked after taming: Familiar (battle), Crew (raft repair in battle, mends harpoon/modules), Mystic (extra crafts, cheaper refines)
- [x] Companions visible on the raft: Mystic at a workbench auto-refines gear, Crew walks the raft, installs modules upstream (left) and builds parts downstream (right) (switchable in the Bag)
- [x] Save / Load (Settings, one JSON save file, auto-save every 5 min in Semi-auto/Auto)
- [x] Crew repairs modules at a % set per module kind (Build); Mystic restocks potions/oils and heals the hunter or Familiar each battle round
- [x] Upload a save file from anywhere (Settings)
- [x] Raft tier upgrade needs kills + monster materials; widening/upgrading needs modules on half the left edge; module catches of live aggressive monsters start an Auto battle (owner's asks)
- [x] Menu settings in the save; module live catches always start an Auto battle
- [x] Rocking raft on moving waves, katig and sail (owner idea U-24, looks only)
- [x] Quick UI cleanup (owner idea U-25): screen size setting, simpler HUD, fewer Build sliders
- [x] Habitats on the raft (each adds roster room): Tide Pool and Sky Perch
- [x] Evolution and old age (U-23): age in battles won, Young → Adult → Elder, death leaves essence + Book memorial (numbers wait for Ed)
- [x] Pets menu (P), separate from the Bag (owner's ask)
- [x] Hammer needed to build and level up (one per raft level, upgradable, crafted from loot, Mystic makes it); Replace a built tile (owner's asks)
- [x] Feeding / infusion with loot and potions (companion growth, no EXP): Pets → Feed
- [x] Fusion in the Transmutation Circle (same + same → rarity up; different → hybrid; essence catalyst; source used up, keeps an echo): Pets → Fuse
- [x] Companion consumables (moved from Phase 6): Kelp Biscuit heals or wakes the battle companion; Semi-auto/Auto use it
- [x] Looks (U-23 varieties): Plain, Scarf, Bandana, Crown; random at taming, changeable in the Pets menu
- [x] Owner's asks during part 2: crafting materials kept after a raft level-up, Reinforce recipes per level, main menu scene, raft rocks with thicker planks per level, nothing overlaps

**Done when:** a tamed monster joins battles, lives in a raft habitat, and can grow by feeding and fusion.

### ✅ Idea Round 3 (after Phase 7) — Alchemy knowledge
- [x] **U-14 Crafting knowledge** — recipes unlock by Alchemy Mastery; "???" in the Workbench and Book → Recipes
- [x] **C-1 Drift reading** — mastery raises the hint accuracy (60% → 96%)
- [x] (merged in) **U-6** essence combining from Mastery Lv 3

**Done when:** mastery grows from hunting and crafting, reveals recipes, makes hints more accurate, and essence can be combined. (Built on `phase/7-growth` at Ed's ask; merged with Phase 7 in PR #5.)

### ✅ Phase 8 — Raft types & absorption
Fortress / Sky / Beast-Driven, hybrids, monsters eating raft parts and recovery.
- [x] Raft kind per part; type from the parts (Fortress / Sky / Beast / Hybrid / Plain) with battle, HP and reach bonuses (`RaftTypeRules`)
- [x] Typed parts at every raft level: Shell Plating, Wing Sail, Beast Harness (+ recipes); parts tinted by kind; Build and HUD show the type
- [x] Absorption: a loss or a flee eats one edge part (`RaftGrid.Absorb`); the monster comes back gorged and stronger (`AbsorbRules`, `GorgedLog`, saved)
- [x] Recovery: beating a gorged monster returns the parts plus extra loot
- [x] FAQ entries; rules tests (raft types, absorption, camera)
- [x] Owner's asks: Familiar no longer glued to the hunter while hooking; camera zoom and rotate
- [x] Ed's ideas added at his ask (2026-10-30): pets have health instead of aging, ambush and outburst hazards (U-32); ailments + cure potions (U-33); raft tree by type (U-31); modules by rarity + floating loot (U-30); Familiar fetches loot (U-29); Claude's C-10 (habitats heal pets faster) and C-11 (ailment sign over drifts)
- [x] Ed's Unity test on the office desktop; Unity-generated data committed (68aac4a). Sky flight and module-battle absorption stay open questions (defaults kept)

**Done when:** building with one kind of part changes the raft's type and its bonuses show in battle, and a monster that ate a part comes back gorged and gives the part back when beaten.

### ✅ Idea Round 4 (after Phase 8) — World conditions
Ed's pick (2026-10-30): row 3 of the combined plan, as one system.
- [x] **U-10 Day/night** with Sun/Moon affinity for monsters, pets and the hunter (via gear element); sky and light follow the clock
- [x] **U-15 Rough seas and thunderstrikes**: bigger waves, faster drifts, shorter throws, smaller reel sweet spot; lightning sets raft parts on fire (click to douse, Crew helps, Fortress resists)
- [x] **C-3 Weather decides what drifts**: storms/night more alive and more floating loot, clear days more dead; Sun/Moon creatures by time
- [x] **U-21 Fog**: far drifts show "???", scene fog, hints worse
- [x] Claude's **C-12 Forecast** (next weather + countdown in the HUD) and **C-13 Wreck field** (rarer floating loot after a storm)
- [x] Saved; FAQ topic "Day & weather"; rules tests (`WorldTests`)
- [x] Ed's asks while testing: hunter faces the monster in battle; sun and moon on screen (sky strip, shadows)
- [x] Ed's U-39 **Minions** (laptop): one kind of minion, levels from eating loot, 12 skills in four branches, auto jobs with Chores switches, skill tree page, HUD minion list
- [x] Ed's U-40 **Quest tab**: right-edge tab (J), quests from game state with ✔/✘ have/need and a Go button
- [x] Ed's U-41 **grand boss fights**: warning + rise + aura, top HP bar, enrage and tidal waves, all minions help, slow-mo finish, treasure burst + BOSS DEFEATED card
- [x] Ed's ask: new HUD and UI everywhere (deep-sea theme on every menu, HUD in sections)
- [x] Ed's Unity test of U-39/40/41 (Test Runner, then Play)
- [x] Fixes from Ed's test: lost parts float after Reset raft, Looter net/hook/harpoon, incantation circle visible, horizon haze, vehicle frame after the pick
- [x] Ed's raft/workbench batch (U-34): workbench sub-tabs, accessories, Salvage Bin, planks-only walking, raft looks by type, one raft skill tree with a single type pick, Mystic circle, Crew hammering
- [x] Ed's Unity test, then merge (2026-10-31, 281/281 tests)
- [x] Also in this round: U-42 reel minigame, smaller boss, battles follow the Settings mode, Claude's C-14…C-19, C-21, C-23, refreshed Build/Book/Bag, knocked-out countdown

**Done when:** the day turns to night and back, weather changes with a forecast, storms strike the raft, fog hides far drifts, and sun/moon creatures are stronger at their time.

### ⬜ Phase 9 — Art pass
Blender pipeline (chibi hunter, raft part, monster), `_col` collision meshes, toon shader + inverted-hull outline.

### 🟨 Phase 10 — Other hunters & party
Trade, tied rafts, RO-style EXP/item share. Starts with a multiplayer feasibility study.

## Ideas

Unscheduled ideas live in `docs/ideas/`: owner's in [USER_IDEAS.md](ideas/USER_IDEAS.md), Claude's in [CLAUDE_IDEAS.md](ideas/CLAUDE_IDEAS.md), compared and decided in [COMPARISON.md](ideas/COMPARISON.md). Only accepted ideas enter a phase above.
