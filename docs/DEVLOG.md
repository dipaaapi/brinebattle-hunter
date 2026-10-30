# BrineBattle Hunter — Devlog

*Newest first. One entry per working session or finished task: what changed, what was decided, what is next.*

## 2026-10-30 — Idea Round 4: World conditions
- Ed picked World conditions after Phase 8 (row 3 of the combined plan: U-10, U-15, C-3, U-21). Branch `idea-round/4-world` from main d60cfa8.
- `World/WorldRules` (plain C#): time of day (8-minute day), Sun/Moon affinity from the element (+20% ATK/+10% DEF at its time, −10% ATK against), weather effects (waves, drift speed, hook reach, reel sweet spot, alive/dead rolls, floating-loot share, hint accuracy, sight range), weather transitions (storms only after rain/rough seas), thunderstrike chance (Fortress ½, Sky ×1.5). `World/WorldState`: clock, weather, forecast, wreck field, events.
- `Hunting/WorldClock` (added at runtime by `HookController`): runs the clock outside menus/battles, sky colour, sun angle/colour, ambient, scene fog from the sight range, `Waves.WeatherScale`, lightning bolts and `RaftFire` (click twice to douse, Crew douses after 5 s, burns the part away after 15 s via `RaftGrid.Burn`; the hook is jammed while a fire burns).
- Hooked in: `DriftSpawner` (affinity-weighted creature pick, state roll, hints, speed, floating loot, wreck field), `Drift` (far drifts "???", `Fighter` empowered), `HookController.Fighter` and the pet (empowered), `BattleController` (pet empowered, a log line says who the time/weather favours), `Raft.HookReach`, reel sweet spot, HUD line with the forecast.
- Claude's ideas, per Ed's ask to add my own: **C-12 Forecast** and **C-13 Wreck field**.
- Save: time, day, weather, forecast, wrecks (`hasWorld`; older saves start in the morning). FAQ topic "Day & weather". No data/builder changes (DataVersion stays 11). +10 rules tests (226 in the cloud check); Runtime compiles. Unity test on Ed's PC next.
- Ed's Unity test: the hunter didn't face the monster in battle, and no sun or moon could be seen. Fixes: `HunterMotion` turns the hunter toward `BattleController.Foe` during battles; `WorldClock` draws a sky strip (top-right) with the sun or a crescent moon on its arc (`WorldRules.SkyArc`), clouds in bad weather, and turns on soft sun shadows. +1 test (227).
- Ed's raft/workbench batch (build now, not later):
  - Workbench tabs → Gear (Weapon/Armor/Accessory), Player (Potions/Oils), Pet (Treats/Habitats), Raft (Planks & parts/Auto-loot/Hammers): `RecipeSub`, `CraftRules.SubOf/SubsOf`.
  - Accessories: `ItemType.Accessory`, `GearSlot.Accessory`, power splits ATK/DEF, element when the weapon is neutral, `GearPassive.Swift`; four accessories + recipes (`SampleDataBuilder.Accessory`, DataVersion 12); saved.
  - `SalvageBin`: its own box next to the storage box; `Inventory.Salvaged` and dismantle crafts fly essence orbs into it; click to open (essence + dismantle).
  - Planks only: `RaftGrid.Walkable`, `Raft.WalkableAt`; hunter and pets never step on modules or habitats.
  - Raft looks by type (`RaftDecor`): sea train, winged plane, pirate ship.
  - Raft tree: Level up + Tree in one page; type picked once (`RaftGrid.Chosen/CanChoose/Choose`, `RaftTree.PickLevel` = 2: Ed picked one choice at the first level-up, since raft levels stop at 6); unpicked types lock.
  - Mystic: incantation circle when it crafts/refines. Crew: walks over and hammers for 4 s with a progress bar (`CompanionWork.CrewInstall`).
  - FAQ updated. +5 rules tests (232 in the cloud check).
  - Ed's test: his Sky raft showed as a sea train (Fortress) because the Crew had built Shellback Plates. Fix: the picked type is the raft's type (`RaftGrid.Type`), so look and bonuses follow the pick.
  - Ed (U-35): once the type is picked, parts of other types come off by themselves. `RaftGrid.ResetOffType` sends them to the Bag and puts basic planks (the starter part) in their cells; `FitsType` / `PlaceResult.WrongType` block placing them (Build greys them out "not for a Sky raft"; the Crew skips them since it uses `CanPlace`).
  - Ed (U-36): Settings → Game → **Reset raft** (asks twice). `RaftGrid.ResetRaft`: back to the starter 3x3, level and type stay, a fifth of the parts that come off float away (`WashAwayShare` 0.2), the rest go to the Bag, habitats are never lost (back on the square's edge). +2 tests (234 in the cloud check).

## 2026-10-30 — Phase 8 (raft types & absorption) + Ed's asks
- Office desktop synced to main 0edfa79 (git bundle; old local changes stashed).
- Ed's playtest: the Familiar moved with the hunter while hooking, as if attached. Now it walks after the hunter at its own pace and stays put, hopping, while the hunter casts and reels (`RaftCompanions`).
- Ed: zoom and rotate. `CameraRig` (added to the main camera at runtime): wheel or +/− zoom, right-drag or Q/E turn, F reset; waits for the battle camera (`BattleController.CameraBusy`). `CameraRules` limits. Settings → Controls and the FAQ list the keys.
- Ed: do the next phase together. **Phase 8** on `phase/8-raft-types`:
  - `RaftKind` on raft parts; `RaftTypeRules.Classify` (≥ 3 parts and ≥ 50% → type; two kinds ≥ 30% → Hybrid at half bonuses), `RaftBonus` (Fortress +40% raft HP/+3 DEF/−1 SPD, Sky +3 SPD/+4 m reach/−10% HP, Beast +4 ATK/+1 SPD). `RaftGrid.Type`, `MaxHp` includes the bonus; the hunter's battle stats (`HookController.Fighter`) and the danger check use it; `Raft.HookReach` adds the reach.
  - Typed parts per raft level: Shell Plating, Wing Sail, Beast Harness (`SampleDataBuilder.TypedParts`, 18 items + recipes; Shellback Plate and Coralbound Plank count as Fortress). DataVersion 10. Parts tinted by kind; Build shows the type, kind counts, and parts held by gorged monsters; HUD shows the type.
  - Absorption: a lost battle (not a module battle) or Flee eats one edge raft part (`RaftGrid.EdibleParts`/`Absorb`; never modules/habitats, never splits, never at 5 parts or fewer). `GorgedLog` (saved as `gorged`) and `AbsorbRules`: the monster returns (30% per spawn, one at a time) alive, bigger, "Gorged!", +20% stats per part; beating it returns the parts plus one extra loot roll each.
  - Sky = bonus only and module battles don't eat parts: Claude's defaults, asked Ed (GDD Open Questions).
  - FAQ: raft types, typed parts, gorged monsters; flee and loss answers updated. +21 tests (171/171 rules tests in the cloud check). Runtime compiles; Editor and Unity test still on Ed's PC.

- Ed's new ideas U-29…U-33; Ed: build them now, not just list them, and asked for Claude's ideas too (added C-10, C-11). All on `phase/8-raft-types`:
  - U-32 health instead of aging: `Companion.Hp/Status`, `KnockedOut`; aging no longer called (`CountKill`). Hunter HP carries over (`HookController.HunterBody`, `Fighter`, `TakeVitals`), resets after a defeat. `HazardRules`: ambush while reeling a live catch, outburst after a kill, regen (hunter 3 s/HP, pets 6 s/HP faster per habitat = C-10), `Treat` outside battle (Bag → Use, Pets → pet). HUD shows Hunter HP; Pets shows HP/ailment instead of Life.
  - U-33 ailments: `StatusRules` (Poison, Confusion, Bleed, Blind), `Combatant.Status/Inflicts`, monster hits inflict, end-of-round ticks, confusion self-hits, blind misses and blocks skills; `ConsumableEffect.CureStatus` + `ItemDefinition.cures`; Toxic Relief, Confusion Relief, Clotting Patch, Eye Cleanser, Big Brine Tonic; Auto/Semi-auto cure. `BattleEventKind.Ailment` popups. C-11: ailment tag over drifts.
  - U-31 raft tree: `RaftTree`, `RaftGrid.Paths/PathLevel/TryUpgrade(inv, path)`, Level up picks Fortress/Sky/Beast, Build → Tree tab, locked recipes say why. 15 tree materials; typed parts Lv 2+ = last part + tree material. Tree materials aren't spent as monster materials.
  - U-30: five more modules by rarity (Tarred Net … Abyssal Maw) and six kinds of floating loot (`MonsterDefinition.isFlotsam`, `DriftSpawner.PickFlotsam`, `ModuleRules.CatchesFlotsam`).
  - U-29: `HookController.FamiliarFetch` (+ Pets → Chores toggle).
  - Save: hunter HP/ailment, pet HP/ailment, raft paths. DataVersion 11. FAQ updated. +23 rules tests (216 in the wider cloud check).
- Ed tested on the office desktop and said proceed. Unity-generated data, scenes and metas committed from the desktop (68aac4a). PR into main: **Phase 8 done.** Next: Idea Round 4 or Phase 9 on Ed's pick.

## 2026-10-29 — Phase 7 part 2 (companion growth: evolution and old age)
- Branch `phase/7-growth` from main after PR #4. Ideas merged into one plan in `ideas/COMPARISON.md` (U-25 and C-9 kept on their own).
- `Companion.Age`/`Stage`/`GrowOlder()`/`Evolve()`, `CompanionRules.Lifespan`/`AgeFor`/`EvolveBoost`/`EssenceLeft`, `CompanionRoster.GrowOlder()` and `Memorials`. `HookController.CountKill()` (every beaten monster): raft kill count + every companion ages; evolutions and deaths go to the log, the dead leave essence. Bag shows stage and battles left; Book Pets page shows memorials. Save keeps age, stage and memorials (older saves load as Young, age 0).
- Numbers are Claude's proposal (GDD §10), waiting for Ed. 122/122 rules tests; Runtime compiles. Next: feeding/infusion.
- Ed's playtest: after leveling the raft, most workbench recipes became uncraftable. Cause: Driftwood Plank and Eel-Skin Patch are Common raft parts, so at Lv 2 they turned into misfits and auto-salvage ate them; and no Uncommon+ raft parts existed. Fix: `Inventory.SalvageIncompatible(tierOf, keep)` keeps recipe ingredients (Bag shows "crafting"). Ed chose Reinforce recipes per level: Tarred Plank (U), Shellback Plate (R, existing), Coralbound Plank (E), Leviathan Beam (L), Abyssal Keel (M) in the Raft tab (`SampleDataBuilder`, DataVersion 6). 123/123 rules tests.
- Ed (U-26): a main menu before the game, as its own scene. `MainMenu` scene built by `MainMenuSceneBuilder` (first in Build Settings, and the Editor's Play start scene): Continue / New game (old save → `save.backup.json` via `SaveSystem.StartNew`) / Settings / Quit. `GameStart` carries the choice into the Hook scene (`HookController.Start` loads the save on Continue). Settings in game: "Save and go to main menu"; in the menu it hides save/load. DataVersion 7. Editor scripts unchecked in the cloud; the new scene and its .meta come from Ed's Unity.
- Ed's playtest (laptop): the main menu works. Two problems: the hunter, pets and monsters overlap; and the raft no longer rocks (the 12% calm zone). Ed's fix for the raft: higher raft level = thicker planks. `RaftRules.Freeboard` (0.30 → 0.90 m) and `SeaShare` (0.5 → 1.0 of the swell) by tier; tiles, modules and habitats reach down past the water line; katig on the water line; full lean. Overlap: drifts keep apart (`Drift.KeepApart`, `BodyRadius`), the reel stops the monster at the raft's edge, pets push out of the hunter, box and each other (`RaftCompanions.KeepApart`), battle lunges stop where bodies touch (`Actor.Radius`). 124/124 rules tests.
- Ed: finish everything in Phase 7, then he tests the whole phase and decides on merging. Unity-generated files from the laptop committed (afb1d41). `CompanionGrowth`: Feed (growth → evolve; potions infuse HP/ATK/DEF, cap by stage), Fuse (same kind → rarity up; hybrid; echo; catalyst; failure loses the source), Looks (Plain/Scarf/Bandana/Crown, random at taming, attire on the raft). `ConsumableEffect.HealCompanion` + Kelp Biscuit (DataVersion 8), auto-used for a low or knocked-out companion. Bag: Feed / Fuse / Look per companion. Save keeps growth, potions, echo, look. 132/132 rules tests.
- Ed: pet management should be its own menu, not in the Bag. New `PetsPanel` (P or the HUD Pets button, `Menu.Pets`, added at runtime) holds the companion rows, chores, Feed, Fuse and Look moved out of `BagPanel`.
- Ed: replace a built tile instead of remove + place; building needs a tool, a hammer, tied to the raft level and upgradable, made from loot (also by the Mystic). `ItemType.Tool`, `RaftRules.HammerFits/BestHammer/HasHammer`, `RaftGrid.Tools` gates place/replace/remove/upgrade (`PlaceResult.NeedsHammer`, `RemoveResult.NeedsHammer`), `RaftGrid.Replace`. Six hammers + recipes (DataVersion 9), starter hammer for new games and older saves, `CompanionWork.MysticHammerPick`. Build shows the hammer and adds a guide step. 135/135 rules tests.
- Ed asked if the game can be standalone and WebGL: yes; logged as U-27 (save sync, web file picker, hide Quit) for later.
- Ed's playtest: after the raft upgrade, Common loot vanished. Cause: his Bag target tiers were set high (weapon/armor tier 4) with auto-salvage on, and every equippable below its tier counted as a misfit. Now `SalvageRules.IsMisfit` only covers weapons and armor; raft parts and everything else are always kept.
- Ed: make the HUD less confusing (U-25 second pass, pulled forward). `HuntIcons` (pixel icons), bottom dock of picture tiles (Bag B, Pets P, Book C, Build R, Settings Esc; lit when open), top-left status card (raft, harpoon + Fix, pets), one centered hint banner, red × close on every panel, panels stay above the dock, coloured result buttons (green Fight/Tame/Harvest, red Finish it, blue Flee/Cut). 135/135 rules tests.
- Ed's playtest: the Workbench didn't say how many items were missing, and every menu felt hard to use. All menus now use tabs (short pages) and cards (GDD §13 "HUD and menus"). Workbench recipe cards show have/need per ingredient in green/red, ready ones first, ready counts on the tabs, "show only what I can make". `CraftRules.Missing`/`ByReadiness` (+2 tests). Bag: Items/Equipment/Essence/Auto-salvage with kind filters; Pets: list + detail with life/growth bars; Settings: Game/Save/Controls; Book: counts and progress bar. 137/137 rules tests.
- Ed: happy with the menus; the whole guide belongs in Settings as an FAQ and nowhere else, and the top-centre text (the HUD hint banner) must go. Banner removed; how-to text taken out of Build, Bag, Pets, Book and battle (they show status only); the upgrade guide became a ✔/✘ requirement list. New `Faq` (8 topics) shown in Settings → FAQ as questions that open to their answers. +1 test (138/138).
- Ed asked what should happen if the player falls asleep and the hunter loses while away. Ed chose **auto-pause when idle**: `IdleRules`/`IdleClock` + `IdleWatch` (added at runtime) set `WorldPause.Asleep` after 10 min with no input (Settings → Game: Off/5/10/20), save once in Semi-auto/Auto, block module battles and auto-save, and show a sleep screen until a key or click. FAQ entry added. +3 tests (141/141).
- Ed: what if the hunter loses to a stronger monster while playing? Ed chose **rest + a little lost**: `DefeatRules` (30 s rest, waves take up to 3 Common materials, nothing else) and `Danger` (⚠ Danger! over drifts that would likely win). A loss after 1 min with no input pauses at once (`IdleWatch.Lost`). +3 tests (144/144).
- Ed: do the next development step together with Phase 7. **Idea Round 3 = Alchemy Mastery** (U-14 + C-1, with U-6), on `phase/7-growth`. `AlchemyMastery` (points: harvest/beat 2, tame 3, craft 1; Lv 1–10, 10 × level per level): hint accuracy 60% → 96% (`DriftSpawner`), recipes need mastery by output rarity (Common 1 … Mythic 6; unknown = "???" in the Workbench and a new Book → Recipes tab; the Mystic only crafts known ones), essence combining 5 → 1 from Lv 3 (`Inventory.RemoveEssence`, Bag → Essence). HUD shows Mastery; save keeps it (older saves start at their raft level). FAQ topic added. +6 tests (150/150). Numbers are Claude's proposal for Ed to tune.
- Ed: on a loss the pets hide in the box and the box shivers (comedy); the hunter has idle moves; when hooking, the hunter walks to the raft edge facing the catch and pulls. `HunterMotion` (added at runtime: idle look/hop/stretch/wander, edge walk + tug, knocked out with "z Z", visor), `RaftCompanions` scurry into / pop out of the box during the rest, `StorageBox.Scared` rattle + squeaks + lid peek. Line starts at the hunter. Looks only; 150/150 rules tests.
- Ed's playtest (again): drifts stuck against the raft by the modules were never hooked. Causes: modules skipped weakened calm drifts (left for the player) and spooked ones forever, and a drift in a notch between protruding modules could sit just outside a module's reach. Now modules take everything touching: `Drift.TouchingRaft` adds a cell of reach, a calm weakened catch goes to the hunter as Finish/Tame (`HookController.ModuleCatchCalm`, held while busy), `Spooked` lasts 20 s.
- Ed tested and said push and merge. Unity-generated assets and metas committed from the laptop (9f3ae87). PR #5 merged into main: **Phase 7 and Idea Round 3 done.** Next: Phase 8 (raft types & absorption) on Ed's go.

## 2026-10-29 — Phase 7 (raft progression asks)
- Settings: **Upload save file...** (Editor file picker; path field in a build) copies a save into the one slot, keeps the old one as `save.backup.json`, and loads it.
- Raft tier upgrade now needs kills since the last upgrade (5, 10, 15 …) and monster materials (same numbers) on top of essence. `RaftGrid.Kills` (saved), `UpgradeBlocker` says what's missing; Build panel shows each requirement.
- Widening and upgrading need auto-loot modules on half the left-edge rows (`RaftGrid.ModulesToGrow`, `PlaceResult.NeedsModules`). Ed confirmed this reading of "50% of installed modules".
- A module that hooks a live aggressive monster starts a forced Auto battle (`HookController.ModuleBattle`, `BattleController.ForceAuto`).
- Rules tests outside Unity: 117/117 (RaftGridTests now included).
- Ed's playtest: a Hook Arm caught a live Shellback Crab and it still bit and swam off (crab aggression 0.45 often rolls calm, and only aggressive ones fought). Now every live catch the raft can fight starts the Auto battle; if the hunter is busy the module holds it until free. Ed: even monsters too big for the raft can't escape a module, so the size gate doesn't apply to module catches. A module catch also cancels the hunter's own cast or reel.
- Ed: menu settings must be in the save too. `SaveData.settings` (save version 2): battle mode, auto-use, auto-salvage, Bag target tiers, Crew/Mystic chore toggles, Mystic keep amount, Crew repair share per module kind. Fixed `RaftModules.Sync` keeping a stale slot when a load put a different module on the same cell.
- Ed (U-24): the raft didn't rock and the sea had no waves. New `Waves` (3 long sines); `OceanSurface` swaps the plane for a 150×150 grid and moves it; the raft rides and leans with the swell (`Raft.LateUpdate`; `CellToWorld`/`WorldToCell` now follow its rotation); drifts float on it. `RaftDecor`: katig and a sail, looks only.
- Ed (U-25): UI too complicated for a kid. Ed chose a quick cleanup now plus a full redesign in Phase 9. Cleanup: `HuntStyles.Scale` (Settings → Screen size Small/Big/Huge, default Big, saved) scales every IMGUI panel via `GUI.matrix`; HUD cut to a hint, raft/harpoon bars and four big buttons; Build shows one Crew repair slider per module kind.
- Ed: HUD now shows the save file and the auto-save countdown at the bottom right (`SettingsPanel.AutoSaveStatus`). Build has an **upgrade guide** (button at the top, and "Guide me" under a blocked upgrade): each requirement as DONE / TO DO with how-to text and a button (close Build to hunt, open Dismantle, pick a module or open the Raft recipes); the left-edge spots that need a module glow yellow (`RaftGrid.UpstreamGaps`). 118/118 rules tests.
- Ed: in Auto, a weakened calm catch with no pet room is finished automatically (log only, no prompt). Current streak lines removed; the swell is taller (4 waves, up to ~0.8 m crest to trough), the ocean mesh finer and glossier so it reads from above; the raft always sits on the water and leans 60%. The raft tier is shown as a level (Common = Lv 1 … Mythic = Lv 6) in the HUD and Build.
- Next Phase 7 step (Ed: go on): **habitats**. `ItemType.Habitat`; Tide Pool and Sky Perch (craft + dismantle recipes, `SampleDataBuilder`, DataVersion 5); placed in Build like parts (no tier or module gate); `RaftGrid.HabitatCount` sets roster room (3 + habitats) through `HookController.SyncHabitats`. Tame button says "no room: build a habitat". 119/119 rules tests; Editor scripts unchecked in the cloud.
- Ed's playtest: at Lv 2 (Uncommon) the last left-edge module couldn't be placed: every module is Common, so the tier rule blocked them (and taking one off made it a misfit). Modules now fit any tier (`SalvageRules.IsEquippable` no longer lists Module), so they also stop being auto-salvaged as misfits.
- Ed: a size-10 raft sank into the swell (flat raft, 10 m waves). The sea is now calm around the raft (`Waves.CalmCenter/CalmRadius`: 12% of the swell within the raft and katig, back to full over 5 m), and the raft sits 8 cm higher, so it still rocks gently but never goes under.
- Ed: the workbench is now the **storage box** (one object: storage + crafting/alchemy table), `StorageBox` on its own raft tile next to the centre (not the hunter's tile, not a module or habitat). The Mystic works beside it; the Crew picks walks whose path keeps clear of the box and the hunter. Storing loot (hook, battle, module catch) and crafting (hunter at the workbench, Mystic crafts and refines) open the lid, bounce the box, fly a cube in or out, and float a "Stored: … / Crafted: …" label.

## 2026-10-29 — Phase 7 (slice 1: taming + companion in battle)
- PR #3 merged → Phase 6 done. Ed picked Phase 7 next. Branch `phase/7-companions`.
- `BrineBattle.Companions`: `CompanionRules` (tame chance by rarity, 60% stat share, capacity 1 + habitats, 25% targeted), `Companion.FromMonster`, `CompanionRoster` (add, active, release). `Combatant.FromCompanion`; `BattleState.Companion` attacks after the hunter, can be hit and knocked out.
- Hook result: "Tame (xx%)" on weakened calm drifts (disabled when the roster is full). HUD shows the companion; Bag has a Companions section (Set active / Release); battle panel has a companion HP bar; Collection book Pets page records tames.
- No sample data change. Rules tests outside Unity: 87/87 (5 new). Unity run pending.
- Ed tamed a Gale Gull and fought with it (works), but: battles are only text and UI, "not fun". Pulled U-19 forward (owner's call): `BattleState.Events` (structured `BattleEvent`: kind, from, to, amount) next to the text log; new `BattleStage` (runtime-added, no scene rebuild) animates them in order with primitives: lunge + hit, harpoon capsule / Tidal Strike sphere projectiles, flash + shake, floating damage/heal numbers, KO sink, companion sphere beside the hunter. The controller waits for the animations before the next round; panel moved bottom-right and the camera aims left of it.
- New compile check: all Runtime scripts now build in the cloud against Unity 2021.3 reference assemblies (NuGet `UnityEngine.Modules`) + an Input System stub, so IMGUI/MonoBehaviour code is compile-checked too (not Editor scripts). 89/89 rules tests.
- Ed's design (U-23): three companion roles — Crew (raft upkeep; Ed's word was "slave", Claude suggested "Crew", Ed to confirm), Mystic (crafting/alchemy except raft), Familiar (battle); cosmetic varieties; evolve; die of old age. Ed chose: the player picks the role after taming.
- Roles built: `CompanionRole`, roster capacity 3 + habitats, only Familiars fight, `Best(role)`. Crew: `BattleState.CrewRepair` patches the raft each round; mends harpoon/modules every 15 s outside battle. Mystic: chance of an extra gear/potion/oil craft; `Equipment.RefineDiscount` −1 essence. Tame now opens a role picker. 93/93 rules tests; Runtime compiles.
- Ed: wants a save file to come back to; and Crew/Mystic felt useless — they should be visible on the raft (Mystic at the workbench, Crew walking around) and both upgrade with the loot on hand.
- Saving: `SaveData` + `SaveGame.Capture/Apply` (plain C#, ids not references) and `SaveSystem` (JsonUtility, `persistentDataPath/Saves/saveN.json`); Settings has 3 slots with Save/Load and Open folder. Restore hooks: `Inventory.Clear/AddEssence`, `RaftGrid.Restore`, `Equipment.Restore/Refines`, `Durability.Set`, `CompanionRoster.Restore`, `CollectionBook.Restore`.
- `RaftCompanions` (runtime-added): spheres on the raft per companion, a workbench cube when a Mystic exists, name tags with "working!". `CompanionWork`: Crew builds the rarest fitting raft part on the nearest open cell every 20 s; Mystic refines the lower equipped piece every 20 s. Toggles in the Bag. 97/97 rules tests; Runtime compiles.
- Ed's playtest (save + crew worked): the Crew walked off the raft; wants one save slot that auto-saves every 5 min (Semi-auto/Auto only) into the latest saved/loaded file; Crew should also install auto-loot modules on the left (drifts come from there) and grow the raft on the right. Done: Crew walks tile to tile; `CompanionWork.CrewPick` modules upstream / parts downstream (upstream from `DriftSpawner.currentDirection`); `SaveSystem` single `save.json` + auto-save in `SettingsPanel`. 97/97.
- Ed: Crew should also repair modules (threshold % per item, in Build); Mystic should craft consumables when they run low and support the hunter and Familiar in battle. Done: `CompanionWork.CrewShouldRepair` + per-kind slider in Build (`CompanionSettings.CrewRepairAt`), paid via `RaftModules.Repair`; free module mend removed (harpoon mend stays). `CompanionWork.MysticCraftPick` (keep slider in Bag), `BattleState.SupportHeal` (heals the lower of hunter/Familiar each round). 100/100.

## 2026-10-29 — Phase 6 (slice 4: elemental oils)
- Same branch/PR as gear skills (#3). `ConsumableEffect.CoatWeapon`: the item's element becomes the hunter's attack element for `effectAmount` rounds (`BattleState.Coating`, `AttackElement`); defense keeps the weapon element. Battle title shows the oil while it lasts.
- `BattleAI.BestOil`: Auto coats with the oil that beats the current attack element against the monster (after heals). Semi-auto/Manual = player.
- New via sample data (`DataVersion` 4): Gale Oil (Wind, vs eel), Grit Oil (Earth, vs gull), Burning Pitch (Fire, vs crab), crafted at the workbench.
- Companion consumables moved to Phase 7. Rules tests outside Unity: 80/80 (4 new). Unity run pending.
- Ed's playtest: the workbench list got long. Workbench now has tabs (Gear, Potions, Oils, Raft, Dismantle) with a one-line hint each; `CraftRules.CategoryOf` sorts recipes by output type. 82/82 outside Unity.
- Ed played gear skills, oils and tabs: OK. Tabs now in two rows so Dismantle isn't cut off. Generated data assets + Hook scene from Ed's PC committed. PR #3 merged → **Phase 6 done**. Next: Ed picks Idea Round 3 or Phase 7.

## 2026-10-29 — Phase 6 (slice 3: gear skills)
- PR #2 (consumables + Settings) merged. Owner ideas U-19 battle effects, U-20 audio, U-21 fog/time of day logged.
- Branch `phase/6-gear-skills`: `GearPassive` / `GearActive` on items, `GearSkillRules` (slots: Rare 1 passive, Epic 1+1, Legendary 2+2, Mythic 3+2; Common/Uncommon element only), `Equipment.Passives/Actives`, `BattleAction.Skill` + `BattleState.PlaySkill`.
- Passives: Leech (heal 20% of damage dealt), Thorns (25% back), Second Wind (+1 stamina/round), Bulwark (raft −20%). Actives: Tidal Strike (×2.2, 6 st), Shell Guard (raft safe this round, 4 st). Auto uses them; Semi-auto waits for the player when one is ready.
- New gear via Create Sample Data: Shell Maul (Rare, Thorns), Tidecaller Lance (Epic, Leech + Tidal Strike), Shellback Carapace (Epic, Bulwark + Shell Guard), all from Shellback Plates.
- Rules tests outside Unity: 76/76 (14 new). Unity run pending.
- Ed's playtest: refine (+8 spear, +6 buckler) and Settings work. But Build Hook Scene had never been re-run, so no new items/recipes/gear power on his PC. New `AutoRebuild` (editor): after a recompile it runs Build Hook Scene once per `DataVersion` bump (not in play mode or batch mode). Bump `AutoRebuild.DataVersion` when sample data or the scene builder changes.

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
