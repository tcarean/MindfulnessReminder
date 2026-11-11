# 0076. The README builds with the full release, into artifacts/X.Y.Z/ with a zip beside it, and the application runs from there

Date: 2026-11-11
Status: accepted 2026-11-11; the README's line superseded by 0078

## Context

Until 2026-11-11 the README's "Build and run" showed `dotnet build` and `dotnet test`, and its
"Deployment note" a hand `dotnet publish` into `%LOCALAPPDATA%\MindfulnessReminder`, the folder
that also holds the decision log. On 2026-11-11 the owner asked that nothing be built by hand:
the README gives one line, the release script, and the script changes if it does not do all
that is needed. Measured that day: the Run entry was
`"E:\Photografie\Code\MindfulnessReminder\artifacts\1.5.0\MindfulnessReminder.exe"`, a file gone
since the project moved to `D:\Tudor\CSharpCode\` (ADR 0074), and the last line of `log.txt` was
from 2026-11-10 15:24:23. The copy that ran was the release folder itself, not a copy published
elsewhere. `artifacts\1.5.0\` holds 9 files, 0.37 MB, and its `MindfulnessReminder.exe` is an
x64 executable (PE machine 8664). AstroDarkroom, a sibling project with the same release
script, releases into `artifacts/X.Y.Z/` and writes a zip of that folder with its SHA-256
(`release.zip`, ADR 0071).

## Decision

The README's one way to build the application is the full release,
`scripts/release.ps1 -Version X.Y.Z`, which runs every test before it changes anything and
publishes into `artifacts/X.Y.Z/`; the application runs from there. `process.json` sets
`release.zip` to `MindfulnessReminder-{version}-win-x64.zip`, so a release also writes that zip
of the folder, with its `.sha256`, into `artifacts/`. A release starts and stops nothing: the
new version is started by hand once the old copy has exited, and that start moves the Windows
startup entry to it (R-006). The script is unchanged.

## Alternatives rejected

- **An `-Install` switch that copies the publish into
  `%LOCALAPPDATA%\Programs\MindfulnessReminder`, stops the copy in the tray and starts the new
  one.** The owner chose to keep the release in `artifacts/`, as AstroDarkroom does.
- **Installing at the end of every release, or from `/release`.** The same answer: a release
  leaves the running copy alone.
- **`-PublishOnly` as the README's line.** It runs from any branch with no checks, and also
  without the tests, which the owner wants run first.
- **A hand `dotnet publish` into `%LOCALAPPDATA%\MindfulnessReminder`, as the deployment note
  said.** Nothing is built by hand when a script does it, and that folder is the log's.
- **No zip.** The owner chose the zip, as in AstroDarkroom: one file of under a megabyte per
  release, in the ignored `artifacts/`.

## Consequences

- The README's line works only where a release can be cut: the main checkout, on `main`, with a
  clean tree, something under `[Unreleased]` and a version above the newest released one (ADR
  0029). Anywhere else the script refuses before it changes anything.
- Each release is a new folder, so the startup entry follows a new version only once that
  version is started by hand, which the README says. A copy started while another runs exits at
  once (R-002), so the old one is closed first.
- `artifacts/` is ignored by git: a `git clean -x` in the main checkout deletes the copy that
  runs and leaves the startup entry pointing at nothing. (Corrected 2026-11-11: `git clean -x`
  alone deletes nothing. Without `-f` it refuses to run, and without `-d` it leaves an
  untracked folder such as `artifacts/` in place. The command that deletes the copy is
  `git clean -fdx`.)
- Guards: `ReleaseTests` (the zip, its entries and its `.sha256` in
  `PublishOnly_WritesReleaseJsonAndTheZip_FromAnyBranch`; the refusals before anything
  changes). Nothing checks the README's line.
