# 0081. The reminder is deaf to Space as well as Enter

Date: 2026-11-11
Status: accepted 2026-11-11
Supersedes: 0060 in part (Space closing the reminder)

## Context

ADR 0060 had `BreakWindow` swallow Enter in `OnPreviewKeyDown`, because the reminder arrives
mid-work and takes the focus, and Enter is the keystroke most likely to be on its way. Space
still closed it, "to which the focused button responds", judged far less likely mid-sentence. On
2026-11-11 the README promised that "a reminder that pops up while you are typing is not
dismissed by accident". The review of the README found that Space broke the promise, since it
falls between any two words. The owner ruled that this was a bug: the README is right, and Space
should not close the reminder.

Measured that day with `--preview`, by posting `WM_KEYDOWN` and `WM_KEYUP` straight to the
reminder's window:

- before the change, Space closed the reminder, Enter left it open and Esc closed it;
- after it, Space and Enter both leave it open, and Esc still closes it.

## Decision

`OnPreviewKeyDown` marks Space handled as well as Enter, so the focused `Close` button sees
neither. A Space swallowed there never presses the button, so its key-up clicks nothing. Esc (the
button is `IsCancel`, and `OnKeyDown` closes on Escape) and a click on `Close` still close the
reminder. The button keeps the focus (R-070).

## Alternatives rejected

- **A `Close` button that cannot take the focus.** It would change R-070 and lose the focus
  ring, and swallowing the key in the same handler as Enter is the place ADR 0060 named for
  another key.
- **Swallowing Space on key-up only.** The button reacts to the key-down by showing itself
  pressed. A handled key-down settles both halves.

## Consequences

- From the keyboard, the reminder now closes only with Esc.
- R-071 says so, with an example. The note `docs/design/windows.md` and the rule in `CLAUDE.md`
  name Enter and Space.
- Guards: none by test. The App is checked by launching it (ADR 0042): `--preview`, then Space,
  Enter and Esc, as measured above.
