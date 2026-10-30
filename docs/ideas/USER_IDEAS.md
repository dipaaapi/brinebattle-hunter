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

### U-6 Combine low-rarity essence into higher-rarity essence — 2026-10-28 · ✅ Idea Round 3 (Mastery Lv 3, 5 → 1) · GDD §11

### U-7 Battle action resource: stamina vs weapon charge — 2026-10-28 · ✅ answered by C-4 (stamina + durability) · GDD §5, §14

### U-8 Are fused companions consumed? — 2026-10-28 · ✅ yes, with an echo (C-5), Phase 7 · GDD §10, §14

## New ideas

### U-9 Harvest mode toggle: Auto / Manual — 2026-10-28 · 🟨 partly in (Auto reel = 50% clean) · GDD §4, §11
A toggle when harvesting a monster.
- **Auto:** 50% chance to catch / get the harvest.
- **Manual:** the player does it themselves and gets it, but with a 30% chance to miss (so 70% success).

Open: what Manual asks of the player (timing input? see C-2), and what a miss costs (lose the loot, or just less?).

### U-10 Day/night cycle (sun & moon) that boosts or weakens — 2026-10-28 · ✅ Idea Round 4 · new system (GDD §4, §5, §10, §12)
Add a day/night cycle as its own system. Some monsters, dead or alive, are tied to the sun or the moon, and the time of day boosts (or weakens) them. The same system also affects the hunter, the party, and companions/pets.

Open: which stats it changes (attack, catch/tame chance, hint accuracy, drift spawns?), whether gear or alchemy can grant sun/moon affinity, and how long a full day lasts.

### U-11 Collection book (Pokédex-style) — 2026-10-28 · ✅ Idea Round 1 · GDD §4, §10, §11
A collection book with pages for **monsters, items, and pets/companions**. Entries you haven't acquired show as a **silhouette**; once acquired, the full entry becomes visible.

Related: C-6 (Hunter's Journal) is similar but only for monsters; U-11 covers all three and adds the silhouette reveal.

### U-12 Skill slots limited by rarity (weapons, gear, accessories) — 2026-10-28 · 🟡 gear skills by rarity done (Phase 6); accessories open · GDD §9
The number of skill slots (passive and active) on weapons, gear **and accessories** depends on rarity. Example: a **Common** weapon literally can't hold any passive or active skill; rarer items get more slots.

Builds on the GDD §9 rarity table (Common = element only, Rare = 1 passive, Epic = 1 active + 1 passive, Legendary/Mythic = multiple slots). New here: **accessories** as a gear type, and slots as a hard limit.
Open: exact slot count per rarity, and whether slots can be added later (refine +, alchemy).

### U-13 Auto-loot raft module — 2026-10-28 · ✅ Idea Round 2 · GDD §3, §6
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
| 1 | Basic net (no hook) | 0 (touch only) | 1.5 m | 8 s | 30 | Bites the module (−5 durability) and swims off |
| 2 | Hook arm | 1 | 4 m | 6 s | 40 | Breaks free (−5) |
| 3 | Twin hooks | 2 | 6 m | 5 s | 50 | Breaks free (−5) |
| 4 | Harpoon arm (weapon) | 2 | 8 m | 4 s | 60 | Killed if size ≤ raft size (−3 per kill), else breaks free |
| 5 | Multi-harpoon | 3 | 10 m | 3 s | 80 | Same as Lv 4 |

- Each loot uses 1 durability. At 0 the module stops until repaired.
- **Rarity** of the module multiplies durability: Common ×1, +20% per rarity step (Mythic ×2).
- **Repair** (per 10 durability): Lv 1–2 = materials only; Lv 3 = materials + essence of the module's rarity; Lv 4–5 = essence only. Cost = level × (rarity + 1). Same-element essence repairs 25% more.
- Fits Phase 5 (raft builder) as a buildable part; durability shares a system with C-4 weapon durability.

### U-14 Crafting knowledge unlocks the book — 2026-10-28 · ✅ Idea Round 3 (Alchemy Mastery, Book → Recipes) · GDD §7, §9, §11, U-11
The book should also show **how to craft** things, but you only know a recipe once you have the **skill/knowledge** for it. That is the point of harvesting: harvesting builds your skills, skills let you craft, and crafting unlocks more.
- Right now the book only lists what you've looted.
- As skills grow, **new "???" entries appear** (craftable items, gear, potions, raft parts) that you can now learn to make.
- Each revealed craftable shows its recipe (ingredients + required skill level).

Open: is the skill the same as Alchemy Mastery (GDD §7) or separate per craft (smithing, raft-building, potions)? Do harvests of a monster type give knowledge of that monster's recipes?

### U-15 Rough seas and thunderstrikes — 2026-10-28 · ✅ Idea Round 4 · GDD §2, §4, §6
The ocean isn't always calm. Sometimes the sea gets rough, which makes hunting and looting harder. Sometimes the enemy is the weather itself: **thunderstrikes** hit the raft, damaging it or stopping the hunter from looting further until it's dealt with.

Related: C-3 (weather and currents decide what drifts) and U-10 (day/night). These three could become one **World conditions** system.
Open: how rough seas hurt hunting (smaller sweet spot, shorter hook range, drifts move faster?), whether a lightning rod / Fortress parts protect the raft, and whether storms also bring rarer drifts as a reward.

### U-16 Parts become modules, and back — 2026-10-29 · ✅ partly (Idea Round 2 feedback) · GDD §6, U-13
Some patches and planks can be turned into a net or a hook, upgraded, and taken apart again. Owner left the recipes to Claude.
- Built: Workbench in build mode. Craft Drift Net = 3 Driftwood Plank + 2 Eel-Skin Patch. Upgrade to Hook Arm (Lv 2, 4 m reach) = Drift Net + 2 Eel Fang + 2 Driftwood Plank. Dismantle returns part of the cost.
- Later: Lv 3–5 recipes, and folding this into Phase 6 crafting.

### U-17 Temperament decides the weakened prompt — 2026-10-29 · ✅ built · GDD §4, §5
Each monster has an aggression (temperament). A weakened monster that is aggressive is killed outright, with no prompt and no menu, because there's no point taming it. Only a calm weakened monster asks Finish or Tame. A strong (alive) monster still goes straight to battle.
- Built: `MonsterDefinition.aggression` (Eel 0.8, Crab 0.45, Gull 0.3), each drift ±0.15; aggressive = 0.5 or more. Also applies after a successful sedate.


### U-18 Readable hunter, monsters and loot — 2026-10-29 · ⬜ open · Phase 9
The hunter, monsters and loot are hard to tell apart because they're all sample shapes. Make them readable, maybe with Blender models.
- Options put to Ed: clearer code placeholders now (distinct shape per monster, hunter figure, loot labels) and keep Blender for Phase 9, or pull the Blender art pass forward.

### U-19 Battle effects and projectiles — 2026-10-29 · ✅ · GDD §5, Phase 9
Battles have no visual effects yet: no projectiles (thrown harpoon, spit, dive), hit flashes or splashes. Add them so attacks read on screen.
- 2026-10-29: Ed asked again during Phase 7 ("text and UI only, not fun"). First pass with placeholder shapes done on `phase/7-companions`: lunges, harpoon/Tidal Strike projectiles, hit flash + shake, damage/heal numbers, KO sink, companion stand-in beside the hunter. Splashes, particles and real models stay for Phase 9.

### U-20 Music and sound effects — 2026-10-29 · 💡 · new system
No music or sound effects yet. Add ocean ambience, battle music and SFX (cast, reel, hit, loot, craft).

### U-21 Fog and time-of-day look — 2026-10-29 · ✅ Idea Round 4 · GDD §2, Phase 9
No fog and no day/night look yet. Fog at night (and maybe in rough seas) that limits how far you can see drifts. Pairs with U-10 (day/night cycle) and U-15 (rough seas).

### U-22 Player manual — 2026-10-29 · 💡 · after the game is finalized
When the game is finished, make a manual for players: viewable inside the game if possible, otherwise a downloadable PDF, whichever is better.
- Claude's suggestion: both from one source. An in-game "How to Play" screen (works offline, always matches the build, can open on the relevant page) plus a PDF made from the same text for the store page / download. Write it at the end, once mechanics stop changing; the Settings menu's Controls list is the seed.

### U-23 Three companion roles, looks, evolution, old age — 2026-10-29 · ✅ (first pass; numbers wait for Ed) · GDD §10
Companions come in three roles:
1. **Crew** (Ed's word: "slave") — looks after the raft: repairs and upkeep.
2. **Mystic** — helps with alchemy and crafting, except the raft: gear, weapons, consumables.
3. **Familiar** — fights beside you against other monsters.
Each role has varieties that only change the look and outfit. Companions can **evolve** to serve better, and they **die of old age**.
- Claude's note: "Crew" is used in code and UI as a neutral name for role 1 until Ed picks the final word. Open: how a companion's role is set, what makes it evolve, how lifespan is counted before a day/night cycle exists (U-10), what happens when one dies.

### U-24 Rocking raft, moving waves, katig — 2026-10-29 · ✅ (first pass) · GDD §6, Phase 9 art later
The raft should rock with the sea and the sea should have real waves, not only lines. A raft has katig (outriggers) and something to catch the wind; here they are looks only. Pulled forward by the owner during Phase 7: `Waves` swell shared by the ocean mesh, raft and drifts; `RaftDecor` adds katig and a sail. Real models come with U-18.

### U-25 Kid-friendly, simpler UI — 2026-10-29 · 🟡 cleanup + icon dock done (Phase 7), full redesign with real art in Phase 9
The menus are too complicated; a child should be able to use them. Fewer words, bigger buttons, icons, one thing per screen. Ed chose both: a quick cleanup now (done in Phase 7: Screen size setting Small/Big/Huge, default Big; shorter HUD with bars and big buttons; one Crew repair slider per module kind) and a full UI redesign with icons in Phase 9.

### U-26 Main menu scene — 2026-10-29 · ✅ (first pass) · GDD §13, UI art in Phase 9
Before the game starts or continues there should be a main menu: New game, Continue and Settings, so the player sets things up first and then plays without stopping. It is its own scene that loads first, not a pop-up over the game (Ed). Pulled forward by the owner during Phase 7.

### U-27 Standalone and WebGL builds — 2026-10-29 · 💡 · GDD §13, after Phase 9
Can the game be a standalone app and also run in a browser (WebGL)? If yes, do both.
- Claude's note: yes, one Unity project builds both. Ed's laptop Unity already has Windows Standalone and WebGL modules (Mac/Linux need theirs added). WebGL needs: a save that reliably persists in browser storage (IndexedDB sync), a browser file picker plus "download save" instead of Upload by path, hiding Quit; Phase 10 online play would need WebSockets. First step: a test Web build to see size and speed.

### U-28 Free on itch.io with microtransactions and small ads — 2026-10-29 · 💡 · after the game is finished
Release the game free on itch.io, with microtransactions and small ads, to earn back what making it cost.
- Claude's notes (to check before building anything): itch.io hosts both the WebGL (play in browser) and downloadable builds and supports "pay what you want" and paid add-ons, but it has no in-game purchase system, so microtransactions need an outside payment service. Many ad networks don't serve inside itch.io's embedded page. The game targets kids, so ads and purchases aimed at children fall under child-privacy rules (e.g. COPPA) and ad networks' child-directed settings. Cosmetic-only purchases (pet looks, raft skins) fit the design best.

### U-29 Familiar helps with looting — 2026-10-30 · ✅ Phase 8 · GDD §10
Some pets, especially the Familiar, can join in on looting.

### U-30 More loot modules, by rarity, that catch raft materials — 2026-10-30 · ✅ Phase 8 · GDD §6
More kinds of looting modules, in rarity tiers, that can be crafted. Better modules widen what can be looted, even planks and other things the raft needs.

### U-31 Material tree by raft type, not a skill tree — 2026-10-30 · ✅ Phase 8 · GDD §6, Phase 8
The further you upgrade, the more variants you choose from. You simply pick which raft type you're upgrading toward, and that unlocks a tree of materials instead of a skill tree.

### U-32 Pets don't age; they have health instead — 2026-10-30 · ✅ Phase 8 · GDD §10 (would replace Phase 7 old age)
No more aging for pets. Instead, pets have health that drops when the enemy attacks them in battle, in a random attack before a battle, or in a sudden outburst after the enemy is beaten that can hurt the player or the pet.

### U-33 Status-curing potions — 2026-10-30 · ✅ Phase 8 · GDD §11 Consumables
Consumable potions in variants: HP, toxic relief (poison), confusion relief, a blood-clotting patch (bleeding) and an eye cleanser (for silence/blindness). Needs status effects in battle first.

### U-34 Raft, workbench and pets polish — 2026-10-30 · ✅ Idea Round 4 (built at Ed's ask) · GDD §6, §9, §13
- Workbench sub-tabs: hammers, planks and auto-loot apart; player (potions + oils) and pet apart; weapon, armor and accessory apart.
- Accessories with unique skills by rarity, element and + refine.
- Dismantling in its own Salvage Bin, with essence flying into it from the Bag and Build menus.
- Pets and the player walk on planks only, never on auto-loot modules.
- Raft looks by type: Fortress (STR tank) = sea train, Sky (INT) = winged plane, Beast (DEX) = pirate ship.
- Raft skill tree: Level up and Tree in one page; pick the type once (Ed: at Lv 10, after the 10 levels of basics); the picked type's tab is the default, the others lock and don't branch out.
- Mystic crafting shows a glowing incantation circle; the Crew hammers each install with a loading bar, never instant.
Ed's call: the raft levels stop at 6, so the type is picked once at the first level-up (Lv 2).

### U-35 Picking a type resets the off-type parts — 2026-10-30 · ✅ Idea Round 4 · GDD §6
Once the raft type is picked, parts that don't suit it reset to basic by themselves and can't be used any more. The parts that come off go to the inventory; nothing is lost from the bag or the Salvage Bin.

### U-36 Reset raft in Settings — 2026-10-30 · ✅ Idea Round 4 · GDD §6
A Reset raft button in Settings to rebuild the raft, but about 20% of the parts that come off are washed away by the sea.

### U-37 Vehicles to leave the sea, and a boss per tier — 2026-10-30 · ✅ Idea Round 4 · GDD §6
The raft type's parts are modules that build into a whole vehicle: Sky a plane, Fortress a sea train with its track, Beast a pirate ship. Finished at the final tier, it can leave the sea (the plane flies). To go further, kill the boss of each tier first. Ed picked: after leaving, a new, harder sea back at Lv 1, keeping pets, gear and the bag.

### U-38 Difficulty by raft types — 2026-10-30 · ✅ Idea Round 4 · GDD §6
Easy: unlock (finish) 1 raft type; Normal: 2 types; Hard: all 3. Harder means less loot and tougher monsters in battle.
