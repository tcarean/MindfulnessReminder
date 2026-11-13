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
window stayed under it, against R-012 and R-023.

Measured on 2026-11-13 with two always-on-top WPF windows, the second covering the first:
after `Topmost = true` on the first it was still under the second; after `false` then `true`
it was above. The review named two routes, and the plan the owner approved that day took the
first.

## Decision

`BringToFront` sets `Topmost` to false and then to true before `Activate()`. The assignment in
`OnSourceInitialized` is removed: the window is `Topmost` from its XAML, so its handle is
created always-on-top and the line did nothing.

## Alternatives rejected

- **`SetWindowPos` to the top of the topmost band, without activation.** The direct call, and
  one more Windows API in the App for what two assignments do; the measurement shows the
  assignments reach Windows.
- **Leaving it to `Activate()`.** It is the call Windows refuses in the case that matters.

## Consequences

- Between the two assignments the window is an ordinary one for an instant. Neither takes the
  keyboard focus; `Activate()` asks for it as before (R-071).
- The reminder is raised among the ordinary always-on-top windows only. Since Windows 8 there
  is a layer above them that `Topmost` does not reach: Task Manager set to "Always on top" is
  in it and still covers a reminder (Raymond Chen, "The Old New Thing", 2023-05-02).
- Guards: none by test. Measured as above; in the tray copy it is looked at with a short
  interval and a reminder left open under another program's always-on-top window, not Task
  Manager's.
