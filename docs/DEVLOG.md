# BrineBattle Hunter — Devlog

*Newest first. One entry per working session or finished task: what changed, what was decided, what is next.*

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
