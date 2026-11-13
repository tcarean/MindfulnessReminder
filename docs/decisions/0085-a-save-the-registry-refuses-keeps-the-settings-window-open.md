# 0085. A Save the registry refuses keeps the Settings window open

Date: 2026-11-13
Status: accepted 2026-11-13
Supersedes: 0060 in part (Settings handing its result back through its Accepted event)

## Context

The review of 2026-11-13 found (CR-01) that a `Save` in the Settings window whose registry
write is refused, by a policy or by security software, ended the application through the crash
dialog: nothing between the button and the dispatcher caught the `UnauthorizedAccessException`,
and Windows then restarted the copy, which did the same at the next `Save`. No requirement row
said what a refused `Save` does, while R-003, R-005, R-006 and R-051 all treat the same refusal
as something that does not stop the application. The review put the question to the owner (Q1)
with two options, and on 2026-11-13 the owner chose the first.

## Decision

A `Save` the registry refuses leaves the Settings window open, with what was typed and the line
`The settings could not be saved: Windows refused the change.`; nothing is applied, the refusal
is written to the decision log, and the application goes on (R-046). The rule, write and then
apply and report a refusal, is `SettingsCommit.TrySaveAndApply` in Core; the window is given it
as a function that returns whether the settings were stored.

## Alternatives rejected

- **A tray balloon, as R-005 does for the startup checkbox, with the window closed.** What was
  typed would be lost and typed again for the next try, and the window already has a line for
  what is wrong with a `Save`.
- **Applying the settings for this run though they were not stored.** The next start would read
  the old ones back with nothing to say why; the settings in force and the stored ones stay the
  same thing.
- **Catching the refusal in the window's code.** The rule could then not be tested: the test
  project does not reference App (ADR 0042).

## Consequences

- The window no longer raises an event: it takes the save as a function and closes only when
  that returns `true`. ADR 0060 had Settings hand its result back through its `Accepted`
  event; that sentence of it is superseded.
- A refusal is what `RegistryRefusal.IsWriteRefusal` says, the two exceptions R-005 names for
  the startup checkbox. An `IOException` on a `Save` is not one, though the registration at
  start counts it (R-003), and ends in the crash handler.
- The complaint stays until a box is edited or the next `Save`, as the other complaints do.
- An error of a `Save` that is not a refusal still ends in the crash handler (R-076).
- Guards: `SettingsCommitTests` pins the rule and the log line on `FakeRegistryStore`. The
  window's line is checked by a tray start (ADR 0042): deny "Set Value" on
  `HKCU\Software\MindfulnessReminder` in regedit, then `Save`.
