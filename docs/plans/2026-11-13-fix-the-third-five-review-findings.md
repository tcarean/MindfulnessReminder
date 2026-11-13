# Fix the third five review findings

Saved, once approved, as `docs/plans/2026-11-13-fix-the-third-five-review-findings.md`.
Branch `claude/Fix-the-third-five-review-findings`, in the worktree
`.claude/worktrees/Fix-the-third-five-review-findings`.

## Context

`docs/reviews/2026-11-13-application-code-review.md` lists twenty findings. CR-01 to CR-04 and
CR-06 to CR-11 were fixed earlier on 2026-11-13. The owner asked for the next five unresolved
ones to be fixed and marked in that file, so that later fix sessions know what is done.

Assumptions (the rule "Two readings", decided as the two earlier rounds were):

- **The five are CR-12, CR-13, CR-14, CR-15 and CR-16.** CR-05 is still `open` and waits for
  the owner's observation in their copy, as its entry says; it is passed over a third time and
  its note says so. CR-12 to CR-15 are line 3 of "Suggested order"; CR-16 is the next open one.
- All five are S4: a test that passes with the code it claims to guard removed. **No code in
  `src/` changes**, no requirement row changes (R-010, R-021, R-035 and R-043 already say what
  the tests will pin), no CHANGELOG line (test-only), no ADR.
- One commit per finding, as in the first two rounds (ADR 0084).

## How each is shown

The review's S4 preamble: each strengthened test is first shown **red against the mutation
named** (the fault put into Core in the worktree only, the one test run, the fault reverted
with `git checkout -- <file>`), then green against the code as it is. The red output is pasted
at wrap-up. The gate runs green before every commit.

## The fixes, in the order they are done

All but the last are in `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs`, on the
fakes already there (`FakePresenter.BringToFrontCount`, `FakeDecisionLog.Lines`,
`MutableClock.Advance`, `FakeActivityMonitor.Snapshot`).

### 1. CR-12: a second subscription is seen

- `R010_Start_twice_subscribes_only_once`: besides `ShowCount == 1`, assert
  `BringToFrontCount == 0` and exactly one `Workstation unlocked` line in the log.
- Mutation 2: the `_started` guard removed from `ReminderCoordinator.Start`. The second handler
  finds the window open and raises it, so the count is 1 and the log has two lines.

### 2. CR-13: the fresh reading after a save and after a lock

- `R035_Applying_settings_takes_a_fresh_reading_on_the_next_tick`: start with the microphone
  pause off and the full-screen pause on (not both off), microphone live; tick at +1 s (a
  reading is stamped, `None`); `ApplySettings(Default)`; tick at +2 s; expect
  `MicrophoneInUse`. Mutation 3: `_lastActivityReadAt = null` removed from `ApplySettings`.
- New `R035_A_lock_takes_a_fresh_reading_on_the_first_tick_after_the_unlock`: unlock reminder
  off (so the unlock itself reads nothing); tick at +1 s; lock; unlock; microphone live; tick
  at +2 s; expect `MicrophoneInUse`. Mutation 4: the same line removed from `HandleLock`.

### 3. CR-14: the tick is unsubscribed on Dispose

- `R021_Dispose_stops_and_disposes_the_timer`: advance the clock by the interval (30 min)
  before `RaiseTick`, so a coordinator still subscribed would show a reminder.
- Mutation 6: `_timer.Tick -= OnTick` removed from `Dispose`.

### 4. CR-15: `CountdownChanged` on a save

- New `R021_A_save_that_switches_the_periodic_reminder_off_raises_the_countdown_change_event`:
  after `Start`, count the event around `ApplySettings(new ReminderSettings(false, 30))`;
  expect exactly one, with `Remaining` null when it is raised (what the tooltip would read).
  The existing four-step test stays as it is.
- Mutation 5: the `CountdownChanged` raise removed from `ApplySettings`.

### 5. CR-16: `IsRetryInRange` has a test

- `tests/MindfulnessReminder.Tests/ReminderSettingsStoreTests.cs`, beside the interval's
  theory: `R043_The_dialog_retry_range_check_matches_the_clamp`, four rows
  (`MinRetrySeconds - 1` false, `Min` true, `Max` true, `Max + 1` false).
- Mutation 7: `IsRetryInRange` changed to `seconds >= 0`.

### 6. The review file and the plan

`docs/reviews/2026-11-13-application-code-review.md`, in the commit after the five, once they
have their hashes:

- the Summary table and each entry of CR-12 to CR-16: `fixed in <commit>`, the test's name and
  the red it gave against its mutation;
- CR-05: still `open`, its note saying it was passed over a third time and why;
- "Suggested order": line 3 done; line 8 says only CR-19 remains.

This plan is saved under `docs/plans/` in the first commit and gets its "What happened"
section at wrap-up.

## Not in this task

- CR-05, CR-17 to CR-20, Q2 to Q9, U1 to U6.
- Any change under `src/`: if a strengthened test is red against the code as it is, that is a
  finding of its own; it is reported and the owner decides.
- The version: the owner decides (ADR 0029).

## Verification

- Each test red against its mutation (one `dotnet test --filter` per mutation), then the
  mutation reverted and `git status` showing `src/` untouched.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` green before each
  commit; `scripts/check-design.ps1 -Since main` at the end.
- `/wrap-up`; then the merge is proposed, not run.

## What happened (2026-11-13)

- Done as planned, one commit per finding: CR-12 in `86578f3`, CR-13 in `59971f4`, CR-14 in
  `7858357`, CR-15 in `a839b6d`, CR-16 in `da8c69f`; the review file was marked in the commit
  after them. Nothing under `src/` changed: every strengthened test was green against the code
  as it is.
- Each test was red against its mutation, and the failure is quoted in its entry of the review
  file. Mutations 3 and 4 (CR-13) were put in together and the two tests run in one go, not one
  run per mutation as planned: the save's test never locks and the lock's test never saves, so
  each failure still belongs to one mutation.
- Two assertions the plan did not name: both CR-13 tests first assert that the tick before the
  save, or before the lock, did read the monitor (`ReadCount == 1`), so that neither can go
  back to starting from a state with no stamp, which was the fault the review found.
- The suite went from 388 to 394 tests: two new facts, and a theory of four rows.
