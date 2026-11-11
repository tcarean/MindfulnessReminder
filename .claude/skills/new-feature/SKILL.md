---
name: new-feature
description: Start a new feature the project way - requirement first, acceptance test second, plan third, code last. Use when the user asks for new behaviour or says /new-feature.
---

# New feature

Follow the order. Do not write implementation code before step 4 is approved.

1. **Requirement.** Read `docs/requirements.md`. Draft the new entries with the next free IDs,
   status `draft`: observable behaviour, one concrete example with numbers, error behaviour,
   the exit code when there is one. A requirement about a reminder says which of the two it is
   (unlock or periodic), what the schedule does (restart, suspend, postpone) and what the
   tooltip reads; one about a setting names the registry value, its default and its bounds (the
   rule "Settings are strings, read forgivingly"); one about a window says what is on screen and
   which keys do what. Show the draft to the user and wait for it to be accepted before going
   on.
2. **Acceptance test.** Write the black-box test(s) named after the requirement IDs
   (`R###_...`), in `tests/MindfulnessReminder.Tests`, on the fakes the suite already has
   (`FakeRegistryStore`, and the fakes inside `ReminderCoordinatorTests`: a clock, a timer, a
   session monitor, a presenter, an activity monitor). A rule gets its test in Core; if it
   cannot be tested there, it is in the wrong project (the rule "Logic in Core, adapters in
   App"). Test names are sentences with underscores, `R###_` first. A change to a script gets
   its test in `tests/MindfulnessReminder.ProcessTests` instead (the rule "Process files"). If
   the feature has measurable output, add a case to `docs/validation.json` with `expected` left
   as `null` and a note in `docs/validation.md` saying it is unmeasured. The tests must fail
   now. Run them and paste the failing output.
3. **Decision.** If the feature needs a design choice with more than one reasonable answer,
   write `docs/decisions/NNNN-slug.md` (copy `0000-template.md`) with status `proposed`, the
   alternatives and your recommendation, and ask. Only the owner's explicit yes in this session
   makes it `accepted` (with the date); then, and not before, go on to the plan and the code
   without re-asking. A choice the owner answered in an earlier session is not asked again: the
   plan names its ADR, and the owner's approval of the plan is that yes. Measure before you
   design: a choice made on a guess is the kind an ADR later records as rejected.
4. **Plan.** Enter plan mode. List the files to touch, the order, and how each step is verified.
   After approval save the plan to `docs/plans/YYYY-MM-DD-slug.md`.
5. **Code.** Implement in `MindfulnessReminder.Core` first (the compile boundary CLAUDE.md
   names), then the adapter or the window, which only call it. Keep
   `scripts/verify.ps1 -SkipTests` green while editing; run the full gate before claiming done
   (on a branch that changes `scripts/` or the process tests, about three minutes: give it a
   10-minute timeout).
   A change under `src/MindfulnessReminder.App/` is not done until it was looked at:
   `--preview` runs the reminder window and `--detect` the activity monitor, and nothing else of
   the App. Anything else (the tray menu, Settings, About, the presenter, the timers, the
   session monitor) runs only in a full tray start, and a full start of a worktree build
   repairs the auto-start entry to that build (R-006): do not start one; list the change for
   the owner to look at in their own copy after the merge (**Exit**, `scripts/release.ps1
   -Build` in the main checkout, then start `artifacts/<version>/MindfulnessReminder.exe`; ADR
   0078).
6. **Figures and golden files.** There is no numeric code, no validation case and no golden
   file (ADR 0039): skip, unless the feature measures something on a machine; then the first
   case and `ValidationTests` are written here, and `/validate` runs them. A golden file, if one
   is ever added, is regenerated only for an intended change of its output: read its whole diff
   and carry each changed line, with the change the task meant to make, into the wrap-up report
   (the rule "No test edited to pass", ADR 0033).
7. **Measure.** If step 6 added a case, fill in the `validation.json` figures from real
   output, paste the output in the session, and record the date in `validation.md`.
8. **Record.** Set the requirement status to `implemented`, add the CHANGELOG line, and update
   `README.md`, the user documentation, if the behaviour is user-visible: its "Using it" list
   and the section of the feature, in the words a user reads and in plain sentences (ADR 0077,
   ADR 0079); a design choice goes in `docs/decisions/`, never in the README; then `/wrap-up`.
