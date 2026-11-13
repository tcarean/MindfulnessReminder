# Review the application code

Saved, once approved, as `docs/plans/2026-11-13-review-the-application-code.md`. Branch
`claude/Review-the-application-code`, worktree `.claude/worktrees/Review-the-application-code`.

## Context

The first version of the application was written by an earlier model and most of it was never
reviewed. On 2026-11-13 the owner asked for a review of the whole project that ends in one
documented list of problems, to be fixed one by one in order of severity, each in a session of
its own. The owner chose: the application now (Core, App, the tests of Core), the process
(`scripts/`, hooks, `ProcessTests`) in a separate pass later.

This task finds and documents. It fixes nothing: no file under `src/` or `tests/` is changed in
what gets committed.

Measured on 2026-11-13: Core 31 files and about 3,000 lines, App 19 files and about 2,700 lines,
the tests of Core 21 files and about 3,900 lines. That fits one session if the reading is done by
subagents, so the list is one list.

## Scope

In:

- `src/MindfulnessReminder.Core/` and `src/MindfulnessReminder.App/`, every `.cs` and `.xaml`,
  the two `.csproj` and `app.manifest`.
- `tests/MindfulnessReminder.Tests/`, read for one question: does each `R###_` test prove its
  requirement, and does a fake behave like the adapter it stands in for.
- `docs/requirements.md` and the design notes, as the statement of what the code should do. A
  place where code and requirement disagree is a finding.

Out: `scripts/`, `.claude/`, `tests/MindfulnessReminder.ProcessTests/`, CI, the README's wording,
missing XML comments (a known task, ADR 0069), the analyzer findings `CLAUDE.md` already counts.

## Steps

### 1. Worktree

`git worktree add .claude/worktrees/Review-the-application-code -b claude/Review-the-application-code main`,
then save this plan to `docs/plans/`.

### 2. Four reviewers in parallel

Four `general-purpose` subagents, each with a clean context, each reading every file of its zone
in full, plus the notes and ADRs of that zone. None is told what to expect to find.

| Zone | Files | Reads first |
| --- | --- | --- |
| A. Core, when a reminder appears | `ReminderCoordinator`, `IntervalSchedule`, `SuppressionPolicy`, `ForegroundState`, `ActivitySnapshot`, `TrayTooltip`, `DetectionLog`, `WindowPlacement`, `CommandLineSwitches`, the seam interfaces; their tests | `periodic-reminder.md`, `pausing.md`, `two-reminders.md`, `tray-tooltip.md`, ADR 0046 to ADR 0059 |
| B. Core, storage and lifecycle | `ReminderSettings`, `RegistrySettingsStore`, `RegistryAutoStartManager`, `StartupRegistrar`, `RegistryRefusal`, `AutoStartStatus`, `DecisionLog`, `FileDecisionLog`, `CrashReport`, `ApplicationRestart`, `Version`, `LicenseInfo`; their tests and `FakeRegistryStore` | `settings-storage.md`, `versioning-and-licence.md`, ADR 0061, ADR 0062, ADR 0070, ADR 0072, ADR 0073 |
| C. App, composition and adapters | `App.xaml.cs`, `WindowsActivityMonitor`, `SystemEventsSessionMonitor`, `DispatcherPeriodicTimer`, `WindowsRegistryStore`, `RestartRegistration`, `TrayIconHost`, the csproj and manifest; the Core interface each adapter fills | `code-map.md`, `pausing.md`, ADR 0042, ADR 0063, ADR 0067, ADR 0072 |
| D. App, windows and theme | `BreakWindow`, `SettingsWindow`, `AboutWindow`, `WpfReminderPresenter`, `DarkTheme.xaml`, `DarkMenuRenderer`, `ImmersiveDarkMode` | `windows.md`, `dark-theme.md`, ADR 0058, ADR 0060, ADR 0064 to ADR 0066, ADR 0081 |

What each looks for:

- A and B: wrong logic against the requirement rows, boundary values, the clock moving backwards
  or jumping (sleep, DST, manual change), state left wrong after lock, unlock, settings change or
  an exception; tests that cannot fail or that assert something other than their requirement.
- C: P/Invoke signatures and marshalling, handles and registry keys not released, events never
  unsubscribed (`SystemEvents` holds static references), which thread each callback arrives on,
  the single-instance mutex, the crash path, anything that decides instead of adapting (ADR 0042).
  For every adapter: what the Core interface promises against what the adapter does.
- D: window lifetime and reuse after close, focus and topmost, several monitors and DPI, the
  keys the reminder swallows, validation in Settings, anything that grows while the app runs for
  weeks.

Each returns findings in one shape: file and line, what is wrong, a concrete scenario (state and
input, then the wrong result), the requirement or ADR it contradicts, how sure it is, and how to
confirm it. Not wanted: style, naming, missing comments, a proposal that undoes an ADR. A decision
that itself looks wrong is reported apart, as a question for the owner.

### 3. Verification, by me

A reviewer's report is a claim, not a finding. For each one I open the code and follow the
scenario myself. Then:

- Core: where it is cheap, a throwaway test in the worktree that reproduces it, run with
  `dotnet test --filter`; its output goes into the entry and the test is reverted, since the real
  one belongs to the fix (the rule "Test before fix").
- App: `--preview` and `--detect` where they reach the code. A full tray start is not run from the
  worktree, because it repairs the auto-start entry to that build (R-006); a finding that needs it
  is marked "confirmed by reading, to be observed in the owner's copy".
- Claims about a Windows API are checked against its documentation, not memory.

I also read the seam myself across zones (A's coordinator against C's composition in
`App.xaml.cs`), since no single reviewer holds both sides. A claim I cannot confirm is listed as
unverified; one I disprove is listed as rejected, in one line, so it is not found again.

### 4. The list

`docs/reviews/2026-11-13-application-code-review.md`, in English, a new folder. One entry per
finding:

- `CR-NN`, a title, the severity;
- where: `file:line`;
- what happens: the scenario and the wrong result;
- contradicts: the requirement row or ADR, or "none stated";
- confirmed by: the test output, the tool run, or "reading";
- direction of the fix, one or two lines, and the test that would reproduce it;
- status: `open` (a fix task later sets it to `fixed` with its commit).

Severity:

| | Meaning |
| --- | --- |
| S1 | Wrong in ordinary use: a reminder not shown or shown during a call, a crash, settings lost |
| S2 | Wrong in a plausible case (sleep and resume, clock change, second monitor, remote session), or a leak that grows over days |
| S3 | Rare case, cosmetic, a misleading text |
| S4 | No wrong behaviour shown: a test that does not prove its requirement, fragile code |

Sorted S1 first; within a level, the cheaper fix first. Findings that touch the same lines are
marked as one fix task. After the entries: questions for the owner (code against a requirement
where it is unclear which is right, a decision that looks wrong), unverified claims, rejected
claims, and what the review did not cover.

### 5. Records

- An ADR, the next free number: review findings live in `docs/reviews/`, one file per review, and
  each is closed by its own task, test first. One line for `docs/reviews/` in the Layout of
  `CLAUDE.md`.
- No CHANGELOG line (documentation only). No version bump.
- "What happened" at the end of the saved plan: how many claims each reviewer made, how many were
  confirmed, rejected and left unverified.

### 6. Gate, commit, proposal

`scripts/verify.ps1` green with `git status` showing nothing under `src/` or `tests/`, read the
diff, commit on the branch, propose the merge.

## Verification

- Every entry of the list has a "confirmed by" that is not a reviewer's word.
- `git diff main...HEAD --stat` names only files under `docs/` and `CLAUDE.md`.
- `scripts/verify.ps1` is green, including the decisions check for the new ADR and the 200-line
  limit of `CLAUDE.md`.
- The count in "What happened" adds up: claims = confirmed + unverified + rejected.

## What happened (2026-11-13)

- The four reviewers ran in parallel and read their zones in full. Claims of a defect or a test
  gap: 8 from zone A, 8 from zone B, 6 from zone C, 4 from zone D, 26 in all. Three were made
  more than once (the refused Save by B, C and D; the unlock debounce by A and B; the DPI mode
  by C and D), which leaves 22 distinct claims.
- Of the 22: 21 confirmed, 1 left unverified (U1, the stale capture record), 0 rejected. The 21
  became 20 entries, CR-01 to CR-20, because "no test puts a surrogate pair at the cut" is part
  of CR-07. No S1, two S2, eight S3, ten S4.
- The reviewers also raised 16 questions. They are in the list as Q1 to Q8 (several asked the
  same thing about the refused Save), U2 to U6, and inside CR-01 and U1.
- How the 21 were confirmed: five probe tests, all red as predicted; seven mutations of Core
  under which all 321 tests stayed green; a measurement on the owner's running copy (CR-04); a
  WPF probe run from PowerShell (CR-06); the WPF source and Microsoft's documentation (CR-04,
  CR-05); and reading alone for CR-01, CR-08, CR-10, CR-17 to CR-20. The probe file and the
  mutations were removed before the gate ran: `git status` showed nothing under `src/` or
  `tests/`.
- Deviations:
  - The mutations were not in the plan, which named throwaway tests only. A reviewer's "this
    test cannot fail" is a claim about the suite, and breaking the code it guards is the direct
    way to check it.
  - Things outside the repository were read, which the plan did not say: the DPI awareness of
    the owner's running tray copy and its decision log, by this session; and, by the reviewer of
    zone C on its own initiative, the ConsentStore records, the System event log for 2026-11-04
    to 2026-11-06 and the IL of three WPF methods. All read-only. Nothing from them left the
    machine.
  - CR-05 is entered as a finding although its scenario was not run: what WPF does on the
    session query is confirmed from its source, and the entry says that the cancel itself is to
    be observed, and names the one observation that goes the other way.
  - The Stop hook asked for a green gate when the turn first ended with the probe file in
    `tests/`, so the probes were deleted then, before the fourth report arrived; its claims
    needed none.
