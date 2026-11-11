# 0078. The README builds the application with release.ps1 -Build, and no publish empties a folder a program runs from

Date: 2026-11-11
Status: accepted 2026-11-11
Supersedes: 0076 in part (the README's line)

## Context

ADR 0076 made the full release, `scripts/release.ps1 -Version X.Y.Z`, the README's one way to
build the application. On 2026-11-11 the review of that branch confirmed that the line fails
the people the README is written for (ADR 0077). Right after any release commit `[Unreleased]`
is empty, and the script refuses ("[Unreleased] is empty; nothing to release."). It also
refuses on a tag (a detached HEAD), in a linked worktree and below the version floor. A run
that succeeds leaves `Directory.Build.props` and `CHANGELOG.md` changed, with a version the
reader chose, so the next run ("Working tree is not clean") is refused, and so is a `git pull`
that touches `CHANGELOG.md`.

The review also confirmed that every publish empties `artifacts/<version>/`, even while the
application runs from it. It deletes files until a locked one stops it: predicted from the code,
`LICENSE` first, then the loaded DLL. Nothing said how the owner refreshes the tray copy between
releases. After the merge, the owner chose a `-Build` switch that runs `verify.ps1`, the plain
gate rather than `-All`, before it publishes.

## Decision

`scripts/release.ps1 -Build` is the README's one line. It reads `<Version>` from
`Directory.Build.props` and runs `scripts/verify.ps1`. When that is green, it publishes that
version as step 5 of a release does: `artifacts/<version>/`, `release.json`, and the zip with
its `.sha256`. It makes none of the release's checks and changes no file of the repository.

Before step 5 empties `artifacts/<version>/`, in every mode, the script looks for a process
that runs an executable from under that folder. If it finds one, it stops and deletes nothing.

The full release, `-Version X.Y.Z`, stays the owner's, through `/release`. The application
still runs from `artifacts/<version>/`, and the new copy is started by hand after **Exit** on
the old one (ADR 0076).

## Alternatives rejected

- **Keeping the full release as the line and documenting its conditions.** The line would still
  be refused after every release, and a run would still leave the reader's clone changed.
- **`verify.ps1 -All` before a build.** It takes about three minutes, for the tests of the
  scripts, which a build does not change. The owner chose the plain gate, which takes about
  25 s and still runs those tests when `scripts/` differs from main.
- **Stopping the running copy before a publish.** The owner ruled out a script that stops or
  starts the tray copy (ADR 0076). A refusal that names the copy leaves that step to the
  person.
- **A separate build folder, such as `artifacts/build/`.** The application runs from
  `artifacts/<version>/` (ADR 0076), and AstroDarkroom builds its local copy into that same
  folder, with `-PublishOnly`.

## Consequences

- The line works on any clone, branch, tag or worktree, as often as needed. It needs git only
  where `verify.ps1` does.
- A build of a version that is already released replaces that release's folder and zip in
  `artifacts/`. `release.json` records which commit it is and whether `[Unreleased]` had
  content.
- The owner refreshes the tray copy after a merge in three steps: **Exit**, `-Build` in the
  main checkout, then start `artifacts/<version>/MindfulnessReminder.exe` (CLAUDE.md and the
  skills).
- Looking for the process takes about 2 s (measured on 2026-11-11, under
  `$ErrorActionPreference = 'Stop'`). A process whose path the account may not read is not seen.
- Guards: `ReleaseTests`, in `Build_PublishesThePropsVersion_AfterVerify_AndChangesNoFile`,
  `Build_StopsWhenVerifyFails`, `Build_WithAVersionOrAnotherMode_IsRefused`,
  `NeitherVersionNorBuild_IsRefused`, `Build_WithWhatIf_ChangesNothing` and
  `APublish_IntoAFolderAProgramRunsFrom_IsRefused`.
