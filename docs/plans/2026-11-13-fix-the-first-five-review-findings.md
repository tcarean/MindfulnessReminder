# Fix the first five review findings

Saved, once approved, as `docs/plans/2026-11-13-fix-the-first-five-review-findings.md`.
Branch `claude/Fix-the-first-five-review-findings`, in the worktree
`.claude/worktrees/Fix-the-first-five-review-findings`.

## Context

`docs/reviews/2026-11-13-application-code-review.md` lists twenty findings, all `open`. On
2026-11-13 the owner asked for the first five unresolved bugs to be fixed and marked in that
file, so that later fix sessions know what is done.

The first five bugs are CR-01 to CR-05. Asked the same day, the owner chose:

- **CR-01 to CR-04 and CR-06.** CR-05 (a cancelled sign-out ends the tray copy) has to be
  observed in the owner's copy first, with a Restart and a Cancel that may close other
  programs. It stays `open`, with a note, and CR-06 takes its place.
- **Q1 (CR-01):** a Save the registry refuses keeps the Settings window open, says so in its
  error line, applies nothing, and the application goes on.

ADR 0084 gives each finding a task of its own. Here the owner asked for five in one session;
the plan keeps what that rule is for: one commit per finding (CR-02 and CR-03 share one, as
the review says), each with its own test and its own diff, so each can be read and reverted
alone.

## The fixes, in the order they are done

Each starts red: a failing test for Core, a measurement for App (no unit tests there,
ADR 0042). The gate runs green before every commit.

### 1. CR-02 with CR-03: a stamp ahead of the clock is stale

- `docs/requirements.md`: R-011 and R-034 each gain the clock-moved-back sentence and example.
- Tests in `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs`, the review's two
  probes (`MutableClock.Advance` takes a negative span):
  - `R011_...`: unlock, close, clock back one hour, lock, unlock; two reminders shown.
  - `R034_...`: tick at +1 s, clock back one hour, tick, microphone live, +10 s, tick;
    `Suppression` is `MicrophoneInUse`.
- `src/MindfulnessReminder.Core/ReminderCoordinator.cs`: one private helper, used by the
  debounce in `HandleUnlock` and by `RefreshSuppression`: a time is "within" a window only
  when it is not after `now` and less than the window ago.
- `docs/design/periodic-reminder.md` and `pausing.md`: the rule, where each describes the
  comparison. CHANGELOG: Fixed.

### 2. CR-01: a refused Save keeps the window open

- A new row beside R-044 (the next free ID of that block, R-046 if free): the refusal
  (`UnauthorizedAccessException`, `SecurityException`, `RegistryRefusal.IsWriteRefusal`)
  leaves the window open with `The settings could not be saved: Windows refused the change.`,
  applies nothing, writes `Settings not saved: the registry refused (<type name>)` to the
  decision log, and the application keeps running. R-044 points to it; R-113 lists the line.
- ADR 0085 records the owner's answer and the balloon alternative.
- Core, tested on `FakeRegistryStore.WriteRefusal`: a small type that saves and then applies
  (`store.Save`, then `coordinator.ApplySettings`), returns `false` on a write refusal after
  writing the log line (`DecisionLog.SettingsSaveRefused`), and lets any other exception
  through. The tests: refused, so `false`, coordinator settings unchanged, the one log line,
  no `Settings applied`; accepted, so `true`, saved and applied.
- App: `SettingsWindow` takes the save as a `Func<ReminderSettings, bool>` instead of raising
  `Accepted`; `OnSaveClick` closes on `true`, and on `false` shows the complaint and stays
  open. `App.ShowSettings` passes the Core call. The stale comment there goes.
- `docs/design/settings-storage.md` and `windows.md`. CHANGELOG: Fixed.

### 3. CR-06: an open reminder is really raised

- Measurement first, a throwaway PowerShell WPF probe (as the review's): two topmost windows,
  the second covering the first; assigning `Topmost = true` to the first leaves it under
  (red); `Topmost = false` then `true` puts it on top (z-order read with `EnumWindows`).
- `WpfReminderPresenter.BringToFront`: `Topmost` false, then true, then `Activate()` as today,
  so it takes the focus no more than now (R-071). No new P/Invoke.
- `BreakWindow.OnSourceInitialized`: the no-op `Topmost = true` goes, and the comment says
  what is true (created topmost from the XAML).
- `docs/design/windows.md`. CHANGELOG: Fixed. R-012 and R-023 already state the behaviour.

### 4. CR-04: the process becomes PerMonitorV2 (a design choice, asked after measuring)

- Measure in the worktree, with `--preview` only (no tray start), reading
  `GetProcessDpiAwareness` from outside:
  - today's build (expected 1, system);
  - route A: `dpiAwareness` in `app.manifest`, WFAC010 muted in the csproj with its reason,
    `ApplicationHighDpiMode` and the `SetHighDpiMode` call removed;
  - route B: an entry point of the application's own that sets the mode before any WPF type
    loads.
  For each route that reaches 2: where `--preview` puts the window (its rectangle against the
  primary work area, R-070).
- **Stop and ask the owner**, with the figures and a recommendation (the rule "Two
  readings"). Then implement the chosen route.
- A new ADR for the route; ADR 0041 gets a dated note beside the sentence the review quotes
  (ADR 0024). The comments in `App.xaml.cs`, `app.manifest` and the csproj are corrected.
  `docs/design/windows.md`. CHANGELOG: Fixed.

### 5. The review file

`docs/reviews/2026-11-13-application-code-review.md`, in the last commit, once the fix commits
have their hashes:

- the Summary table and each entry: `fixed` with its commit, for CR-01 to CR-04 and CR-06;
- CR-05: `open`, with the note that it was passed over on 2026-11-13 and why;
- Q1: answered, with ADR 0085;
- "Suggested order": the lines that are done say so.

## Not in this task

- CR-05, CR-07 to CR-20, Q2 to Q8, U1 to U6.
- The version: five fixes seem to warrant 1.5.2; the owner decides (ADR 0029).

## Verification

- For each Core fix: the new test red against the code as it is (output pasted at wrap-up),
  then green.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` green before each
  commit; `scripts/check-design.ps1 -Since main` for the notes touched.
- App changes looked at with `--preview` from the worktree build: the window opens centred and
  topmost, Esc closes it; the DPI awareness measured before and after.
- For the owner, in their own copy after the merge (`scripts/release.ps1 -Build`, ADR 0078):
  - CR-01: deny "Set Value" on `HKCU\Software\MindfulnessReminder` in regedit, then Save;
  - CR-06: a 1-minute interval, the reminder left open under Task Manager set to "Always on
    top";
  - CR-04: Settings, About and the tray menu on a monitor at another scale, if one is at hand.
- `/wrap-up`, then the merge is proposed, not run.

## What happened (2026-11-13)

- Done as planned, one commit per fix: CR-02 with CR-03 in `c17fc03`, CR-01 in `e7cc5ae`,
  CR-06 in `0abb279`, CR-04 in `1613376`. The review file was marked in `a98fd0f` for the
  first four and in the commit after `1613376` for CR-04.
- CR-01: the Core type is `SettingsCommit`, and it takes the apply step as a function, not the
  coordinator, so its tests need no coordinator.
- CR-04: both routes reached per-monitor v2; the owner chose the manifest (ADR 0086). The owner
  also set the primary monitor to 150 % for the measurement, which the plan had left for after
  the merge. That showed something the plan did not foresee: with the handle started on the
  monitor at 100 %, the reminder was off-centre (1110, 570 instead of 630, 315). `BreakWindow`
  centres itself again in `OnDpiChanged`, in the same commit. R-077 is new.
- A measuring run cut short left one `--preview` process of the worktree build open, which
  made the next build keep the old executable; it was found by the copy warning, the process
  closed and the measurement repeated. The owner's tray copy was not touched.
- Not done here: a primary monitor at a lower scale than the other one was not measured, and
  the three things listed under "Verification" for the owner's copy are still to be looked at.
