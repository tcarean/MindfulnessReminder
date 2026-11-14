# Fix the last four review findings

Saved, once approved, as `docs/plans/2026-11-14-fix-the-last-four-review-findings.md`.
Branch `claude/Fix-the-last-four-review-findings`, in the worktree
`.claude/worktrees/Fix-the-last-four-review-findings`.

## Context

`docs/reviews/2026-11-13-application-code-review.md` lists twenty findings. Fifteen were fixed
on 2026-11-13, in three rounds of five. The owner asked for the next five unresolved ones to be
fixed and marked in that file, so that later fix sessions know what is done.

Only five are still open, and one of them is not a session's to fix. Assumptions (the rule
"Two readings", decided as the three earlier rounds were):

- **This round is four findings, not five: CR-17, CR-18, CR-19 and CR-20.** The fifth open one,
  CR-05 (a cancelled sign-out ends the tray copy), waits for the owner's observation in their
  own copy, as its entry says: Notepad with unsaved text, Restart, Cancel, then look for the
  icon. It is passed over a fourth time and its note says so. After this round it is the only
  open finding.
- All four are S4. CR-17, CR-18 and CR-19 are tests that do not prove what their requirement
  or their name says; CR-20 is a comment that is no longer true. **No behaviour changes**: the
  only edit under `src/` is that comment. No requirement row changes (R-113 and R-032 already
  say what the tests will pin), no CHANGELOG line (test-only and a comment), no ADR, no
  version.
- One commit per finding, as in the earlier rounds (ADR 0084).

## How each is shown

The review's S4 preamble: each strengthened or new test is first shown **red against a fault**
put into Core in the worktree only (the one test run, the fault reverted with
`git checkout -- <file>`), then green against the code as it is. The review ran no mutation
for these three, so the faults are named here. The red output goes into the review entry.

## The fixes, in the order they are done

### 1. CR-17: the repair's line precedes the refusal's (R-113)

- `tests/MindfulnessReminder.Tests/DecisionLogTests.cs`, beside
  `R113_A_refused_registration_logs_the_refusal_by_its_type`: new
  `R113_A_path_repaired_before_a_refusal_keeps_its_line_ahead_of_the_refusal`.
- State, that of `R003_A_refused_first_run_repairs_an_entry_it_found_and_keeps_its_veto`: a Run
  value `"D:\Old\App.exe"`, a Task Manager veto (`0x03`), `WriteRefusal =
  UnauthorizedAccessException` with `RefusedKeyPath = StartupRegistrar.SettingsKeyPath`, and
  the registrar given the `FakeDecisionLog`.
- Expect exactly two lines, in order: `Auto-start entry repaired to "<ExePath>"`, then
  `Auto-start: the registry refused (UnauthorizedAccessException), no registration made`.
- Faults in `StartupRegistrar.EnsureRegisteredOnFirstRun`: (a) the
  `_log.Write(DecisionLog.AutoStartRepaired(...))` removed; (b) that line written after the
  refusal's instead of where the repair happens.

### 2. CR-18: fallbacks and two clamps in one line (R-113)

- Same file, beside `R113_Loading_a_full_registry_names_nothing_and_a_clamped_number_is_reported`:
  new `R113_A_missing_value_and_two_clamped_numbers_share_one_line`.
- State: `Save(Default)`, log cleared, `PauseWhileFullScreen` deleted
  (`FakeRegistryStore.DeleteValue`), `PeriodicIntervalMinutes` = `0`, `PauseRetrySeconds` =
  `9999`. The missing value is one read after the interval, so the line also shows that the
  fallbacks come first whatever the reading order.
- Expect the one line `Settings loaded: <summary with "every 1 min" and "retry 300 s">;
  defaults used for PauseWhileFullScreen; clamped PeriodicIntervalMinutes 0 -> 1; clamped
  PauseRetrySeconds 9999 -> 300`.
- Faults: (a) the `AddClamp` of `RetryValueName` removed from `RegistrySettingsStore.Read`;
  (b) in `DecisionLog.SettingsLoaded`, the clamps appended before the fallbacks.

### 3. CR-19: the full-screen switch is shown turning something off (R-032)

- `tests/MindfulnessReminder.Tests/SuppressionPolicyTests.cs`,
  `R032_The_full_screen_switch_turns_off_only_the_full_screen_check`: add the assertion the
  review asks for, `new ActivitySnapshot(false, false, ForegroundState.FullScreen)` with the
  switch off gives `None`. The assertion it has today (microphone live and full screen gives
  `MicrophoneInUse`) stays: it is the "only" of the name, the microphone check still on.
- Fault: `settings.PauseWhileFullScreen &&` removed from `SuppressionPolicy.Evaluate`.

### 4. CR-20: `IDecisionLog`'s comment

- `src/MindfulnessReminder.Core/IDecisionLog.cs`, the `<summary>` of the interface: "the one
  implementation, in App" becomes the truth, that the implementation that keeps the lines is
  `FileDecisionLog`, in Core, and App only names its folder (ADR 0073). A comment, so no test
  comes first.

### 5. The review file and the plan

`docs/reviews/2026-11-13-application-code-review.md`, in the commit after the four, once they
have their hashes:

- the Summary table and each entry of CR-17 to CR-20: `fixed in <commit>`, the test's name and
  the red it gave against its faults;
- CR-05: still `open`, its note saying it was passed over a fourth time, and that it is now
  the only open finding;
- "Suggested order": lines 8, 9 and 10 done.

This plan is saved under `docs/plans/` in the first commit and gets its "What happened" section
at wrap-up.

## Not in this task

- CR-05, Q2 to Q9, U1 to U6.
- Any change of behaviour under `src/`: if a new test is red against the code as it is, that is
  a finding of its own; it is reported and the owner decides.
- The version: the owner decides (ADR 0029).

## Verification

- Each test red against its faults (one `dotnet test --filter` per fault), then the fault
  reverted and `git status` showing `src/` untouched but for the comment of CR-20.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` green before each
  commit; `scripts/check-design.ps1 -Since main` at the end.
- `/wrap-up`; then the merge is proposed, not run.

## What happened (2026-11-14)

- Done as planned, one commit per finding: CR-17 in `6cd52f7`, CR-18 in `482328e`, CR-19 in
  `421d82e`, CR-20 in `5e4b28b`; the review file was marked in the commit after them. The only
  change under `src/` is the comment of CR-20: every new or strengthened test was green
  against the code as it is.
- Each test was red against each of its faults, five runs in all, and the failures are quoted
  in the entries of the review file.
- One deviation, in CR-18: the test first compared the list of lines, and its failure showed
  only the start of the line, the same on both sides. It now compares the one line as a
  string, so a failure points at the suffix that differs; the two faults were run again with
  that assertion, and those are the failures quoted.
- CR-20's comment also stopped saying that the writer "never throws": it refuses a null text,
  and what it swallows is every failure of the write.
- The suite went from 394 to 396 tests: two new facts. CR-19 strengthened a test that was
  there.
- CR-05 was passed over a fourth time and is the only finding left open; it waits for the
  owner's observation in their own copy.
