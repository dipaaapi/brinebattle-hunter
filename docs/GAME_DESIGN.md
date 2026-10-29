# BrineBattle Hunter — Game Design Document

*Living document · v0.6*

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
  - Set in **Settings** (Esc); applied automatically when battle starts. Settings also has a toggle for auto-use of consumables in Semi-auto and Auto.
  - Can be changed anytime, including mid-battle.
- Resource for actions: *TBD* (stamina only, or weapon charge/durability — no mana, since hunters have no magic).

## 6. Raft (Builder)

- Built from monster parts. The parts decide the vessel type:
  - **Fortress Raft:** shells and scales; slow but sturdy.
  - **Sky Raft:** wings and gas bladders; can fly.
  - **Beast-Driven Raft:** powered by reanimated/tamed monsters; fast.
  - Hybrids possible, with trade-offs.
- **Grid (Phase 5):** parts snap to a grid (max 9x9), must touch the raft and can't split it. Raft size = 1 at 9 parts, +1 per 4 more parts; size gates fights. Battle HP = sum of part HP (10 × (rarity + 1), +20% per raft tier).
- **Raft tier:** a higher tier gives more HP, but parts with rarity below the tier can no longer be added. Raising it one step needs (owner's call, tunable): **kills** since the last upgrade (5 × (tier + 1): 5, 10, 15 …; battle wins and finishing weakened monsters count), **monster materials** spent (5 × (tier + 1), any rarity), Monster Essence spent (10 × (tier + 1), rarity ≥ current tier), and the module rule below. Kills are kept in the save.
- **Looks (owner's ask, U-24):** the raft rides a moving swell (rises, falls and leans with the water under it); drifts float on the same waves. Katig (outrigger floats on bamboo arms) along both long sides and a small sail at the right edge, turned to the current: decoration only, no gameplay.
- **Modules before growth (owner's call):** the raft can only be **widened** (raft parts) or **upgraded** while at least **50%** of its rows end on the left (upstream) edge in an auto-loot module (3 rows → 2 modules). Modules themselves can always be installed.
- **Upgrade guide (owner's ask):** Build has a guide that lists each tier requirement as done / to do, says how to get it, offers a shortcut button, and highlights in yellow the left-edge spots that still need a module.
- **Module catches a live monster (owner's call):** it can't swim off; a battle starts at once and plays in **Auto** (mode can't be switched), the monster striking first. It comes first: if the hunter is casting or reeling, that line is dropped. Only during another battle or a prompt does the module hold it on the line, and its battle starts right after. This ignores the raft-size gate: even a monster too big for the raft is fought (owner's call).
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
- **Implemented skills (Phase 6, tunable):** slots by rarity — Common/Uncommon element only, Rare 1 passive, Epic 1 passive + 1 active, Legendary 2 + 2 (secondary element still to come), Mythic 3 + 2. Passives: Leech, Thorns, Second Wind, Bulwark. Actives (cost stamina, take the turn): Tidal Strike, Shell Guard. The same passive on weapon and armor doesn't stack.
- **Implemented (Phase 6, tunable):** one weapon and one armor slot. Gear has a power value (weapon = attack, armor = defense) added to the hunter; the weapon's element is the hunter's attack element. Equipping needs rarity ≥ the slot's tier. Refine +1…+10: each + adds ¼ of base power (min 1) and costs 2 × the new level in essence of the item's rarity or higher; no failure yet (Alchemy Mastery later). Gear is crafted at the workbench from monster parts.

## 10. Companions

- Tamed from **weakened** monsters.
- **Three roles (owner idea U-23):** **Crew** looks after the raft (repairs, upkeep); **Mystic** helps craft and do alchemy for everything but the raft (gear, weapons, consumables); **Familiar** fights beside the hunter. Varieties within a role change only looks and outfit.
- **Evolve** to serve their role better, and **die of old age** (lifespan).
- **Rarity:** Common → Uncommon → Rare → Epic → Legendary → Mythic.
- **No EXP.** Grown by:
  - **Feeding / Infusion** with loot and potions.
  - **Fusion** in the Transmutation Circle:
    - Same + Same → rarity upgrade.
    - Different + Different → hybrid.
    - Catalysts influence the result; failure has risk.
- Need habitats on the raft; can power Beast-Driven / Sky rafts.
- *Open question:* are fused companions consumed?

**Implemented (Phase 7 slice 1, tunable):** Tame appears on a weakened, calm drift. Chance by rarity: Common 90%, Uncommon 75%, Rare 55%, Epic 40%, Legendary 25%, Mythic 15%; a failed tame lets it slip away. A companion has 60% of the monster's HP/ATK/DEF (speed kept). The raft keeps 1 companion, +1 per habitat (habitats next). One is active: it attacks after the hunter every round, the monster goes for it 25% of the time, and a knocked-out companion sits out the rest of the battle. It rests to full HP between battles. Release from the Bag (asks twice).

**Roles (Phase 7 slice 2, tunable):** the player picks the role right after a successful tame (any monster can take any role). Room for 3 companions before habitats. **Familiar:** only Familiars can be the battle companion (one active). **Crew:** patches the raft at the end of each battle round (Common 2 HP, +2 per rarity step) and, between fights, mends +1 durability on the harpoon and every module every 15 s. **Mystic:** gear, potion and oil crafts have a chance to make one extra (Common 20%, +5% per rarity step), and refines cost 1 less essence (min 1). With several of a role, the rarest one's bonus counts.

**On the raft (Ed's ask):** companions are visible on the raft. The Familiar waits beside the hunter; the Mystic works at a workbench and, every 20 s, refines the equipped piece with the lower + if the bag has the essence; the Crew walks from tile to tile (never off the raft) and, every 20 s, installs an auto-loot module from the bag on the upstream side (left, where drifts come from), or else builds the rarest fitting raft part on the downstream side (right) (owner's call). Both chores can be switched off in the Bag.

**More chores (Ed's ask):** the Crew also **repairs auto-loot modules** once one drops to a share set **per module kind** in Build (R) (slider, default 50%), paying the module's normal repair cost; between fights it mends the harpoon +1 every 15 s. The Mystic **restocks consumables first**: when a potion or oil the workbench can make is below the "keep at least" amount (Bag slider, default 2) it crafts one (potions before oils), otherwise it refines gear. In battle the Mystic **supports**: at the end of each round it heals whichever of the hunter or the Familiar is lower on HP (Common 3, +2 per rarity step).

### Saving

Save/Load in the Settings menu, **one save file** (`Saves/save.json` under the persistent data path; "Open folder" button). **Upload save file** makes a save from anywhere the one save file and loads it; the previous save is kept as `save.backup.json` (owner's call). In the Editor it opens a file picker; in a build, paste the file path. **Auto-save every 5 minutes** into that file once a save exists (made or loaded), only in Semi-auto and Auto, and never during a battle, menu or prompt (owner's call). A save holds the bag and essence, gear and refine levels, harpoon wear, raft parts, installed modules and their wear, tier and kills toward the next tier, companions and their roles, the collection book, and the menu settings (owner's call): battle mode and auto-use (Settings), auto-salvage, target tiers and companion chores (Bag), and the Crew repair share per module kind (Build). Saves from before settings were saved load with the current settings. Items are stored by id, so renaming an asset is safe but changing an id is not.

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

**Elemental oils (Phase 6, tunable):** Gale Oil (Wind; 2 Gale Feather + 1 Eel Scale), Grit Oil (Earth; 1 Shellback Plate + 1 Eel Scale) and Burning Pitch (Fire; 1 Eel-Skin Patch + 2 Plank). Using one takes the turn and makes the hunter's attacks that element for 3 rounds, replacing the weapon's element (defense keeps the weapon's). Can't re-apply the element already on. Auto uses an oil when it beats the current attack element against the monster; Semi-auto and Manual leave oils to the player. Companion consumables come with companions (Phase 7).

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
- Companion roles: what triggers evolution; how lifespan is counted before a day/night cycle; what happens on death.
- Level-range limits on item share?
- Target platform(s): PC, mobile, or both.
- Title trademark check.
