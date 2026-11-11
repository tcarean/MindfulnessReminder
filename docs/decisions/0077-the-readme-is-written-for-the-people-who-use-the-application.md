# 0077. The README is written for the people who use the application

Date: 2026-11-11
Status: accepted 2026-11-11

## Context

ADR 0068 made `README.md` the user documentation and the only place for it. By 2026-11-11 it
also held what only whoever maintains the code needs: "Development: the gate, the documentation
and Claude Code", a "Design decisions" table, "Layout", "Versioning", the four things
`LicenseHeaderTests` pins, the format of the decision log, test, class and API names
(`DispatcherTimer`, `QUNS_NOT_PRESENT`), the version each feature arrived in, an incident of
2026-10-26, and sentences about the owner, down to "The year and the author are a deliberate
choice, not something to roll forward automatically." Its examples still showed 1.4.0 at 1.5.0.
The owner reviewed it that day and asked for a README that says at the top what the app does
now, builds it in one line, reads as written by a person, names neither Claude nor the owner,
and holds nothing that means nothing to a real user.

## Decision

`README.md` holds what a person who uses the application needs: what it does, the one line that
builds it (ADR 0076) and how to start it, how to use it (the tray menu, the two reminders, the
periodic one, the pauses, `--preview` and `--detect`), where its log is, and the licence notice.
It holds no process, no design rationale, no version history, no version number (its examples
write `X.Y.Z`), no test, class or API names, and no mention of Claude or of the owner. Whoever
maintains the code reads `CLAUDE.md`, `.claude/rules/process.md` and `docs/`, where everything
removed already was. The licence notice keeps its copyright line, which is the GPL's own.

## Alternatives rejected

- **A short "For developers" section pointing at `CLAUDE.md` and `docs/`, as AstroDarkroom
  has.** It would name Claude and bring back the development section the owner asked to drop;
  whoever maintains the code finds `CLAUDE.md` without a pointer.
- **Keeping the design table and the explanations, rewritten.** They are the ADRs and the
  design notes in short, a second copy, and it had already drifted.
- **Dropping the copyright line with the other mentions of the author.** It is part of the GPL
  notice, the same line every source file and About carry.

## Consequences

- `/new-feature` records a user-visible change in the README's "Using it" list and the
  feature's own section, in the words a user reads; a design choice goes in `docs/decisions/`.
- `Directory.Build.props` points to `docs/design/versioning-and-licence.md`, and the release
  skill to "Build and run", instead of the sections that are gone.
- The README names no version, so a release or a version bump changes nothing in it.
- Guards: none by test. `/wrap-up` reads the README at its step 9, and the rule "User
  documentation lives in `README.md`" in `CLAUDE.md` says what stays out.
