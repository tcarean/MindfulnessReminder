# The windows the app shows

> Moved unchanged on 2026-10-09 (ADR 0035) from `CLAUDE.md`, "Windows the app shows". Where a
> paragraph became a decision record, its number is given after it. **Covers:**
> `src/MindfulnessReminder.App/BreakWindow.xaml`, `src/MindfulnessReminder.App/BreakWindow.xaml.cs`,
> `src/MindfulnessReminder.App/SettingsWindow.xaml.cs`,
> `src/MindfulnessReminder.App/AboutWindow.xaml`,
> `src/MindfulnessReminder.App/WpfReminderPresenter.cs`, `src/MindfulnessReminder.App/App.xaml.cs`,
> `src/MindfulnessReminder.App/app.manifest`.
> A change to the code this describes means this note is updated in the same task, or deleted with
> the decision it explains.

## Windows the app shows

| window | modality | focus | why |
|---|---|---|---|
| `BreakWindow` | modeless, `Topmost` | takes it | It has to be seen. Shown after a ~600 ms delay because a window created into a freshly unlocked desktop lands behind everything. |
| `SettingsWindow` | **modeless**, not topmost | ordinary | A modal `ShowDialog` disables every other window in the process, so a reminder falling due while it sat open would appear with a dead Close button. Single-instance: reopening raises the existing one. |
| `AboutWindow` | **modeless**, not topmost | ordinary | Same reasoning, and single-instance the same way. Its Close button *is* `IsDefault`, unlike the reminder's: this window was opened on purpose, so Enter closing it is what the user means. |

`BreakWindow` is deliberately **not** `IsDefault` on its Close button, and it swallows Enter
and Space in `OnPreviewKeyDown`. Since the reminder became periodic it arrives mid-work and
takes the focus, and the focused button answers both keys, so the next Enter or Space already
on its way would both dismiss an unread reminder and be lost to whatever was being typed. It
must be *Preview*: plain `KeyDown` bubbles up from the focused button, which would already have
handled the key. A Space swallowed there never presses the button, so its key-up clicks nothing.
Esc still closes it, and so does a click on Close; Alt+F4 and the system menu's Close, which
nobody presses mid-sentence, close it as they close any window. If another key ever turns out
to be likely mid-sentence, the same handler is the place.

See ADR 0060 and ADR 0081; About's own reasons are ADR 0066.

**The scale the windows are drawn at.** The process is per-monitor DPI aware, declared in
`app.manifest` and nowhere in the code: WPF declares the process system-aware as it loads,
before `App.OnStartup` runs, so a call there has no effect (ADR 0086). WPF then draws each window
at the scale of the monitor it is on and handles a change of scale by itself; the one place
that works in pixels is `BreakWindow.MoveToPrimaryScreen`. `Screen.WorkingArea` is in physical
pixels and the window's `Left` and `Top` are in units of the window's own scale, so the
placement converts with the window's `CompositionTarget.TransformFromDevice`. That transform is
the scale of the monitor the handle is on, which Windows picks and which is not always the
primary one. Assigning `Left` then carries the window onto the primary monitor, and the change
of scale arrives inside that assignment, before `Top` is assigned: the size in pixels and what
`Left` and `Top` mean change under a placement worked out for the other monitor, and it is
left off-centre. `OnDpiChanged` therefore places the window again, and it queues that
placement on the dispatcher instead of making it: made from inside the change, it is
overwritten when the first placement resumes. The same override re-centres a reminder that is
open while the user changes the primary monitor's scale.

Measured on 2026-11-13 with `--preview`, the handle made to start on the second monitor, and a
trace of what the window saw (taken out again):

| Primary, second monitor | No second placement | Placed again inside the change | Queued | Centred is |
| --- | --- | --- | --- | --- |
| 150 %, 100 % | 1110, 570 | not traced | 630, 315 | 630, 315 |
| 100 %, 125 % | 548, 286 | 685, 286 | 740, 390 | 740, 390 |

Windows honours the start position in about one run in three, so a run proves something only
when the trace shows a change of scale. Whether the window is drawn for a moment at its first
position before the queued placement runs was not looked at.

**Raising a reminder that is already open.** `BreakWindow` is `Topmost` from its XAML, so its
handle is created always-on-top, above everything open at that moment. A reminder left open
can still end up under another always-on-top window opened later (Task Manager set to "Always
on top", a picture-in-picture player). `WpfReminderPresenter.BringToFront` therefore sets
`Topmost` to false and then to true before it calls `Activate()`. Setting it to true alone
does nothing: the property already holds that value, WPF sees no change and makes no call to
Windows. And `Activate()` alone is not enough, since Windows grants the foreground only to a
process with recent input. Measured on 2026-11-13 with two always-on-top WPF windows, the
second covering the first: after `Topmost = true` on the first it was still under the second;
after `false` then `true` it was above. Neither assignment takes the keyboard focus;
`Activate()` asks for it, as it did before.
