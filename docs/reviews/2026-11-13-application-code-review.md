# Review of the application code, 2026-11-13

The first review of the whole application: `src/MindfulnessReminder.Core/`,
`src/MindfulnessReminder.App/` and the tests of Core in `tests/MindfulnessReminder.Tests/`, as
they stood at commit `17cc429` (version 1.5.1). The process (`scripts/`, the hooks,
`tests/MindfulnessReminder.ProcessTests/`) was left for a review of its own. How findings are
kept and closed is ADR 0084; the plan of this review is
`docs/plans/2026-11-13-review-the-application-code.md`.

Nothing was fixed. Each entry is meant to be enough for a session that has not read the rest:
it starts with the failing test (the rule "Test before fix"), and sets the entry's status to
`fixed` with its commit.

Line numbers are those of `17cc429`; each entry also names the member, since the lines move as
fixes land.

## Severity

| | Meaning |
| --- | --- |
| S1 | Wrong in ordinary use: a reminder not shown or shown during a call, a crash, settings lost |
| S2 | Wrong in a plausible case (sleep and resume, a clock change, a second monitor, a remote session), or a leak that grows over days |
| S3 | A rare case, a cosmetic fault, a misleading text |
| S4 | No wrong behaviour shown: a test that does not prove its requirement, a comment that is no longer true |

## Summary

| ID | Severity | Finding | Status |
| --- | --- | --- | --- |
| CR-01 | S2 | A settings save the registry refuses ends the application through the crash dialog | fixed in `e7cc5ae` |
| CR-02 | S2 | After the clock moves back, every unlock reminder is skipped until the clock catches up | fixed in `c17fc03` |
| CR-03 | S3 | After the clock moves back, the tooltip stops following a call or full screen | fixed in `c17fc03` |
| CR-04 | S3 | The process is system-DPI-aware, not PerMonitorV2 as the code and ADR 0041 say | fixed in `1613376` and `a9ae321` |
| CR-05 | S3 | A sign-out or restart that is cancelled still ends the tray copy, with no trace | open: to be observed first |
| CR-06 | S3 | Assigning `Topmost = true` again does nothing, so an open reminder is raised by `Activate()` alone | fixed in `0abb279` |
| CR-07 | S3 | A message cut at 50 can end in half a surrogate pair, and log lines carrying it are lost | fixed in `3eb5736` |
| CR-08 | S3 | The text boxes of Settings open a light context menu in the dark window | fixed in `d1d909d` |
| CR-09 | S3 | A carriage return on its own is not folded in a log line | fixed in `cdab57e` |
| CR-10 | S3 | `--preview --restarted` shows `--restarted` as the message | fixed in `3766c7f` |
| CR-11 | S4 | `R062_The_longest_tooltip...` cannot fail | fixed in `234da8f` |
| CR-12 | S4 | `R010_Start_twice_subscribes_only_once` cannot see a second subscription | fixed in `86578f3` |
| CR-13 | S4 | R-035's fresh reading after a save and after a lock is not pinned | fixed in `59971f4` |
| CR-14 | S4 | `R021_Dispose_stops_and_disposes_the_timer` does not pin the tick's unsubscription | fixed in `7858357` |
| CR-15 | S4 | `CountdownChanged` on `ApplySettings` is not pinned | fixed in `a839b6d` |
| CR-16 | S4 | `IsRetryInRange` has no test, though R-043 names it | fixed in `da8c69f` |
| CR-17 | S4 | R-113's order of lines after a refused registration is not pinned | fixed in `6cd52f7` |
| CR-18 | S4 | R-113's combined suffixes are not pinned | fixed in `482328e` |
| CR-19 | S4 | `R032_The_full_screen_switch...` does not show the switch turning anything off | fixed in `421d82e` |
| CR-20 | S4 | `IDecisionLog`'s comment places its implementation in App | fixed in `5e4b28b` |

A status of `fixed in` names the commit of the fix, on `main` once its branch is merged; the
entry below says what was done and what is left for the owner to look at. The entries not
marked are open, and the next fix session takes the first of them in "Suggested order".

No S1 was found: for a clock that only moves forward, with a registry that accepts writes, the
reviewers and the probes found the reminders appearing and being held back as
`docs/requirements.md` says.

## How the findings were confirmed

Four reviewers, each with a clean context, read one zone each in full (Core's decision logic,
Core's storage and lifecycle, App's composition and adapters, App's windows and theme). Their
reports were claims. Each claim was then followed in the code by the session that wrote this
file, and where possible shown by something that runs:

- **Probes.** Five throwaway tests, each asserting the correct behaviour, run in the task's
  worktree and deleted afterwards. All five failed; their output is quoted in the entries.
- **Mutations.** Seven deliberate faults put into Core together, then the whole suite run:
  `Passed! - Failed: 0, Passed: 321`. The faults were reverted. The seven:
  1. the microphone reason in `TrayTooltip.Format` lengthened well past 63 characters;
  2. the `_started` guard removed from `ReminderCoordinator.Start`;
  3. `_lastActivityReadAt = null` removed from `ApplySettings`;
  4. the same line removed from `HandleLock`;
  5. the `CountdownChanged` raise removed from `ApplySettings`;
  6. `_timer.Tick -= OnTick` removed from `Dispose`;
  7. `IsRetryInRange` changed to `seconds >= 0`.
- **The running copy.** The owner's tray copy, 1.5.1 started 2026-11-13 07:19 from
  `artifacts/1.5.1/`, was read from outside, never touched: its DPI awareness, and its
  decision log (419 lines, 2026-10-26 to 2026-11-13).
- **A WPF probe.** A PowerShell script that shows a 40-pixel WPF window and reads its
  `WS_EX_TOPMOST` bit (CR-06). It ran on Windows PowerShell's WPF (.NET Framework 4); the
  dependency-property rule it shows is the same in the WPF of .NET 8.
- **Documentation.** The WPF source of `Application.WmQueryEndSession` (branch `release/8.0` of
  `dotnet/wpf`) and Microsoft's page for `Application.SetHighDpiMode`.

No tray start was made from the worktree: it would repair the auto-start entry to that build
(R-006). A finding that needs one says "to be observed in the owner's copy".

## S2

### CR-01. A settings save the registry refuses ends the application through the crash dialog

- Where: `src/MindfulnessReminder.App/App.xaml.cs:358-364`, the `Accepted` handler in
  `ShowSettings`; raised from `SettingsWindow.xaml.cs:168-169` (`OnSaveClick`); the throw comes
  from `RegistrySettingsStore.Save` (`RegistrySettingsStore.cs:125-141`) through
  `WindowsRegistryStore.SetString` (`WindowsRegistryStore.cs:41-45`).
- What happens: `HKCU\Software\MindfulnessReminder` refuses writes (a policy, security
  software: the condition R-003, R-005 and R-051 already name). The user opens Settings and
  clicks Save. `CreateSubKey` or `SetValue` throws `UnauthorizedAccessException`. Nothing
  between the button and the dispatcher catches it, so `OnDispatcherUnhandledException` calls
  `ReportCrash`: the dialog `Mindfulness Reminder stopped`, then `Environment.FailFast`. Windows
  starts the copy again if it had run 60 seconds, and the next Save does the same. The handler's
  own comment, "if the write fails, the app has not started behaving in a way the next launch
  will contradict", expects the application to survive the failure.
- Contradicts: none stated. R-044 says what Save does and nothing about a refusal, though the
  rules of `docs/requirements.md` say "Error behaviour is a requirement too". R-003, R-005,
  R-006 and R-051 all treat the same refusal as something that "does not stop the application".
- Confirmed by: reading, every step. `ISettingsStore.Save` is documented "Throws when the store
  cannot be written"; there is no `try` in `Save`, in the `Accepted` handler or in `OnSaveClick`.
  Three reviewers reported it independently. Not provoked: to be observed in the owner's copy by
  denying the user "Set Value" on the key in regedit, then Save.
- Direction: the owner first decides what a refused Save does (Q1), as a new requirement row.
  The catch belongs where `Accepted` is handled, using `RegistryRefusal`'s test for a write
  refusal; the coordinator must not be given settings that were not stored. The Core half can be
  tested on `FakeRegistryStore` refusing writes; if the decision is "the window says so and
  stays open", the window needs a way to be told, which is App and is looked at by hand.
- Status: fixed in `e7cc5ae`, 2026-11-13. The owner answered Q1 (ADR 0085): the window stays
  open and says so, nothing is applied. The rule is `SettingsCommit.TrySaveAndApply` in Core,
  the row is R-046, the tests are `SettingsCommitTests`. Left for the owner, in their copy:
  deny "Set Value" on the key in regedit, click Save, and see the line in the window.

### CR-02. After the clock moves back, every unlock reminder is skipped until the clock catches up

- Where: `src/MindfulnessReminder.Core/ReminderCoordinator.cs:297-302`, the debounce in
  `HandleUnlock`: `if (_lastShownAt is { } last && now - last < _debounce)`.
- What happens: a reminder is shown at 10:00:00 and closed. The clock is set back one hour (by
  hand, or by a time sync correcting a clock that ran ahead). On the next unlock `now - last` is
  minus one hour, which is below the 2-second debounce, so the unlock reminder is skipped and
  the log says `Unlock reminder skipped: one shown -3600 s ago`. It stays so until the clock
  reads 10:00:02 again, or until a periodic reminder is served (which sets `_lastShownAt`); an
  unlock restarts the countdown, so someone who locks more often than the interval never gets
  there, and with the periodic reminder off nothing ends it early. `IntervalSchedule` guards its
  own deadline against this (R-025); the coordinator's comparison has no guard.
- Contradicts: R-011 ("one after it shows the reminder again").
- Confirmed by: a probe. `Start`, unlock, close, clock back one hour, lock, unlock; expected two
  reminders shown: `Assert.Equal() Failure: Values differ. Expected: 2, Actual: 1`. The owner's
  log holds no line with a negative age, so it has not happened on that machine since
  2026-10-26.
- Direction: a last-shown time in the future is not "recent": treat a negative difference as
  outside the debounce (or forget `_lastShownAt` when the schedule reports
  `JumpRestartedCountdown`, which alone would not cover the periodic reminder being off). The
  test is the probe, in `ReminderCoordinatorTests`, whose `MutableClock.Advance` takes a
  negative span. One fix task with CR-03.
- Status: fixed in `c17fc03`, 2026-11-13. `ReminderCoordinator.IsWithin` treats a stamp ahead
  of the clock as stale; R-011 says so; the test is
  `R011_An_unlock_after_the_clock_moved_back_shows_the_reminder_again`.

## S3

### CR-03. After the clock moves back, the tooltip stops following a call or full screen

- Where: `src/MindfulnessReminder.Core/ReminderCoordinator.cs:474-477`, `RefreshSuppression`:
  `if (_lastActivityReadAt is { } last && now - last < _activityRefresh) return;`. The branch at
  415-418 that sees `JumpRestartedCountdown` clears the hold and not the stamp.
- What happens: the same negative difference as CR-02. After the clock goes back, no idle
  reading is taken until the clock passes the old stamp, or until a reminder falls due, a lock
  or a save. A call that starts in that time is not named in the tooltip, which keeps counting
  down; a call that ended stays named. Decisions are not affected: a due tick and an unlock read
  the monitor directly.
- Contradicts: R-034 ("within about ten seconds").
- Confirmed by: a probe. `Start`, tick at +1 s, clock back one hour, tick, microphone live,
  +10 s, tick: `Assert.Equal() Failure: Values differ. Expected: MicrophoneInUse, Actual: None`.
- Direction: with CR-02, one rule for both comparisons: a stamp ahead of the clock is stale.
- Status: fixed in `c17fc03`, 2026-11-13, with CR-02 and by the same function; R-034 says so;
  the test is `R034_The_tooltip_follows_a_call_after_the_clock_moved_back`.

### CR-04. The process is system-DPI-aware, not PerMonitorV2 as the code and ADR 0041 say

- Where: `src/MindfulnessReminder.App/App.xaml.cs:142-144` (`SetHighDpiMode(PerMonitorV2)`, its
  `bool` result discarded); `MindfulnessReminder.App.csproj:18` (`ApplicationHighDpiMode`);
  `app.manifest:28-30` (the comment that says where the DPI mode comes from).
- What happens: the mode asked for is never in force. On a machine with two monitors at
  different scales, or after the scale changes within a session, the tray menu, Settings and
  About on the monitor that does not match are stretched as bitmaps by Windows and look
  blurred. `ApplicationHighDpiMode` is read only by Windows Forms' generated
  `ApplicationConfiguration.Initialize()`, which this WPF-entry application never calls.
  `--preview` returns before the call. The reminder's placement is not harmed today: in a
  system-aware process `Screen.WorkingArea` and `CompositionTarget.TransformFromDevice` in
  `BreakWindow.MoveToPrimaryScreen` use the same DPI.
- Contradicts: ADR 0041, Consequences ("`App.OnStartup` sets the one process-wide DPI mode,
  `PerMonitorV2`, before any window exists"), and the two comments above.
- Confirmed by: measurement on the running tray copy (pid 19156, 1.5.1):
  `GetProcessDpiAwareness` returned `hr=0x00000000 PROCESS_DPI_AWARENESS=1`, where 1 is system
  and 2 is per-monitor. Microsoft's page for `SetHighDpiMode`: it returns `false` when the mode
  was not set, and "The high DPI mode is set once per process". Why the mode is already set by
  then (WPF declaring system awareness as it loads) is the reviewers' explanation and was not
  measured, and the blur was not looked at: no second monitor at another scale was at hand.
- Direction: a design choice to measure in the fix task, not here. The usual way for a WPF
  entry point is the manifest's `dpiAwareness`, which the comment in `app.manifest` says the
  Windows Forms analyzer (WFAC010) rejects; the task decides between that with a justified
  suppression and another route, and corrects ADR 0041 by a dated note or a new record
  (ADR 0024). Once the process really is PerMonitorV2, `MoveToPrimaryScreen` runs under
  conditions it has never been checked under: look at `--preview` on the primary monitor at a
  scale other than the sign-in one. Log or assert the result of the call so that a refusal is
  seen.
- Status: fixed in `1613376`, 2026-11-13. The owner chose the manifest (ADR 0086, R-077):
  `app.manifest` declares `PerMonitorV2`, WFAC010 is muted in the csproj with its reason, and
  the call and `ApplicationHighDpiMode` are gone, so there is no call left whose result could
  be refused. Measured with `--preview`: the process went from system-aware to per-monitor v2.
  With the two monitors at different scales, a reminder whose handle started on the second
  monitor was off-centre (1110, 570 instead of 630, 315 with the primary at 150 %), so
  `BreakWindow` centres itself again when its scale changes. The first version of that, in
  `1613376`, placed the window from inside the change and was wrong in the other direction;
  `a9ae321` queues the placement instead, and both directions are measured centred (the table
  is in `docs/design/windows.md` and ADR 0086). Left for the owner, in their copy: the tray
  menu, Settings and About on a monitor at another scale.

### CR-05. A sign-out or restart that is cancelled still ends the tray copy, with no trace

- Where: `src/MindfulnessReminder.App/App.xaml:22` (`ShutdownMode="OnExplicitShutdown"`) and
  `App.xaml.cs` as a whole: there is no `OnSessionEnding` override and no `SessionEnding`
  handler.
- What happens: WPF's `Application` answers `WM_QUERYENDSESSION` by calling `Shutdown()` unless
  a `SessionEnding` handler cancels, whatever the `ShutdownMode`. The user chooses Sign out or
  Restart while another program has unsaved work, Windows shows its "this app is preventing"
  screen, and the user cancels. A tray copy that received the query has already run `Shutdown()`
  and `OnExit`: the icon is gone until the next sign-in. It is a clean exit, so Windows does not
  restart it (R-100 covers crashes), and no line is written to the decision log. The same path
  runs when an installer asks the application to close.
- Contradicts: none stated. R-001 and ADR 0067 list the ways out as Exit, the `--preview` close,
  the `--detect` report, a second copy and the crash handler.
- Confirmed by: the WPF source, `Application.WmQueryEndSession`: it raises `OnSessionEnding`
  and, when the handler has not cancelled, calls `Shutdown()`; `ShutdownMode` is not consulted.
  The scenario itself was not run. One observation of the reviewer goes the other way and was
  not checked again: the System log has a restart request at 2026-11-05 19:43:40 with no logoff
  after it, and the tray copy logged a lock a minute later, so it survived that one. To be
  observed in the owner's copy: unsaved text in Notepad, Restart, Cancel, then look for the
  icon.
- Direction: first observe it. If it reproduces, the fix needs a decision: WPF offers no event
  for "the session did not end after all", so the choices include handling the two session
  messages on a window of the application's own instead of leaving them to `Application`. A
  line in the decision log on every exit (`OnExit`) is cheap, would have settled this from the
  file, and is worth its own requirement row whatever is decided.
- Status: open. Passed over on 2026-11-13, when the owner had the first five bugs fixed and
  CR-06 was taken in its place: the scenario has to be observed in the owner's copy before
  anything is decided, and the Restart it needs may close other programs. Passed over again
  the same day, for the same reason, when the owner had the next five fixed (CR-07 to CR-11),
  and a third time when the five after those were fixed (CR-12 to CR-16). Passed over a fourth
  time on 2026-11-14, when the owner asked for the next five and the four others still open
  were fixed (CR-17 to CR-20). It is the only finding left open, and it waits for the owner's
  observation, not for a session.

### CR-06. Assigning `Topmost = true` again does nothing, so an open reminder is raised by `Activate()` alone

- Where: `src/MindfulnessReminder.App/BreakWindow.xaml:30` (`Topmost="True"`);
  `BreakWindow.xaml.cs:62-66` (`OnSourceInitialized`); `WpfReminderPresenter.cs:119-120`
  (`BringToFront`).
- What happens: the property is already true from the XAML, and setting a dependency property
  to the value it has raises no change, so WPF makes no `SetWindowPos` call. In
  `OnSourceInitialized` the line is harmless: the window is created topmost. In `BringToFront`
  it means that only `Activate()` can raise an open reminder, and Windows refuses foreground
  activation to a process without recent input. A reminder left open under another always-on-top
  window (Task Manager set to "Always on top", a picture-in-picture player) stays under it when
  the next reminder falls due, while the log says `window already open, brought to front`. The
  comment at `BreakWindow.xaml.cs:62-64` describes an effect the line does not have.
- Contradicts: R-012 and R-023 ("brings that window to the front").
- Confirmed by: the WPF probe. `after Show, Topmost property = True, WS_EX_TOPMOST = True`;
  after the bit was cleared from outside with `SetWindowPos(HWND_NOTOPMOST)`: `after assigning
  Topmost = true again, WS_EX_TOPMOST = False`; `after assigning false then true, WS_EX_TOPMOST =
  True`. That the covered reminder stays covered was not observed: `BringToFront` cannot be
  reached from `--preview`. To be observed in the owner's copy with a 1-minute interval.
- Direction: raise the window in a way that reaches Win32 (`Topmost` set false and then true is
  the smallest; a `SetWindowPos` to the top of the topmost band without activation is the
  direct one), and correct the comment. Check that it does not take the keyboard focus more
  than today (R-071 is about keys already on their way).
- Status: fixed in `0abb279`, 2026-11-13. `WpfReminderPresenter.BringToFront` sets `Topmost`
  to false and then to true before `Activate()`; the no-op line in `OnSourceInitialized` is
  gone. Measured with two always-on-top WPF windows, the second covering the first: `Topmost =
  true` again left the first under, false then true put it above
  (`docs/design/windows.md`, ADR 0087). Both windows belonged to one test process. The
  scenario's Task Manager example is not a way to check it: set to "Always on top" it is not
  an ordinary always-on-top window and is expected to go on covering a reminder, which was not
  measured. Left for the owner, in their copy: a 1-minute interval and a reminder left open
  under another program's always-on-top window, a browser's picture-in-picture video for one.

### CR-07. A message cut at 50 can end in half a surrogate pair, and log lines carrying it are lost

- Where: `src/MindfulnessReminder.Core/ReminderSettings.cs:193-196`, `NormalizeMessage`:
  `trimmed = trimmed[..MaxMessageLength].TrimEnd();`; the loss is in
  `FileDecisionLog.Write` (`FileDecisionLog.cs:70`), whose `catch` swallows the encoder's error.
- What happens: the cut counts UTF-16 units. A stored `UnlockMessage` of 49 letters and an
  emoji (51 units), written by hand in the registry or given to `--preview`, loads as 50 units
  ending in a lone high surrogate. The reminder is handed half a character to draw; a Save in
  Settings writes it back, since `IsMessageAcceptable` accepts 50 units; and every log line that
  carries the message (`Settings loaded`, `Unlock reminder shown`, `Settings applied`,
  `Settings saved`) is not written, because `File.AppendAllText` refuses an unpaired surrogate.
  The message boxes of Settings stop at 50 units; whether typing there can leave half a pair was
  not checked.
- Contradicts: R-112 and R-113 (the lines are not written). R-041 says "characters" without
  saying which kind.
- Confirmed by: two probes. `NormalizeMessage(new string('x', 49) + "\U0001F600")`: `length 50,
  last char U+D83D`. `new FileDecisionLog(folder).Write("a\uD83Db")`, then the file read back:
  `Assert.Single() Failure: The collection was empty`.
- Direction: cut at 49 when unit 50 is a high surrogate, and state in R-041 what the limit
  counts. Whether the log should also survive a text it cannot encode (an exception message
  could carry one) is a second, smaller question for the same task. The test is the first
  probe; no test today puts anything but `x`, `y` and a space at the cut.
- Status: fixed in `3eb5736`, 2026-11-13, and completed in `3e9338a`.
  `NormalizeMessage` drops a high surrogate at the end of a message, whether the cut left it
  or the text was stored so (a value the old version had saved is 50 units and is not cut
  again; the first fix missed it, the branch review found it), and R-041 says the limit
  counts UTF-16 units. A second review, of those fixes, found the guard dropping one half
  where a hand-written value can end in several: `9b38106` drops them all. The owner
  answered the second question with yes (ADR 0088): `FileDecisionLog` writes with an encoding
  of its own, and half a pair becomes U+FFFD instead of costing the line (R-110). The tests
  are `R041_A_message_cut_at_the_limit_does_not_end_in_half_a_character`,
  `R041_A_message_that_ends_in_half_a_character_loses_it_whatever_its_length`,
  `R041_A_character_of_two_units_that_ends_at_the_limit_is_kept` and
  `R110_A_text_that_cannot_be_encoded_is_written_with_a_replacement_character`. Seen with
  `--preview` and 49 letters, an emoji and more text: the window shows the 49 letters. Not
  done: only the end is guarded, so a stored message with half a pair in its middle still
  reaches the reminder window as written; a character made of several code points (a flag)
  can still be cut between them (Q9); and whether typing in Settings can leave half a pair
  at the 50th unit is still not checked.

### CR-08. The text boxes of Settings open a light context menu in the dark window

- Where: `src/MindfulnessReminder.App/Theme/DarkTheme.xaml`, which styles `TextBox` and has no
  `ContextMenu` or `MenuItem` style; the four `TextBox` elements of `SettingsWindow.xaml`.
- What happens: a right-click in a message box or a number box opens WPF's built-in Cut, Copy
  and Paste menu, drawn with the stock light templates over the dark window.
- Contradicts: R-075 ("The Settings window, About and the tray menu are dark").
- Confirmed by: reading: no file under `src/MindfulnessReminder.App/` contains `ContextMenu` or
  `MenuItem`. To be observed in the owner's copy: Settings, right-click in a box.
- Direction: either a dark `ContextMenu` and `MenuItem` template in the theme, each with its
  disabled trigger (ADR 0065), or no context menu on these boxes; the keyboard shortcuts work
  either way. A choice for the owner when the task starts.
- Status: fixed in `d1d909d`, 2026-11-13. The owner chose the dark menu (ADR 0089). Implicit
  `ContextMenu` and `MenuItem` styles in the theme were measured not to reach the menu WPF
  builds (it stayed `#F5F5F5`), so `DarkTheme.xaml` declares a menu of its own, Cut, Copy and
  Paste, and its `TextBox` style sets it on every text box. Measured on the worktree build's
  `SettingsWindow`, opened by a throwaway program: all four boxes carry it, background
  `#2A302E`, disabled items grey, the highlighted one on `#343B39`
  (`docs/design/dark-theme.md`). Measured again after the branch review with the menu
  opened by WPF's own opener, the method behind a right-click: the commands reach the box
  the menu was opened on, and the popup is a transparent layered window (ADR 0089). Left for
  the owner, in their copy: Settings, a right-click in a message box and in a number box,
  Shift+F10, a Paste, which was disabled in every run and so not seen enabled, and the
  menu's corners, which no capture could show while the workstation was locked.

### CR-09. A carriage return on its own is not folded in a log line

- Where: `src/MindfulnessReminder.Core/DecisionLog.cs:67`, `FormatLine`:
  `text.Replace("\r\n", BreakSeparator).Replace("\n", BreakSeparator)`.
- What happens: a `\r` not followed by `\n` stays in the line, and an editor or
  `File.ReadAllLines` shows one event as two lines, the second without a time. The reminder
  messages cannot carry one (`NormalizeMessage` removes it); an exception's message in the
  `Crashed:` line or an argument in the `Started` line could.
- Contradicts: R-110 ("every line break in the text replaced by ` | `").
- Confirmed by: a probe. `FormatLine(at, "first\rsecond")`: `Expected: "2026-08-28 10:00:00
  first | second"`, `Actual: "2026-08-28 10:00:00  first\rsecond"`.
- Direction: fold `\r\n`, then `\n`, then `\r`. The test is the probe, in `DecisionLogTests`.
- Status: fixed in `cdab57e`, 2026-11-13, as the direction says; R-110 names the three forms
  of a line break; the test is `R110_A_carriage_return_on_its_own_becomes_a_separator_too`.

### CR-10. `--preview --restarted` shows `--restarted` as the message

- Where: `src/MindfulnessReminder.App/App.xaml.cs:203`, `PreviewMessage`:
  `args[index + 1]` is taken as the text whatever it is.
- What happens: `MindfulnessReminder.exe --preview --restarted` opens a reminder that says
  `--restarted`. Windows never builds that command line (the registered one is `--restarted`
  alone), so only a hand-typed command does.
- Contradicts: R-102 ("`--preview` and `--detect` ignore the argument").
- Confirmed by: reading the three lines.
- Direction: the parsing is a rule, and it sits in App; `CommandLineSwitches` in Core is where
  a test can reach it (ADR 0042). Skip an argument that is one of the application's own
  switches.
- Status: fixed in `3766c7f`, 2026-11-13. `CommandLineSwitches.TryGetPreviewText` picks the
  text: the first argument after `--preview` that is not `--preview`, `--detect`,
  `--detect=N` or `--restarted`, nor the value after a bare `--detect` (`--preview --detect
  30` showed `30` until the branch review found it; fixed in `3e9338a`). A switch is passed
  over, not read as "no text", so `--preview --restarted "Breathe"` shows `Breathe`. R-045
  says so and is now `implemented`; the tests are in `CommandLineSwitchesTests`. Seen with
  `--preview --restarted`: the window shows `Ground yourself`. The branch review found the
  same fault in `--detect`, which read `--restarted` as its seconds: fixed in `84154e0`,
  where its parsing moved to Core as
  `CommandLineSwitches.TryGetDetectDuration` (R-081; R-082, now `implemented`; R-102). Seen
  with `--detect --restarted 3`: the dialog comes after the three seconds and begins
  `Watched for 3 seconds.` The second review found `--detect --preview 30` still showing
  `30`, a `--detect` before `--preview` not being counted: fixed in `7116d61`.

## S4

The tests named here pass today and would go on passing with the code they claim to guard
removed. Strengthening one is a change to a test, not a test edited to pass (the rule "No test
edited to pass"): each fix first shows the strengthened test red against the mutation named,
then green against the code as it is.

### CR-11. `R062_The_longest_tooltip_the_app_can_produce_fits_the_tray_limit` cannot fail

- Where: `tests/MindfulnessReminder.Tests/TrayTooltipTests.cs:62-84`, against
  `TrayTooltip.cs:80` (`return text.Length <= MaxLength ? text : text[..MaxLength];`).
- What is wrong: every candidate comes out of `Format`, which already cuts at 63, so the
  assertion `longest.Length <= MaxLength` holds whatever the texts are. This is the test
  `docs/design/tray-tooltip.md` and ADR 0059 name as the proof that the longest text "fits
  whole" (R-062); a reason a few characters too long would be cut in the tray with the test
  green.
- Confirmed by: mutation 1.
- Direction: assert that each candidate is uncut, for example that it ends with ` min`,
  `mic is on` or `full screen`, or equals the banner.
- Status: fixed in `234da8f`, 2026-11-13, as the direction says. Red against mutation 1
  (`tooltip was cut at 63 characters: Mindfulness Reminder 1.5.1 - paused for as long as your
  microph`), green against the code as it is. Rewritten in `48421c2` after the branch
  review: each tooltip is compared with its uncut text, not with three endings copied from
  `TrayTooltip`, so a reworded reason that fits no longer reads as a cut.

### CR-12. `R010_Start_twice_subscribes_only_once` cannot see a second subscription

- Where: `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs:55-65`.
- What is wrong: it asserts `ShowCount == 1` after one unlock. With two subscriptions the first
  handler shows and the second finds `IsShowing` and raises the window, so the count is 1
  either way.
- Confirmed by: mutation 2.
- Direction: also assert `BringToFrontCount == 0`, or one `Workstation unlocked` line in the
  log.
- Status: fixed in `86578f3`, 2026-11-13. The test asserts both: no raise, and one
  `Workstation unlocked` line. Red against mutation 2 (`Assert.Equal() Failure: Values differ.
  Expected: 0, Actual: 1`, the raise by the second handler), green against the code as it is.
  The branch review found the test still blind to a tick or a lock heard twice; since
  `4f87cfb` it raises all three events and expects the countdown event once for the tick and
  the three log lines of one lock and one unlock. Red against a second `_timer.Tick += OnTick`
  (`Expected: 1, Actual: 3`) and against a second `_monitor.Locked += OnLocked` (three
  `Workstation locked` lines where one was expected), each put before the guard. Not pinned:
  that a second `Start` leaves the countdown where it was, which no requirement row says.

### CR-13. R-035's fresh reading after a save and after a lock is not pinned

- Where: `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs:976-1000`
  (`R035_Applying_settings_takes_a_fresh_reading_on_the_next_tick`), against
  `ReminderCoordinator.cs:223` (`ApplySettings`) and `:343` (`HandleLock`).
- What is wrong: the test starts with both pauses off, a state in which nothing ever sets
  `_lastActivityReadAt`, so the reset it means to test changes nothing. R-035's other half,
  "after a lock ... the next idle tick reads the monitor again", has no test.
- Confirmed by: mutations 3 and 4.
- Direction: start with one pause on so that a reading is stamped before the save: microphone
  pause off and full-screen pause on, microphone live, tick at +1 s, `ApplySettings(Default)`,
  tick at +2 s, expect `MicrophoneInUse`. For the lock: unlock reminder off, tick at +1 s, lock,
  unlock, microphone live, tick at +2 s, expect `MicrophoneInUse`.
- Status: fixed in `59971f4`, 2026-11-13, as the direction says.
  `R035_Applying_settings_takes_a_fresh_reading_on_the_next_tick` starts with the full-screen
  pause on and asserts that the tick before the save took a reading; the lock's half is the
  new `R035_A_lock_takes_a_fresh_reading_on_the_first_tick_after_the_unlock`. Red against
  mutations 3 and 4, put in together, each test on its own path (`Expected: MicrophoneInUse,
  Actual: None`, twice), green against the code as it is.

### CR-14. `R021_Dispose_stops_and_disposes_the_timer` does not pin the tick's unsubscription

- Where: `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs:143-155`, against
  `ReminderCoordinator.cs:513` (`_timer.Tick -= OnTick;`).
- What is wrong: the tick raised after `Dispose` comes with the clock not advanced, so nothing
  would be shown even by a coordinator still subscribed. `Disposed` and `!IsRunning` are real
  assertions; the unsubscription is not proven.
- Confirmed by: mutation 6.
- Direction: advance the clock by the interval before `RaiseTick`.
- Status: fixed in `7858357`, 2026-11-13, as the direction says. Red against mutation 6
  (`Assert.Equal() Failure: Values differ. Expected: 0, Actual: 1`, the reminder a coordinator
  still subscribed shows), green against the code as it is. The branch review found the same
  gap beside it: nothing raised a lock after `Dispose`, so `_monitor.Locked -= OnLocked` could
  go with the suite green. Since `4f87cfb`
  `R010_Dispose_unsubscribes_and_disposes_the_monitor` raises both session events and expects
  an empty log; red with that line removed (`Assert.Empty() Failure: Collection was not
  empty`).

### CR-15. `CountdownChanged` on `ApplySettings` is not pinned

- Where: `tests/MindfulnessReminder.Tests/ReminderCoordinatorTests.cs:392-406`
  (`R021_The_countdown_change_event_fires_on_every_tick_and_on_every_reset`), against
  `ReminderCoordinator.cs:227`.
- What is wrong: the test counts the event on `Start`, one tick, a lock and an unlock. Without
  the raise in `ApplySettings`, a save that switches the periodic reminder off would stop the
  timer and leave the tooltip saying `next in N min` with no later tick to repair it.
- Confirmed by: mutation 5.
- Direction: count the event around `ApplySettings(new ReminderSettings(false, 30))`.
- Status: fixed in `a839b6d`, 2026-11-13. A test of its own,
  `R021_A_save_that_switches_the_periodic_reminder_off_raises_the_countdown_change_event`,
  expects the event once from that save, with `Remaining` already null when it is raised; the
  four-step test is as it was. Red against mutation 5 (`Assert.Equal() Failure: Collections
  differ. Expected: [null], Actual: []`), green against the code as it is.

### CR-16. `IsRetryInRange` has no test, though R-043 names it

- Where: `src/MindfulnessReminder.Core/ReminderSettings.cs:218-219`. No file under `tests/`
  contains `IsRetryInRange`; `R043_The_dialog_range_check_matches_the_clamp` covers
  `IsIntervalInRange` only.
- What is wrong: with the check loosened, the Settings window would save a retry gap of 1 that
  `Normalize` then turns into 5 without telling the user, which is what R-043 rules out.
- Confirmed by: mutation 7.
- Direction: the same four-row theory as the interval's: `MinRetrySeconds - 1`, `Min`, `Max`,
  `Max + 1`.
- Status: fixed in `da8c69f`, 2026-11-13, as the direction says:
  `R043_The_dialog_retry_range_check_matches_the_clamp` in `ReminderSettingsStoreTests`. Red
  against mutation 7 in the two rows outside the range, 4 and 301 (`Expected: False, Actual:
  True`), green against the code as it is. The branch review found that the theory, like the
  interval's beside it, compared the check with the two constants and never with the clamp
  its name cites. Since `4f87cfb` both also expect `Normalize` to leave a number alone exactly
  when the check accepts it; red with the lower clamp of `Normalize` moved to 10 for the retry
  gap and to 5 for the interval (the row of the minimum, `Expected: True, Actual: False`).

### CR-17. R-113's order of lines after a refused registration is not pinned

- Where: `tests/MindfulnessReminder.Tests/StartupRegistrarTests.cs:130`
  (`R003_A_refused_first_run_repairs_an_entry_it_found_and_keeps_its_veto`) builds the state
  and passes no log; `DecisionLogTests.cs:230`
  (`R113_A_refused_registration_logs_the_refusal_by_its_type`) has no entry to repair.
- What is wrong: R-113 says a path repaired before a refusal keeps "its own line", which
  "precedes" the refusal's. The code writes both in that order; no test fails if the first is
  lost or the order changes.
- Confirmed by: reading the two tests. No mutation was run.
- Direction: the first test's state with a `FakeDecisionLog`, asserting the two lines in order.
  One fix task with CR-18.
- Status: fixed in `6cd52f7`, 2026-11-14, as the direction says: the new
  `R113_A_path_repaired_before_a_refusal_keeps_its_line_ahead_of_the_refusal` in
  `DecisionLogTests` builds the state of the R-003 test (an entry at an old path, switched off
  in Task Manager, the marker's key refusing writes) and expects the two lines, the repair's
  first. The review had run no mutation, so two faults were put into
  `StartupRegistrar.EnsureRegisteredOnFirstRun`, one at a time. Red with the repair's line
  removed (`Assert.Equal() Failure: Collections differ`, the refusal's line alone) and with it
  written after the refusal's (the two lines in the other order), green against the code as
  it is.

### CR-18. R-113's combined suffixes are not pinned

- Where: `tests/MindfulnessReminder.Tests/DecisionLogTests.cs:133-159`.
- What is wrong: only "all eight fallbacks" and "one interval clamp alone" are asserted.
  Nothing asserts a retry clamp, two clamps together, or fallbacks followed by a clamp in one
  line.
- Confirmed by: reading: the only expected text with `clamped` is at line 157. No mutation was
  run.
- Direction: one test with a missing value, an interval of `0` and a retry of `9999`.
- Status: fixed in `482328e`, 2026-11-14, as the direction says: the new
  `R113_A_missing_value_and_two_clamped_numbers_share_one_line` in `DecisionLogTests`. The
  missing value is `PauseWhileFullScreen`, which is read after the interval, so the line also
  shows the fallbacks standing first whatever the reading order: `...; defaults used for
  PauseWhileFullScreen; clamped PeriodicIntervalMinutes 0 -> 1; clamped PauseRetrySeconds 9999
  -> 300`. Red with the retry gap's `AddClamp` removed from `RegistrySettingsStore.Read`
  (`Assert.Equal() Failure: Strings differ`, at position 236, where the second suffix is
  missing) and with `DecisionLog.SettingsLoaded` writing the clamps before the fallbacks (at
  position 158, `; clamped PeriodicInter` where `; defaults used for Pau` was expected),
  green against the code as it is. Not pinned: two missing values together with a clamp; the
  comma between names is pinned by the test of the empty registry alone.

### CR-19. `R032_The_full_screen_switch_turns_off_only_the_full_screen_check` does not show the switch turning anything off

- Where: `tests/MindfulnessReminder.Tests/SuppressionPolicyTests.cs:105-112`.
- What is wrong: the activity has the microphone live and full screen, and the expected answer
  is `MicrophoneInUse`, which is also the answer with the full-screen switch on, since the
  microphone is asked first. The test of both switches off (114-121) would catch a full-screen
  switch that was ignored, so the behaviour is guarded; this test's name is not.
- Confirmed by: reading.
- Direction: `new ActivitySnapshot(false, false, ForegroundState.FullScreen)` with the switch
  off, expecting `None`.
- Status: fixed in `421d82e`, 2026-11-14. The test asks about full screen alone, as the
  direction says, and expects `None`; the assertion it had, a call in full screen giving
  `MicrophoneInUse`, stays beside it as the "only" of its name. Red with
  `settings.PauseWhileFullScreen &&` removed from `SuppressionPolicy.Evaluate`
  (`Assert.Equal() Failure: Values differ. Expected: None, Actual: FullScreen`, at the new
  assertion), green against the code as it is.

### CR-20. `IDecisionLog`'s comment places its implementation in App

- Where: `src/MindfulnessReminder.Core/IDecisionLog.cs:22`: "the one implementation, in App".
- What is wrong: `FileDecisionLog` is in Core (ADR 0073); App only names the folder.
- Confirmed by: reading.
- Direction: correct the sentence, with the next change to that file.
- Status: fixed in `5e4b28b`, 2026-11-14, in a change of its own, since no other change to
  the file was coming: the comment names `FileDecisionLog`, in Core, and says that App only
  names the folder. A comment, so no test came first.

## Questions for the owner

Each comes with a recommendation (the rule "Questions to the owner"). None was decided here.

- **Q1. What does a Save do when the registry refuses it (CR-01)?** There is no requirement
  row. Recommendation: the Settings window stays open and says so in its error line, nothing is
  applied, and the application goes on; a new row beside R-044. A balloon as in R-005 is the
  alternative, with the window closed and the edit lost. **Answered 2026-11-13:** the
  recommendation, recorded as ADR 0085 and R-046.
- **Q2. R-025's first sentence is wider than the guard.** "A clock moved backwards does not
  postpone the reminder" holds only when the jump is larger than the time gone since the
  deadline was armed (`IntervalSchedule.TryConsumeDue`, `due - now > _armedSpan`). A 30-minute
  interval armed at 09:30 and a clock set back from 09:50 to 09:35: the reminder still comes at
  10:00 on the clock, 45 real minutes after the last. A smaller jump cannot be told from a wait
  on the wall clock alone. Recommendation: reword the row to promise what its example shows.
- **Q3. R-112 against the code, twice.** It cites "(R-012, R-025)" for the two "window already
  open" lines; the periodic one is R-023. And it says a clock moved back "ends a hold without a
  line of its own, the line of that event following", but a clock jump writes no line
  (`ReminderCoordinator.HandleTick`, the `JumpRestartedCountdown` branch): the file shows
  `Periodic reminder held back: ...` with nothing after it. Recommendation: correct the
  citation, and write one line when the countdown is restarted by a jump, which is the event a
  reader of the log would look for.
- **Q4. A stored number too large for an `int`.** `PeriodicIntervalMinutes` of `99999999999`
  fails `int.TryParse` and reads as 30, logged under "defaults used for"; `99999` reads as 480,
  logged as clamped. R-051 has a clause for each. Recommendation: leave the code, and say in
  R-051 that a number that does not fit is one that does not parse.
- **Q5. A blank or over-long stored message leaves no trace in the log.**
  `RegistrySettingsStore.ReadText` records a fallback only for a missing or non-string value. An
  `UnlockMessage` of `""` loads as `Ground yourself` and a 70-character one is cut, each with a
  plain `Settings loaded:` line, while ADR 0073 gives "a setting that fell back to its default"
  as a reason the log exists. Recommendation: name them, as the numbers are named when clamped.
- **Q6. A message of invisible characters.** `NormalizeMessage` and `IsMessageAcceptable` use
  `char.IsWhiteSpace`, so a message made only of U+200B or U+FEFF is accepted and the reminder
  shows nothing. Recommendation: leave it; it needs a deliberate paste.
- **Q7. "Restarted after an error" after a restart with no error.** The registration also
  brings the copy back after a hang and after an installer closes it (R-100), and every
  `--restarted` start shows the balloon that points to the Windows Application log (R-102),
  where there is then nothing. Recommendation: reword the balloon to "Restarted by Windows.",
  with the log named only as where an error would be.
- **Q8. A complaint that outlives its cause.** After a refused Save, the complaint is cleared
  only by typing (`OnAnyTextChanged`). Unticking the reminder or the pause, which also resolves
  it, leaves it on screen until Save. Recommendation: clear it on the checkboxes too.
- **Q9. A character made of several code points can still be cut in the middle.** After
  CR-07 the cut at 50 never leaves half a surrogate pair, but a flag (two regional
  indicators), a joined family or a letter with a skin tone is several whole pairs, and the
  cut can fall between them: 48 letters and a flag keep the letters and one regional
  indicator, drawn as a boxed letter. Found by the branch review of 2026-11-13, by reading;
  no probe was run. Backing the cut off to a text-element boundary
  (`StringInfo`) would cover it without changing what the limit counts. Recommendation: leave
  it; it needs such a character exactly at the 50th unit of a message, and the result is one
  odd symbol at the end.

## Unverified claims

Reported by a reviewer, not confirmed and not disproved. None is a finding until someone
observes it.

- **U1. A capture record left at "stop = 0" would hold every reminder back with no end.**
  `WindowsActivityMonitor.IsOpenNow` (`WindowsActivityMonitor.cs:259-263`) counts a key whose
  start is above 0 and whose stop is 0. If Windows leaves such a record after a power loss or a
  crash during a call, every reading reports `MicrophoneInUse` until that application next
  opens and closes the device, and ADR 0052 puts no cap on the hold. The comment above the
  method, and ADR 0050, say the check prevents exactly this; it only rejects a record with no
  stop value. Whether Windows ever leaves one is not known: the reviewer found none among the
  119 records on this machine. To check: start a recording, `taskkill /f` the application, run
  `--detect`; and, for the power loss, hold the power button during a recording, boot, and run
  `--detect` with nothing open.
- **U2. After sleep with sign-in required, the overdue tick could arrive before the lock.** The
  periodic reminder would then open behind the lock screen (R-022; `UserAway` does not hold it
  back), and the unlock would raise it instead of showing the unlock reminder, which is what
  ADR 0048 set out to avoid. Not observed: of 187 periodic lines in the owner's log, none is
  followed by `Workstation locked` within 5 seconds.
- **U3. `QUNS_APP` (7) is mapped to `FullScreen` (ADR 0051).** If Windows reports 7 while an
  ordinary windowed Store application has the focus, reminders are held back whenever one is
  in front. `--detect 20` with Calculator or Settings focused settles it. The owner's log has
  three holds for full screen in 18 days.
- **U4. A two-line message may be scaled down too.** The documents say two lines fit above the
  button at full size. By the reviewer's arithmetic, not measured, about 75 px are left for the
  text and two lines of Segoe UI at 30 px need about 80, and the button's 2 px focus border
  would make a wrapped message rescale slightly as the window gains or loses the focus.
  `--preview "a message of about thirty characters"` settles it. R-072's two examples hold
  either way.
- **U5. A reason in `docs/design/dark-theme.md` and ADR 0065, rule 2.** They say a `Foreground`
  setter in the implicit `TextBlock` style "would beat the inherited value on the TextBlock a
  ContentPresenter generates for a CheckBox caption". The reviewer holds that an implicit style
  for a non-`Control` type in `Window.Resources` does not cross a template boundary, so the
  reason would be true only for a dictionary merged into `Application.Resources`, which
  ADR 0064 forbids. The behaviour is right either way.
- **U6. A remote session.** ADR 0063 forwards only `SessionLock` and `SessionUnlock`. If a
  remote-desktop disconnect arrives without a lock, the timer keeps running in a session nobody
  sees. Not checked. On the console the reviewer found the owner's log pairing 38 locks with 38
  unlocks.

## Rejected claims

None. Every claim of a defect or a test gap was either confirmed or left unverified above.

## Found sound

So that the next review does not start from nothing. Read by a reviewer in full and found to
match its requirement rows; not probed further.

- `IntervalSchedule`: due exactly at the deadline, re-armed as it says yes, suspend and resume,
  `Apply` while suspended, `Remaining` never negative (R-020 to R-028, R-033). Its tests can
  fail.
- `HandleUnlock`'s order (restart, switch, already showing, debounce, activity read) matches
  ADR 0048 and ADR 0055; two unlocks in a row, an unlock with no lock, and lock, unlock and
  tick in any order leave a consistent state. No seam calls back into the coordinator.
- `SuppressionPolicy` and `UserNotificationState.FromShellState` (R-030 to R-032);
  `TrayTooltip`'s rounding; `DetectionLog`; `WindowPlacement`; `CommandLineSwitches` against
  how App parses the two switches.
- Settings: invariant culture on read and write, the clamp agreeing with the dialog's checks,
  `Normalize` idempotent, so a saved value reads back the same.
- Auto-start: the quoting, the case-insensitive comparison, the marker and each refusal step
  (R-003 to R-006, ADR 0070). `FakeRegistryStore` against `WindowsRegistryStore`: no difference
  found that lets Core pass on the fake and fail on the registry.
- The decision log file: rotation strictly above the cap, the whole file moved, one writer on
  one thread.
- Every adapter against the comments of its Core interface (`IActivityMonitor`,
  `ISessionMonitor`, `IPeriodicTimer`, `IRegistryStore`): no promise found broken. The two
  P/Invoke signatures of the adapters and `DwmSetWindowAttribute`; registry keys, the icon,
  pens and brushes disposed; the mutex a field of `App`.
- The presenter: a new `BreakWindow` per reminder, never reused after `Close`, `IsShowing`
  true on every route that leaves a window open or queued and false after every route that
  closes it.
- The Settings window's validation: empty, spaces, signs, other scripts' digits, pasted text
  and overflow all refused; nothing saved on a refusal; Cancel, Esc and the X save nothing.
  Settings and About modeless, one instance each; no `ShowDialog` anywhere.
- The dark theme: merged per window, every `StaticResource` key defined before use, each
  template with its disabled trigger.

## Not covered

- Anything that needs a tray start of the reviewed build: a real registry refusal (CR-01), a
  cancelled sign-out (CR-05), a reminder covered by another topmost window (CR-06), the context
  menu (CR-08). Each entry says what to look at in the owner's copy.
- Two monitors at different scales, a remote session, and a power loss during a call: no such
  setup was at hand (CR-04, U6, U1).
- How the windows look: no XAML was rendered. U4 is arithmetic.
- The process: `scripts/`, the hooks, `tests/MindfulnessReminder.ProcessTests/`, CI. A review
  of its own.
- `README.md`, the missing XML comments (ADR 0069) and the analyzer findings `CLAUDE.md` counts.

## Suggested order

By severity, then by what unblocks what. Each line is one task.

1. CR-01, after Q1 is answered: it needs a requirement row first. Done.
2. CR-02 with CR-03: one cause, one guard, two tests. Done.
3. CR-12, CR-13, CR-14 and CR-15 together: four tests of `ReminderCoordinatorTests`, no code.
   Doing them before any further change to the coordinator makes that change safer. Done.
4. CR-05: observe it first; then a decision.
5. CR-04: a design choice to measure; recheck the reminder's placement afterwards. Done.
6. CR-06. Done.
7. CR-07. Done.
8. CR-11, CR-16 and CR-19 together: three small tests, no code. Done.
9. CR-17 with CR-18: the tests of R-113. Done.
10. CR-09, CR-10, CR-08, CR-20: small, any order. Done.
11. Q2 to Q9 as the owner answers them; U1 to U6 as someone observes them.
