# 0084. Review findings live in docs/reviews, and each is closed by its own task

Date: 2026-11-13
Status: accepted 2026-11-13

## Context

On 2026-11-13 the owner asked for a review of the whole application, most of which was written
by an earlier model and never reviewed, ending in one documented list of problems to fix one by
one in order of severity, in separate sessions. Until then a review's findings had lived inside
the plan of the task that fixed them (`docs/plans/2026-11-11-fix-the-findings-of-the-branch-review.md`),
which works when one task fixes them all and not when the fixes are spread over many sessions.
A session that picks up one finding has none of the review's context, so the entry itself has to
carry the scenario and how it was confirmed.

## Decision

A review of existing code writes its findings to `docs/reviews/YYYY-MM-DD-slug.md`, one file per
review. Each finding has an ID (`CR-NN`, numbered within its file), a severity from S1 to S4 as
the file defines them, where it is, the scenario and the wrong result, what it contradicts, how
it was confirmed, the direction of the fix and a status. A finding is entered only after someone
other than the reviewer that claimed it has followed it in the code; a claim not confirmed is
listed apart as unverified, and one disproved is listed as rejected. The review fixes nothing.
Each finding, or each group the file marks as one fix, is closed by a task of its own, which
starts with the failing test (the rule "Test before fix") and sets the entry's status to `fixed`.

## Alternatives rejected

- **The findings in a plan under `docs/plans/`.** A plan is approved once and then records what
  happened; this list is edited by every fix task for as long as an entry is open.
- **An issue tracker.** The repository's issues are public and outside the checkout, and a
  session reads files, not a tracker; the list also cites lines and requirement rows that are
  reviewed with the code.
- **Fixing during the review.** A fix needs its test first and its own diff to read; a whole list
  of them in one branch could not be reviewed or reverted one at a time.
- **One file that every later review appends to.** A review is dated: its line numbers and its
  "not covered" are true for the commit it read.

## Consequences

- A fix session needs only the entry: it does not reread the review.
- The line numbers in a review go stale as fixes land; each entry therefore names the member as
  well as the line, and the file names the commit it read.
- The list has to be kept: a fix task that forgets the status leaves an entry open that is not.
  Nothing checks it; the fix task's brief names the entry, and its wrap-up sets the status.
- `CLAUDE.md` names the folder in its Layout.
- Guards: none by test.
