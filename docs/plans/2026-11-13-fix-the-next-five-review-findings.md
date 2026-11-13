# Fix the next five review findings

Saved, once approved, as `docs/plans/2026-11-13-fix-the-next-five-review-findings.md`.
Branch `claude/Fix-the-next-five-review-findings`, in the worktree
`.claude/worktrees/Fix-the-next-five-review-findings`.

## Context

`docs/reviews/2026-11-13-application-code-review.md` lists twenty findings. CR-01 to CR-04 and
CR-06 were fixed earlier on 2026-11-13. The owner asked for the next five unresolved bugs to
be fixed and marked in that file, so that later fix sessions know what is done.

Asked the same day, the owner chose:

- **CR-07, CR-08, CR-09, CR-10 and CR-11.** CR-05 (a cancelled sign-out ends the tray copy)
  still has to be observed in the owner's copy first; it stays `open`, its note extended, and
  CR-11, the next in the list, takes its place.
- **CR-08:** a dark context menu (templates in the theme), not "no context menu".
- **CR-07:** the cut is fixed, and the decision log also writes a line it cannot encode, with
  the half pair replaced, instead of losing it.

As in the first round (ADR 0084): one commit per finding, each with its own test and diff.

## The fixes, in the order they are done

Each Core fix starts red (the test first, its failure pasted at wrap-up). The gate runs green
before every commit.

### 1. CR-09: a carriage return on its own is folded

- `docs/requirements.md`, R-110: "every line break" names its three forms (`\r\n`, `\n`, `\r`).
- Test in `tests/MindfulnessReminder.Tests/DecisionLogTests.cs`, the review's probe:
  `R110_A_carriage_return_on_its_own_becomes_a_separator_too`
  (`FormatLine(at, "first\rsecond")` gives `... first | second`).
- `src/MindfulnessReminder.Core/DecisionLog.cs`, `FormatLine`: fold `\r\n`, then `\n`, then `\r`.
- CHANGELOG: Fixed.

### 2. CR-07: a message is never cut through a character, and the log writes what it cannot encode

- `docs/requirements.md`: R-041 says the limit counts UTF-16 units and that a cut never leaves
  half a surrogate pair (example: 49 letters and an emoji keep the 49 letters). R-110 says a
  text with an unpaired surrogate is written with U+FFFD in its place.
- Tests, the review's two probes:
  - `ReminderSettingsStoreTests.cs`:
    `R041_A_message_cut_at_the_limit_does_not_end_in_half_a_character`
    (`NormalizeMessage(new string('x', 49) + "\U0001F600")` is the 49 letters).
  - `FileDecisionLogTests.cs`:
    `R110_A_text_that_cannot_be_encoded_is_written_with_a_replacement_character`
    (`Write("a\uD83Db")`, one line read back, ending in `a`, U+FFFD and `b`).
- `src/MindfulnessReminder.Core/ReminderSettings.cs`, `NormalizeMessage`: after the cut at
  `MaxMessageLength`, drop a trailing high surrogate, then `TrimEnd` as today.
- `src/MindfulnessReminder.Core/FileDecisionLog.cs`, `Write`: `File.AppendAllText` with an
  explicit `UTF8Encoding(false)` (no byte order mark, replacement instead of throwing); the
  existing no-BOM test keeps guarding the first half.
- ADR 0088 (the cut and the log, with "lose the line" as the rejected alternative).
  `docs/design/two-reminders.md` or `settings-storage.md`, whichever describes the message
  rule, and the note that covers `FileDecisionLog`. CHANGELOG: Fixed.
- Not changed, and said so in the review entry: a lone surrogate elsewhere in a hand-written
  registry value still reaches the reminder window (only the log is made safe), and whether
  typing in Settings can leave half a pair at 50 stays unchecked.

### 3. CR-10: `--preview` does not take one of the application's own switches as its text

- `docs/requirements.md`: R-045 says the text is the first argument after `--preview` that is
  not one of the application's own switches (`--preview`, `--detect`, `--detect=N`,
  `--restarted`); R-102's "`--preview` ... ignore the argument" gains the example
  `--preview --restarted` showing `Ground yourself`. R-045 becomes `implemented` (it gets a
  test; `RepoInvariantTests` requires the status to follow).
- Assumption (the rule "Two readings", decided): a switch is skipped, not taken as "no text",
  so `--preview --restarted "Breathe"` shows `Breathe`. `--detect`'s own parsing is not touched.
- New `tests/MindfulnessReminder.Tests/CommandLineSwitchesTests.cs` (licence header): a theory
  `R045_The_preview_text_is_the_first_argument_after_the_switch_that_is_not_a_switch` and
  `R102_Preview_does_not_show_the_restarted_switch_as_its_message`.
- `src/MindfulnessReminder.Core/CommandLineSwitches.cs`: `PreviewText(args)`, returning the raw
  text or `null`, reusing `Has`/`HasExactly`'s matching. `App.xaml.cs`, `PreviewMessage`:
  `HasExactly(args, Preview)` then `NormalizeMessage(CommandLineSwitches.PreviewText(args))`;
  the parsing leaves App (ADR 0042).
- `docs/design/code-map.md` / `windows.md` where `--preview` is described. CHANGELOG: Fixed.
- Looked at with `--preview --restarted` and `--preview "text"` from the worktree build.

### 4. CR-11: the tooltip test can fail

- Test only (`TrayTooltipTests.cs`,
  `R062_The_longest_tooltip_the_app_can_produce_fits_the_tray_limit`): besides the length,
  every candidate must be uncut, by ending in ` min`, `mic is on` or `full screen`, or being the
  banner.
- Shown red against the review's mutation 1 (the microphone reason lengthened past 63 in
  `TrayTooltip.Format`, in the worktree only, reverted), then green against the code as it is.
  No CHANGELOG line (test-only).

### 5. CR-08: the text boxes of Settings open a dark context menu

- Measure first (the rule "Measured numbers"): a throwaway PowerShell WPF probe, in the
  scratchpad, that merges the worktree's `DarkTheme.xaml` into a window's resources, opens a
  text box's built-in menu and renders it to a PNG. Red: the menu is light today.
- Route A, tried first: implicit `ContextMenu` and `MenuItem` styles in
  `src/MindfulnessReminder.App/Theme/DarkTheme.xaml`, from the palette already there
  (`SurfaceBrush`, `ControlBorderBrush`, `SurfaceHoverBrush`, `TextPrimaryBrush`,
  `TextDisabledBrush`), each template with its disabled trigger (ADR 0065); the gesture text
  (`Ctrl+V`) in `TextSecondaryBrush`.
- Route B, only if the probe shows the window's implicit styles do not reach the menu WPF
  builds in code: the theme's `TextBox` style sets an explicit `ContextMenu` (Cut, Copy, Paste
  on `ApplicationCommands`) carrying the same templates by key. The theme stays merged per
  window either way (ADR 0064).
- The probe's PNG after the change is the measurement recorded: dark menu, a disabled item
  (Cut with nothing selected) greyed, hover visible.
- ADR 0089 (dark menu over no menu, and the route the probe chose).
  `docs/design/dark-theme.md`. R-075 names the menu. CHANGELOG: Fixed.
- Left for the owner, in their copy: Settings, right-click in a message box and in a number
  box (Settings opens only from a tray start, ADR 0042).

### 6. The review file

`docs/reviews/2026-11-13-application-code-review.md`, in the last commit, once the fix commits
have their hashes:

- the Summary table and each entry: `fixed in <commit>` with what was done and what is left for
  the owner, for CR-07 to CR-11;
- CR-05: still `open`, the note saying it was passed over a second time and why;
- "Suggested order": the lines that are done say so (line 8 partly: CR-16 and CR-19 remain;
  line 10 partly: CR-20 remains).

## Not in this task

- CR-05, CR-12 to CR-20, Q2 to Q8, U1 to U6.
- `--detect`'s parsing, `IsMessageAcceptable`, and lone surrogates inside a stored message.
- The version: the owner decides (ADR 0029).

## Verification

- Each new Core test red against the code as it is, then green; CR-11 red against mutation 1.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` green before each
  commit; `scripts/check-design.ps1 -Since main` for the notes touched.
- `--preview` from the worktree build for CR-10 and for an over-long message ending in an
  emoji (CR-07), the whole output kept so no preview process is left open.
- The WPF probe for CR-08, before and after.
- `/wrap-up`; then the merge is proposed, not run.

## What happened (2026-11-13)

- Done as planned, one commit per fix: CR-09 in `cdab57e`, CR-07 in `3eb5736`, CR-10 in
  `3766c7f`, CR-11 in `234da8f`, CR-08 in `d1d909d`; the review file was marked in the commit
  after them.
- CR-10: the tests were first run against today's parsing moved to Core unchanged, so that
  the red was the behaviour and not a missing function: 7 of 15 failed, the cases with a
  switch after `--preview`.
- CR-08: route A did not work. With implicit `ContextMenu` and `MenuItem` styles in the theme
  the probe still read `EditorContextMenu` and `#F5F5F5`, so route B was taken: the theme
  declares the menu and the `TextBox` style sets it. Two things the plan did not foresee:
  - The probe opens the menu without input, and WPF asks a command whether it can run only
    after input, so Cut and Copy read as disabled with text selected until the probe asked
    for the requery itself. That was the probe's fault, not the menu's.
  - Without a command target the items stayed disabled in a window that was not the active
    one, even after the requery; each item now names the box the menu was opened on.
  The plan's PowerShell probe runs on WPF for .NET Framework 4, so a second throwaway
  program, on .NET 8, opened the worktree build's `SettingsWindow` itself with a save function
  that does nothing: no tray start, no registry. The figures in ADR 0089 are from that one.
- A test written with the escape for U+FFFD was saved with the character itself; found when
  the diff was read back, and put right before the commit.
- Not done here: the things listed for the owner's copy (CR-08: a right-click, Shift+F10 and a
  Paste in Settings), and CR-07's two leftovers, which the review entry names.
- The owner then ran `/code-review` on the branch. It reported ten findings, none severe,
  and the owner had the two that were faults in the new code fixed, each test first:
  - CR-07's guard sat inside the cut, so a value the old version had already saved, 50 units
    ending in half a pair, was never repaired. The end is now looked at whatever the length.
    The first test for it was red for the wrong reason: a row of a theory cannot carry half a
    pair (an attribute stores UTF-8), so the test got U+FFFD; the half is now added in the
    test's body, and the test was shown red against the code before the fix.
  - `--preview --detect 30` showed `30`: the value after a bare `--detect` is now passed over.
- Asked whether the other eight were worth fixing, the owner had everything fixed that was:
  - `--detect --restarted 30` took one reading. Its parsing moved from App to Core
    (`TryGetDetectDuration`), tests first against the parsing as it was (two rows red), and
    `--preview` got one call (`TryGetPreviewText`) in place of two scans. `84154e0`.
  - The tooltip test compares each tooltip with its uncut text. `48421c2`.
  - The field `Encoding` in `FileDecisionLog` is `Utf8WithReplacement`. `9192267`.
  - The menu's command target, measured with WPF's own opener in place of the probe setting
    the properties: it works. No code changed.
  - The menu's font: measured, the two setters are needed, and now say why.
  - The menu's corners: the popup is measured transparent, but a screen capture showed the
    lock screen, the workstation being locked while the owner worked from another device.
    Left for the tray copy.
- Not fixed: a character of several code points can still be cut between them. It is Q9 in
  the review file, with a recommendation to leave it.
