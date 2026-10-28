---
name: roadmap
description: Keep BrineBattle Hunter's living docs in sync (ROADMAP status box and checklists, DEVLOG, idea backlog, GAME_DESIGN decisions). Use at the end of every session or finished task, when a new idea or design decision comes up, and at the start of a session to find where to continue.
---

# roadmap

Where each kind of information goes — write it once, in the right place:

| What happened | Update |
|---|---|
| Task finished | Tick it in `docs/ROADMAP.md`; add a line to today's `docs/DEVLOG.md` entry |
| Phase finished (its *Done when* is true) | Mark ✅, set the next phase 🟨, update the "Where we are" box |
| New idea from the owner | `docs/ideas/USER_IDEAS.md` as `U-<n>` |
| New idea from Claude | `docs/ideas/CLAUDE_IDEAS.md` as `C-<n>` — never into the owner's file |
| Ideas compared / decided (owner decides) | `docs/ideas/COMPARISON.md` table + decision log; mark status in the idea file |
| Idea accepted | Add it to a phase checklist in `ROADMAP.md` **and** to `GAME_DESIGN.md` |
| Design decision / open question answered | Edit the `GAME_DESIGN.md` section, remove it from §14 Open Questions, note it in DEVLOG |
| Unity version or tooling changed | "Where we are" box and `docs/TOOLING.md` |

## Rules
- Always refresh **Current phase**, **Next step** and **Last updated** in the "Where we are" box. The next step must be one concrete action.
- DEVLOG is newest first, one `## YYYY-MM-DD` heading per day, short bullets (what changed, what was decided, what's next).
- Bump the GDD version line (`v0.x`) when its content changes.
- Commit doc updates with the work they describe, prefix `docs:` when alone.

## Resuming
Read only the "Where we are" box and the latest DEVLOG entry, then the checklist of the current phase. That is enough to continue; don't read the whole GDD.
