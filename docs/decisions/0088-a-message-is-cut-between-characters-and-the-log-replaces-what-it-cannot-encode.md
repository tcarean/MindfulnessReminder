# 0088. A message is cut between characters, and the log replaces what it cannot encode

Date: 2026-11-13
Status: accepted 2026-11-13

## Context

The review of 2026-11-13 showed (CR-07) two faults with one cause. `NormalizeMessage` cut a
message at `MaxMessageLength` by counting UTF-16 units, so a stored text of 49 letters and an
emoji came out as 50 units ending in a high surrogate, half a character. And
`FileDecisionLog.Write` appended with the encoding `File.AppendAllText` uses when none is
named, which throws on half a pair; the writer swallows every error (ADR 0073), so each line
carrying such a message (`Settings loaded`, `Unlock reminder shown`, `Settings applied`,
`Settings saved`) was lost without a trace. Both were shown by a test that failed: `length 50,
last char U+D83D`, and a log file left empty after `Write("a\uD83Db")`.

The review left one question to the fix task: whether the log should also survive a text it
cannot encode, since a message is not the only text that can carry one (an exception's
message in the `Crashed:` line, an argument in the `Started` line). The owner answered on
2026-11-13: yes.

## Decision

- The limit goes on counting UTF-16 units, the unit the `TextBox` of Settings counts with its
  `MaxLength`. When the unit a cut would end on is a high surrogate, the cut is made one unit
  earlier, so the character is dropped whole (R-041).
- `FileDecisionLog` writes with a `UTF8Encoding` of its own: no byte order mark, and U+FFFD in
  place of what has no UTF-8 form. The line is kept (R-110).

## Alternatives rejected

- **Counting characters (text elements) instead of units.** The box and the clamp would then
  disagree: the box's `MaxLength` and `IsMessageAcceptable` count units, and R-043 has the
  dialog's checks agree with the clamp.
- **Fixing only the cut, and letting the log lose a line it cannot encode.** The cut is not
  the only source of half a pair, and the line lost would be the one that reports the odd
  text. A log that goes silent is the failure ADR 0073 ranks worst.
- **Removing every unpaired surrogate in `NormalizeMessage`.** A wider rule than the finding:
  only a hand-written registry value can hold one away from the cut, and nothing was shown to
  go wrong with it once the log no longer loses the line.

## Consequences

- A stored message with half a pair away from the cut still reaches the reminder window as
  written; what the window draws for it was not looked at.
- Whether typing or pasting in Settings can leave half a pair at the 50th unit was not
  checked; `IsMessageAcceptable` is unchanged and would accept it, and the next load would
  not cut it, since it is not over the limit.
- A reader of the log sees U+FFFD where the text had half a pair, not the original unit.
- Guards: `R041_A_message_cut_at_the_limit_does_not_end_in_half_a_character`,
  `R041_A_character_of_two_units_that_ends_at_the_limit_is_kept`,
  `R110_A_text_that_cannot_be_encoded_is_written_with_a_replacement_character`, and
  `R110_Lines_end_in_the_platform_newline_and_the_file_has_no_byte_order_mark` for the encoding's
  other half.
