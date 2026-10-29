# BrineBattle Hunter — Game Design Document

*Living document · v0.5*

## 1. Overview

| | |
|---|---|
| **Genre** | Survival-builder × Alchemy, turn-based monster hunting |
| **View** | Top-down exploration/building + separate battle view |
| **Engine** | Unity |
| **Art** | Chibi 2.5D, outlined characters, Blender-made models |
| **Modes** | Single-player with optional party (other hunters) |

## 2. World & Lore

The world is an endless ocean full of monsters. Only a handful of humans remain, all of them **Monster Hunters**. Each lives on a raft built from monster remains and artifacts. Meeting another hunter is rare and valuable.

Hunters have **no magic**. Their strength comes from alchemy and gear made from monsters.

## 3. Core Loop

1. Drift brings objects near your raft: monster corpses and parts.
2. Hook them in.
3. Each hooked creature is **Dead**, **Weakened**, or **Alive**.
4. Harvest, tame, or fight.
5. Use alchemy to craft, refine, fuse, or salvage.
6. Upgrade raft, gear, and companions.
7. Meet other hunters to trade or party.
8. Take on bigger monsters.

## 4. Encounters

- **No waves.** Battles happen by chance, based on what you hook.

| State | Result |
|---|---|
| Dead | Harvest materials and ingredients. |
| Weakened | Finish for loot, or **tame** it. |
| Alive | Sudden battle launch (Pokémon-style). |

- Hints (shadows, movement, alchemy readings) may suggest the state but are not always accurate.
- **Raft size gate:** you can only fight once your raft is big and strong enough. If it isn't, cut the line, flee, or sedate the creature.
- Formats: **1 vs 1**, **1 vs Many**, **Many vs Many** (player + companions + party).
- **Line tension (C-2):** while reeling, one timing press in a sweet spot (smaller and faster for bigger monsters). No press → the catch slips away. Sloppy → normal result, and an Alive monster acts first. Clean → +1 per drop, and the hunter acts first against an Alive monster. In **Auto** mode the reel presses itself: 50% clean (from U-9), never slips; the bar is shown greyed out as AUTO. In **Semi-auto** the bar stays active and labelled SEMI-AUTO: the player may press first, otherwise it presses itself at 80% of the reel (50% clean).

## 5. Battle

- **Turn-based.**
- The player stays **on the raft**. Monsters attack from the **sea** (sea creatures) or **sky** (flying creatures).
- Raft parts can be damaged during battle.
- **Battle modes:** Manual, Semi-auto, Auto.
  - Set in **Settings**; applied automatically when battle starts.
  - Can be changed anytime, including mid-battle.
- Resource for actions: *TBD* (stamina only, or weapon charge/durability — no mana, since hunters have no magic).

## 6. Raft (Builder)

- Built from monster parts. The parts decide the vessel type:
  - **Fortress Raft:** shells and scales; slow but sturdy.
  - **Sky Raft:** wings and gas bladders; can fly.
  - **Beast-Driven Raft:** powered by reanimated/tamed monsters; fast.
  - Hybrids possible, with trade-offs.
- **Grid (Phase 5):** parts snap to a grid (max 9x9), must touch the raft and can't split it. Raft size = 1 at 9 parts, +1 per 4 more parts; size gates fights. Battle HP = sum of part HP (10 × (rarity + 1), +20% per raft tier).
- **Raft tier:** raised with Monster Essence (10 × next tier, essence rarity ≥ current tier). A higher tier gives more HP, but parts with rarity below the tier can no longer be added.
- Raft holds weapon stations, alchemy stations, Transmutation Circle, and companion habitats.
- **Absorption / downgrade:** when you lose, flee, or are exhausted, monsters eat raft parts. The monster grows stronger; defeating it later recovers your parts plus a bonus.

### Goal paths

- **The Unsinkable:** strongest raft in the world.
- **The Self-Sustained:** fully self-sufficient raft.
- **The Apex Hunter:** most powerful living hunter.

## 7. Player Progression

- **No job/class. No magic.**
- **Level (EXP):** base stats and free stat points.
- **Alchemy Mastery:** recipe unlocks, fusion success rate, taming rarity, potion and gear quality.
- **Power comes from gear.**

## 8. Elements

**Main:** Water, Fire, Earth, Wind

**Secondary (proposed):**

| Combination | Element |
|---|---|
| Water + Fire | Steam |
| Water + Earth | Mud |
| Water + Wind | Ice |
| Fire + Earth | Magma |
| Fire + Wind | Lightning |
| Earth + Wind | Sand |

Applies to gear, companions, and monsters (type chart).

**Type chart (current default, pending owner decision — see `ideas/CLAUDE_IDEAS.md` C-9):** Water > Fire > Earth > Wind > Water; strong ×1.5, weak ×0.75. Secondary elements count as both parts and multiply (clamped 0.5–2). Tunable in the `ElementChart` asset.

## 9. Gear

- Weapons and armor crafted from monster parts via alchemy.
- Rarity decides skills (proposed):

| Rarity | Skills |
|---|---|
| Common | Element only |
| Rare | 1 passive |
| Epic | 1 active + 1 passive |
| Legendary / Mythic | Multiple slots, secondary element |

- Refine (+) with Monster Essence.
- **Implemented (Phase 6, tunable):** one weapon and one armor slot. Gear has a power value (weapon = attack, armor = defense) added to the hunter; the weapon's element is the hunter's attack element. Equipping needs rarity ≥ the slot's tier. Refine +1…+10: each + adds ¼ of base power (min 1) and costs 2 × the new level in essence of the item's rarity or higher; no failure yet (Alchemy Mastery later). Gear is crafted at the workbench from monster parts.

## 10. Companions

- Tamed from **weakened** monsters.
- **Rarity:** Common → Uncommon → Rare → Epic → Legendary → Mythic.
- **No EXP.** Grown by:
  - **Feeding / Infusion** with loot and potions.
  - **Fusion** in the Transmutation Circle:
    - Same + Same → rarity upgrade.
    - Different + Different → hybrid.
    - Catalysts influence the result; failure has risk.
- Need habitats on the raft; can power Beast-Driven / Sky rafts.
- *Open question:* are fused companions consumed?

## 11. Loot & Salvage

- Every loot item has a **Type** (Raft, Weapon, Armor, Companion, Consumable) and a **Rarity**.
- **Compatibility:** an item can be used only if its rarity fits the target's tier (e.g. Uncommon raft part cannot go on an Epic raft).
- Incompatible items are **salvaged into Monster Essence** (has rarity + element), used for:
  - Crafting new items
  - Refining (+) weapons and armor
  - Secondary material for raft/gear upgrades
  - Companion growth
- Proposed: combine low-rarity essence into higher-rarity essence.
- **Implemented (Phase 4):** an equippable item fits when its rarity ≥ the target's tier. Salvage gives essence of the item's rarity and element, Common 1 … Mythic 6 per item.

### Collection book (U-11)

Pages for **Monsters**, **Items** and **Pets**. Unacquired entries are silhouettes ("???"). A monster unlocks when harvested, finished or defeated; an item when it enters the bag; a pet when tamed. Revealed monster pages show element, habitat, size, stats, drops (unknown drops stay "???") and description. Shows % completion per page.

### Consumables

Potions and items for health, companions, raft (repair, coatings), and weapons (elemental oils).

**Implemented (Phase 6, tunable):** Brine Tonic (+25 hunter HP) and Tar Patch (+30 raft HP), crafted at the workbench. Battle-only for now; using one takes the hunter's turn and can't be done at full HP. Semi-auto and Auto use them by themselves at ≤35% HP (the lower of hunter/raft first); Manual leaves it to the player.

## 12. Other Hunters & Party

- Other hunters appear rarely on the current.
- **Trade:** items, ingredients, recipes, raft parts, companions.
- **Party:** rafts are tied together. On each player's screen, **their own raft leads** and the others follow.
- Encounters by any member pull the whole party into the battle.

### Ragnarok Online-style share (leader sets)

- **EXP:** Each Take (contribution-based) or Even Share (level range required). Party bonus EXP per member.
- **Items:** Each Take, Party Share (Random), or Party Share (Need/Role).
- **Hook owner:** the player who hooked the monster gets bonus/first pick and exclusive taming rights.
- Share only applies to rafts tied to the party.

## 13. Tech & Art

- **Unity**, top-down camera; dedicated battle view.
- **Chibi** characters with **outlines** (inverted hull / URP renderer feature).
- **Blender** 3D models rendered as 2.5D.
- **Collision** meshes authored in Blender (simplified, `_col` naming) for easy iteration.
- **Modular, grid-based** raft parts.

## 14. Open Questions

- Action resource in battle (stamina vs weapon charge).
- Fusion: are source companions consumed?
- Level-range limits on item share?
- Target platform(s): PC, mobile, or both.
- Title trademark check.
