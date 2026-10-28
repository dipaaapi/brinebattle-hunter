---
name: unity-verify
description: Compile BrineBattle Hunter and run its EditMode tests headless with Unity batchmode, then report errors and failing tests. Use after every C# change, before committing, and when the user reports a compile error.
---

# unity-verify

Unity is installed through Unity Hub. The editor path is
`C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`; `<version>` is in the "Where we are" box of `docs/ROADMAP.md` (and in `ProjectSettings/ProjectVersion.txt`).

## Run (PowerShell, from the project root)

```powershell
$v = (Select-String -Path ProjectSettings/ProjectVersion.txt -Pattern 'm_EditorVersion: (.+)').Matches[0].Groups[1].Value
& "C:\Program Files\Unity\Hub\Editor\$v\Editor\Unity.exe" -batchmode -nographics -projectPath . `
  -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/verify.log
"exit $LASTEXITCODE"
```

Run it in the background; the first run imports the whole project and can take several minutes.

## Read the result
- Exit code: `0` all passed · `2` some tests failed · `3`/other: run error (usually compile errors).
- Compile errors: search `Logs/verify.log` for `error CS`. Report file:line and message only.
- Failing tests: in `TestResults/editmode.xml`, find `<test-case ... result="Failed">` and its `<message>`.
- Don't paste the whole log; it is huge.

## Gotchas
- If the project is open in the Unity Editor, batchmode refuses ("another Unity instance is running"). Ask the user to close Unity, or to run Window → General → Test Runner → EditMode themselves.
- `-runTests` quits on its own; don't add `-quit`.
- `TestResults/` and `Logs/` are git-ignored.
