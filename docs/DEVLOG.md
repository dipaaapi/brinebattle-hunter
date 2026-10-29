# BrineBattle Hunter — Devlog

*Newest first. One entry per working session or finished task: what changed, what was decided, what is next.*

## 2026-10-29 — Phase 6 (slice 2: consumables)
- Gear slice: Ed ran the Test Runner (103/103) and played it; PR #1 merged. Bag gear row narrowed so Refine/Take off fit.
- Branch `phase/6-consumables`: `ConsumableEffect` (HealHunter, RepairRaft) + `effectAmount` on items; `BattleAction.Item` and `BattleState.PlayItem` (takes the hunter's turn, no use at full HP); battle panel shows a button per consumable.
- Ed's call: consumables are used automatically. Semi-auto and Auto use a Tar Patch at raft ≤35% HP and a Brine Tonic at hunter ≤35% (lower side first, `BattleAI.ChooseItem`); Manual = player presses.
- New items via Create Sample Data: Brine Tonic (+25 HP; 2 Eel Scale + 1 Gale Feather; 20% eel drop), Tar Patch (+30 raft; 1 Eel-Skin Patch + 1 Plank).
- Rules tests compiled and passed outside Unity (62 incl. 4 new); Unity run pending.
- Settings menu (`SettingsPanel`, Esc or HUD button): battle mode moved off the HUD, toggle for auto-use of consumables (`BattleSettings.AutoItems`, default on), controls list. Created at runtime, so no scene rebuild needed.
- Ed: placeholder shapes make hunter, monsters and loot hard to read → logged as U-18; Ed chose to finish Phase 6 first.
- Ed's playtest: "net touches but doesn't loot". By design (calm weakened drifts are left to tame; a drift that bit a module is left alone), but silent. Modules now log once per drift why they leave it.

## 2026-10-29 — Phase 6 (slice 1: gear)
- Ed picked gear first. Branch `phase/6-alchemy`: `ItemDefinition.gearPower`, `GearRules` (power + refine step = ¼ base, min 1; refine cost 2 × next level; max +10), `Equipment` (weapon/armor slots, equip/swap/take off, refine kept per item kind), `Combatant.Hunter(gear)`.
- Bag: Equipment rows (refine, take off) and Equip buttons on gear. Workbench: craft Eel-Fang Spear (2 Eel Fang + 1 Plank) and Gull-Bone Buckler (3 Gale Feather + 1 Plank). Spear power 4, Buckler 3 (set by Create Sample Data).
- Written in a cloud session: rules + tests compiled and passed outside Unity (58 rules tests incl. 6 new gear tests); `unity-verify` and play mode not run yet.
- Next: Ed verifies and playtests; then gear rarity skills, consumables, Settings menu.

## 2026-10-29 — Idea Round 2
- Branch `round/2-ideas`: **C-4** `Durability` (shared): Harpoon has 20, each throw uses 1, broken = can't throw; repair +10 for 1 material on the HUD. **U-13** `ItemType.Module` + `ModuleRules` (U-13 scaling table, Lv 1–5) + `RaftModules`: modules sit on the raft grid; Lv 1 Drift Net grabs one touching drift every 20 s, dead/weakened = loot (−1), alive = bites (−5) and swims off; durability shown over the module, repair in the build panel (Lv 1 = materials). Removing and re-placing keeps the wear. Starter: 1 Drift Net in the bag.
- Phase 5 feedback fixes: build panel X button; drifts bump off the raft edge instead of passing through.
- `unity-verify`: 90/90 passed. Play mode not yet checked by hand.
- Ed's playtest fixes: notices go to the log (popup only for real choices), battle closes itself, Book/Bag beside the HUD, build panel scrolls, drifts collide with real parts and keep the current, visible ocean (waves + current streaks), activity log = 3-row box bottom-left, world pauses (`WorldPause`) during battle and prompts.
- More playtest fixes: hook arm casts and reels (not instant), modules skip calm tameable drifts, one owner per drift (no stealing between player hook / hook arms / nets), a drift that bit a module is left alone, scrollable 99-entry log, new sea texture, swim-off follows the current, **U-17** temperament (weakened + aggressive = killed without a prompt).
- Ed QA'd everything; Phases 5 + Round 2 merged to `main` (93/93 tests). Workflow note: keep Unity open while testing — Stop, click Unity to recompile, Play; use BrineBattle → Build Hook Scene after data/scene changes.
- **U-16** recipes: `RecipeDefinition` + `CraftRules`; Workbench in build mode (craft Drift Net, upgrade to Hook Arm Lv 2, dismantle). 91/91 passed.

## 2026-10-28 — Phase 5
- Branch `phase/5-raft`: `RaftGrid` (cells, place/remove rules, connectivity, size, HP, tier upgrade), `Inventory.SpendEssence`, `Raft` draws tiles from the grid (colour by rarity), `RaftBuilder` (R: ghost cells, place/remove, upgrade panel).
- Removed the debug raft-size −/+; size now comes from parts. Bag's raft tier now reads the real raft. New item: Driftwood Plank (starter raft, also drops from Gale Gull).
- `unity-verify`: 85/85 passed. Play mode not yet checked by hand.
- Idea Round 2 picked: U-13 Auto-loot module + C-4 Stamina + durability.

## 2026-10-28 — Idea Round 1
- Branch `round/1-ideas`: **C-2** `TensionRules` (sweet spot shrinks / sweep speeds up with size) + reel timing bar (Space or click) + `BattleState.FirstRound` initiative; **U-11** `CollectionLog` (ids per page, save string) + `CollectionBook` panel (C key; Monsters / Items / Pets, silhouettes, completion %, saved in PlayerPrefs).
- Bag panel widened ("All" buttons were cut off). GDD v0.2.
- `unity-verify`: 70/70 passed. Play mode not yet checked by hand.
- Ed played it. Fixes from his feedback: no press now lets the catch slip away; clean reel gives +1 per drop; in Auto mode the bar is greyed "AUTO" and presses itself (50% clean, from U-9).
- Semi-auto reel: player can press first; otherwise auto-press at 80% of the reel (50% clean); bar labelled SEMI-AUTO, not greyed.
- Owner ideas U-14 (crafting knowledge unlocks book entries) and U-15 (rough seas, thunderstrikes).
- Ed played all three reel modes: working. Idea Round 1 done.

## 2026-10-28 — Phase 4
- Branch `phase/4-loot`: `Inventory` (stacks + essence keyed by rarity/element), `SalvageRules` (compatibility, yield), `BagPanel` (B key: tiers −/+, fits/misfit, salvage 1/all/misfits, auto-salvage toggle).
- New sample gear: Eel-Skin Patch (Raft, Common → misfit at the default Uncommon raft tier), Eel-Fang Spear (Weapon), Gull-Bone Buckler (Armor).
- Rule from owner: an **Idea Round** (top U + top C) after every core phase. Round 1 = U-11 Collection book + C-2 Line tension.
- `unity-verify`: 53/53 passed. Play mode not yet checked by hand.
- Ed played it: bag, tiers and salvage work. Phase 4 done.
- Owner ideas U-12 (skill slots by rarity) and U-13 (auto-loot raft module).

## 2026-10-28 — Phase 3
- Branch `phase/3-battle`: `BattleState` (1v1 rounds in speed order, raft HP, stamina), `BattleRules` (damage with element multiplier, brace), `BattleAI` (Manual waits; Semi-auto auto-attacks but waits on low HP or harpoon ready; Auto plays all), `BattleSettings` (saved mode), `BattleController` (battle camera, HP bars, mode toolbar, actions).
- Actions: Attack, Harpoon (4 stamina, ×1.8), Brace (half damage, +3 stamina). Stamina is a placeholder for U-7/C-4.
- Win → loot to inventory; loss → monster leaves (absorption in Phase 8). `unity-verify`: 42/42 passed. Play mode not yet checked by hand.
- Owner ideas added: U-9 harvest Auto/Manual toggle, U-10 day/night sun/moon affinity.
- Ed played it: battle works (Auto won vs Gale Gull). No Settings screen yet; mode is on the HUD. Phase 3 done.
- Owner idea U-11: collection book (Pokédex-style, silhouettes until acquired).

## 2026-10-28 — Phase 2
- Branch `phase/2-hook` (from `phase/1-data`): `EncounterRules` (weighted state roll, unreliable hint, raft-size gate), `DriftSpawner`, `Drift`, `HookController` (click → cast → reel → result panel, inventory + log HUD, raft size −/+ for testing), `Raft`.
- Sample monsters now 3: Brinefang Eel (size 1), Gale Gull (Sky, size 1), Shellback Crab (size 3, tests the gate).
- Scene built from code (`HookSceneBuilder`), placeholder IMGUI. `unity-verify`: 31/31 passed. Play mode not yet checked by hand.
- Sedate: 50% → Weakened, else line snaps (placeholder until sedative items).
- Ed played it: harvest, tame placeholder, flee and the size gate all work. Phase 2 done.

## 2026-10-28 — Phase 1
- Branch `phase/1-data`: `Element` + `ElementRules` (combine, components, type chart) + `ElementChart` asset; `Rarity`, `ItemType`, `ItemDefinition`; `MonsterDefinition` (habitat, size class, stats, loot); `LootTable` + `LootRoller`.
- Sample data via `SampleDataBuilder` (Brinefang Eel, Eel Scale, Eel Fang). `unity-verify`: 21/21 passed.
- Type chart is a tunable default (C-9), waiting for Ed's decision.
- Next: Ed checks the sample in the Inspector, then merge.

## 2026-10-28 — Phase 0
- Concept docs imported (README, GAME_DESIGN v0.1).
- Decided: develop in phases (see [ROADMAP.md](ROADMAP.md)); first commit = docs + Unity project skeleton, not docs alone.
- Added CLAUDE.md, project skills (`roadmap`, `unity-verify`) and the tooling plan ([TOOLING.md](TOOLING.md)).
- Ideas split: owner's (`ideas/USER_IDEAS.md`) vs Claude's (`ideas/CLAUDE_IDEAS.md`), compared in `ideas/COMPARISON.md`.
- Unity project created headless (6000.3.11f1, Universal 3D/URP template) at `C:\Users\USER\brinebattle-hunter`; not auto-registered in Unity Hub (Add → Add project from disk).
- Added `Assets/_Project` layout, three asmdefs, `GameInfo` + smoke test. `unity-verify`: 1/1 passed, 0 compile errors.
- Pushed to `main` (c502bb6). Phase 0 done.
- Next: Phase 1, starting with Element data.
