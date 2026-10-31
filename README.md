# BrineBattle Hunter

> A top-down, turn-based **survival-builder × alchemy** monster-hunting game set on an endless ocean.

The world has drowned. The last few humans alive are **Monster Hunters**, each surviving alone on a raft built from the bodies of slain sea monsters. You hook whatever the current carries toward you — but not everything that floats is dead.

**Status:** Concept / pre-production (initial patch)
**Engine:** Unity · **Art:** Chibi 3D with outlines (Blender) · **View:** Top-down 3D camera (zoom and rotate)

---

## Core Pillars

1. **Hook the unknown.** Every drifting monster is a roll: dead, weakened, or *still alive*.
2. **Build from what you kill.** Bones, scales, shells and hides become your raft, gear and potions.
3. **Alchemy is power.** Hunters have no magic. Strength comes from alchemy-forged weapons, armor, potions and companions.
4. **Rare human contact.** Other hunters are few. Meeting one means trade, a party, or both.

## Core Loop

```
Hook drift → Check state (dead / weakened / alive)
   → Harvest loot  OR  Tame  OR  Turn-based battle
   → Alchemy: craft, refine, fuse, salvage
   → Upgrade raft, gear, companions
   → Meet hunters: trade / party
   → Hook bigger monsters → repeat
```

## Key Systems (summary)

| System | Summary |
|---|---|
| **Encounters** | No waves. Battles start only when a hooked creature turns out alive (Pokémon-style sudden launch). 1v1, 1vMany, or ManyvMany. |
| **Battle** | Turn-based. Player stays on the raft; monsters attack from sea or sky. Modes: Manual / Semi-auto / Auto, set in Settings and changeable anytime. |
| **Raft** | Vessel type depends on parts: Fortress, Sky, Beast-Driven. Raft size gates what you can fight. Monsters can absorb raft parts (downgrade). |
| **Player** | No job/class, no magic. Level (EXP) + Alchemy Mastery. Power comes from gear. |
| **Elements** | Water, Fire, Earth, Wind + secondary combinations (Steam, Mud, Ice, Magma, Lightning, Sand). |
| **Gear** | Weapons/armor made from monster parts; rarity decides active/passive skills. |
| **Companions** | Tamed monsters with rarity. Grown via loot and **fusion** (alchemy), not EXP. |
| **Loot** | Every item has **Type** + **Rarity**. Incompatible items are salvaged into **Monster Essence**. |
| **Party** | Ragnarok Online-style EXP and item share. Rafts are tied together; each player sees their own raft leading. |

Full details: **[docs/GAME_DESIGN.md](docs/GAME_DESIGN.md)**

## Tech & Art Direction

- **Engine:** Unity
- **Style:** Chibi, cute, outlined characters (toon shader, inverted-hull outline)
- **Pipeline:** 3D models made in Blender, shown in real 3D from a top-down camera you can zoom and turn
- **Collision:** simplified collision meshes authored in Blender (e.g. `_col` suffix) for easy iteration
- **Raft parts:** modular, grid-snapped pieces

## Repository Layout

```
/docs
  GAME_DESIGN.md   Game design document (living)
  ROADMAP.md       Phases and current status
  DEVLOG.md        Dated change log
  TOOLING.md       Skills, MCP servers, artifacts
  ideas/           Owner ideas, Claude ideas, comparison
/.claude/skills    Project skills for Claude Code
CLAUDE.md          Working rules for Claude Code
README.md
```

Unity project folders (`Assets/`, `Packages/`, `ProjectSettings/`) are added in Phase 0.

## Roadmap

Development runs in phases; the current phase and next step are always at the top of **[docs/ROADMAP.md](docs/ROADMAP.md)**.

| Phase | Goal |
|---|---|
| 0 | Setup (Unity URP project, repo, headless checks) |
| 1 | Data foundation (elements, rarity, monsters, loot) |
| 2 | Hook & encounter |
| 3 | Turn-based battle |
| 4 | Loot & salvage |
| 5 | Raft builder → **vertical slice** |
| 6–10 | Alchemy & gear, companions, raft types & absorption, art pass, party |

Change history: [docs/DEVLOG.md](docs/DEVLOG.md) · Tooling: [docs/TOOLING.md](docs/TOOLING.md)
