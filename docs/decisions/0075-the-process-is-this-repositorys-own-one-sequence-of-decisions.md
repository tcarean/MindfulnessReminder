# 0075. The process is this repository's own: one sequence of decisions, the scripts tested here, nothing of a kit left to run

Date: 2026-11-10
Status: accepted 2026-11-10
Supersedes: 0004; 0006; 0011 in part (the guide and the test of its paths); 0012 in part (Romanian for what only people read); 0015; 0016; 0017 in part (the name kit.json, the kit key, sync.ps1, a project never editing scripts/, the tests of the scripts in the kit only); 0018 in part (the skills and the agent mirrored by sync.ps1 and guarded by the Stop hook, the project steps in docs/skills.md); 0024 in part (a kit ADR as a reference); 0025; 0027 in part (the absolute core.hooksPath of the main checkout's .githooks/); 0028 in part (the synced rules file under .claude/rules/kit/, the process keys compared by sync.ps1, the shared-folder list); 0035 in part (the kit's files synced and never edited here); 0071 in part (an entry in another project, a component); 0074 in part (the kit key, step 0 skipped, sync.ps1, the closed copy in docs/decisions/kit/, citing the kit's ADRs, GHID.md, the tests of the scripts left in the kit)

## Context

ADR 0074 made the project the root of its own repository and let it carry the kit's files, but
changed none of them: the project declared itself the kit (`"kit": "."`), so that the scripts
stayed the bytes the kit's tests had passed. A kit stayed visible everywhere. The gate printed
`== kit files: skipped (this project is the kit)` at every commit. `scripts/sync.ps1`, 343
lines, had nothing to copy. The Stop hook, `install-hooks.ps1` and `release.ps1` kept branches
for a kit beside the project and for several projects in one repository. The process rules
said the scripts and skills are never edited in a project, and `CLAUDE.md` cancelled that with
a rule of its own. Two series of decision records, `docs/decisions/` and `docs/decisions/kit/`,
used the same numbers, told apart by "the kit's ADR NNNN". The guide, `GHID.md`, described a
kit shared by several projects. And nothing here tested the scripts: their tests had stayed in
the kit's test project, beside its sample code, and the move deleted that folder. On
2026-11-10, after seeing the gate's kit line, the owner asked for the kit to be absorbed
completely, for the decisions to be renumbered once into a single sequence, and for the tests
of the scripts to be brought from `photo`, which is not modified. The plan is
`docs/plans/2026-11-10-absorb-the-kit-into-the-project.md`.

## Decision

- **Nothing that needs a kit folder or a second project stays.** `scripts/sync.ps1` and step 0
  of `scripts/verify.ps1` are deleted, and the `kit` key is no longer read. The Stop hook no
  longer guards "shared folders". `scripts/check-decisions.ps1` takes a number as the only kind
  of reference and no longer reads the project's configuration. `scripts/install-hooks.ps1`
  sets `core.hooksPath = scripts/hooks` and refuses a project below the root of its
  repository. `scripts/release.ps1` tags `vX.Y.Z` only, refuses a project below the root and a
  `release.publish` entry outside the project, and its `release.json` lists this project as
  its one component. What works on one project stays, even where it was written for several:
  the hooks still find the project of a commit or an edit by walking up to its
  `scripts/verify.ps1` or its configuration, which is how a commit in a worktree runs that
  worktree's gate.
- **The scripts are tested here.** The kit's tests of the scripts are copied from
  `E:\Photografie\Code\ClaudeCodeStarterKit\tests\Starter.Tests\` (`HookTests`,
  `GitHookTests`, `ReleaseTests`, `DecisionsCheckTests`, `DesignCheckTests`,
  `MergePublishTests` and their `ScriptRunner`) into a project of their own,
  `tests/MindfulnessReminder.ProcessTests/`, which references no project of the solution.
  Their names and assertions are the kit's, except where this decision removes or changes what
  they tested; the plan lists each one. `SyncTests` and the kit's `RepoInvariantTests` are not
  brought. New tests: `InstallHooksTests`, `VerifyTests` (the real gate, on a one-library
  solution) and `VerifyScopeTests`.
- **The process tests run when they can be affected.** Every class of that project carries the
  trait `Category=Process`. The configuration's `processTests` names the category and the paths
  the tests cover (`scripts/` and their own project). `verify.ps1` leaves the category out
  unless one of those paths differs from where the branch left `main` (committed, staged,
  modified or untracked; on `main`, what is not committed yet), CI is `"true"`, or it runs with
  `-All`, which `release.ps1` passes; where git cannot tell, they run. A line says which it was.
  The Stop hook judges an edit under those paths like an edit of code.
- **The names say what the files are.** `kit.json` is `process.json`, `scripts/kit-config.ps1`
  is `scripts/process-config.ps1` and `.claude/rules/kit/process.md` is
  `.claude/rules/process.md`. The functions are `Read-ProcessConfig`, `Find-Project`,
  `ConvertTo-ConfigFolder`, `Get-ConfigFolderPath` and `Get-ScriptShell`.
- **One sequence of decisions, by date.** The kit's 36 records move from `docs/decisions/kit/`
  into `docs/decisions/`, and the 74 records are numbered in the order of their dates (the
  table below). A record's text changes in its numbers and, where this decision supersedes it,
  in its Status line, nothing else. Every living file cites a record by its number alone, and
  "the kit's ADR NNNN" is gone. `docs/plans/` and the released sections of `CHANGELOG.md` keep
  the numbers of their date; the table maps them. A citation of another project's record
  (FlatCheck's, AstroDarkroom's, "its", "their") keeps its number.
  `RepoInvariantTests.Every_cited_decision_exists` checks that every number a living file
  cites is a record.
- **The process rules and the skills are this project's.** `.claude/rules/process.md` holds
  the process rules, with no kit and no project to tell apart, and the rules of `CLAUDE.md`
  that only varied a kit rule are gone. `docs/skills.md` is folded into the six skills and
  deleted. `GHID.md` is deleted from this repository; it stays in `photo`, and no file here
  points to it. Everything in the repository is in English.

## Alternatives rejected

- **Keep the kit as ADR 0074 left it.** The analysis of 2026-11-10 counted about 80 citations
  of the kit in 38 living files, a 343-line script with nothing to do, and process rules that
  `CLAUDE.md` had to contradict; the owner asked for the absorption.
- **Only silence step 0.** It hides the one line and leaves every other trace.
- **Number the project's records first (0001 to 0038), the kit's after (0039 to 0074).** Fewer
  citations would change (none in `src/`, the tests or `docs/design/`), but the numbers would
  contradict the dates: decisions of September would come after those of November.
- **Number the kit's records first (0001 to 0036), the project's after.** Nearly
  chronological, but two kit records of 21 and 24 October would come before the project's
  records of 9 October.
- **Run the process tests at every commit, as the kit did.** Measured on 2026-11-10: the kit's
  suite ran its 150 tests in 253 s; here the gate went from about 20 s to 159 to 175 s with the
  process tests.
- **Keep `GHID.md`, rewritten or as history.** The owner chose to delete it.
- **Keep the names `kit.json`, `kit-config.ps1` and `.claude/rules/kit/`.** Fewer renames, but
  three names that describe this project would still say "kit".

## Consequences

- A later version of the kit can no longer be copied over these files: they differ, and the
  numbers changed. The kit in `photo` is a source of ideas, not of files.
- A commit that touches `scripts/` or the process tests runs them, which takes minutes; any
  other commit keeps the gate's earlier time. CI and a release always run them.
- The first CI run with the process tests is the next push; until then they have run on this
  machine only.
- Commit messages, plans and released CHANGELOG entries written before 2026-11-10 cite the old
  numbers; the table maps them.

## Numbers before and after

"Was" is the record's number before 2026-11-10: "kit" in `docs/decisions/kit/`, "project" in
`docs/decisions/`.

| Now | Was | Title |
| --- | --- | --- |
| 0001 | kit 0001 | Record decisions as ADRs in the repository |
| 0002 | kit 0002 | All logic lives in a library that cannot reach Console or a UI |
| 0003 | kit 0003 | One verify script is the gate, run from pre-commit, the Claude Code hook and CI |
| 0004 | kit 0004 | The kit works as a folder inside a monorepo, not only as a repository root |
| 0005 | kit 0005 | Rules the model must remember become mechanisms the model cannot skip |
| 0006 | kit 0006 | Rules live in CLAUDE.md, explanations live once in the kit's guide |
| 0007 | kit 0007 | One-line English commit messages; the owner decides every commit and merge |
| 0008 | kit 0008 | Mutation testing on Core, on demand, never in the commit gate |
| 0009 | kit 0009 | The documentation audit runs in a subagent with a clean context |
| 0010 | kit 0010 | A release is a deterministic script plus a proposed commit and tag |
| 0011 | kit 0011 | Paths in the guide and README are tested; their content is reviewed at wrap-up |
| 0012 | kit 0012 | English in everything the model reads or writes; Romanian only in the human guide |
| 0013 | kit 0013 | Branches are named claude/ plus their task and deleted once merged |
| 0014 | kit 0014 | LF in the repository and on disk, on every platform; fixtures are never converted |
| 0015 | kit 0015 | In a monorepo, Claude Code worktrees are ignored at the root; the kit keeps its own line |
| 0016 | kit 0016 | In a monorepo, core.hooksPath is the absolute path of the main checkout's .githooks |
| 0017 | kit 0017 | scripts/ is the kit's: parameters in kit.json, copied by sync.ps1, never edited in a project |
| 0018 | kit 0018 | The skills and the agent are the kit's too: values from kit.json, project steps in docs/skills.md |
| 0019 | kit 0019 | A worktree folder the assistant makes is named after its branch |
| 0020 | kit 0020 | The Claude Code hooks judge the project of the commit and of each edited file |
| 0021 | kit 0021 | The assistant commits on its own claude/ branches; the owner commits on main and decides every merge |
| 0022 | kit 0022 | A merge git records by itself runs the commit gate, through pre-merge-commit |
| 0023 | kit 0023 | Commit lines up to 100 characters; a CHANGELOG line by judgment, not by a gate |
| 0024 | kit 0024 | An accepted ADR changes by a dated note or a superseding ADR; one header grammar, checked |
| 0025 | kit 0025 | A project cites the kit's process decisions and records only its own |
| 0026 | kit 0026 | The main checkout refuses a Claude session's commit on a claude/ branch |
| 0027 | kit 0027 | The desktop app's worktree option is not used in photo; task worktrees are made by hand |
| 0028 | kit 0028 | The process rules live in a synced rules file; settings.json shares its process keys |
| 0029 | kit 0029 | The owner bumps the version; a release floors at the newest released heading and runs on main |
| 0030 | kit 0030 | Design choices are measured, put to the owner with a recommendation, and not asked again |
| 0031 | kit 0031 | How code works lives in the design note of its area; a synced script checks the notes |
| 0032 | kit 0032 | A Covers sentence names what its note describes, not the folder around it |
| 0033 | kit 0033 | The assistant reads and judges the golden diffs; a change the task did not mean is a finding |
| 0034 | kit 0034 | A project whose release publishes for a runtime declares it, and a single-file publish turns the analyzer off |
| 0035 | project 0001 | Adopt the kit's process; CLAUDE.md splits into requirements, decisions and design notes |
| 0036 | project 0002 | The layout stays src/ and tests/; kit.json holds the values; a release publishes framework-dependent |
| 0037 | project 0003 | Gate strictness: what is on stays on, in Directory.Build.props; dotnet format adds whitespace and IDE0005; the kit's code style and the analyzers stay off |
| 0038 | project 0004 | English everywhere, the README included: the project's rule "All text is in English" extends the kit's rule "Languages" |
| 0039 | project 0005 | No validation figures: docs/validation.json lists no case, there is no ValidationTests and no test data |
| 0040 | project 0006 | The changelog starts at 1.4.0; the git hooks are not copied; root CI is deferred; nothing is deleted |
| 0041 | project 0007 | Zero external dependencies: WPF for the windows, Windows Forms for NotifyIcon, both from the shared framework; a hand-validated TextBox, not a spinner package |
| 0042 | project 0008 | Logic in Core, adapters in App: every rule in a library without UI that the tests reference; the App is a thin layer with no unit tests |
| 0043 | project 0009 | Every .cs and .xaml file starts with the GPL notice, kept by LicenseHeaderTests; a XAML comment rewords the double-hyphen dash |
| 0044 | project 0010 | GPLv3-or-later: About shows the copyright and the warranty disclaimer, the copyright string has one source, and LICENSE is copied next to the executable |
| 0045 | project 0011 | The version has one source, Directory.Build.props, read back from the assembly: no literal in code, no version in a project file |
| 0046 | project 0012 | One timer, ticking every second, drives a deadline; no timer is ever armed for the interval |
| 0047 | project 0013 | The deadline is on the wall clock, never a count of ticks; only a backwards clock jump is guarded, against the span the deadline was armed with |
| 0048 | project 0014 | A lock suspends the countdown and every unlock restarts it in full, before anything decides on a window; a due deadline re-arms at once; saving settings restarts it |
| 0049 | project 0015 | The coordinator is the single place that decides when a reminder appears: one debounce, a raise instead of a second window, nothing on a lock |
| 0050 | project 0016 | Being on a call is read from the device, the capture record Windows keeps for its own privacy indicator, never from window titles |
| 0051 | project 0017 | Full screen is SHQueryUserNotificationState, mapped in Core; QUNS_NOT_PRESENT, quiet time and an unknown state hold nothing back |
| 0052 | project 0018 | A held-back periodic reminder is postponed by the retry gap, with no cap; a held-back unlock reminder is skipped; the tooltip gives the reason |
| 0053 | project 0019 | The activity monitor is read when a reminder is due, once per retry, and every 10 s when idle; never on the one-second tick |
| 0054 | project 0020 | --detect shows the raw readings and exits; with a duration it samples once a second and renders a timeline through DetectionLog, in Core |
| 0055 | project 0021 | Two reminders, two switches, two messages, independent of each other and of the pauses; the unlock gate sits after the schedule restart and before everything else |
| 0056 | project 0022 | The coordinator picks the text and the presenter draws it; BringToFront takes nothing, so a window keeps the text it opened with |
| 0057 | project 0023 | Messages are normalized like numbers are clamped: silently for the registry, refused by the dialog only for a reminder that is on; --preview takes a text |
| 0058 | project 0024 | The reminder window stays a fixed size, 440 x 260, and a message that would not fit is scaled down to fit |
| 0059 | project 0025 | TrayTooltip is a pure type that keeps the tooltip within 63 characters, clear of where NotifyIcon.Text throws; minutes round up, a pause shows its reason, and only a change is written |
| 0060 | project 0026 | The reminder is topmost, shown after 600 ms on the primary monitor and deaf to Enter; Settings and About are modeless and single-instance |
| 0061 | project 0027 | Settings are eight strings under HKCU\Software\MindfulnessReminder, read forgivingly and clamped; the Settings dialog is the one place that refuses |
| 0062 | project 0028 | Auto-start is one Run value under HKCU, read together with Task Manager's veto, registered on the first run only and repaired on each launch |
| 0063 | project 0029 | Unlock detection is SystemEvents.SessionSwitch behind ISessionMonitor; Locked exists only to suspend the schedule, and a lock never shows a reminder |
| 0064 | project 0030 | The dark theme is merged per window, never into Application.Resources, so the reminder stays light; three surfaces, three mechanisms, the palette outside Core |
| 0065 | project 0031 | The dark theme's template rules: a disabled trigger in every template, no TextBlock Foreground setter, DynamicResource on the Window tag, focus as a colour, two button styles, a DWM call allowed to fail |
| 0066 | project 0032 | About is a window, not a MessageBox, showing AboutText in one TextBlock and the icon's largest frame; the crash and --detect dialogs stay MessageBox |
| 0067 | project 0033 | One instance per session through a named mutex, no main window and OnExplicitShutdown, a crash dialog with the version; --preview and --detect run before the mutex |
| 0068 | project 0034 | User documentation lives in README.md, and only there |
| 0069 | kit 0035 | Every type and method in the code has an XML documentation comment; judgment, not a gate |
| 0070 | project 0035 | Auto-start under a registry that refuses access: an unreadable value counts as missing, Enable lifts only a veto it can read, and a first run takes back what it wrote and only repairs an entry it finds |
| 0071 | kit 0036 | A release stages each publish and merges it with a hash check; it may bundle sibling projects, writes release.json and may zip |
| 0072 | project 0036 | A crash ends in FailFast, and Windows restarts the registered tray copy |
| 0073 | project 0037 | A decision log in a capped text file, formatted in Core, written by App |
| 0074 | project 0038 | The project is the root of its own repository and carries the kit's files itself |
