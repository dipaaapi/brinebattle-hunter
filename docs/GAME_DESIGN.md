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

### World conditions (Idea Round 4: U-10, U-15, U-21, C-3, C-12, C-13)

Numbers are tunable, in `WorldRules`; the clock is `WorldState` (saved), shown by the `WorldClock` in the Hook scene. Time stops in menus and battles.

- **Day and night (U-10):** a full day is 8 minutes: dawn (8%), day, dusk (8%), night (42%). New games start in the morning. The sky, sun and ambient light follow the clock; night keeps a dim blue moonlight.
- **Sun and Moon affinity (U-10):** taken from the element, so gear gives the hunter one too. Sun = fire, magma, sand, steam; Moon = water, ice, mud; earth, wind, lightning = neither. In their own time: +20% ATK, +10% DEF; in the opposite time: −10% ATK; dawn and dusk: nothing. Counts for monsters, pets and the hunter (and for the "Danger!" check). Sun creatures spawn ×2 by day and ×0.5 at night, Moon creatures the other way.
- **Night:** alive rolls ×1.4, hints −10%, drift names readable only within 16 m.
- **Weather (U-15, C-3):** Clear, Rain, Rough sea, Storm, Fog; each lasts 60–150 s. A storm only follows rain or a rough sea and never twice in a row; fog is likelier at night and dawn.

| Weather | Waves | Drifts | Hunting | Fighters |
|---|---|---|---|---|
| Clear | ×1 | clear days: dead rolls ×1.3 | — | — |
| Rain | ×1.15 | — | hints −5% | water +25% ATK |
| Rough sea | ×1.5 | ×1.35 speed, alive ×1.3, +5% floating loot | hook reach −2 m, reel sweet spot ×0.75 | — |
| Storm | ×1.8 | ×1.6 speed, alive ×1.6, +15% floating loot | reach −3 m, sweet spot ×0.6, hints −5%, sight 18 m, lightning | lightning +25% ATK |
| Fog | ×0.6 | — | hints −25%, sight 10 m (8 m at night) | wind +25% ATK |

- **Fog and sight (U-21):** beyond the sight range a drift shows only "???"; scene fog starts at the raft. Hints never drop below 34%.
- **Thunderstrikes (U-15):** in a storm, every 20 s a 35% chance (Fortress raft half, Sky raft ×1.5) that lightning sets an edge raft part on fire. While any fire burns the hook is jammed. Click the fire twice to put it out; a Crew pet does it by itself after 5 s; after 15 s the part burns away (never one that would split the raft). Answers U-15's open questions: Fortress parts protect, storms bring more floating loot.
- **Sun and moon on screen:** the camera looks down at the sea, so a small sky strip (top-right) shows the sun by day and a crescent moon at night moving along their arc, behind clouds in rain, storms and fog. The sun light casts soft shadows that swing round as it moves (Ed: he couldn't see a sun or moon).
- **Forecast (C-12):** the HUD shows the day, time, weather and the next weather with a countdown, so a storm is never a surprise.
- **Wreck field (C-13):** when a storm clears, the next 3 drifts are floating loot one rarity higher.

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

- Hints (shadows, movement, alchemy readings) may suggest the state but are not always accurate. Fog, rain and night make them worse (§2 World conditions).
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
- **Losing (Ed's call: rest + a little lost):** the monster swims off, the hunter rests 30 s before hooking again (module catches wait too), and the waves wash away up to 3 Common materials. Gear, pets, raft parts, tools, potions and rarer items are never lost; the next fight starts at full HP. No game over. Since Phase 8 the monster also eats one raft part (§6 Absorption).
- **Looks of a loss (Ed's ask):** the hunter lies knocked out ("z Z") while resting; the pets scurry into the storage box, which shivers and squeaks ("eep!") with the lid peeking, and they pop back out when the rest is over.
- **Ailments (Ed's U-33, tunable):** a monster's hit can leave its ailment (chance per hit): Brinefang Eel ☠ Poison (5% max HP a round, 5 rounds), Shellback Crab ♦ Bleed (10% a round, 3 rounds), Gale Gull ◉ Blind (40% miss, no gear skills, 3 rounds); outbursts can leave ? Confusion (35% chance a turn hits yourself, 3 rounds). One ailment at a time. Ailments carry over after battle and tick every 5 s outside it, but never take the last HP there. Cures: Toxic Relief, Clotting Patch, Confusion Relief, Eye Cleanser (Potions tab); Big Brine Tonic (60 HP). Semi-auto and Auto use the right cure. The sign over a drift shows which ailment it carries (Claude's C-11).
- **Health between battles (Ed's U-32):** the hunter's and pets' HP carries from fight to fight and heals slowly on the raft (hunter 1 HP / 3 s; pets 1 HP / 6 s, 50% faster per habitat, Claude's C-10). A live catch may **lash out while reeled in** (15–50% by temperament) and a beaten monster may **thrash** (10–30%, +20% if gorged), hitting the hunter or a pet for part of its attack; neither knocks the hunter out, but a pet can be knocked out (it rests until healed). Potions work outside battle too (Bag → Use, Pets → pet).
- **⚠ Danger!** shows over a drift whose monster would likely beat the hunter and the battle pet (hits needed on each side, average damage with elements).
- Resource for actions: *TBD* (stamina only, or weapon charge/durability — no mana, since hunters have no magic).

## 6. Raft (Builder)

- Built from monster parts. The parts decide the vessel type:
  - **Fortress Raft:** shells and scales; slow but sturdy.
  - **Sky Raft:** wings and gas bladders; can fly.
  - **Beast-Driven Raft:** powered by reanimated/tamed monsters; fast.
  - Hybrids possible, with trade-offs.
- **Raft types (Phase 8, tunable; Claude's numbers until Ed confirms):** every raft part has a kind (Plain, Fortress, Sky, Beast). Only raft parts count (not modules or habitats). A kind with ≥ 3 parts and ≥ 50% of the raft parts sets the type; two kinds with ≥ 3 parts and ≥ 30% each make a **Hybrid** with half of both bonuses; otherwise **Plain**. Bonuses: **Fortress** +40% raft HP, +3 DEF, −1 SPD; **Sky** +3 SPD, +4 m hook reach, −10% raft HP; **Beast** +4 ATK, +1 SPD. Parts: Shell Plating (Fortress; Eel Scale), Wing Sail (Sky; Gale Feather), Beast Harness (Beast; Eel Fang), one per raft level (Lv 1 = 1 plank + 2 material, each next level = 2 of the last + level × material). Shellback Plate and Coralbound Plank count as Fortress. Typed parts are tinted by kind; Build and the HUD show the type. For now the Sky raft doesn't really fly: its type is a speed and reach bonus (open question below).
- **Grid (Phase 5):** parts snap to a grid (max 9x9), must touch the raft and can't split it. Raft size = 1 at 9 parts, +1 per 4 more parts; size gates fights. Battle HP = sum of part HP (10 × (rarity + 1), +20% per raft tier).
- **Raft tier = raft level (owner's call):** shown as Lv 1 (Common) … Lv 6 (Mythic). A higher tier gives more HP, but raft parts with rarity below the tier can no longer be added. Auto-loot modules and habitats fit any tier (a module's strength is its level), so the left-edge rule can always be met. Raising it one step needs (owner's call, tunable): **kills** since the last upgrade (5 × (tier + 1): 5, 10, 15 …; battle wins and finishing weakened monsters count), **monster materials** spent (5 × (tier + 1), any rarity), Monster Essence spent (10 × (tier + 1), rarity ≥ current tier), and the module rule below. Kills are kept in the save.
- **Looks (owner's ask, U-24):** the raft rides a moving swell (rises, falls and leans with the water under it); the raft leans fully with the water. **Raft level = plank thickness (owner's call):** each level the deck stands higher above the water (Lv 1 0.30 m … Lv 6 0.90 m, above the tallest wave) and the raft rides a bigger share of the swell (Lv 1 half-height waves … Lv 6 the open sea), so low levels rock gently and high levels ride real waves without water over the deck; drifts float on the same waves. **Nothing overlaps (owner's ask):** drifts push apart, a reeled monster stops at the raft's edge, pets keep clear of the hunter, the box and each other, and a battle lunge stops where the bodies touch. Katig (outrigger floats on bamboo arms) along both long sides and a small sail at the right edge, turned to the current: decoration only, no gameplay.
- **Modules before growth (owner's call):** the raft can only be **widened** (raft parts) or **upgraded** while at least **50%** of its rows end on the left (upstream) edge in an auto-loot module (3 rows → 2 modules). Modules themselves can always be installed.
- **Upgrade guide (owner's ask):** Build has a guide that lists each tier requirement as done / to do, says how to get it, offers a shortcut button, and highlights in yellow the left-edge spots that still need a module.
- **Modules hook everything that touches them (Ed's ask):** any monster, any state. A drift pressed against the raft next to a module counts as touching, even in a notch between parts. A **weakened, calm** catch is brought to the hunter as a Finish / Tame choice (in Auto with no pet room it is finished for loot); if the hunter is busy the module holds it until they're free. A drift that bit a module is left alone for 20 s while it swims off, not forever.
- **Module catches a live monster (owner's call):** it can't swim off; a battle starts at once and plays in **Auto** (mode can't be switched), the monster striking first. It comes first: if the hunter is casting or reeling, that line is dropped. Only during another battle or a prompt does the module hold it on the line, and its battle starts right after. This ignores the raft-size gate: even a monster too big for the raft is fought (owner's call).
- Raft holds weapon stations, alchemy stations, Transmutation Circle, and companion habitats.
- **Absorption / downgrade:** when you lose, flee, or are exhausted, monsters eat raft parts. The monster grows stronger; defeating it later recovers your parts plus a bonus.
- **Raft tree (Ed's U-31, reworked 2026-10-30 on his ask):** the type is picked **once**, at `RaftTree.PickLevel` (Lv 2 for now; Ed wants Lv 10 once the raft has that many levels, see Open Questions). Lv 1 typed parts of every type are open before that. After the pick only that type's branch grows, one level per level-up; the other types lock for good (even Lv 1) and don't branch out. A raft already past the pick level with no type picks it in the tree without leveling up. Build → **Raft tree** is one page: level, type, requirements and the Level-up button on top, then one tab per type (the picked one opens by default, the others show 🔒). Each type's branch: one refined material per level (Fortress: Hardened Scale, Shell Weave, Coral Bulwark, Leviathan Hide, Abyssal Carapace; Sky: Gale Down, Storm Silk, Cloud Bladder, Tempest Plume, Aether Membrane; Beast: Fang Rope, Sinew Strap, Bone Yoke, Leviathan Tendon, Abyssal Chain) made from the level's base part + level × the kind's raw material, and the typed part of that level (last level's part + the material). Locked recipes say why.
- **Looks by type (Ed's ask):** Fortress (STR, tank) is a **sea train**: locomotive with a smoking stack, cowcatcher, rails and turning wheels. Sky (INT) is a **winged plane**: wings to both sides, a spinning propeller, a tail. Beast (DEX) is a **pirate ship**: hull walls with cannons, pointed bow and bowsprit, stern castle, two masts with square sails and a skull flag. Plain keeps the katig and small sail. The look follows the parts' type, or the picked type until the parts make one.
- **Walking (Ed's ask):** the hunter and pets only stand on planks (raft parts), never on auto-loot modules or habitats.
- **Crew installs take time (Ed's ask):** the Crew walks to a plank next to the spot and hammers for 4 s with a progress bar and a ghost of the part; the part is taken from the bag only when it's done (and only if the spot still fits).
- **Salvage Bin (Ed's ask):** a metal bin on its own plank next to the storage box. Salvaging (Bag, auto-salvage) and dismantling send glowing essence orbs flying from the storage box into it, with a label. Clicking it opens it: the essence inside and the dismantle recipes (no longer a Workbench tab). Tree materials aren't spent as "monster materials".
- **Modules by rarity and floating loot (Ed's U-30):** Tarred Net (U), Shell Trap (R), Coral Snare (E), Leviathan Grapple (L), Abyssal Maw (M), each upgraded from the last. Floating loot (Driftwood Bundle C, Tar Barrel U, Shell Wreck R, Coral Chunk E, Leviathan Bones L, Abyssal Driftwood M) drifts in (30% of drifts, up to one rarity above the raft) carrying raft materials; a module only catches floating loot up to its own rarity, creatures at any rarity.
- **Absorption (Phase 8, tunable):** losing a battle or pressing Flee lets the monster eat **one raft part on the edge** (not modules or habitats, never one that would split the raft, never when the raft has 5 parts or fewer). Losing a battle a module started costs no part (Claude's default; open question below). The monster swims off **gorged** and is kept in the save. While no gorged drift is on the water, each new drift has a 30% chance to be the oldest gorged monster coming back: always alive, bigger, "Gorged! Ate N raft parts" over it, and +20% HP/ATK/DEF per part (up to double). Beating it (by hand or by a module) puts every eaten part back in the Bag plus one extra loot roll per part. Cut line costs nothing.

### Goal paths

- **The Unsinkable:** strongest raft in the world.
- **The Self-Sustained:** fully self-sufficient raft.
- **The Apex Hunter:** most powerful living hunter.

## 7. Player Progression

- **No job/class. No magic.**
- **Level (EXP):** base stats and free stat points.
- **Alchemy Mastery:** recipe unlocks, fusion success rate, taming rarity, potion and gear quality.
- **Implemented (Idea Round 3: U-14 + C-1, with U-6):** mastery points from harvesting or beating a monster (2), taming (3) and crafting (1, not dismantling). Lv 1–10; each level needs 10 × level more points. Effects so far:
  - **Hint accuracy (C-1):** 60% at Lv 1, +4% per level (96% at Lv 10).
  - **Recipe knowledge (U-14):** a recipe needs mastery by its output's rarity (Common 1 … Mythic 6, one per raft level; dismantling always known). Unknown recipes show as "???" with the level that reveals them, in the Workbench and in a new **Book → Recipes** tab; a level-up names the new recipes. The Mystic only crafts known recipes.
  - **Essence combining (U-6):** from Lv 3, 5 essence of one rarity and element → 1 of the next rarity (Bag → Essence).
  - Older saves start at the mastery level of their raft, so nothing they already used is hidden. Fusion, taming and quality effects are still open.
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

- Weapons, armor and **accessories** (Ed's ask, a third slot) crafted from monster parts via alchemy. An accessory's power (+ refine) splits into ATK (larger half) and DEF; its worth is its skills by rarity and its element, which counts when the weapon is neutral (and gives sun/moon affinity). New passive **Swift** (+2 SPD). Accessories: Shell Charm (U, Earth), Gale Feather Earring (R, Wind, Swift), Moonpearl Amulet (E, Water, Second Wind + Tidal Strike), Sunstone Band (L, Fire, Leech + Swift + Shell Guard).
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

**Implemented (Phase 7 slice 1, tunable):** Tame appears on a weakened, calm drift. Chance by rarity: Common 90%, Uncommon 75%, Rare 55%, Epic 40%, Legendary 25%, Mythic 15%; a failed tame lets it slip away. A companion has 60% of the monster's HP/ATK/DEF (speed kept). The raft keeps 3 companions, +1 per habitat. One is active: it attacks after the hunter every round, the monster goes for it 25% of the time, and a knocked-out companion sits out the rest of the battle. It rests to full HP between battles. Release from the Pets menu (asks twice).

**Habitats (Phase 7, tunable):** a habitat is a raft piece crafted at the workbench (Raft tab) and placed in Build like a part; each one on the raft gives room for one more companion (3 + habitats). **Tide Pool** (4 Driftwood Plank + 3 Eel Scale) and **Sky Perch** (3 Driftwood Plank + 3 Gale Feather); both can be dismantled. Habitats fit any raft tier and don't need the left-edge modules. Taking one off doesn't release anyone, but no new companion can be tamed until there is room. *Claude's proposal; any companion can use either habitat for now.*

**Roles (Phase 7 slice 2, tunable):** the player picks the role right after a successful tame (any monster can take any role). Room for 3 companions before habitats. **Familiar:** only Familiars can be the battle companion (one active). **Crew:** patches the raft at the end of each battle round (Common 2 HP, +2 per rarity step) and, between fights, mends +1 durability on the harpoon and every module every 15 s. **Mystic:** gear, potion and oil crafts have a chance to make one extra (Common 20%, +5% per rarity step), and refines cost 1 less essence (min 1). With several of a role, the rarest one's bonus counts.

**On the raft (Ed's ask):** companions are visible on the raft. The Familiar waits beside the hunter; the Mystic works at a workbench and, every 20 s, refines the equipped piece with the lower + if the bag has the essence; the Crew walks from tile to tile (never off the raft) and, every 20 s, installs an auto-loot module from the bag on the upstream side (left, where drifts come from), or else builds the rarest fitting raft part on the downstream side (right) (owner's call). Both chores can be switched off in the Bag.

**Storage box (Ed's ask):** one object on the raft is both the loot storage and the crafting/alchemy table (no separate workbench). It has its own tile next to the raft's centre and nothing overlaps it: the hunter, pets and modules keep clear, and it rides the raft on the calm water. Whenever loot is stored or something is crafted or refined (by the hunter or a pet) it animates (lid opens, box bounces, an item flies in or pops out, a short label says what).

**More chores (Ed's ask):** the Crew also **repairs auto-loot modules** once one drops to a share set **per module kind** in Build (R) (slider, default 50%), paying the module's normal repair cost; between fights it mends the harpoon +1 every 15 s. The Mystic **restocks consumables first**: when a potion or oil the workbench can make is below the "keep at least" amount (Bag slider, default 2) it crafts one (potions before oils), otherwise it refines gear. In battle the Mystic **supports**: at the end of each round it heals whichever of the hunter or the Familiar is lower on HP (Common 3, +2 per rarity step).

**Hammer and replacing (Ed's ask):** building is done with tools, not bare hands. Placing, replacing and removing raft parts, modules and habitats, and leveling the raft up, need a **hammer of the raft's level or better** in the bag (not used up). After a level-up the hammer must be upgraded to keep building. Hammers are crafted from loot in the Raft tab: Driftwood Hammer (2 Driftwood Plank + 1 Eel Fang, Lv 1; a new game starts with one, and older saves get one), then each upgrades from the one before plus that level's raft part: Tarred (+ Tarred Plank + 1 Eel Fang), Shell (+ Shellback Plate + 2 Eel Fang), Coral (+ Coralbound Plank + 2 Eel Scale), Leviathan (+ Leviathan Beam + 3 Eel Fang), Abyssal (+ Abyssal Keel + 2 Shellback Plate). A **Mystic** makes or upgrades the hammer by itself (before potions) when the raft has outgrown it; the **Crew** only builds when the hammer fits. In Build, clicking a built tile with a part selected **replaces** it; the old part goes back to the bag.

**Reinforced raft parts (Ed's call):** a leveled-up raft only takes parts of its level or higher, so the workbench's Raft tab has a **Reinforce** recipe for each level: 2 Driftwood Plank + 1 Eel-Skin Patch → Tarred Plank (Lv 2, Uncommon); 2 Tarred Plank + 2 Eel Fang → Shellback Plate (Lv 3, Rare); 2 Shellback Plate + 2 Eel Scale → Coralbound Plank (Lv 4, Epic); 2 Coralbound Plank + 3 Eel Fang → Leviathan Beam (Lv 5, Legendary); 2 Leviathan Beam + 3 Shellback Plate → Abyssal Keel (Lv 6, Mythic). Items that any craft or reinforce recipe uses are never salvaged as misfits; the Bag marks them "crafting". Auto-salvage on pickup only ever touches **weapons and armor** below their target tier (Ed's call): raft parts, modules, materials and consumables of any rarity are always kept, so a higher raft level never stops you looting Common items.

**Old age is off (Ed's U-32, 2026-10-30):** pets no longer age, evolve by age or die of old age; they have health instead (§5 Health between battles) and grow by feeding and fusion. A **Familiar fetches** (Ed's U-29): the best Familiar that isn't knocked out swims out every 20 s (−2 s per rarity, at least 8 s) for the nearest dead drift or floating loot within 4 m of the raft's edge (Pets → Chores toggle). The old numbers below stay for older saves.

**Growth and old age (Phase 7 part 2, tunable, *Claude's proposal until Ed confirms*):** until day/night exists (U-10), a companion's **age counts battles won** (every companion on the raft gets one older for each monster beaten). Lifespan by rarity: Common 40 battles, +20 per rarity step (Mythic 140). Stages: **Young → Adult** at a quarter of its life, **Adult → Elder** at three quarters; each step is an **evolution**: HP, ATK and DEF ×1.25, SPD +1. At the end of its lifespan it **dies of old age**: it leaves the roster (the next Familiar becomes active), drops Monster Essence of its rarity and element (1 + rarity step), and the Book's Pets page keeps a memorial ("In memory of …, lived N battles"). The Bag shows each companion's stage and battles left. Feeding and fusion will add other ways to evolve.

**Feeding, fusion, treats and looks (Phase 7 part 2, tunable, *Claude's proposal until Ed confirms*):**
- **Feed** (Pets menu → Feed): monster loot (materials) adds growth, 1 + rarity step, doubled for its own element ("favourite"). Growth 10 evolves a Young one, 20 an Adult one (same boost as aging). Elders are fully grown. Potions and oils **infuse** a permanent stat instead: healing +3 HP, oils +1 ATK, raft patches +1 DEF; a Young companion takes 3, +2 per stage. Gear, raft parts and modules aren't food. No EXP anywhere.
- **Fuse** (Pets menu → Fuse, the Transmutation Circle): pick a second companion to fuse into the first. The second is **always used up** (U-8 answered with C-5). Same kind: one rarity up, each stat the better of the two ×1.15. Different kinds: a **hybrid** named from both (Brinefang Eel + Gale Gull → Brinefang Gull) with the rarer rarity, its own element, each stat the better ×1.05. Either way it keeps an **echo** of the source: a quarter of its HP, ATK and DEF and the higher stage ("Echo of Gale Gull"; a new fusion replaces it). Chance: same kind 85%, hybrid 65%; an essence **catalyst** (2 + rarity step, of the result's rarity or rarer) adds 15%. On a failure the source slips away and the first is unchanged.
- **Companion treats:** **Kelp Biscuit** (1 Eel Scale + 1 Eel Fang, Potions tab) heals the battle companion 20 HP or wakes it up if knocked out; Semi-auto and Auto use one when the companion is at 35% HP or less (after the hunter's and raft's needs). The Mystic restocks it like other potions.
- **Looks (U-23 varieties):** Plain, Scarf, Bandana or Crown, random at taming, changeable for free in the Pets menu; looks only.
- **Pets menu (Ed's ask):** pet management has its own menu (HUD **Pets** button or **P**), not the Bag: roles, the active Familiar, Crew/Mystic chores, Feed, Fuse, Look and Release.

### Saving

Save/Load in the Settings menu, **one save file** (`Saves/save.json` under the persistent data path; "Open folder" button). **Upload save file** makes a save from anywhere the one save file and loads it; the previous save is kept as `save.backup.json` (owner's call). In the Editor it opens a file picker; in a build, paste the file path. **Auto-save every 5 minutes** into that file once a save exists (made or loaded), only in Semi-auto and Auto, and never during a battle, menu or prompt (owner's call). A save holds the bag and essence, gear and refine levels, harpoon wear, raft parts, installed modules and their wear, tier and kills toward the next tier, companions and their roles, age, stage, growth, potions, echo and look, memorials, the collection book, and the menu settings (owner's call): battle mode and auto-use (Settings), auto-salvage, target tiers and companion chores (Bag), and the Crew repair share per module kind (Build). Saves from before settings were saved load with the current settings. Items are stored by id, so renaming an asset is safe but changing an id is not.

## 11. Loot & Salvage

- Every loot item has a **Type** (Raft, Weapon, Armor, Companion, Consumable) and a **Rarity**.
- **Compatibility:** an item can be used only if its rarity fits the target's tier (e.g. Uncommon raft part cannot go on an Epic raft).
- Incompatible items are **salvaged into Monster Essence** (has rarity + element), used for:
  - Crafting new items
  - Refining (+) weapons and armor
  - Secondary material for raft/gear upgrades
  - Companion growth
- **Combining (U-6, Idea Round 3):** from Alchemy Mastery Lv 3, 5 essence → 1 of the next rarity, same element.
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

### HUD and menus (owner idea U-25)

Placeholder IMGUI until Phase 9 art, kept simple for a kid. **HUD:** a status card top-left (raft level and HP, harpoon wear with Fix, pets) and a bottom **dock of picture buttons**: Bag (B), Pets (P), Book (C), Build (R), Settings (Esc). A button lights up while its menu is open. Every menu has a red **×** and is split into **tabs**, so each page is short:
- **Build:** Build (parts, hammer, place/remove) · Workbench · Modules · Raft tree (✔/✘ requirements, the button, the type pick and the branches).
- **Workbench tabs (Ed's ask):** Gear (Weapon · Armor · Accessory), Player (Potions · Oils), Pet (Treats · Habitats), Raft (Planks & parts · Auto-loot · Hammers). Dismantling is in the Salvage Bin.
- **Mystic crafting (Ed's ask):** a glowing magic circle with spinning runes opens under the Mystic for a moment whenever it crafts or refines.
- **Workbench:** each recipe is a card listing every ingredient as have/need, green ✔ or red ✘ with "need N more". Ready recipes come first, each tab shows how many are ready, and a box hides what can't be made yet (Ed's ask).
- **Bag:** Items (filter by kind; one line on what each item is for) · Equipment · Essence · Auto-salvage.
- **Pets:** the list of pets on the left, the picked pet on the right (life and growth bars, Feed, Fuse, Look, battle, Release) · Chores.
- **Book:** tab labels show found/total; a progress bar.
- **Settings:** Game · Save · Controls · **FAQ**.
- **Guide only in Settings → FAQ (Ed's call):** no hint text on the play screen and no how-to text in the other menus; they show status only (have/need, ✔/✘). The FAQ holds the whole guide by topic (Start, Hunting, Day & weather, Battle, Bag & loot, Workbench, Raft, Pets, Book & saving), each question opening to its answer. Its text lives in `Faq` so the player manual (U-22) can reuse it.

### Camera (Ed's ask)

The view orbits the raft: mouse wheel or + / − zooms (8–45 m), right-drag or Q / E turns it (tilt 25°–85°), F puts it back. Battles still move the camera by themselves and hand it back afterwards. The camera follows the raft's centre at sea level, so it doesn't bob.

### Hunter on the raft (Ed's ask)

Outside battles the hunter isn't a statue: every 3–7 s a small idle move (look around, hop, stretch, a few steps). When casting or reeling, the hunter walks to the raft's edge nearest the catch, faces it, and leans back and tugs while reeling; the line starts at the hunter's hands. A visor shows which way the hunter faces. In battle the hunter turns to face the monster (Ed's ask). The Familiar walks after the hunter at its own pace and stays where it is, hopping, while the hunter hooks (Ed: it used to move as if stuck to the hunter).

### Away pause (Ed's call)

If the player falls asleep with the game running, nothing should happen while they're gone. After **10 minutes with no key, click or mouse move** (Settings → Game: Off / 5 / 10 / 20 min), the game **pauses**: no hooks, no module battles, no auto-save, and a "You fell asleep" screen. A battle already running finishes first. In Semi-auto and Auto it saves once when it pauses. Any key or click continues. A battle lost after a minute with no input pauses the game at once.

### Main menu (Ed's ask)

The game opens on its own **MainMenu scene** before the game (Hook) scene: **Continue** (shows the save's date; loads the save), **New game** (asks first if a save exists; the old save is kept as `save.backup.json`, so auto-save can't overwrite it, and Upload save file brings it back), **Settings** (screen size, battle mode, auto-use; saving needs a game) and **Quit**. In the game, Settings has **Save and go to main menu**. In the Editor, Play always starts at the main menu. Both scenes are built from code (`MainMenuSceneBuilder`, `HookSceneBuilder`).

## 14. Open Questions

- Action resource in battle (stamina vs weapon charge).
- Companion growth: confirm feeding, fusion and treat numbers in §10 (aging was replaced by health, Ed's U-32).
- Level-range limits on item share?
- Raft type pick at Lv 10 (Ed): raft levels stop at 6 now; pick level is Lv 2 until Ed decides (decision card).
- Sky raft: real flight (over waves, other monsters) or keep it a speed and reach bonus? (Phase 8 default: bonus only.)
- Should losing a module battle also cost a raft part? (Phase 8 default: no.)
- Target platform(s): PC standalone and WebGL both look possible (U-27); mobile still open.
- Title trademark check.
