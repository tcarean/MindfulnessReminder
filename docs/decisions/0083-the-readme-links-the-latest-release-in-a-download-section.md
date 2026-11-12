# 0083. The README links the latest release in a Download section of its own

Date: 2026-11-12
Status: accepted 2026-11-12
Supersedes: 0082 in part (where the sentence stands)

## Context

ADR 0082 put one sentence about the release zip at the top of "Build and run", with no link. The
same day the owner did not find it in the README, and asked whether a project usually gives a
direct download link. A link to one file, `releases/latest/download/<name>`, works only while
the name stays the same from release to release, and this project's zip carries its version
(`MindfulnessReminder-X.Y.Z-win-x64.zip`, ADR 0076). `releases/latest` sends the reader to the
newest release whatever its name: on 2026-11-12,
`https://github.com/tcarean/MindfulnessReminder/releases/latest` answered 302 to
`.../releases/tag/v1.5.1`. The owner chose that link, in a section after the introduction and
before "Build and run".

## Decision

The README has a "Download" section between the introduction and "Build and run". It says that
the latest version is on the Releases page, links `releases/latest` of the repository, names
`MindfulnessReminder-X.Y.Z-win-x64.zip`, and says to unzip it, start `MindfulnessReminder.exe`
and have the .NET 8 Desktop Runtime for x64. "Build and run" no longer carries the sentence. The
owner ruled that the repository's URL, which holds the account's name, is not a mention of the
owner in the sense of ADR 0077.

## Alternatives rejected

- **A direct link to the zip, `releases/latest/download/<name>`.** The name changes with every
  version, so the link would break at the next release, unless the zip lost its version (a
  change to `release.zip` in `process.json` and to ADR 0076) or the README named the version,
  which ADR 0077 rules out.
- **A release badge from shields.io.** It adds an image from another service and leads to the
  same page as the plain link.
- **A relative link, `../../releases/latest`.** It works only where GitHub renders the README,
  and nowhere else the file is read.
- **The sentence kept at the top of "Build and run", with the link.** The owner did not find it
  there and asked for it before that section.

## Consequences

- The link stays true at every release with no edit to the README, as long as each release is
  published on GitHub with its zip, which the owner does by hand (the release skill, step 7).
- The README still names no version: the section writes `X.Y.Z` (ADR 0077).
- If the repository moves or is renamed, the link changes with it.
- `CLAUDE.md` (the rule "User documentation lives in `README.md`") names this record.
- Guards: none by test. `/wrap-up` step 9 reads the README.
