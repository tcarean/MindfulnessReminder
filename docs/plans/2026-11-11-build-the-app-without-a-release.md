# Build the app without a release: `release.ps1 -Build`

Saved, once approved, as `docs/plans/2026-11-11-build-the-app-without-a-release.md`.
Branch `claude/Build-the-app-without-a-release`, worktree
`.claude/worktrees/Build-the-app-without-a-release`.

## Context

The review of 2026-11-11, of the branch merged as `1e32a41`, confirmed these problems with the
README's one build line, the full release `scripts/release.ps1 -Version X.Y.Z` (ADR 0076):

- It is refused in states a reader commonly has. After any release commit `[Unreleased]` is
  empty, so `release.ps1:362` stops ("[Unreleased] is empty; nothing to release."). It also
  stops on a checked-out tag (a detached HEAD), in a linked worktree, without `.git`, and
  below the version floor. The README states none of these conditions.
- A run that succeeds leaves `Directory.Build.props` and `CHANGELOG.md` modified, with a
  version the reader picked. The next run is then refused (`release.ps1:338-340`, "Working
  tree is not clean"), and so is a `git pull` that touches `CHANGELOG.md`.
- Every publish empties `artifacts/<version>/` (`release.ps1:193`), even while the app runs
  from it, and stops on the first locked file.
- Nothing says how the owner gets a merged build into the tray. `/release` step 7 does not
  hand over the step that switches to the new version (Exit, then start it).

The owner answered on 2026-11-11 (after the merge): add `release.ps1 -Build`, and run
`verify.ps1` (the plain gate, not `-All`) before it publishes.

## What `-Build` does

1. It takes the version from `<Version>` in `Directory.Build.props` (the `$VersionPattern`
   regex already there). It accepts no `-Version`, and it cannot be combined with
   `-PublishOnly` or `-NoPublish`.
2. It runs `scripts/verify.ps1`, the same way line 385 does but without `-All`. If verify is
   red, the script stops and publishes nothing.
3. It calls the existing `Publish-Release` for that version. This writes
   `artifacts/<version>/`, `release.json`, and the zip with its `.sha256`, exactly as
   `-PublishOnly` does.
4. It changes no tracked file and makes none of the release checks (branch, main checkout,
   clean tree, `[Unreleased]`, version floor, tag). So it works on any clone, a tag or a
   worktree, and it can run again after a `git pull`.
5. With `-WhatIf` it prints what it would do and stops.

`-Version` stops being `Mandatory`. Without `-Version` and without `-Build`, the script stops
with a message naming both.

**New check in `Publish-Release`, for all three modes.** Before it empties
`artifacts/<version>/`, it looks for a process running an executable from under that folder.
If it finds one, it stops, names the process, and says to close it (**Exit** in the tray
menu) and run again. Nothing is deleted. Measured on 2026-11-11: `Get-Process` with a `Path`
filter works under `$ErrorActionPreference = 'Stop'` with no errors, and takes about 2 s.

## Tests first (`tests/MindfulnessReminder.ProcessTests/ReleaseTests.cs`)

The existing fixture: props at 2.3.0, newest release 2.2.0, a stub `verify.ps1` that writes
`ran.txt`. Each test is run red against the current script first.

- `Build_PublishesThePropsVersion_AfterVerify_AndChangesNoFile`. On a branch, with
  `[Unreleased]` emptied and committed: exit 0, `ran.txt` exists,
  `artifacts/2.3.0/release.json` says 2.3.0, and `git status` is clean. A second run also
  exits 0.
- `Build_StopsWhenVerifyFails`. The stub exits 1: exit 1, and `artifacts/2.3.0/` does not
  exist.
- `Build_WithAVersionOrPublishOnly_IsRefused`. Exit 1, the message, and verify did not run.
- `NoVersionAndNoBuild_IsRefused`. Exit 1, and the message names `-Version` and `-Build`.
- `Build_WithWhatIf_ChangesNothing`. Exit 0, "WhatIf", no artifacts and no `ran.txt`.
- `APublish_IntoAFolderAProgramRunsFrom_IsRefused`, a `[WindowsFact]`. It copies
  `System32\PING.EXE` into `artifacts/2.3.0/`, starts it with `-n 60 127.0.0.1`, and runs
  `-PublishOnly`: exit 1, the message names it, and the folder's files are still there. The
  process is killed in `finally`.

## Script (`scripts/release.ps1`)

- `param`: `[string]$Version` without `Mandatory`, plus `[switch]$Build`.
- Argument checks first. The `-Build` path goes before the `-PublishOnly` path and reuses
  `$VersionPattern`, `Get-ScriptShell` and `Publish-Release`.
- New `Assert-FolderNotInUse`, called in `Publish-Release` before `Remove-Item`.
- Header: WHAT IT DOES, WHO CALLS IT (the README's line, the owner's copy between releases),
  OPTIONS, RESULT.

## Docs

- `README.md` "Build and run": the line becomes `... scripts/release.ps1 -Build`. It runs the
  tests, then builds the version the app's About shows into `artifacts\X.Y.Z\`, with the zip
  beside it. It changes no file, so it can run again after a `git pull`. It stops while a copy
  runs from that folder, so **Exit** first. Then start the new copy, and the startup entry
  moves to it. "On `main`" and the sentence about the release's steps go.
- **ADR 0078**: the README builds with `-Build`, and a publish refuses while a program runs
  from its folder. It supersedes ADR 0076 in part (the README's line), and ADR 0076's Status
  gains "; the README's line superseded by 0078". ADR 0076 gets a
  `(Corrected 2026-11-11: ...)` note beside its `git clean -x` sentence: the command that
  deletes the folder is `git clean -fdx`.
- `CLAUDE.md`:
  - Commands gains the `-Build` line (verify, publish and zip the current version, change no
    file).
  - The Release line loses "the one way the README builds the app".
  - Workflow says how the owner's copy is refreshed after a merge: **Exit**, then `-Build` in
    the main checkout, then start `artifacts/<version>/MindfulnessReminder.exe`.
- `.claude/skills/release/SKILL.md`: step 5 says `-Build` is the README's line and that a
  publish refuses while a program runs from its folder. Step 7 hands the owner the switch:
  **Exit**, then start `artifacts/X.Y.Z/MindfulnessReminder.exe`.
- `.claude/skills/wrap-up/SKILL.md` step 9 and `.claude/skills/new-feature/SKILL.md` step 5:
  "their own copy after the merge" gains how that copy is made (Exit, `-Build`, start).
- `CHANGELOG.md` `[Unreleased]`, `Internal`: `-Build`, and the refusal while a program runs
  from the folder.

Out of scope here, left from the review as their own work: Space closing the reminder in the
README's text, the `--detect` example, the pause claims, the `CLAUDE.md` rule's exceptions,
the stale ADRs 0059, 0024 and 0045, `win-x64`, and .NET 8 and 9 support.

## Verification

1. Paste the new tests red before the script change, then green.
2. In the worktree: `release.ps1 -Build -WhatIf`, then `release.ps1 -Build` for real. It
   writes only the worktree's ignored `artifacts/`. Check that `git status` is clean, and run
   it a second time.
3. `scripts/verify.ps1`, which runs the process tests because `scripts/` changes (about three
   minutes).
4. `/wrap-up`: commit on the branch, read the diff back, propose the merge.

## What happened (2026-11-11)

- Red first. Against the unchanged script, all 8 new test cases failed (the Theory counts as
  three). `-Build` was an unknown parameter, the message for a missing version named neither
  switch, and the copy of `ping.exe` did not stop the publish. With closed standard input, the
  prompt for the old mandatory `-Version` failed at once instead of waiting.
- Green: all 36 `ReleaseTests` passed, the 28 existing ones and the 8 new ones, in 3 min 2 s.
- Measured in the worktree. `release.ps1 -Build -WhatIf` printed its plan and wrote nothing.
  The first `release.ps1 -Build` took 255 s and exited 0. Its verify ran 321 app tests and 120
  process tests (the process tests because `scripts/` differs from main) and passed in 249 s.
  It then published `artifacts/1.5.0/` (9 files) and the zip. Afterwards `git status` listed
  only this task's own edits, with `Directory.Build.props` untouched. The second run took 199 s
  and exited 0, and the zip's SHA-256 matched its `.sha256`
  (`2ecd25d70c67d9758531cbaaeef994425716b332d443258d3df60d877eccc569`).
- Deviations:
  - `CHANGELOG.md`: the `[Unreleased]` line from the previous task said the release is the
    README's one way to build. That is no longer true, so it was rewritten, and the `-Build`
    line was added beside it.
  - The release skill: the README's `-Build` line is named in step 7, next to the handover of
    the switch to the new version, rather than in step 5. Step 5 names the refusal while a
    program runs from the folder.
- `check-design.ps1 -Since main` named `code-map.md`, for `ReleaseTests.cs`. Its line still
  holds ("`release.ps1` and the publish merge it uses").
