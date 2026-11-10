# Absorb the kit into the project

> Note, 2026-11-10: the decision numbers in this plan are those before the renumbering it
> carries out, when the kit's records and the project's were two series. ADR 0075 maps
> each old number to the present one.

Saved, once approved, as `docs/plans/2026-11-10-absorb-the-kit-into-the-project.md`.
Branch `claude/Absorb-the-kit-into-the-project`, worktree
`.claude/worktrees/Absorb-the-kit-into-the-project`.

## Context

On 2026-11-10 the project became the root of its own repository (ADR 0038). That plan chose,
on purpose, to edit no file under `scripts/` or `.claude/` (its D2) and to leave the kit's
tests behind (its D9), so the project only *declared* itself the kit (`"kit": "."`). What is
left shows: the gate prints `== kit files: skipped (this project is the kit)`, a 343-line
`sync.ps1` has nothing to copy, the process rules still say "never edit these folders in a
project", and two ADR series with the same numbers live side by side.

The owner now wants the absorption finished, in two parts carried out as one task:

- **B** - remove the mechanism that can only work with a kit folder or a second project, and
  bring the tests of the scripts, which this repository does not have.
- **C** - the word "kit" leaves every living file: files and functions are renamed and the 74
  decision records become one sequence. One time only.

**Why the tests did not come with the folders.** They did come: the owner's copy of
`ClaudeCodeStarterKit/` held `tests/Starter.Tests/` whole. The move plan then took only the
shared folders out of it and deleted the rest in its step 8, because the seven test files of
the scripts sit in the sample's test project, beside the tests of `Starter.Core`, and find the
repository through `Starter.sln`. Nothing is missing today: the files are in
`E:\Photografie\Code\ClaudeCodeStarterKit\tests\Starter.Tests\` and, byte-identical, in
`D:\Tudor\CSharpCode\MindfulnessReminder-backup-2026-11-10\ClaudeCodeStarterKit\`. `scripts/`
here is byte-identical with the kit's in `photo` (only `rename.ps1` is absent), so those tests
describe exactly the scripts this repository runs.

## The owner's answers (2026-11-10, do not ask again)

| # | Question | Answer |
| --- | --- | --- |
| A1 | Order of the single ADR sequence | Chronological, by `Date:` |
| A2 | When the tests of the scripts run | Only when the change touches the scripts or their tests; always in CI and at a release |
| A3 | `GHID.md` | Deleted from this repository (it stays in `photo`) |
| A4 | Name of `kit.json` and its relatives | `process.json`, `scripts/process-config.ps1`, `.claude/rules/process.md` |
| A5 | Tests of the scripts | Copy from `photo` everything needed; **`photo` is not modified** |

## Facts measured on 2026-11-10 (do not re-derive)

- Gate here: green, 320 tests, about 20 s.
- The kit's whole suite, run from the backup's built binary: 150 tests, all passed, 253 s wall.
  Sum per class: `ReleaseTests` 238 s, `HookTests` 167 s, `GitHookTests` 134 s, `SyncTests`
  104 s, `DesignCheckTests` 83 s, `DecisionsCheckTests` 17 s, `MergePublishTests` 16 s. The
  kit's gate was recorded at 127 to 161 s in October. Every test writes under `%TEMP%` only.
- The seven test files use nothing of the sample except the namespace and
  `TestData.FindRepoRoot()` (looks for `Starter.sln`). The kit builds `net9.0`; this project
  builds `net8.0` (C# 12).
- ADR dates: the kit's 0001 to 0034 are 2026-09-24 to 2026-10-05; the project's 0001 to 0034
  are all 2026-10-09; then kit 0035 (10-21), project 0035 (10-22), kit 0036 (10-24), project
  0036 and 0037 (10-26), project 0038 (11-10).
- ADR citations outside `docs/plans/`: 414 `ADR NNNN`, 71 `the kit's ADR NNNN`, and ten other
  shapes (`ADR N to ADR N`, `ADR N, ADR N`, `the kit's ADRs N, N and N`, `ADRs N to N`, ...).
  21 files hold a citation that wraps onto the next line. Nine citations name **another
  project's** ADR or rule (`FlatCheck ADR 0045`, `FlatCheck's ADR 0051`, `FlatCheck rule 18`,
  in kit ADRs 0015, 0016, 0017, 0022 and project ADR 0001): these are never renumbered.
- `scripts/check-decisions.ps1` requires a superseding ADR to have the larger number; the
  chronological order keeps every existing supersession valid.
- `LicenseHeaderTests` names three project folders; a fourth must be added to its list.

## The new numbers (A1)

| Was | Becomes |
| --- | --- |
| kit 0001 to 0034 | 0001 to 0034 (unchanged) |
| project 0001 to 0034 | 0035 to 0068 (old + 34) |
| kit 0035 | 0069 |
| project 0035 | 0070 |
| kit 0036 | 0071 |
| project 0036, 0037, 0038 | 0072, 0073, 0074 |
| this task's ADR | 0075 |

## Decisions this plan makes (the owner's, by approving it)

| # | Decision | Why |
| --- | --- | --- |
| P1 | **What goes is whatever needs a kit folder or a second project to do anything**: `sync.ps1`, step 0 of the gate, the `kit` key, the Stop hook's shared-folder check, kit references in ADR headers, the monorepo branch of `install-hooks.ps1`, the `<folder>-vX.Y.Z` tag, sibling "components" of a release. | It cannot run here. |
| P2 | **What stays is whatever works on one project, even if written generally**: the hooks still find "the project of the work" by walking up to `scripts/verify.ps1` / the config file. | That walk is what makes a commit in a worktree run the worktree's gate; its tests come unchanged. Only comments change there. |
| P3 | `release.json` keeps its shape; `components` holds this project alone. | Nothing here reads it; reshaping it would rewrite five tests for no gain. |
| P4 | The tests of the scripts go in a new project, `tests/MindfulnessReminder.ProcessTests/`, with the trait `Category=Process`. | `MindfulnessReminder.Tests` stays "every rule on fakes, Core only" (ADR 0008); Stryker runs from that project and must not start four minutes of script tests. |
| P5 | `process.json` gets `processTests: { filter, paths }`; `verify.ps1` leaves those tests out unless a path in `paths` differs from `main` (committed, staged, modified or untracked), or `CI=true`, or `-All`. `release.ps1` passes `-All`. The Stop hook asks for a green verify after an edit under `paths` too. | A1/A2. `scripts/` now has tests here, so an unverified edit to it should not end a turn. |
| P6 | Ported tests keep their names and assertions. Only these change in stage 1: namespace, GPL header, how the repository root is found, temp-folder prefix, the trait, and syntax C# 12 does not accept. | "No test edited to pass". A ported test that fails in stage 1 is a finding: stop and report. |
| P7 | The 36 kit records move into `docs/decisions/`, text unchanged except numbers. Those P1 makes false are marked superseded by ADR 0075, not deleted. | The kit's ADR 0024: a decision changes by a superseding ADR. |
| P8 | `docs/plans/` and the released sections of `CHANGELOG.md` keep the numbers of their date. ADR 0075 holds the table above; each of the six older plans gets one dated line at its top pointing to it. | The kit's ADR 0028 did the same when rules lost their numbers. Plans quote header lines and outputs literally; a mechanical rewrite would corrupt them. |
| P9 | `docs/skills.md` is folded into the six skills and deleted; the project's rules that only "vary" a kit rule are folded into `.claude/rules/process.md`. | The split "generic file + project additions" exists only because the generic file was shared. |
| P10 | The kit's `RepoInvariantTests` are not brought (the project has its own); `SyncTests` are not brought. One invariant is added: every `ADR NNNN` cited outside `docs/plans/` and the released CHANGELOG sections names a file in `docs/decisions/`. | The first tested the kit's guide and the "the kit's ADR" wording, both gone; the second tests a deleted script. |
| P11 | No version bump: no behaviour of the application changes. | The owner's rule; said here so it is not asked. |

## Rules for this task

- Start the session in the main checkout, on `main`, clean. Make the worktree and branch first.
- `E:\Photografie\Code\` is **read only**: copy out of it, never write, build or test in it.
- Until the branch is merged, the Claude Code hooks that run are `main`'s old copies. After
  stage 4 the old Stop hook no longer finds `kit.json` in the worktree and judges nothing
  there: run `scripts/verify.ps1` by hand after the last edit of every stage. The commit gate
  and git's `pre-commit` still run the worktree's `verify.ps1`.
- Every commit of this branch touches `scripts/`, so from stage 3 on every gate run includes
  the process tests: allow about five minutes per commit.
- One stage, one commit (stage 5 and 6 may be two each), each with the gate green. One-line
  messages, at most 100 characters.
- Do not touch `src/` beyond the ten ADR numbers in comments (stage 5). No package added: the
  new test project uses the three packages already in `Directory.Packages.props`.
- A removed or changed test must be one this plan names. Anything else that goes red: stop and
  report.

## Stage 1 - Bring the tests of the scripts

1. New `tests/MindfulnessReminder.ProcessTests/MindfulnessReminder.ProcessTests.csproj`:
   `net8.0`, not packable, the three test packages, no project reference, no global
   `Using Include="Xunit"` (the copied files have their own `using`, and IDE0005 would flag a
   duplicate). Add it to `MindfulnessReminder.sln`; commit its `packages.lock.json` (CI
   restores in locked mode).
2. Copy from `E:\Photografie\Code\ClaudeCodeStarterKit\tests\Starter.Tests\`:
   `ScriptRunner.cs`, `HookTests.cs` (it also holds `WindowsFactAttribute` and
   `WindowsTheoryAttribute`), `GitHookTests.cs`, `ReleaseTests.cs`, `DecisionsCheckTests.cs`,
   `DesignCheckTests.cs`, `MergePublishTests.cs`. Apply P6 and nothing else; add a small
   `Repo.Root` (walks up from `AppContext.BaseDirectory` to `MindfulnessReminder.sln`) in place
   of `TestData.FindRepoRoot()`.
3. `docs/requirements.md` first if R-092 names the projects, then `LicenseHeaderTests`: the
   fourth folder in its list and in `R092_The_scan_sees_all_...`.
4. Run the gate (it runs everything at this stage). Record in this plan's "What deviated" the
   number of tests and the time. Commit.

## Stage 2 - Remove the dead mechanism (test first, then the script)

For each item: change or add the test, see it red, change the script, see it green.

| Script | Change | Tests |
| --- | --- | --- |
| `scripts/sync.ps1` | Deleted. Its `allow` entry leaves `.claude/settings.json`. | none brought |
| `scripts/verify.ps1` | Step 0 and its three "skipped" lines go; the header lists seven checks, 1 to 7. | covered by stage 3's |
| `scripts/kit-config.ps1` | `kit` is no longer read; `Get-KitPath`, `Get-KitSharedFolders`, `Get-KitSharedFolderNames` deleted. `kit.json` loses the key. | fixtures: `ScriptRunner.KitJson` and `HookTests.WriteKitJson` lose the `kit` parameter |
| `scripts/hooks/claude-stop-gate.ps1` | Check A (shared folders) deleted. | removed: `StopGate_SharedFolderEdit_DependsOnThatProjectsKitJson`, `StopGate_RulesEdit_IsRefusedOnlyUnderKit` |
| `scripts/check-decisions.ps1` | REF is a number only; `Test-KitRef` and the kit lookup deleted. | removed: the three `KitReference_*`; added: `ACitationOfAKitAdr_IsNotAReference` (a Status naming "the kit's ADR 0001" is a STATUS problem) |
| `scripts/install-hooks.ps1` | The `.githooks/` branch goes: a project folder that is not the git root is an error. | added `InstallHooksTests`: at the root sets `core.hooksPath = scripts/hooks`; below the root exits 1 |
| `scripts/release.ps1` | The tag is always `vX.Y.Z`; a project below the git root is refused; a `release.publish` entry outside the project folder is refused; the checks and the `-WhatIf` lines for other components go. P3 holds. | removed: the tests whose subject is a second project (`AnEntryInAFolderWithoutKitJson_...`, `AComponentWith...` x2, `ACleanComponent_IsListedByWhatIf`, `PublishOnly_ListsEveryComponent_InReleaseJson`; read each before removing: one that is about this project's own tree stays); added: `AnEntryOutsideTheProject_IsRefused`, `AProjectBelowTheGitRoot_IsRefused` |
| `pre-commit`, `pre-merge-commit`, `commit-msg`, `claude-commit-gate.ps1`, `claude-track-edits.ps1`, `mutate.ps1`, `check-testdata.ps1`, `check-design.ps1`, `merge-publish.ps1` | Logic untouched (P2). Comments reworded in stage 6. | unchanged |

Gate, commit.

## Stage 3 - The process tests run when the process changed (P5)

1. `[Trait("Category", "Process")]` on every class of the new project (done in stage 1).
2. `kit.json`: `"processTests": { "filter": "Category=Process", "paths": ["scripts/",
   "tests/MindfulnessReminder.ProcessTests/"] }`; `Read-KitConfig` normalises it, absent
   meaning "no such tests".
3. `Test-ProcessPathsChanged` in `kit-config.ps1`: true when git is missing, the folder is not
   a repository or has no commit; otherwise the base is `git merge-base HEAD main` (HEAD itself
   on `main` or where `main` does not exist) and it is true when
   `git diff --name-only <base> -- <paths>` or
   `git ls-files --others --exclude-standard -- <paths>` prints anything. During a merge on
   `main` the index holds the merged files, so `pre-merge-commit` sees them.
4. `verify.ps1`: new switch `-All`. The test step adds `Category!=Process` to the filter
   (joined with `testFilter` by `&`) unless `-All`, `CI=true` or step 3 says true; it prints
   which of the two it ran. `release.ps1` calls it with `-All`.
5. `claude-stop-gate.ps1`: check B also counts an edit under `processTests.paths`.
6. Tests first: `VerifyScopeTests` (a throwaway repository: a change under `scripts/` staged,
   unstaged, untracked, and committed on a branch off `main` each give true; a change under
   `src/` gives false; no repository gives true) and
   `StopGate_AnEditUnderScripts_NeedsAGreenVerify`.

Gate, commit.

## Stage 4 - Rename (A4)

`git mv`, then every reference:

- `kit.json` -> `process.json`; `scripts/kit-config.ps1` -> `scripts/process-config.ps1`;
  `.claude/rules/kit/process.md` -> `.claude/rules/process.md`.
- Functions: `Read-KitConfig` -> `Read-ProcessConfig`, `Find-KitProject` -> `Find-Project`,
  `ConvertTo-KitFolder` -> `ConvertTo-ConfigFolder`, `Get-KitFolderPath` ->
  `Get-ConfigFolderPath`, `Get-KitShell` -> `Get-ScriptShell`; the variable `$kit` -> `$config`.
- The test fixtures (`ScriptRunner.KitJson` -> `ProcessJson`, the file names they write) and
  every mention of the three paths in `CLAUDE.md`, `README.md`, `docs/`, the skills, the agent
  and `.claude/settings.json`.

Check: `git grep -n -e "kit\.json" -e "kit-config" -e "rules/kit" -e "KitConfig" -e "KitProject"`
prints only lines under `docs/decisions/`, `docs/plans/` and the released CHANGELOG sections.
Gate, commit.

## Stage 5 - One sequence of decisions (A1)

1. `git mv` every record to its new number (slug unchanged): the kit's from
   `docs/decisions/kit/` into `docs/decisions/`, the project's in descending order.
   `docs/decisions/kit/README.md` is deleted.
2. A one-time PowerShell script, kept in the scratchpad and not committed, rewrites the
   numbers and writes a log line per replacement (`file:line  old -> new`):
   - **Kit context** (the 36 moved records): line 1, the Status and Supersedes lines, and every
     bare `ADR NNNN` in the body take the kit column of the table.
   - **Project context** (every other tracked file except `docs/plans/**` and the released
     sections of `CHANGELOG.md`; `src/` and `tests/` included): `the kit's ADR(s) ...` loses
     "the kit's " and takes the kit column; a bare `ADR NNNN`, and the header lines of the 38
     project records, take the project column. All twelve measured shapes, lists and ranges
     included.
   - The gap between `ADR` and a number, and between "the kit's" and `ADR`, may be a line break
     followed by indentation and a comment marker (`#`, `//`, `///`).
   - Never touched: a citation preceded by `FlatCheck` or `FlatCheck's`; `0000-template.md`;
     `rule NN`.
3. Read the whole log and the whole diff. Check: the number of citations is the same before and
   after; the count per old number equals the count per its new number; the nine FlatCheck
   citations are unchanged.
4. Write `docs/decisions/0075-...md` (stage 7 lists its content) in this commit, because the
   Status clauses below need it. Supersedes, to be confirmed by reading each record in full:
   whole - 0002 (the sample's Core rule; this project's is 0042), 0004, 0006, 0015, 0016, 0025;
   in part - 0011 (the guide and the test of its paths), 0017 (`sync.ps1`, the `kit` key, "never
   edited in a project"), 0018 (the mirrored skills, `docs/skills.md`), 0027 (the `.githooks/`
   path), 0028 (the synced rules file, the shared settings keys), 0071 (sibling projects),
   0074 (`kit` is ".", step 0 skipped, the closed copy in `docs/decisions/kit/`, "the kit's ADR
   NNNN", the tests left in the kit). Each gets its Status clause.
5. `RepoInvariantTests.Every_cited_decision_exists` (P10), written before the script runs and
   seen green before and after.
6. One dated line at the top of each of the six plans in `docs/plans/` (P8).

Expected: `ok       75 decisions in docs/decisions/`. Gate, commit.

## Stage 6 - The words (C)

- `.claude/rules/process.md`: new opening (this repository's process rules; no "same bytes",
  no sync, no guide). "Shared folders" becomes "Process files" (they are edited here, test
  first, and the gate runs the process tests for them). "Decisions" and the skills' ADR step
  lose the kit/project distinction. "Languages" becomes English everywhere. The project's
  rules "The kit's files are this repository's", "A kit decision is cited, never edited" and
  "All text is in English" leave `CLAUDE.md`; the others drop "extends the kit's rule".
- `CLAUDE.md`: description, Commands (no step 0; the process tests and `-All`), Layout (the new
  test project, `process.json`, no kit history beyond one sentence), at most 200 lines.
- The six skills and `docs-auditor.md`: reworded; each section of `docs/skills.md` folded into
  its skill at the step it names; `docs/skills.md` deleted (P9).
- Script headers and comments: what the script does here, no kit, no monorepo, no
  `Starter.Tests`, no `bitbucket-pipelines.yml`, no `.githooks/`.
- `README.md` (the development section), `docs/design/*.md`, `docs/requirements.md`,
  `docs/validation.md` and `.json`, `Directory.Build.props`, `Directory.Packages.props`,
  `.editorconfig`, `.gitattributes`, `.claude/settings.json`'s `$comment`, the four mentions in
  `RepoInvariantTests.cs`, the comments of the ported tests.
- `GHID.md` deleted (A3); nothing points to it any more.
- `"the kit's rule "X""` becomes `the rule "X"`.

Check: `git grep -n -i -w -e kit -e ClaudeCodeStarterKit -e monorepo -e GHID` prints only lines
under `docs/decisions/`, `docs/plans/`, the released CHANGELOG sections, and one sentence of
provenance in `README.md` ("the process came from ClaudeCodeStarterKit, the owner's starter
kit, adopted on 2026-10-09 and absorbed on 2026-11-10, ADR 0075"). Read every hit. Gate, commit.

## Stage 7 - ADR 0075, CHANGELOG, wrap-up

- ADR 0075, "The process is this repository's own: one sequence of decisions, the scripts
  tested here, nothing of a kit left to run": Context (what ADR 0074 left), Decision (P1 to
  P10 and A1 to A5), the renumbering table in full (74 rows, old -> new), Alternatives
  rejected (project-first and kit-first numbering with their counts; process tests in every
  gate, 253 s against 20 s; keeping `GHID.md`; keeping the names), Consequences (a later kit
  version can no longer be copied over; `photo`'s kit is no longer a source; the first CI run
  with the process tests is the first push).
- `CHANGELOG.md`, `[Unreleased]`: the existing line about ADR 0038 is rewritten to the end
  state, under `Internal`.
- `/wrap-up`. Report: the test counts, the gate time with and without the process tests, the
  list of removed tests, the checks of stages 4 to 6, and that nothing was changed in `photo`.
  Propose the merge; the owner decides.

## Verification, end to end

1. `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 -All` is green:
   75 decisions, 9 design notes, 320 tests plus the process tests.
2. On a throwaway branch with one change under `src/` only: the gate says it left the process
   tests out and takes about 20 s. With one change under `scripts/`: it runs them.
3. `git commit` in the worktree prints no line about kit files.
4. `scripts/release.ps1 -Version 1.5.1 -WhatIf` cannot run in a worktree (it refuses, as
   before); the owner runs it on `main` after the merge and sees `tag v1.5.1`.
5. The two `git grep` checks of stages 4 and 6.
6. `git -C E:\Photografie\Code status --short` prints nothing new.

## Out of scope

- Any change to `photo`, or to the backup folder (the owner deletes it after the push).
- Rewriting the text of an old decision record beyond its numbers and its Status line.
- Making the process tests faster.
- A README path test, analyzers, or a style pass over the ported tests.
- Anything in `src/` except the ten ADR numbers in comments.

## Risks

- **The first CI run** executes the process tests on `windows-latest` for the first time (git
  identity, `powershell` 5.1, the SDK `dotnet publish` picks in a temp folder). It cannot be
  proven locally; a red run after the push is a task of its own.
- **C# 13 syntax** in the copied tests would not build on `net8.0`: rewrite the syntax, never
  the assertion, and list each case in "What deviated".
- **The renumbering script** is the one step where a silent error is possible: the log and the
  two count checks of stage 5 are not optional.

## What deviated

The owner approved this plan on 2026-11-10.

- Stage 1: the seven files, copied from `photo`, differ from the kit's only by the GPL notice,
  the namespace, `Repo.Root` for `TestData.FindRepoRoot()`, the temp-folder prefix and the
  trait (checked with `diff` against the originals). No syntax needed changing for C# 12. The
  classes hold 95 tests (Hook 39, GitHook 9, Release 29, DesignCheck 8, DecisionsCheck 5,
  MergePublish 5); all passed at the first run: `Passed: 320` and `Passed: 95`, `== OK in
  174s`. `dotnet sln add` writes CRLF and, without `--in-root`, a new `tests` solution folder:
  the project was added at the solution root, as `MindfulnessReminder.Tests` is, and the
  carriage returns were removed (the byte order mark kept). R-092 now says "the four
  projects"; its scan test is renamed `R092_The_scan_sees_all_four_projects_and_the_xaml` and
  also asserts `ScriptRunner.cs`.
- Stage 2, tests changed beyond the ones this plan names, each because the change makes its
  data false: `DecisionsCheckTests.WellFormedHeaders_...` used "the kit's ADR 0007" as a valid
  reference in two Status lines (0004 and 0010), now plain ones; the fixture of
  `DecisionsCheckTests` holds one project, without `kit-config.ps1` and `kit.json`, which
  `check-decisions.ps1` no longer reads. `ReleaseTests` put the project one folder below the
  git root, the monorepo layout the release now refuses: git is initialized in the project
  folder, so `Git`, `AssertNothingChanged`, the `.gitignore`, the linked worktree's paths and
  the `rev-parse` of `PublishOnly_WritesReleaseJsonAndTheZip_FromAnyBranch` point at it,
  `PublishOnly_OutsideAGitRepository_WritesEmptyCommits` deletes the `.git` it now copies, and
  `AnExistingTag_IsRefused` tags `v2.3.0` instead of `proj-v2.3.0`.
- Stage 2, tests removed: `StopGate_SharedFolderEdit_DependsOnThatProjectsKitJson`,
  `StopGate_RulesEdit_IsRefusedOnlyUnderKit`, the three `KitReference_*`, and in
  `ReleaseTests` `AnEntryInAFolderWithoutKitJson_IsRefused_WithTheFolderNamed`,
  `AComponentWithAnUncommittedFile_IsRefusedAsNotClean`, `ACleanComponent_IsListedByWhatIf`,
  `AComponentWithAModifiedFile_IsListedAsGitPrintsIt` and
  `PublishOnly_ListsEveryComponent_InReleaseJson`, with the helper `MakeComponent`. Tests
  added: `StopGate_AnEditOfTheProcessFiles_IsNotRefusedAsAnotherFolders` (three cases),
  `ACitationOfAKitAdr_IsNotAReference`, `InstallHooksTests` (two), `VerifyTests` (one, which
  runs the real gate on a one-library solution, about 15 s), and in `ReleaseTests`
  `AnEntryOutsideTheProject_IsRefused`, `AProjectBelowTheGitRoot_IsRefused` and
  `ADirtyTree_IsRefused_WithThePathsAsGitPrintsThem`. The last one replaces the coverage of
  `AComponentWithAModifiedFile_...` for this project's own tree, which no test checked.
- Stage 3: the key is `processTests: { category, paths }`, not `{ filter, paths }`: the gate
  has to negate it, and a `dotnet test` filter expression cannot be negated in general, a
  category can (`Category!=Process`, joined to `testFilter` with `&`). Measured on the
  solution: `dotnet test --filter "Category!=Process"` runs the 320 tests of
  `MindfulnessReminder.Tests` and exits 0 in 3 s; the process test assembly, with nothing
  left to run, does not fail the run. `VerifyScopeTests` (nine cases) ask the real
  `Test-ProcessPathsChanged` through a probe script; `VerifyTests` gained
  `ACleanCheckout_LeavesTheProcessTestsOut_UnlessAll`; `HookTests` gained
  `StopGate_AnEditUnderTheProcessTestPaths_NeedsAGreenVerify`. All twelve were red before the
  scripts changed and green after.
- Stage 3 measured at its commit: `Passed: 320` and `Passed: 104` (the process tests, all
  run, the branch changes `scripts/`), `== OK in 159s`.
- Stage 4: 216 replacements in 31 files (92 `kit.json`, 36 `$kit`, 24 `kit-config.ps1`, 20
  `KitJson`, the five function names, 12 paths under `.claude/rules/kit/`). The check found
  one line left, in the released 1.4.0 section of `CHANGELOG.md`, which stays. `git mv` left
  `.claude/rules/kit/` empty on disk; it was removed.
- Stage 5, the records ADR 0075 supersedes, after reading each: kit 0002 (now 0002) is not
  among them: it is the rule this project's ADR 0042 (was 0008) records in its own form, and
  it still holds. Three are added: 0012 in part (Romanian for what only people read, now that
  `GHID.md` goes), 0024 in part (a kit ADR as a reference) and 0035 in part (was project 0001:
  the kit's files "synced and never edited here", false since ADR 0074 and not recorded then).
- Stage 5, the script, rehearsed first on a copy in the scratchpad. The rehearsal found two
  mistakes before the worktree was touched: the tests ported from the kit cite the kit's
  records with bare numbers (written inside the kit), four of them in `HookTests` and
  `GitHookTests`, which the first version took for project numbers; and
  `ACitationOfAKitAdr_IsNotAReference` holds "the kit's ADR 0007" as data, which must not be
  rewritten. The script then treated `tests/MindfulnessReminder.ProcessTests/` as kit context
  and protected that string. A "the kit's" broken across two lines left "(" at the end of a
  line; the parenthesis now moves down with the citation. The run: 74 records moved (git sees
  74 renames), 97 files changed; 185 kit and 313 project citations, 27 Status and 25
  Supersedes numbers, 74 titles, 18 foreign citations kept (FlatCheck, AstroDarkroom, "its",
  "their", all numbers of other projects), 11 qualifiers broken across lines. The citations
  counted per file are the same before and after, 519 and 517, the difference being the two of
  the deleted `docs/decisions/kit/README.md`. In `src/` and the tests, every changed line
  differs only in its numbers or its "the kit's". The comment of the new invariant test, whose
  examples ("ADR 0012", "ADRs 0017, 0018 and 0028") the script renumbered too, now reads
  "ADR 0046", "ADRs 0051, 0052 and 0062". `Every_cited_decision_exists` was green before and
  after, and failed on a planted "ADR 0999" and "ADRs 0012, 0998 and 0014" in a probe file
  that was then deleted. `check-decisions.ps1`: `ok 75 decisions`. All seven plans, this one
  included, got the dated note under their title.
- Stage 5 measured at its commit: `ok 75 decisions`, `Passed: 321` and `Passed: 104`,
  `== OK in 159s`.
- Stage 6: the gate times in `CLAUDE.md` and `README.md` were measured on a clone of the branch
  renamed to `main`, clean, so nothing under the process-test paths differed: `process tests:
  left out`, `Passed: 321`, `== OK in 25s` and, run again, `22s`; with the process tests, 159
  to 175 s at the commits of stages 3 to 5. The six sections of `docs/skills.md` went into
  their skills at the steps they named (`/docs-health` and `/mutate` in their opening and step
  1, `/validate` in its opening), and the file was deleted. `.claude/rules/process.md` opens
  without the kit; "Shared folders" became "Process files"; "Languages" says English
  everywhere and replaces `CLAUDE.md`'s "All text is in English"; `CLAUDE.md`'s "The kit's files
  are this repository's" and "A kit decision is cited, never edited" are gone, and its line on
  the main checkout moved into the rule "Worktrees". The auditor's 200-line count leaves out
  `.claude/rules/process.md`, as it left out the kit's rules file. `docs/design/code-map.md`
  lists the process test project, file by file, and names it in its Covers sentence; the
  README's "Layout" and "Build and run" name it too. `GHID.md` was deleted. The `[Unreleased]`
  CHANGELOG line of ADR 0074 now says the end state, with a second line for the process tests.
  The check, `git grep -i -e kit -e monorepo -e GHID -e photo -e Starter -e skills.md -e sync.ps1`
  outside `docs/decisions/` and `docs/plans/`, prints the released sections of
  `CHANGELOG.md`, the README's sentence on where the process came from, and two tests that keep
  an old form on purpose: `ACitationOfAKitAdr_IsNotAReference` (its data is "the kit's ADR
  0007", which the check must refuse) and `AGreenRun_RunsEveryStep_ComparesWithNoKit_...`
  (it asserts that no "kit files" line comes back).
