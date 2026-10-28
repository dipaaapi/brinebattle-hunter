# BrineBattle Hunter — Tooling

*Which tools Claude Code uses on this project, and when to add more. Rule: add a tool when a phase needs it, not before.*

## Skills (`.claude/skills/`)

| Skill | Status | Purpose |
|---|---|---|
| `roadmap` | ✅ ready | Keeps ROADMAP / DEVLOG / GDD in sync; tells a new session where to continue |
| `unity-verify` | ✅ tested 2026-10-28 | Headless compile + EditMode tests via batchmode |
| `codemap` | ⬜ Phase 1+ | Map of scripts and what each exports, so sessions read only what they need. Add once there are ~15+ scripts |
| `content` | ⬜ Phase 1–2 | Recipe + cross-check for adding a monster / item / element (ScriptableObject fields, loot refs) |
| `balance` | ⬜ Phase 3 | Headless battle sim on the real C# rules (win rate, turns to kill per rarity) |
| `blender-export` | ⬜ Phase 9 | Blender CLI export to FBX with `_col` mesh checks |

## MCP servers (candidates — verify the current project page before installing)

| Server | When | Why |
|---|---|---|
| **Unity MCP** (e.g. `CoplayDev/unity-mcp`, `CoderGamester/mcp-unity`) | Phase 2+ (scenes, prefabs) | Lets Claude create GameObjects, read the console and run tests inside the open Editor. Not needed for Phase 0–1, which is pure C# + data |
| **Blender MCP** (`ahujasid/blender-mcp`) | Phase 9 | Drive Blender for placeholder/final models and collision meshes |
| **GitHub** | not needed | `gh` CLI already covers PRs and issues |

Note: MCP tools add to every session's context, so enable each only for the phases that use it.

## Artifacts (claude.ai pages)

| Artifact | When | Why |
|---|---|---|
| Roadmap / status page | optional, any time | Shareable view of the phases for collaborators |
| Element & type-chart viewer | Phase 1 | Check the 10-element chart visually |
| Balance report | Phase 3 | Charts from the `balance` sim |

## Git
- Git LFS for binary art and audio (see `.gitattributes`, Phase 0).
- Unity's own `.gitignore` template (Library/, Temp/, Logs/, UserSettings/, Builds/).
