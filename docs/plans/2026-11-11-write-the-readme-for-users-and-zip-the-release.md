# Write the README for the people who use the app, and zip the release

## Context

The owner reviewed `README.md` on 2026-11-11 and asked for:

1. At the top, what the app does now, with no version history. Below that, "Build and run" and
   "Deployment note" rewritten briefly around the release script: nothing is built by hand,
   one line.
2. Text that does not read as AI-written: no mention of the owner or the author, and no
   artificial phrasing.
3. No information that means nothing to a real user. The closing sentence "The year and the
   author are a deliberate choice..." goes.
4. The section "Development: the gate, the documentation and Claude Code" goes, and Claude is
   not mentioned anywhere in the README.
5. The question ".NET SDK 9.0.318 or a later 9.0.3xx? I thought I had set net8".
6. The sections "Design decisions", "The decision log", "Layout" and "Versioning" go.
7. The sentence "Four things hold this together..." and its four bullets go.

Answers in chat, after the options were put:

- The one line is the full release, `scripts/release.ps1 -Version X.Y.Z`. No install switch.
- The app lives in `artifacts\X.Y.Z\`, and the release goes there, as in AstroDarkroom. No
  stable folder outside the repository, and nothing that stops or starts the tray copy.
- Tests run before the build. A full release already runs `verify.ps1 -All` first.
- The release also writes the zip and its `.sha256`, as AstroDarkroom does
  (`release.zip` in its `kit.json`). The script already supports this (ADR 0071); only
  `process.json` changes.

Measured on 2026-11-11:

- The Run entry is `"E:\Photografie\Code\MindfulnessReminder\artifacts\1.5.0\MindfulnessReminder.exe"`,
  and that file no longer exists: the project moved to `D:\Tudor\CSharpCode\`. The last line
  of `log.txt` is from 2026-11-10 15:24:23, and no copy is running. Starting the new
  `artifacts\1.5.0\` copy once repairs the entry (R-006). That is for the owner to do; this
  task does not do it.
- `artifacts\1.5.0\` holds 9 files, 0.37 MB. `MindfulnessReminder.exe` is machine type 8664
  (x64), so the zip name `-win-x64` is true. The runtimeconfig asks for
  `Microsoft.NETCore.App` 8.0.0 and `Microsoft.WindowsDesktop.App` 8.0.0, which is the .NET 8
  Desktop Runtime.
- The SDK question: all four projects target `net8.0` or `net8.0-windows`, which is what the
  owner set. `global.json` pins the SDK, the build tool, at `9.0.318` with `latestPatch`
  (since 2026-10-09, ADR 0037). That accepts 9.0.318 or any later 9.0.3xx, and 9.0.320 is the
  one installed. An SDK 9 builds .NET 8 apps. So the sentence was right; for users, the README
  now says SDK 9 to build and the .NET 8 Desktop Runtime to run.

## Branch and worktree

`git worktree add .claude/worktrees/Write-the-README-for-users-and-zip-the-release -b claude/Write-the-README-for-users-and-zip-the-release main`.
All edits go there. This plan is saved as
`docs/plans/2026-11-11-write-the-readme-for-users-and-zip-the-release.md`.

## Changes

### `README.md`, rewritten

Order: title, what it does, **Build and run**, **Using it** (the old "Behaviour"),
**Two reminders, two messages**, **Periodic reminders**, **Pausing for calls and full
screen** (with "How it detects them", "What it will and will not catch", "Checking it on
your machine"), **Licence**.

Top paragraph, in substance: a small break reminder for Windows that sits in the
notification area. A window saying *Ground yourself* appears on top of everything at every
unlock and, while you work, every 30 minutes. It waits during a call or a full-screen app.
Either reminder can be switched off or given its own message, and the interval set, in
**Settings...**.

**Build and run** (replaces "Build and run" and "Deployment note"):

- Needs the .NET SDK 9.0.318 or a later 9.0.3xx (`global.json`; it builds the .NET 8 app) and,
  to run it, the .NET 8 Desktop Runtime.
- The one line, from the repository folder on `main`:
  `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1 -Version X.Y.Z`
  (`X.Y.Z` is the new version). It runs the tests, then puts the app in `artifacts\X.Y.Z\`,
  with `MindfulnessReminder-X.Y.Z-win-x64.zip` and its `.sha256` beside the folder.
- Start `artifacts\X.Y.Z\MindfulnessReminder.exe`. The first run adds the app to Windows
  startup. Only one copy runs at a time, so **Exit** the old one before starting a new
  version. Starting it moves the startup entry to it, and the same goes after a move.

Rules for the rest:

- No history. Drop "since 1.x", "Added in 1.x", "used to", "no longer", and the 2026-10-26
  runtime anecdote.
- No version numbers in examples: the tooltip reads `Mindfulness Reminder X.Y.Z - next in 12 min`,
  like the paths. The current examples say 1.4.0, which already went stale at 1.5.0.
- No mention of the owner, the author, Claude, ADRs, R-IDs, test names or class and API names
  (`DispatcherTimer`, `SHQueryUserNotificationState`, `QUNS_*`). Detection is described in
  plain words: the record behind Windows' own microphone indicator, and the "is the user
  busy" question Windows asks before a notification.
- No design rationale. The "Four things about it are deliberate" list becomes plain facts a
  user can see: sleep does not stretch the interval, a lock stops the countdown and an unlock
  starts it over, Enter does not close the reminder. The `QUNS_NOT_PRESENT` paragraph goes;
  `docs/design/pausing.md` already has it.
- Plain sentences. No em-dash asides, no bold lead-in on every bullet, no "deliberately", "on
  purpose", "honest", "happily", "the whole point".
- `--preview` and `--detect` run `artifacts\X.Y.Z\MindfulnessReminder.exe`, not the `bin\`
  folder of a hand build.
- With "The decision log" section gone, one bullet under **Using it** keeps what a user
  needs: where `log.txt` is, what it records, that it stays under half a megabyte and stays on
  the machine.
- **Licence** keeps the notice, `Copyright (C) 2026 Tudor Carean` included. That line is the
  GPL copyright line, not a mention of "the owner". The warranty sentence takes the GNU
  wording ("WITHOUT ANY WARRANTY; without even..."). The sentence "Four things...", its four
  bullets and the closing sentence all go. They are already in
  `docs/design/versioning-and-licence.md`.
- The removed sections are already covered for whoever maintains the code. "Design
  decisions" is in the ADRs, "Layout" in `CLAUDE.md` and `code-map.md`, "Versioning" in
  `versioning-and-licence.md` and ADR 0045, "The decision log" in R-110 to R-114 and ADR 0073,
  and "Development" in `CLAUDE.md` and `.claude/rules/process.md`.

### `process.json`

- `release.zip`: `"MindfulnessReminder-{version}-win-x64.zip"`.
- `$comment`: drop "as the README's deployment note does" and cite ADR 0076 for the zip.

### What pointed at the removed sections

- `.claude/skills/release/SKILL.md`, step 5: the "Deployment note" sentence becomes "the
  README's 'Build and run' says the app runs from `artifacts/X.Y.Z/`, and that starting the
  new version after **Exit** on the old one moves the startup entry; the release starts
  nothing". Step 7: "Build and run" is this release in one line and names no version.
- `.claude/skills/new-feature/SKILL.md`, step 8: the README's "Using it" list and the
  feature's section; no "Design decisions" table, since a design choice goes in
  `docs/decisions/`.
- `Directory.Build.props`: the comment points to `docs/design/versioning-and-licence.md`
  instead of "Versioning" in `README.md`.
- `CLAUDE.md`: the rule "User documentation lives in `README.md`" adds that it is for the
  people who use the app, with no process, no design rationale, no history and no Claude
  (ADR 0077). The release command and the release workflow line add the zip (ADR 0076).

### Decision records (next numbers, 2026-11-11)

- **0076**: the application is built only by a full release, which runs the tests first,
  into `artifacts/X.Y.Z/` with a zip and its SHA-256 beside it, as AstroDarkroom does, and
  runs from there. Rejected: a `-Install` switch into
  `%LOCALAPPDATA%\Programs\MindfulnessReminder` that restarts the tray copy, installing at the
  end of every release, a hand `dotnet publish`, and `-PublishOnly` as the README line (it
  skips the tests). Consequences: a fresh clone builds only on `main` with something under
  `[Unreleased]`. `git clean -x` deletes the running copy, since `artifacts/` is ignored.
  Each new version is started once by hand.
- **0077**: the README is for the people who use the application, as listed above.
  Rejected: a short "For developers" pointer like AstroDarkroom's (it would name `CLAUDE.md`,
  and the owner wants no development section). It extends ADR 0068 and supersedes nothing.
- Dated notes, per ADR 0024 (the decisions stand, a supporting line no longer holds):
  `(Restated 2026-11-11: ...)` after the Decision of **0036** ("the one the README's
  'Deployment note' documents"), and next to the consequence of **0062** that says "the
  README's deployment note says to publish somewhere stable".

### `CHANGELOG.md`

One `Internal` line under `[Unreleased]`: a release also writes the zip with its SHA-256
(ADR 0076), and the README is for the people who use the app (ADR 0077).

No script changes, so no process test is added or run at commit.

## Verification

1. In the worktree, run `scripts/release.ps1 -Version 1.5.0 -PublishOnly`. It writes only the
   worktree's own `artifacts/`, which git ignores. Paste the zip line, list the zip's
   entries, and compare `Get-FileHash` with the `.sha256`.
2. Grep `README.md` for `owner`, `author`, `Claude`, `ADR`, `R-0`, `since`, `deliberate`,
   `1.4.0`, `—`, ` -- ` and `#` links. Grep the repo, outside `docs/plans/` and the ADR
   history, for "Deployment note", "Design decisions", and "Versioning" in `README.md`.
3. Read the whole README back and check every statement against the requirement it comes
   from (R-001 to R-006, R-060, R-070 to R-076, R-080 to R-082, R-100 to R-110).
4. Run `scripts/verify.ps1`, the gate, and `scripts/check-design.ps1 -Since main`.
5. Run `/wrap-up`: commit on the branch, read the diff back, propose the merge.

## What happened (2026-11-11)

- Measured: `release.ps1 -Version 1.5.0 -PublishOnly` in the worktree wrote
  `artifacts/MindfulnessReminder-1.5.0-win-x64.zip`, 192,458 bytes. It has 9 entries: `LICENSE`,
  the two DLLs and their PDBs, the `.exe`, `.deps.json`, `.runtimeconfig.json` and
  `release.json`. Its `.sha256` holds the hash `Get-FileHash` computes,
  `b006d9f7e2c77641e5ddba6542fa39d7bc93eb58630993fbdfe83119d796629f`. The component in
  `release.json` is named after the worktree's folder, as `Get-ProjectName` does; in the main
  checkout it is `MindfulnessReminder`.
- Deviation: the grep of step 2 found one more pointer to the removed "Versioning" section, in
  the header comment of `src/MindfulnessReminder.Core/Version.cs`. It now points to
  `docs/design/versioning-and-licence.md`. `check-design.ps1 -Since main` then names
  `code-map.md` and `versioning-and-licence.md`; both still hold, since only a comment
  changed.
- The gate passed: 321 tests in 93 s, with the process tests left out because nothing under
  `scripts/` changed.
