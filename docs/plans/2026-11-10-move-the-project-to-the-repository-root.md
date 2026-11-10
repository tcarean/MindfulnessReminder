# 2026-11-10 Move the project to the repository root and merge the kit into it

Status: awaiting the owner's approval. Written for an assistant that has not seen the
conversation it came from: everything needed is in this file. Follow the steps in order, run
the check at the end of each step, and stop where a check says stop.

This file starts at `MindfulnessReminder\docs\plans\` and, after step 3, is at `docs\plans\`.

## Goal

The repository `D:\Tudor\CSharpCode\MindfulnessReminder` (remote
`github.com/tcarean/MindfulnessReminder`, empty, no commit yet) holds two untracked folders
copied from the `photo` monorepo:

- `MindfulnessReminder\` - the application, a project made from the kit;
- `ClaudeCodeStarterKit\` - the kit: the source of the shared scripts, skills, agent and rules,
  plus a sample project (`Starter`), its own documentation and the files a project does not
  carry (git hooks, `install-hooks.ps1`, `GHID.md`, CI templates).

This repository will hold Mindfulness Reminder and nothing else. When this plan is done:

- everything of `MindfulnessReminder\` is at the repository root, at the same relative paths;
- the pieces of the kit the project needs once it is alone are at the root too;
- `ClaudeCodeStarterKit\` and `MindfulnessReminder\` no longer exist;
- `scripts/verify.ps1` is green at the root;
- everything is staged, nothing is committed: the first commit is the owner's.

## Facts measured on 2026-11-10 (do not re-derive)

- The gate is green where the project is now:
  `MindfulnessReminder\scripts\verify.ps1` ends with `Passed: 320` and `== OK in 30s`, and
  prints `ok 37 decisions` and `ok 9 design notes`.
- `scripts/`, `.claude/skills/`, `.claude/agents/` and `.claude/rules/kit/` are byte-identical
  in the two folders, except five files that exist only in the kit: `scripts/rename.ps1`,
  `scripts/install-hooks.ps1`, `scripts/hooks/pre-commit`, `scripts/hooks/pre-merge-commit`,
  `scripts/hooks/commit-msg`.
- `global.json`, `LICENSE` and `.config/dotnet-tools.json` are identical in the two folders.
- Every text file in both folders has LF line endings. `core.autocrlf` is `true` on this
  machine, so the root needs the kit's `.gitattributes` before anything is staged.
- The repository is on an unborn branch `master`; `core.hooksPath` is not set; the remote has
  no branch.
- No test and no script depends on the project's folder name or on its parent:
  `RepoInvariantTests`, `LicenseHeaderTests` and `VersionTests` find the project folder from
  their own file's path, three levels up.

## Decisions (the owner's, by approving this plan)

| # | Decision | Why |
| --- | --- | --- |
| D1 | `kit.json` gets `"kit": "."`: the project holds the kit's files and is their only user. | With `"."` the scripts skip the comparison with a kit (step 0 of the gate) and the Stop hook allows edits to `scripts/` and `.claude/` here. Deleting the key, which the kit's README suggests for a separate repository, would make the Stop hook refuse those edits and point at a kit this repository does not contain. |
| D2 | **No file in `scripts/`, `.claude/skills/`, `.claude/agents/` or `.claude/rules/kit/` is edited.** Files are only added (the four kit-only ones). What changes for this repository is said in `kit.json`, `CLAUDE.md` and a new ADR. | The scripts have tests only in the kit. Unchanged, they stay the tested bytes, and a later kit version can be copied over them. `process.md` itself says a project's deviations go in its `CLAUDE.md`, by rule name. |
| D3 | The kit's 36 decision records are copied unchanged to `docs/decisions/kit/`. They are not renumbered. | "the kit's ADR NNNN" is cited 82 times outside `docs/plans/`, 60 of them inside the shared folders (counted 2026-11-10). The numbers 0001 to 0036 collide with the project's own. `scripts/check-decisions.ps1` reads `docs/decisions/*.md` without recursion, so a subfolder is not checked and does not collide. |
| D4 | `GHID.md` goes to the root, with a dated note at its top. | Every kit file says "`GHID.md` in the kit folder"; with `"kit": "."` the kit folder is the root. The guide itself says a project that leaves `photo` takes a copy. |
| D5 | The git hooks are installed the kit's standalone way: `core.hooksPath = scripts/hooks`. | `scripts/install-hooks.ps1` detects that the project folder is the git root and does exactly this. |
| D6 | `.github/workflows/ci.yml` is copied as it is. | The remote is on GitHub and the file is the kit's CI for a standalone repository. It cannot be tried locally: its first run is the owner's first push. |
| D7 | The branch is `main`. | `scripts/release.ps1` and every rule assume `main`; the branch is unborn, so renaming it loses nothing. |
| D8 | A new ADR, 0038, records all of this and supersedes parts of ADR 0002, 0003 and 0006. | The kit's rule "Decisions". The part of ADR 0003 is forced: its Status cites "the kit's ADR 0035", which `check-decisions.ps1` refuses in a header once `kit` is `"."`. |
| D9 | Not brought along: the `Starter` sample and its tests (the tests of the scripts included), `rename.ps1`, `monorepo/`, `bitbucket-pipelines.yml`, `AI Prompt to Generate Starter Project.md`, and the kit's own `README.md`, `CHANGELOG.md`, `CLAUDE.md`, `kit.json`, `docs/plans/`, `docs/design/`, `docs/requirements.md`, `docs/skills.md`, `docs/validation.*`, `tests/testdata/`. | They describe or serve the kit as a product. They stay in the kit in `photo`, and in the backup of step 2. |

## Rules for this task

- Start the session in the repository root, `D:\Tudor\CSharpCode\MindfulnessReminder`. If the
  session's working folder is inside `MindfulnessReminder\` or `ClaudeCodeStarterKit\`, stop
  and ask the owner to restart it at the root: a folder that is a process's working folder
  cannot be moved or deleted on Windows.
- Commands are Windows PowerShell 5.1. No `&&`.
- **No worktree and no `claude/` branch**, unlike every other task: the repository has no
  commit, so there is nothing to branch from. Work in the root.
- **Do not commit, do not push, do not tag.** Staging (`git add`) is allowed.
- Do not touch `src/` or `tests/`. Do not change the version. Do not add a package.
- Do not edit: any file under `scripts/`, `.claude/skills/`, `.claude/agents/`,
  `.claude/rules/`; any file under `docs/plans/` except the "What deviated" section of this
  one; the body of any existing ADR (only the three Status lines step 5.8 names); the released
  sections of `CHANGELOG.md`; anything under `docs/decisions/kit/` after copying it.
- Never change a test, a script or a check to make the gate pass. If a check fails and the
  cause is not a typo in something this plan had you write, stop and report.
- If a command fails or a check does not give the expected result, stop and report. Do not
  improvise a way around it. The backup of step 2 is the way back.
- Old names (`photo`, `E:\Photografie\Code`, `../ClaudeCodeStarterKit`, `FlatCheck`,
  `.githooks/`) stay in the history files listed above. That is intended.

## Step 1 - Preconditions

Ask the owner to close Visual Studio and any running `MindfulnessReminder.exe` started from
this folder. Then, in the repository root:

```powershell
Set-Location 'D:\Tudor\CSharpCode\MindfulnessReminder'
git status --short
git log --oneline -1
Test-Path -LiteralPath .\.claude
powershell -NoProfile -ExecutionPolicy Bypass -File .\MindfulnessReminder\scripts\verify.ps1
```

Expected:

- `git status --short` prints exactly `?? ClaudeCodeStarterKit/` and `?? MindfulnessReminder/`
  (a `?? .claude/` line is also acceptable, see step 3);
- `git log` fails with "does not have any commits yet";
- `verify.ps1` ends with `== OK`, `Failed: 0`.

Stop if the repository already has commits, if other files exist at the root, or if the gate
is red: the plan assumes this starting point.

## Step 2 - Remove build output, then back up

Nothing here is under version control yet, so a deleted file cannot be restored by git.

```powershell
dotnet build-server shutdown
$junk = @(
    'MindfulnessReminder\.vs',
    'MindfulnessReminder\.verify-stamp',
    'MindfulnessReminder\src\MindfulnessReminder.App\bin',
    'MindfulnessReminder\src\MindfulnessReminder.App\obj',
    'MindfulnessReminder\src\MindfulnessReminder.Core\bin',
    'MindfulnessReminder\src\MindfulnessReminder.Core\obj',
    'MindfulnessReminder\tests\MindfulnessReminder.Tests\bin',
    'MindfulnessReminder\tests\MindfulnessReminder.Tests\obj'
)
foreach ($p in $junk) { if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Recurse -Force } }
```

(`bin/`, `obj/` and `.vs/` hold absolute paths of the old location and are rebuilt by the
gate.)

```powershell
$backup = Join-Path (Split-Path -Parent (Get-Location).Path) 'MindfulnessReminder-backup-2026-11-10'
if (Test-Path -LiteralPath $backup) { throw "backup folder already exists: $backup" }
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath .\MindfulnessReminder -Destination $backup -Recurse -Force
Copy-Item -LiteralPath .\ClaudeCodeStarterKit -Destination $backup -Recurse -Force
foreach ($n in 'MindfulnessReminder', 'ClaudeCodeStarterKit') {
    $a = @(Get-ChildItem -LiteralPath ".\$n" -Recurse -File -Force).Count
    $b = @(Get-ChildItem -LiteralPath (Join-Path $backup $n) -Recurse -File -Force).Count
    "$n source=$a backup=$b"
}
```

Check: for both folders `source` equals `backup`. Stop if not. Never delete the backup; the
owner does, after the first push.

## Step 3 - Move the project to the root

`.claude` is moved child by child, because a `.claude\` folder may already exist at the root
(Claude Code creates one when a permission is granted). Two things are left behind on purpose:
`.claude\settings.local.json` (grants for commands with `E:\Photografie\Code` paths, useless
here, and git-ignored) and `.claude\worktrees\` (empty).

```powershell
$app = '.\MindfulnessReminder'
if (-not (Test-Path -LiteralPath .\.claude)) { New-Item -ItemType Directory -Path .\.claude | Out-Null }
foreach ($n in 'agents', 'rules', 'skills', 'settings.json') {
    if (Test-Path -LiteralPath ".\.claude\$n") { throw ".claude\$n already exists at the root" }
    Move-Item -LiteralPath "$app\.claude\$n" -Destination ".\.claude\$n"
}
Get-ChildItem -LiteralPath $app -Force | Where-Object { $_.Name -ne '.claude' } | ForEach-Object {
    if (Test-Path -LiteralPath (Join-Path (Get-Location).Path $_.Name)) { throw "$($_.Name) already exists at the root" }
    Move-Item -LiteralPath $_.FullName -Destination (Get-Location).Path
}
Get-ChildItem -LiteralPath $app -Recurse -Force | ForEach-Object { $_.FullName }
```

Check: the last command lists only `MindfulnessReminder\.claude`,
`MindfulnessReminder\.claude\settings.local.json` and `MindfulnessReminder\.claude\worktrees`
(the last two may be absent). Stop if it lists anything else. Then:

```powershell
Remove-Item -LiteralPath $app -Recurse -Force
Get-ChildItem -LiteralPath . -Force -Name
```

Check: the root now holds `.claude`, `.config`, `.editorconfig`, `.git`, `.gitignore`,
`artifacts`, `CHANGELOG.md`, `CLAUDE.md`, `ClaudeCodeStarterKit`, `Directory.Build.props`,
`Directory.Packages.props`, `docs`, `global.json`, `kit.json`, `LICENSE`,
`MindfulnessReminder.sln`, `README.md`, `scripts`, `src`, `tests`. (`artifacts\` is a published
release, git-ignored; it stays.)

## Step 4 - Copy what the project needs from the kit

Copy, do not move: the kit folder stays whole until step 8.

```powershell
$kit = '.\ClaudeCodeStarterKit'
Copy-Item -LiteralPath "$kit\.gitattributes" -Destination .\.gitattributes
Copy-Item -LiteralPath "$kit\.gitignore" -Destination .\.gitignore -Force
Copy-Item -LiteralPath "$kit\GHID.md" -Destination .\GHID.md
New-Item -ItemType Directory -Path .\.github\workflows -Force | Out-Null
Copy-Item -LiteralPath "$kit\.github\workflows\ci.yml" -Destination .\.github\workflows\ci.yml
Copy-Item -LiteralPath "$kit\scripts\install-hooks.ps1" -Destination .\scripts\install-hooks.ps1
foreach ($h in 'pre-commit', 'pre-merge-commit', 'commit-msg') {
    Copy-Item -LiteralPath "$kit\scripts\hooks\$h" -Destination ".\scripts\hooks\$h"
}
New-Item -ItemType Directory -Path .\docs\decisions\kit | Out-Null
Get-ChildItem -LiteralPath "$kit\docs\decisions" -Filter '*.md' -File |
    Where-Object { $_.Name -ne '0000-template.md' } |
    Copy-Item -Destination .\docs\decisions\kit
```

Notes:

- `.gitignore` is **replaced** by the kit's. The project's own had one line the kit's lacks,
  `!src/MindfulnessReminder.App/`, which undid a `*.app` rule of `photo`'s root `.gitignore`;
  that rule does not exist here, so the line goes. The kit's file adds `.claude/worktrees/`,
  `*.suo`, `*.trx`, `*.secrets.json` and `.env`.
- `scripts\rename.ps1` is **not** copied: it renames the word `Starter` throughout a fresh
  copy of the kit and has no use here.

Check, which must print `kit decisions: 36` and no `DIFFERS` or `MISSING` line:

```powershell
"kit decisions: " + @(Get-ChildItem -LiteralPath .\docs\decisions\kit -File).Count
foreach ($folder in 'scripts', '.claude\skills', '.claude\agents', '.claude\rules\kit') {
    Get-ChildItem -LiteralPath (Join-Path $kit $folder) -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring((Resolve-Path -LiteralPath $kit).Path.Length + 1)
        if ($rel -eq 'scripts\rename.ps1') { return }
        if (-not (Test-Path -LiteralPath $rel)) { "MISSING $rel" }
        elseif ((Get-FileHash -LiteralPath $_.FullName).Hash -ne (Get-FileHash -LiteralPath $rel).Hash) { "DIFFERS $rel" }
    }
}
```

## Step 5 - Edit the project's own files

Use exact replacements. In every "replace" below, the old text is quoted exactly as it is in
the file; if it is not found, stop and report rather than guessing. Line ends are LF; keep
them.

### 5.1 `kit.json`

Replace the whole `"$comment"` value and the `"kit"` value. The other keys stay as they are.
The result:

```json
{
  "$comment": "What the scripts, skills and agent need to know about MindfulnessReminder. Since 2026-11-10 the project is the root of its own repository and holds the kit's files itself (ADR 0038): 'kit' is '.', so scripts/, .claude/skills/, .claude/agents/ and .claude/rules/kit/ are edited here, step 0 of scripts/verify.ps1 is skipped and scripts/sync.ps1 has nothing to copy; the kit's decisions are in docs/decisions/kit/ and its guide is GHID.md. The layout is the kit's default, src/ and tests/ (ADR 0002). There is no test data and no validation case (ADR 0005): the manifest lists nothing, testFilter is empty, and MINDFULNESS_TESTDATA_LARGE is the override kit-config.ps1 requires a name for, which nobody sets. The release publishes the application framework-dependent, as the README's deployment note does, so no runtime is declared (ADR 0002; the kit's ADR 0034 does not apply). See scripts/kit-config.ps1 for every key.",
  "solution": "MindfulnessReminder.sln",
  "testFilter": "",
  "codeFolders": ["src/"],
  "codeFiles": [],
  "testFolders": ["tests/"],
  "testData": {
    "folder": "tests/testdata/",
    "largeEnv": "MINDFULNESS_TESTDATA_LARGE"
  },
  "mutate": {
    "testProject": "tests/MindfulnessReminder.Tests/"
  },
  "release": {
    "publish": [
      {
        "project": "src/MindfulnessReminder.App/MindfulnessReminder.App.csproj",
        "output": "",
        "args": []
      }
    ]
  },
  "audit": {
    "alsoRead": ["README.md"]
  },
  "kit": "."
}
```

### 5.2 `.gitattributes`

One replacement, in the comment at the top (in the kit, "ADR 0014" was the kit's own number):

- old: `(matches .editorconfig; ADR 0014).`
- new: `(matches .editorconfig; the kit's ADR 0014).`

### 5.3 `.claude/settings.json`

One replacement inside the `"$comment"` string; nothing else in the file changes. Neither text
contains a double quote.

- old: `are the kit's, the same in every project: scripts/sync.ps1 copies them and step 0 of verify.ps1 compares them; the other 'allow' entries and this comment are MindfulnessReminder's (the kit's ADR 0028).`
- new: `came from the kit and stay as the kit wrote them (the kit's ADR 0028); since 2026-11-10 this repository holds the kit's files itself (kit.json's 'kit' is '.', ADR 0038), so nothing copies or compares them here; the other 'allow' entries and this comment are MindfulnessReminder's.`

Check that the file is still valid JSON (no output means valid):

```powershell
Get-Content -LiteralPath .\.claude\settings.json -Raw | ConvertFrom-Json | Out-Null
Get-Content -LiteralPath .\kit.json -Raw | ConvertFrom-Json | Out-Null
```

If the harness refuses the edit of `.claude/settings.json`, skip 5.3 and say so in the report;
nothing depends on it.

### 5.4 `CLAUDE.md`

Seven replacements.

**(a)** old:

```text
- Gate (what pre-commit, pre-merge-commit and the Claude commit hook run, about 20 s):
```

new:

```text
- Gate (what pre-commit, pre-merge-commit, the Claude commit hook and CI run, about 20 s):
```

**(b)** old:

```text
- Take the kit's shared folders and settings keys after a change there: `scripts/sync.ps1`
  (`-Check` only compares)
```

new:

```text
- Git hooks, once in every clone (sets `core.hooksPath` to `scripts/hooks`):
  `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-hooks.ps1`
```

**(c)** old:

```text
- `kit.json` - what the kit's scripts, skills and agent need to know about this project.
  `docs/skills.md` - this project's additions to the kit's skills. `scripts/`,
  `.claude/skills/`, `.claude/agents/`, `.claude/rules/kit/` and the process keys of
  `.claude/settings.json` are the kit's, the same here (ADR 0002; the kit's rule "Shared
  folders").
```

new:

```text
- `kit.json` - what the scripts, skills and agent need to know about this project; its `kit`
  is `"."`. `docs/skills.md` - this project's additions to the skills. `scripts/` (the three
  git hooks in `scripts/hooks/` included), `.claude/skills/`, `.claude/agents/`,
  `.claude/rules/kit/` and the process keys of `.claude/settings.json` came from the kit and
  are this repository's own (ADR 0038; the rule "The kit's files are this repository's").
```

**(d)** old:

```text
- `docs/requirements.md` - the behaviour, with IDs. `docs/decisions/` - ADRs.
```

new:

```text
- `docs/requirements.md` - the behaviour, with IDs. `docs/decisions/` - ADRs;
  `docs/decisions/kit/` - the kit's, a closed copy: "the kit's ADR NNNN" is `NNNN-*.md` there.
```

**(e)** old:

```text
- The process rules are the kit's, in `.claude/rules/kit/process.md`; this project's own are in
  this file. The reasons behind the process are in `GHID.md` in the kit folder
  `ClaudeCodeStarterKit` beside this project in the `photo` repository (for people, not loaded).
- This folder lives inside the `photo` monorepo (`E:\Photografie\Code`); the git root is the
  parent. The root `.gitignore` has a `*.app` rule that would hide `src/MindfulnessReminder.App/`;
  this project's `.gitignore` un-ignores it, keep that line (ADR 0006).
```

new:

```text
- The process rules are the kit's, in `.claude/rules/kit/process.md`; this project's own are in
  this file. The reasons behind the process are in `GHID.md`, the kit's guide, in Romanian (for
  people, not loaded).
- The project is the root of its own repository since 2026-11-10. Until then it was a folder of
  the `photo` monorepo, beside the kit, `ClaudeCodeStarterKit`; its history up to that day is
  in that repository. "The kit folder" in a kit file is this folder (ADR 0038).
  `.github/workflows/ci.yml` runs the gate on every push to `main` and every pull request.
```

**(f)** old:

```text
- **All text is in English** (extends the kit's rule "Languages"): UI strings, comments,
  documentation, and the README too. The sibling `FlatCheck` tool is Romanian; that convention
  is its own, do not carry it over (ADR 0004).
```

new:

```text
- **All text is in English** (extends the kit's rule "Languages"): UI strings, comments,
  documentation, and the README too. `GHID.md`, the kit's guide, is the one Romanian file (ADR
  0004, ADR 0038).
- **The kit's files are this repository's** (varies the kit's rule "Shared folders"). There is
  no kit beside this project and no other project to keep in step: `scripts/`,
  `.claude/skills/`, `.claude/agents/`, `.claude/rules/kit/` and the process keys of
  `.claude/settings.json` are edited here, with an ADR when the change is a decision.
  `scripts/sync.ps1` has nothing to copy and step 0 of the gate is skipped (ADR 0038).
- **A kit decision is cited, never edited** (varies the kit's rule "Decisions").
  `docs/decisions/kit/` is closed: a process decision made here is a project ADR with the next
  number in `docs/decisions/`, and its text names the kit ADR it replaces. No ADR header cites
  "the kit's ADR NNNN": `scripts/check-decisions.ps1` refuses it where `kit` is `"."` (ADR
  0038).
```

**(g)** old:

```text
- A release is tagged `mindfulnessreminder-vX.Y.Z`, the name `scripts/release.ps1` prints; it
  publishes the application framework-dependent into `artifacts/X.Y.Z/` (ADR 0002).
- The main checkout of the kit's rule "Worktrees" is the shared checkout `E:\Photografie\Code`.
```

new:

```text
- A release is tagged `vX.Y.Z`, the name `scripts/release.ps1` prints; it publishes the
  application framework-dependent into `artifacts/X.Y.Z/` (ADR 0002, ADR 0038).
- The main checkout of the kit's rule "Worktrees" is the clone itself, the folder that holds
  `.git/`; a session is started there.
```

### 5.5 `README.md`

Three replacements, all in the section "Development: the gate, the documentation and Claude
Code".

**(a)** old:

```text
Since 2026-10-09 the project follows the process of `ClaudeCodeStarterKit` (the folder beside
this one in the `photo` repository; the guide with the reasoning behind each piece is `GHID.md`
there, in Romanian), like FlatCheck, AstroPhotoAnalyzer and ImageVisualCompare. In short, what
is enforced mechanically:
```

new:

```text
Since 2026-10-09 the project follows the process of `ClaudeCodeStarterKit`, the owner's starter
kit for .NET projects developed with Claude Code. Since 2026-11-10 it has a repository of its
own and carries the kit's files itself: the scripts and the git hooks in `scripts/`, the
skills, the agent and the process rules under `.claude/`, the kit's decision records in
`docs/decisions/kit/`, and its guide, `GHID.md` (in Romanian), with the reasoning behind each
piece. In short, what is enforced mechanically:
```

**(b)** Two edits in the bullet that starts with ``- **`scripts/verify.ps1` is the gate**``.

First, old:

```text
- **`scripts/verify.ps1` is the gate**: the scripts identical with the kit's, the test data
  manifest, the decision records' headers, the design notes (each in the index, with its files
  named in its `Covers:` sentence), restore, `dotnet format` (check only), the build with
  warnings as errors, all the tests. About 20 seconds. The git `pre-commit` hook at the
  repository root runs it on every commit that touches the project, the `pre-merge-commit` hook
  on every clean merge, the Claude Code hook before any `git commit` of the model, and
  `scripts/release.ps1`. Without it green no commit goes in.
```

new:

```text
- **`scripts/verify.ps1` is the gate**: the test data manifest, the decision records' headers,
  the design notes (each in the index, with its files named in its `Covers:` sentence),
  restore, `dotnet format` (check only), the build with warnings as errors, all the tests.
  About 20 seconds. The git `pre-commit` hook runs it on every commit, the `pre-merge-commit`
  hook on every clean merge, the Claude Code hook before any `git commit` of the model,
  `scripts/release.ps1`, and CI on GitHub (`.github/workflows/ci.yml`). Without it green no
  commit goes in.
```

Second: right after the fenced block that follows it (the one holding
`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1`), and before the
bullet `- **The commit message** is one line`, insert these lines, indented two spaces like
the block above them, with one blank line before and one after:

````text
  The git hooks are installed once in every clone:

  ```
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-hooks.ps1
  ```
````

**(c)** old:

```text
- **`scripts/`, `.claude/skills/`, `.claude/agents/` and `.claude/rules/kit/` are the kit's**,
  byte for byte, and are not edited here; what is specific to the project as values is in
  `kit.json`, the steps it adds to the skills are in `docs/skills.md`.
```

new:

```text
- **`scripts/`, `.claude/skills/`, `.claude/agents/` and `.claude/rules/kit/` came from the
  kit** and have been this repository's own since 2026-11-10 (ADR 0038); what is specific to
  the project as values is in `kit.json`, the steps it adds to the skills are in
  `docs/skills.md`.
```

### 5.6 `docs/skills.md`

**(a)** old:

```text
The skills under `.claude/skills/` are the kit's, byte-identical in every project (the kit's
ADR 0018; ADR 0002). What Mindfulness Reminder needs on top of them is here, one section per
```

new:

```text
The skills under `.claude/skills/` came from the kit and hold nothing about this project (the
kit's ADR 0018; ADR 0038). What Mindfulness Reminder needs on top of them is here, one section per
```

**(b)** old:

```text
  is `mindfulnessreminder-vX.Y.Z`. The README's "Deployment note" says where the owner
```

new:

```text
  is `vX.Y.Z` (ADR 0038). The README's "Deployment note" says where the owner
```

### 5.7 `docs/decisions/0000-template.md`

Line 4. Old:

```text
Status: proposed | accepted YYYY-MM-DD | superseded by NNNN (after the last two, for each part another ADR replaced: "; <part> superseded by NNNN"; in a project, "the kit's ADR NNNN" for a kit ADR)
```

new:

```text
Status: proposed | accepted YYYY-MM-DD | superseded by NNNN (after the last two, for each part another ADR replaced: "; <part> superseded by NNNN")
```

### 5.8 Status lines of three existing ADRs

Only line 4 of each file changes. `scripts/check-decisions.ps1` checks these lines character
by character, so copy them exactly (straight quotes, one space after each semicolon).

`docs/decisions/0002-the-layout-stays-src-and-tests-kit-json-holds-the-values.md`

- old: `Status: accepted 2026-10-09`
- new: `Status: accepted 2026-10-09; the kit value and the tag name superseded by 0038`

`docs/decisions/0003-gate-strictness-what-is-on-stays-on-format-adds-whitespace-and-ide0005.md`

- old: `Status: accepted 2026-10-09; comments for the "why" only superseded by the kit's ADR 0035`
- new: `Status: accepted 2026-10-09; comments for the "why" only superseded by 0038 (it carries the kit's ADR 0035)`

`docs/decisions/0006-changelog-starts-at-1-4-0-root-ci-deferred.md`

- old: `Status: accepted 2026-10-09`
- new: `Status: accepted 2026-10-09; the git hooks, the root CI and the *.app exception superseded by 0038`

### 5.9 New file `docs/decisions/0038-the-project-is-the-root-of-its-own-repository-and-carries-the-kits-files.md`

Exactly this content. Line 2 and line 6 are blank; line 5 is one long line.

```markdown
# 0038. The project is the root of its own repository and carries the kit's files itself

Date: 2026-11-10
Status: accepted 2026-11-10
Supersedes: 0002 in part (the kit value and the tag name); 0003 in part (comments for the "why" only); 0006 in part (the git hooks, the root CI and the *.app exception)

## Context

Until 2026-11-10 Mindfulness Reminder was a folder of the `photo` repository, beside the kit,
`ClaudeCodeStarterKit`, and several other projects made from it. `kit.json` named the kit as
`../ClaudeCodeStarterKit` (ADR 0002); the git hooks ran from that repository's root
`.githooks/`, and CI at that root was left for later (ADR 0006); the kit's decisions and its
guide, `GHID.md`, were read in the kit folder. On 2026-11-10 the owner gave the project a
repository of its own, `github.com/tcarean/MindfulnessReminder`, which will hold this project
and nothing else, copied the project folder and the kit folder into it, and decided to merge
the two at the repository root: a kit folder that serves one project has no purpose of its
own. The kit's guide foresees the move: a project that leaves `photo` takes a copy of the guide
with it. The new repository starts with no commit.

## Decision

- The project folder is the repository root. Nothing moved relative to it: `src/`, `tests/`,
  `docs/`, `scripts/`, `.claude/` and `kit.json` are where they were.
- `kit.json`'s `kit` is `"."`: this project holds the kit's files and is their only user.
  `scripts/`, `.claude/skills/`, `.claude/agents/`, `.claude/rules/kit/` and the process keys
  of `.claude/settings.json` are edited here. Step 0 of `scripts/verify.ps1` is skipped and
  `scripts/sync.ps1` has nothing to copy. On the day of the move every file in those folders is
  byte-identical with the kit's.
- The files the kit keeps to itself come along: `scripts/install-hooks.ps1` and the three git
  hooks in `scripts/hooks/` (`pre-commit`, `pre-merge-commit`, `commit-msg`), installed with
  `core.hooksPath = scripts/hooks`, the kit's layout for a standalone repository; the kit's
  `.gitattributes` and `.gitignore`; `.github/workflows/ci.yml`, which runs the gate on every
  push to `main` and every pull request; and `GHID.md`, at the root, in Romanian, with a dated
  note at its top.
- The kit's 36 decision records are copied, unchanged, to `docs/decisions/kit/`, with a README
  that says what they are. "The kit's ADR NNNN" names a file there. The folder is closed: a
  process decision made from now on is a project ADR, whose text names the kit ADR it replaces.
- Every decision of the kit holds here as before. One link changes its form: the kit's ADR
  0035 (a documentation comment on every type and method) superseded the "comments for the
  why only" part of ADR 0003, and ADR 0003's Status said so by citing the kit's ADR. A header
  may not cite a kit ADR where `kit` is `"."` (`scripts/check-decisions.ps1`), so ADR 0003's
  Status names this ADR, which carries that rule.
- A release is tagged `vX.Y.Z`, the name `scripts/release.ps1` gives a project that is its
  repository's root.
- Not brought along: the kit's sample code and its tests (`Starter.sln`, `src/Starter.*`,
  `tests/Starter.Tests`, the fixtures), `scripts/rename.ps1`, `monorepo/`,
  `bitbucket-pipelines.yml`, and the kit's own README, CHANGELOG, plans, requirements,
  validation and design files. They remain in the kit, in `photo`.

## Alternatives rejected

- **Keep the kit as a folder beside the project, as in `photo`.** A second project folder, with
  a sample solution, in a repository that holds one project; the owner ruled it out.
- **Delete the `kit` key, as the kit's README says for a separate repository.** The shared
  folders would stay the kit's: the Stop hook would refuse an edit to them and point at a kit
  this repository does not contain, and a fix would have to be made in `photo` and copied by
  hand.
- **Renumber the kit's decisions into `docs/decisions/`.** "The kit's ADR NNNN" is cited 82
  times outside `docs/plans/`, 60 of them in the shared folders (counted 2026-11-10); every one
  would have to be rewritten, in files that are otherwise the kit's bytes.
- **Bring the kit's tests of the scripts.** They live in the kit's test project, beside the
  tests of its sample code. The scripts are unchanged from the kit, where those tests run;
  porting them is a task of its own, for the day a script is first changed here.

## Consequences

- The gate no longer compares anything with a kit: a change to a script, a skill or a rule is
  reviewed like any other change, and nothing in this repository tests the scripts.
- Taking a later version of the kit is a copy by hand of the four folders from `photo`, read as
  a diff before the commit.
- The ignore exception for `src/MindfulnessReminder.App/` is gone, with the `*.app` rule of
  `photo`'s root `.gitignore` that made it necessary.
- CI runs on GitHub for the first time; its first run is the first push.
- `git log` here starts on 2026-11-10. The earlier commits, the ones ADR 0006 points to, are in
  `photo`.
```

### 5.10 New file `docs/decisions/kit/README.md`

```markdown
# The kit's decision records

A closed copy, taken on 2026-11-10, of the 36 decision records of `ClaudeCodeStarterKit`, the
starter kit whose process this project adopted on 2026-10-09 (ADR 0001) and whose files it has
carried itself since it moved to a repository of its own (ADR 0038). Every "the kit's ADR
NNNN" in this repository names the file `NNNN-*.md` in this folder.

- The numbers are the kit's and are independent of the project's own, in `docs/decisions/`.
  Inside these files a bare "ADR NNNN" is another file of this folder.
- The files are not edited and none is added. A process decision made from 2026-11-10 on is a
  project ADR in `docs/decisions/`, whose text names the record here that it replaces.
- `scripts/check-decisions.ps1` reads `docs/decisions/*.md` only, not this folder.
- The records were written for a kit shared by several projects in one repository. What they
  say about the monorepo, `scripts/sync.ps1`, `.githooks/` and the `Starter` sample describes
  that setting, not this repository.
```

### 5.11 `GHID.md`

The file is Romanian, written without diacritics; the note follows suit. Insert it after line
1 (the title), as a new paragraph: a blank line, the note on one line, a blank line. Change
nothing else in the file.

```text
> **Nota, 2026-11-10.** Ghidul a fost scris in kit-ul `ClaudeCodeStarterKit`, din repo-ul `photo`, pentru mai multe proiecte aflate unul langa altul. MindfulnessReminder a plecat in repo-ul lui si a luat ghidul cu el, cum spune paragraful "Impartirea" de mai jos; copia aceasta nu se mai sincronizeaza cu originalul. Aici proiectul este radacina repo-ului si tine singur fisierele kit-ului: `kit.json` are `"kit": "."` (ADR 0038 al proiectului, in `docs/decisions/`). Ce scrie ghidul despre monorepo, `.githooks/`, `monorepo/`, `scripts/sync.ps1`, `scripts/rename.ps1`, codul de exemplu `Starter` si celelalte proiecte descrie kit-ul de atunci, nu repo-ul acesta. Un "ADR NNNN" din ghid este un ADR al kit-ului: `docs/decisions/kit/NNNN-*.md`.
```

### 5.12 `CHANGELOG.md`

old:

```text
## [Unreleased]

## [1.5.0] - 2026-10-26
```

new:

```text
## [Unreleased]

### Internal

- The project is the root of its own repository (ADR 0038): the kit's scripts, git hooks,
  skills, rules, decision records (`docs/decisions/kit/`) and guide (`GHID.md`) live in it,
  `kit.json`'s `kit` is `"."`, the git hooks are installed from `scripts/hooks/`, CI runs the
  gate on GitHub, and a release is tagged `vX.Y.Z`.

## [1.5.0] - 2026-10-26
```

## Step 6 - Branch and git hooks

```powershell
git symbolic-ref HEAD refs/heads/main
git branch --show-current
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-hooks.ps1
git config --get core.hooksPath
```

Expected: `main`; `standalone repo: core.hooksPath = scripts/hooks`; `scripts/hooks`.
Stop if `install-hooks.ps1` prints `monorepo:` instead: the project is then not at the git
root.

## Step 7 - The gate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
```

Expected, in this order:

- `== kit files: skipped (this project is the kit)`
- `ok       38 decisions in docs/decisions/`
- `ok       9 design notes in docs/design/`
- `Build succeeded.` with `0 Warning(s)`, `0 Error(s)`
- `Failed: 0`, and the same number of tests passed as in step 1 (320 on 2026-11-10)
- `== OK in ...s`

If it is red:

- `FAILED: decisions` - a header written in 5.8 or 5.9 differs from this plan. Compare
  character by character and fix the header, nothing else.
- `kit.json` cannot be read - 5.1 broke the JSON.
- `== kit files` runs a comparison instead of being skipped - `"kit"` is not `"."`.
- anything else (restore, format, build, test) - nothing in this plan touches code; stop and
  report with the output.

## Step 8 - Delete the kit folder

Only after step 7 is green.

```powershell
Remove-Item -LiteralPath .\ClaudeCodeStarterKit -Recurse -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1
Get-ChildItem -LiteralPath . -Force -Name
```

Check: the gate is green again, and the root holds exactly: `.claude`, `.config`,
`.editorconfig`, `.git`, `.gitattributes`, `.github`, `.gitignore`, `.verify-stamp`,
`artifacts`, `CHANGELOG.md`, `CLAUDE.md`, `Directory.Build.props`, `Directory.Packages.props`,
`docs`, `GHID.md`, `global.json`, `kit.json`, `LICENSE`, `MindfulnessReminder.sln`,
`README.md`, `scripts`, `src`, `tests`.

## Step 9 - Look for what was missed

```powershell
Select-String -Path CLAUDE.md, README.md, kit.json, docs\skills.md, .gitignore, .gitattributes, .claude\settings.json -Pattern 'Photografie|\.\./ClaudeCodeStarterKit|mindfulnessreminder-v|\.githooks|FlatCheck'
```

Expected: no line. Any hit is a sentence of step 5 that was not replaced; fix it from step 5.

```powershell
Select-String -Path CLAUDE.md, README.md -Pattern 'photo|ClaudeCodeStarterKit|sync\.ps1'
```

Expected: only the sentences step 5 wrote (the history bullet and the rule "The kit's files
are this repository's" in `CLAUDE.md`, the opening paragraph of the README section). Read each
hit: none may describe the kit as a folder that exists beside the project, or `sync.ps1` as
something to run.

Then read `CLAUDE.md` once from top to bottom, as a new session would, and report any
sentence that is no longer true at the repository root. Report it; do not rewrite beyond
step 5.

## Step 10 - Stage and check what would be committed

```powershell
git add -A
git ls-files | Select-String -Pattern '(^|/)(bin|obj|\.vs|artifacts|StrykerOutput|TestResults)/|settings\.local\.json|\.verify-stamp|ClaudeCodeStarterKit'
git ls-files --eol | Select-String -Pattern 'w/crlf|w/mixed|i/crlf|i/mixed'
(git ls-files | Measure-Object -Line).Lines
git status --short | Select-String -Pattern '^(\?\?| M|MM|AM)'
```

Expected:

- the first two `Select-String` commands print nothing (no build output, no local settings,
  no kit folder, no CRLF);
- the count is 214 if nothing else changed since 2026-11-10: 169 files of the project (this
  plan included; `artifacts/`, `.verify-stamp` and the local settings are ignored), the 36 kit
  decisions, and 9 more this plan adds (`.gitattributes`, `GHID.md`, `ci.yml`,
  `install-hooks.ps1`, the three git hooks, ADR 0038, the README of `docs/decisions/kit/`).
  Under 200 or over 230: an ignore rule or a copy is wrong; stop and report with
  `git status --short`;
- the last command prints nothing: everything is staged, nothing is left modified or
  untracked.

`git add` must not print "LF will be replaced by CRLF". If it does, `.gitattributes` is not
at the root or was changed; stop.

## Step 11 - Report and hand over

Append a section `## What deviated` at the end of this file (now `docs/plans/2026-11-10-move-the-project-to-the-repository-root.md`):
every step that did not go as written and why, or "Nothing." Run `git add -A` once more so the
change is staged.

Then report to the owner, in this order:

1. The last ten lines of the gate from step 8.
2. The number of staged files and the result of each check of steps 9 and 10.
3. Anything skipped (5.3 if the harness refused it) and anything found in step 9.
4. That nothing is committed, and the commands that are the owner's to run:

   ```powershell
   git commit -m "Make the project the repository root and carry the kit's files, hooks and decisions itself"
   git push -u origin main
   ```

   The commit runs the gate through `scripts/hooks/pre-commit` (about 30 s) and the one-line
   rule through `commit-msg`. The push starts the first CI run on GitHub; if it is red, that
   is a task of its own.
5. What is left for the owner afterwards:
   - delete the backup folder `..\MindfulnessReminder-backup-2026-11-10` once the push is done;
   - optionally check the tag name: `scripts/release.ps1 -Version 1.5.1 -WhatIf` on `main`
     with a clean tree prints `tag v1.5.1` and changes nothing;
   - Claude Code sessions for this project are started in the repository root from now on.

## Out of scope (do not start)

- Rewording `.claude/rules/kit/process.md`, the skills, the agent or the script comments so
  they stop saying "the kit" and "sync".
- Porting the kit's tests of the scripts (`HookTests`, `GitHookTests`, `SyncTests`,
  `ReleaseTests`, `DecisionsCheckTests`, `DesignCheckTests`, `MergePublishTests`).
- Deleting `scripts/sync.ps1` or `scripts/check-testdata.ps1`, which have nothing to do here
  but are called or named by other kit files.
- Updating `GHID.md` beyond the note of 5.11.
- Carrying the git history over from `photo`.
- Anything in `src/` or `tests/`.

## What deviated

The owner approved this plan on 2026-11-10 ("Da, aprob"); it was carried out the same day, in
the repository root, in one session.

- Step 1: the owner was not asked to close programs; the running processes were checked
  instead. No Visual Studio was running, and the one `MindfulnessReminder.exe` running was the
  copy in `E:\Photografie\Code\MindfulnessReminder\artifacts\1.5.0\`, outside this folder, so
  nothing here was locked. The gate was green (320 tests).
- Step 2: `dotnet build-server shutdown` ran inside `MindfulnessReminder\`, so that its
  `global.json` chose the SDK the gate had used, 9.0.320. At the root, with no `global.json`
  there yet, `dotnet` picks SDK 10.0.403, whose shutdown may not reach the servers SDK 9
  started. No build process was left afterwards. The backup holds 179 and 358 files, as the
  two folders did.
- Step 3: `Get-ChildItem -LiteralPath $app -Force` was wrapped in `@( )`, so the list was read
  whole before the first entry moved.
- Steps 4 to 8: as written; 5.3 was not refused. The gate was green at the root before and
  after the kit folder was deleted: 38 decisions, 9 design notes, 320 tests.
- Step 9 found one statement the move made false outside the files step 5 edits: ADR 0001's
  Consequences say every commit runs the gate through the root `.githooks/pre-commit`. On the
  owner's word, ADR 0038 supersedes that part too: `0001 in part (the hooks in the root
  .githooks/)` on its Supersedes line, the matching clause on ADR 0001's Status line, and
  `(ADR 0001)` after the root `.githooks/` in its Context. ADR 0038 differs from the text of
  5.9 in those two places.
- Step 10: one check failed. `MindfulnessReminder.sln` had CRLF line ends on disk (`w/crlf`,
  69 lines; the index held LF), and `git add` warned that CRLF would be replaced by LF. The
  measured fact "every text file in both folders has LF line endings" was wrong for this file:
  the check behind it, a `grep` for a carriage return in Git Bash, did not find this one. On
  the owner's word the carriage returns were removed from the file on disk, which made it
  byte-identical with the blob in the index (`git hash-object --no-filters`); its byte order
  mark was kept. The gate was green again afterwards (320 tests, 32 s). Every other check of
  step 10 passed: 214 files staged, no build output and no local settings in the index,
  nothing left unstaged, the 22 files of `src/MindfulnessReminder.App/` tracked.
