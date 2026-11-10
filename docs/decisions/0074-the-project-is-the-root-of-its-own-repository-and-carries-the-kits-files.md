# 0074. The project is the root of its own repository and carries the kit's files itself

Date: 2026-11-10
Status: accepted 2026-11-10; the kit key, step 0 skipped, sync.ps1, the closed copy in docs/decisions/kit/, citing the kit's ADRs, GHID.md, the tests of the scripts left in the kit superseded by 0075
Supersedes: 0035 in part (the hooks in the root .githooks/); 0036 in part (the kit value and the tag name); 0037 in part (comments for the "why" only); 0040 in part (the git hooks, the root CI and the *.app exception)

## Context

Until 2026-11-10 Mindfulness Reminder was a folder of the `photo` repository, beside the kit,
`ClaudeCodeStarterKit`, and several other projects made from it. `kit.json` named the kit as
`../ClaudeCodeStarterKit` (ADR 0036); the git hooks ran from that repository's root
`.githooks/` (ADR 0035), and CI at that root was left for later (ADR 0040); the kit's decisions and
its guide, `GHID.md`, were read in the kit folder. On 2026-11-10 the owner gave the project a
repository of its own, `github.com/tcarean/MindfulnessReminder`, which will hold this project
and nothing else, copied the project folder and the kit folder into it, and decided to merge
the two at the repository root: a kit folder that serves one project has no purpose of its
own. The kit's guide foresees the move: a project that leaves `photo` takes a copy of the guide
with it. The new repository starts with no commit.

## Decision

- The project folder is the repository root. Nothing moved relative to it: `src/`, `tests/`,
  `docs/`, `scripts/`, `.claude/` and `kit.json` are where they were.
- `kit.json`'s `kit` is `"."`: this project holds the kit's files and is their only user.
  `scripts/`, `.claude/skills/`, `.claude/agents/`, `.claude/rules/kit/` and the process keys
  of `.claude/settings.json` are edited here. Step 0 of `scripts/verify.ps1` is skipped and
  `scripts/sync.ps1` has nothing to copy. On the day of the move every file in those folders is
  byte-identical with the kit's.
- The files the kit keeps to itself come along: `scripts/install-hooks.ps1` and the three git
  hooks in `scripts/hooks/` (`pre-commit`, `pre-merge-commit`, `commit-msg`), installed with
  `core.hooksPath = scripts/hooks`, the kit's layout for a standalone repository; the kit's
  `.gitattributes` and `.gitignore`; `.github/workflows/ci.yml`, which runs the gate on every
  push to `main` and every pull request; and `GHID.md`, at the root, in Romanian, with a dated
  note at its top.
- The kit's 36 decision records are copied, unchanged, to `docs/decisions/kit/`, with a README
  that says what they are. "The kit's ADR NNNN" names a file there. The folder is closed: a
  process decision made from now on is a project ADR, whose text names the kit ADR it replaces.
- Every decision of the kit holds here as before. One link changes its form: ADR
  0069 (a documentation comment on every type and method) superseded the "comments for the
  why only" part of ADR 0037, and ADR 0037's Status said so by citing the kit's ADR. A header
  may not cite a kit ADR where `kit` is `"."` (`scripts/check-decisions.ps1`), so ADR 0037's
  Status names this ADR, which carries that rule.
- A release is tagged `vX.Y.Z`, the name `scripts/release.ps1` gives a project that is its
  repository's root.
- Not brought along: the kit's sample code and its tests (`Starter.sln`, `src/Starter.*`,
  `tests/Starter.Tests`, the fixtures), `scripts/rename.ps1`, `monorepo/`,
  `bitbucket-pipelines.yml`, and the kit's own README, CHANGELOG, plans, requirements,
  validation and design files. They remain in the kit, in `photo`.

## Alternatives rejected

- **Keep the kit as a folder beside the project, as in `photo`.** A second project folder, with
  a sample solution, in a repository that holds one project; the owner ruled it out.
- **Delete the `kit` key, as the kit's README says for a separate repository.** The shared
  folders would stay the kit's: the Stop hook would refuse an edit to them and point at a kit
  this repository does not contain, and a fix would have to be made in `photo` and copied by
  hand.
- **Renumber the kit's decisions into `docs/decisions/`.** "The kit's ADR NNNN" is cited 82
  times outside `docs/plans/`, 60 of them in the shared folders (counted 2026-11-10); every one
  would have to be rewritten, in files that are otherwise the kit's bytes.
- **Bring the kit's tests of the scripts.** They live in the kit's test project, beside the
  tests of its sample code. The scripts are unchanged from the kit, where those tests run;
  porting them is a task of its own, for the day a script is first changed here.

## Consequences

- The gate no longer compares anything with a kit: a change to a script, a skill or a rule is
  reviewed like any other change, and nothing in this repository tests the scripts.
- Taking a later version of the kit is a copy by hand of the four folders from `photo`, read as
  a diff before the commit.
- The ignore exception for `src/MindfulnessReminder.App/` is gone, with the `*.app` rule of
  `photo`'s root `.gitignore` that made it necessary.
- CI runs on GitHub for the first time; its first run is the first push.
- `git log` here starts on 2026-11-10. The earlier commits, the ones ADR 0040 points to, are in
  `photo`.
