# CLAUDE.md

## Start and end of every session

1. **Start:** read the "Where we are" box at the top of `docs/ROADMAP.md`, then the latest entry in `docs/DEVLOG.md`. Don't re-read the whole GDD; open only the section the task needs (`docs/GAME_DESIGN.md` §n).
2. **End:** use the `/roadmap` skill — tick finished tasks, update the "Where we are" box, add a DEVLOG entry, and put new ideas in `docs/ideas/` (owner's ideas in `USER_IDEAS.md`, Claude's in `CLAUDE_IDEAS.md`; never mix).

## Project

BrineBattle Hunter: top-down, turn-based survival-builder × alchemy monster-hunting game on an endless ocean. Unity (URP), C#. Chibi 2.5D art from Blender, added in Phase 9; until then placeholder primitives only. Design: `docs/GAME_DESIGN.md`. Phases: `docs/ROADMAP.md`.

## Working rules

- Work only on the current phase. Anything else goes to `docs/ideas/`, not into code. Claude may suggest ideas, but only the owner accepts them (`COMPARISON.md`).
- Branch per phase: `phase/<n>-<name>`, PR into `main`. Conventional Commit prefixes (`feat:`, `fix:`, `docs:`, `chore:`).
- The Unity Editor can't be driven from here. After any C# change run `/unity-verify` (headless compile + EditMode tests). If Unity is open on the project, ask the user to close it or to run the tests in the Test Runner.
- Never hand-write files Unity generates (`ProjectSettings/*`, `Packages/packages-lock.json`, `*.meta` for existing assets, `Library/`). New `.cs` files are fine; Unity creates their `.meta` on next import — commit those after.
- Game data lives in ScriptableObjects under `Assets/_Project/Data`, not hardcoded in scripts.
- Gameplay rules (element chart, state roll, damage, loot, salvage) are plain C# classes with no `MonoBehaviour`, so EditMode tests can cover them.
- When a design decision is made, update `docs/GAME_DESIGN.md` in the same commit and remove it from Open Questions.

## Layout

```
Assets/_Project/
  Scripts/Runtime   BrineBattle.Runtime.asmdef
  Scripts/Editor    BrineBattle.Editor.asmdef
  Tests/EditMode    BrineBattle.Tests.asmdef
  Data/             ScriptableObject assets
  Art/ Prefabs/ Scenes/
docs/               GAME_DESIGN, ROADMAP, DEVLOG, TOOLING
docs/ideas/         USER_IDEAS, CLAUDE_IDEAS, COMPARISON
```
