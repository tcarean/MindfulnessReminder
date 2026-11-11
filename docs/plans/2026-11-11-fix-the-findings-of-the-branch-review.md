# Fix the findings of the branch review

Saved, once approved, as `docs/plans/2026-11-11-fix-the-findings-of-the-branch-review.md`.
This continues on the same branch, `claude/Fix-the-findings-of-the-README-review`, in its
worktree.

## Context

A subagent reviewed `main...HEAD` (`63acc30` `-Build` and `39b692d` the README fixes) and found
six problems. I confirmed the two medium ones myself. On 2026-11-11 the owner said "Da, toate":
fix all six before the merge.

1. **The in-use check runs late.** In a full release it runs only inside step 5, after
   `verify -All` and the CHANGELOG edit. That case is now easy to hit: refresh the tray copy
   with `-Build`, then release the `<Version>` that is already set. The message also says
   `Directory.Build.props` changed when it did not. In `-Build` the check comes only after
   verify.
2. **`<RuntimeIdentifiers>win-x64` downloads unused runtime packs.** Measured on 2026-11-11:
   the App's `project.assets.json` lists three runtime packs at 8.0.34 under
   `downloadDependencies`:
   - `Microsoft.AspNetCore.App.Runtime.win-x64` (11.6 MB);
   - `Microsoft.NETCore.App.Runtime.win-x64` (34.5 MB);
   - `Microsoft.WindowsDesktop.App.Runtime.win-x64` (35.1 MB).

   That is 81.2 MB on every restore without a cache, CI's included, and a framework-dependent
   publish uses none of it. The SDK's `Microsoft.NET.RuntimeIdentifierInference.targets:196`
   sets `DefaultAppHostRuntimeIdentifier` to `win-x64` when `PlatformTarget` is `x64`, with no
   runtime declared.
3. **The in-use check misses some processes.** It reads `Process.Path`, which is empty for an
   elevated process and for a 64-bit process seen from 32-bit Windows PowerShell. In those
   cases the folder is emptied anyway.
4. **ADR 0080 changed ADR 0034's `.Tests` condition** without recording that it supersedes it.
5. **`release.json` names the project after the checkout folder**, so a build in a worktree
   shipped `Fix-the-findings-of-the-README-review` inside the zip.
6. **The README does not list git**, which `-Build` needs.

## Changes (tests first, in `ReleaseTests.cs`)

1. **The in-use check moves forward.**
   - A full release checks `artifacts/<version>/` with the step-1 checks, before the tag check
     and `-WhatIf`. A refusal there changes nothing.
   - `-Build` checks before verify, and in `-WhatIf` too.
   - The check inside `Publish-Release` stays as a second line.
   - Its message after the version edits names only the files actually changed: just
     `CHANGELOG.md` when `<Version>` was already the release's.
   - Tests:
     - a full release with the folder in use is refused before verify, with nothing changed;
     - `-Build` likewise;
     - a collision in a full release of the version already in the props says
       "CHANGELOG.md is already changed".
2. **PlatformTarget instead of RuntimeIdentifiers.**
   - `<PlatformTarget>x64</PlatformTarget>` goes in `MindfulnessReminder.App.csproj`.
   - The `RuntimeIdentifiers` group is removed from `Directory.Build.props`.
   - `process.json` `args` goes back to `[]`.
   - `dotnet restore` returns both lock files to main's bytes.
   - ADR 0080 is not on main yet, so it is rewritten:
     - the decision is `PlatformTarget` x64;
     - it supersedes ADR 0036 in part (the any-CPU publish), and ADR 0036's Status clause
       follows;
     - the rejected alternatives include `-r win-x64` with `RuntimeIdentifiers` (the measured
       81.2 MB) and dropping the label.
   - This removes finding 4: ADR 0034 is untouched.
   - The texts follow: the `process.json` `$comment`, release skill step 5, the `CLAUDE.md` line,
     and the CHANGELOG line.
3. **Locked files are also probed.** `Assert-FolderNotInUse` also tries to open every `.exe` and
   `.dll` under the folder for writing with no sharing. A mapped image refuses that whoever runs
   it, so elevated and 64-bit copies are caught too. The message names the processes it could
   see, or else the locked files. Test: `ping.exe` copied into the folder, with the release run
   by the 32-bit `SysWOW64` Windows PowerShell, is refused. Skipped where that shell is absent.
4. Finding 4 needs nothing further.
5. **The name comes from git.** `Get-ProjectName` takes the folder that holds git's common
   directory (`rev-parse --path-format=absolute --git-common-dir`, then its parent). In a linked
   worktree that is the main checkout's folder. In the main checkout, and through a junction,
   it is the same folder as now, so the existing tests keep their expectations. Outside git the
   folder is used, as now. Test: `-PublishOnly` in a linked worktree names `Proj`.
6. **git in the README.** The requirements line gains git. ADR 0078 (not on main) has "It needs
   git only where `verify.ps1` does" corrected to say it needs git, and records the early check
   and the probe.

## Verification

1. Run the new tests red against the current script, then all `ReleaseTests` green.
2. Run `release.ps1 -Build` in the worktree and check:
   - the exe is PE machine 8664, and so is `MindfulnessReminder.dll`;
   - the App's `project.assets.json` has no runtime pack under `downloadDependencies`;
   - both lock files have main's bytes;
   - `release.json` names `MindfulnessReminder`;
   - the zip's hash matches.
3. `check-decisions.ps1`, the gate, commit, then propose the merge.

## What happened (2026-11-11)

- Red first. All 5 new tests failed against the script as it was:
  - the worktree's name was the worktree folder;
  - the collision message named `Directory.Build.props` too;
  - the full release edited `CHANGELOG.md` before its refusal;
  - `-Build` ran verify first;
  - from 32-bit PowerShell the copy of `ping.exe` was not seen.
- Green: all 41 `ReleaseTests` passed in 2 min 15 s. The 32-bit test ran, since
  `SysWOW64\WindowsPowerShell\v1.0\powershell.exe` is present.
- After the switch to `PlatformTarget`, `dotnet restore` left the App's `project.assets.json`
  with no `downloadDependencies`, and both lock files with main's content.
- `release.ps1 -Build` in the worktree took 252 s and exited 0. Its verify ran with 0 warnings,
  321 app tests and 125 process tests, and passed in 247 s.
  - `MindfulnessReminder.exe` and `MindfulnessReminder.dll` are PE machine 8664, and
    `MindfulnessReminder.Core.dll` is 014C.
  - `release.json` names `MindfulnessReminder`, the main checkout's folder, although the build
    ran in the worktree.
  - The lock files do not differ from main, and the zip's SHA-256 matches its `.sha256`.
- `check-decisions.ps1`: 80 decisions.
- Deviations:
  - ADR 0080 was renamed to
    `0080-the-executable-is-x64-through-platformtarget-with-no-runtime-declared.md`, since its
    decision changed. It was not on main, so no reference had to follow.
  - ADR 0080's consequences say that an arm64 SDK downloads only the small x64 apphost pack.
    That is from the SDK's targets, not measured: there is no arm64 machine here.
