# BrineBattle Hunter — Roadmap

*Living document. Update it whenever a task is finished, a phase changes, or a new idea comes up.*

## ▶ Where we are

| | |
|---|---|
| **Current phase** | Phase 1 — Data foundation |
| **Next step** | Ed: open `Assets/_Project/Data/Monsters/BrinefangEel` in the Inspector, decide the type chart (COMPARISON.md), then merge `phase/1-data` |
| **Unity version** | 6000.3.11f1 (URP 17.3, template Universal 3D) |
| **Project folder** | `C:\Users\USER\brinebattle-hunter` |
| **Last updated** | 2026-10-28 |

> Start every session by reading this box. Finish every session by updating it.

## Rules

- One phase at a time. Each phase is its own branch (`phase/<n>-<name>`) and PR into `main`.
- A phase is **done** only when its *Done when* line is true in the Unity Editor (playable / testable), not just when code exists.
- Placeholder art (primitives, flat colours) until Phase 5. Blender pipeline and toon shader come after the vertical slice.
- New ideas go to `docs/ideas/` first (owner's and Claude's in separate files). They move into a phase only when we decide to build them; then `GAME_DESIGN.md` is updated too.
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

### ⬜ Phase 2 — Hook & encounter
- [ ] Drift spawner (objects float toward the raft)
- [ ] Hook action (aim, cast, reel)
- [ ] State roll: Dead / Weakened / Alive, with unreliable hints
- [ ] Raft-size gate: cut line / flee / sedate when too small

**Done when:** you can hook a drift and see the correct outcome for each state.

### ⬜ Phase 3 — Turn-based battle
- [ ] Battle scene + turn order
- [ ] 1v1 first; sea vs sky attackers
- [ ] Battle modes Manual / Semi-auto / Auto (Settings + switchable mid-battle)
- [ ] Raft parts take damage
- [ ] Decide the action resource (stamina vs weapon charge) — see Open Questions in GDD

**Done when:** a full battle can be won or lost in all three modes.

### ⬜ Phase 4 — Loot & salvage
- [ ] Harvest on Dead / victory
- [ ] Inventory with Type + Rarity
- [ ] Compatibility check → salvage to Monster Essence

**Done when:** a kill produces loot, and incompatible loot becomes essence.

### ⬜ Phase 5 — Raft builder (vertical slice)
- [ ] Grid-snapped modular parts
- [ ] Raft size/strength stats feeding the Phase 2 gate
- [ ] Upgrade with parts + essence

**Done when:** the loop hook → battle → salvage → upgrade works end to end. **Vertical slice complete.**

### ⬜ Phase 6 — Alchemy & gear
Craft, refine (+), gear rarity skills, consumables.

### ⬜ Phase 7 — Companions
Taming Weakened monsters, habitats, feeding/infusion, fusion in the Transmutation Circle.

### ⬜ Phase 8 — Raft types & absorption
Fortress / Sky / Beast-Driven, hybrids, monsters eating raft parts and recovery.

### ⬜ Phase 9 — Art pass
Blender pipeline (chibi hunter, raft part, monster), `_col` collision meshes, toon shader + inverted-hull outline.

### 🟨 Phase 10 — Other hunters & party
Trade, tied rafts, RO-style EXP/item share. Starts with a multiplayer feasibility study.

## Ideas

Unscheduled ideas live in `docs/ideas/`: owner's in [USER_IDEAS.md](ideas/USER_IDEAS.md), Claude's in [CLAUDE_IDEAS.md](ideas/CLAUDE_IDEAS.md), compared and decided in [COMPARISON.md](ideas/COMPARISON.md). Only accepted ideas enter a phase above.
