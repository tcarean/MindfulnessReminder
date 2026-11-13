# 0087. An open reminder is raised by setting Topmost off and on

Date: 2026-11-13
Status: accepted 2026-11-13
Supersedes: 0060 in part (OnSourceInitialized re-asserting Topmost)

## Context

ADR 0060 has `BreakWindow.OnSourceInitialized` "re-assert" `Topmost`, and
`WpfReminderPresenter.BringToFront` did the same to raise a reminder that was already open. The
review of 2026-11-13 showed (CR-06) that assigning `true` to a property that is already `true`
makes no call to Windows, so an open reminder was raised by `Activate()` alone, which Windows
refuses to a process without recent input. A reminder left open under another always-on-top
window would then stay under it, against R-012 and R-023; the review inferred that from a
probe and did not observe it in the tray copy.

Measured on 2026-11-13 with two always-on-top WPF windows of one test process, the second
covering the first: assigning `true` again left the first under, `false` then `true` put it
above (`docs/design/windows.md`). The review named two routes, and the plan the owner approved
that day took the first.

## Decision

`BringToFront` sets `Topmost` to false and then to true before `Activate()`. The assignment in
`OnSourceInitialized` is removed: the window is `Topmost` from its XAML, so its handle is
created always-on-top and the line did nothing.

## Alternatives rejected

- **`SetWindowPos` to the top of the always-on-top windows, without activation.** The direct call, and
  one more Windows API in the App for what two assignments do; the measurement shows the
  assignments reach Windows.
- **Leaving it to `Activate()`.** It is the call Windows refuses in the case that matters.

## Consequences

- Between the two assignments the window is an ordinary one for an instant. Neither takes the
  keyboard focus; `Activate()` asks for it as before (R-071).
- The reminder is raised among the ordinary always-on-top windows only. Task Manager set to
  "Always on top" is not one of them and is expected to go on covering a reminder; the note
  has the source, and that it was not measured.
- Guards: none by test. Measured as above, within one process. Still to be looked at in the
  tray copy: a short interval and a reminder left open under another program's always-on-top
  window, a browser's picture-in-picture video for one, not Task Manager.
