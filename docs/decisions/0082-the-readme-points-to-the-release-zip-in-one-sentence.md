# 0082. The README points to the release zip on GitHub in one sentence

Date: 2026-11-12
Status: accepted 2026-11-12

## Context

On 2026-11-12 version 1.5.1 became the first release on GitHub, tagged `v1.5.1`. Its zip,
`MindfulnessReminder-1.5.1-win-x64.zip`, and the `.sha256` beside it are uploaded to the
repository's Releases page by hand (ADR 0076). The README only said how to build the application
from a clone with `scripts/release.ps1 -Build` (ADR 0078), so a reader who wanted the
application without the .NET 9 SDK and git was not told that the zip exists. The owner was
offered three options, one sentence, a "Download" section of its own, or no change, and chose
one sentence.

## Decision

"Build and run" opens with one sentence: each release on the Releases page has
`MindfulnessReminder-X.Y.Z-win-x64.zip`, which is unzipped and started as
`MindfulnessReminder.exe`, and it needs only the .NET 8 Desktop Runtime for x64. This extends
ADR 0077's list of what the README holds; `scripts/release.ps1 -Build` stays its one line that
builds the application (ADR 0078).

## Alternatives rejected

- **A "Download" section before "Build and run", with how to update a downloaded copy, and the
  `--preview` and `--detect` examples without their `artifacts\X.Y.Z\` prefix.** The owner
  chose the smaller change; the release notes on GitHub say how to install and update.
- **No change, with the steps only in each release's notes.** A reader of the README would not
  know that a release can be downloaded.

## Consequences

- The README still names no version: the sentence writes `X.Y.Z` (ADR 0077).
- The examples keep `artifacts\X.Y.Z\`; whoever runs a downloaded copy reads it as the folder
  they unzipped into.
- The sentence is true only while each release uploads its zip to the Releases page, which the
  owner does by hand after the release commit and tag (the release skill, step 7).
- `CLAUDE.md` (the rule "User documentation lives in `README.md`") names this record.
- Guards: none by test. `/wrap-up` step 9 reads the README.
