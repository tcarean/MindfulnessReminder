# 0086. The DPI mode is declared in the manifest

Date: 2026-11-13
Status: accepted 2026-11-13

## Context

ADR 0041 records that `App.OnStartup` sets the process-wide DPI mode, `PerMonitorV2`, with
`Application.SetHighDpiMode`, and the csproj carried `ApplicationHighDpiMode` beside it. The
review of 2026-11-13 measured the running tray copy and found it system-aware (CR-04): the
property is read only by Windows Forms' generated bootstrap, which a WPF entry point never
calls, and the call comes after WPF has declared a mode of its own, so it has no effect (the
third row below is the same call made before WPF loads, where it works). On a
machine with monitors at different scales the tray menu, Settings and About were stretched by
Windows on the monitor that did not match.

Measured on 2026-11-13 with `--preview`, reading the process and its window from outside:

| Build | `GetProcessDpiAwareness` | The window's awareness context |
| --- | --- | --- |
| As it was | 1 (system) | system aware |
| `dpiAwareness` in `app.manifest` | 2 (per-monitor) | per-monitor v2 |
| An entry point of its own calling `SetHighDpiMode` before WPF loads | 2 (per-monitor) | per-monitor v2 |

With the manifest setting and nothing else, the build fails on the Windows Forms analyzer's
WFAC010 ("Remove high DPI settings from app.manifest").

The same day the owner set the two monitors to different scales, and the reminder's handle was
made to start on the second monitor (`STARTF_USEPOSITION`, which Windows honoured in about one
run in three; a trace in the window, taken out again, showed which runs). The first placement
is worked out at the scale of the monitor the handle is on, and the change of scale arrives
while that placement assigns `Left`:

| Primary, second monitor | No second placement | Placed again inside the change | Placed again, queued | Centred is |
| --- | --- | --- | --- | --- |
| 150 %, 100 % | 1110, 570 | not traced | 630, 315 | 630, 315 |
| 100 %, 125 % | 548, 286 | 685, 286 | 740, 390 | 740, 390 |

The system-aware 1.5.1 was centred in both. The first version of this fix placed the window
again inside `OnDpiChanged` and was committed as centred on a run that had not started on the
second monitor; the trace showed it wrong, and the queued placement replaced it.

The owner chose the manifest, of three options put with these figures.

## Decision

`app.manifest` declares the DPI mode (`dpiAwareness` `PerMonitorV2, PerMonitor`, with `dpiAware`
`true/pm` for a Windows that does not read the first); WFAC010 is muted in the App's csproj,
with the reason beside it; the `SetHighDpiMode` call and `ApplicationHighDpiMode` are removed.
`BreakWindow` centres itself again when its scale changes: `OnDpiChanged` queues the placement
on the dispatcher.

## Alternatives rejected

- **An entry point of the application's own that calls `SetHighDpiMode` before any WPF type
  loads.** It needs no suppression, and it reached per-monitor v2 as well. It holds only while
  nothing in that method touches WPF first, and an edit that breaks the order shows no sign:
  the call returns `false` and the process is system-aware again, which is how CR-04 went
  unseen.
- **Staying system-aware and correcting the documents.** No risk to the placement, but the
  blur on a monitor at another scale stays, against what ADR 0041 set out to have.
- **Working out the first placement at the primary monitor's scale instead of repeating it.**
  It needs the primary monitor's DPI, which Windows Forms' `Screen` does not give: one more
  Windows API for what an override does.

## Consequences

- The mode is in force before any code of the process runs, for the tray copy, `--preview` and
  `--detect` alike, and no call can come too late. R-077 states it.
- One analyzer warning is muted for the whole App project. It has one meaning, DPI settings in
  the manifest, and the comment beside it says why they are there.
- `BreakWindow` is placed more than once when its handle starts on a monitor at another scale
  than the primary one, the last time from the dispatcher's queue. Whether it is drawn for a
  moment at its first position was not looked at.
- Not looked at: the tray menu, Settings and About on a monitor at another scale, which need a
  tray start (ADR 0042). The owner looks at them in their own copy.
- Guards: none by test. Measured as above, with `--preview`.
