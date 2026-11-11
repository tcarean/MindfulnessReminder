# 0079. The README is written in plain sentences

Date: 2026-11-11
Status: accepted 2026-11-11

## Context

On 2026-11-11 the owner asked for a README that does not read as written by AI, and for the
artificial phrasing in it to go. The rewrite recorded what that meant only in its plan
(`docs/plans/2026-11-11-write-the-readme-for-users-and-zip-the-release.md`). ADR 0077 records
what the README holds and leaves out, not how it is written. The review of 2026-11-11 found
these rules nowhere a later edit would read them, so the next feature written into the README
by `/new-feature` could bring the old style back. The owner asked for them to be recorded.

## Decision

The README is written in plain sentences:

- no em-dash asides, whether `—` or ` -- `;
- no bold lead-in on every bullet;
- none of the words "deliberately", "on purpose", "honest", "happily" or "the whole point".

A sentence states what the application does, for the person who uses it. Bold marks the names
of menu items, settings and buttons as the application shows them. This extends ADR 0077, and
the rule in `CLAUDE.md` names both.

## Alternatives rejected

- **Leaving the rules in the plan.** A plan records one task. `/new-feature` and `/wrap-up` read
  `CLAUDE.md`, which every session loads, not old plans.
- **A test that greps the README for the patterns.** The words and the dashes could be checked,
  but a sentence that reads as artificial cannot. The rule stays with the judgement at
  `/wrap-up` step 9, as ADR 0077's exclusions do.

## Consequences

- `CLAUDE.md` (the rule "User documentation lives in `README.md`") and `/new-feature` step 8
  cite this record.
- The licence notice keeps its own wording: it is the GPL's text, not the README's.
- Guards: none by test. `/wrap-up` step 9 reads the README.
