# Fix the findings of the README review

Saved, once approved, as `docs/plans/2026-11-11-fix-the-findings-of-the-readme-review.md`.
Branch `claude/Fix-the-findings-of-the-README-review`, worktree
`.claude/worktrees/Fix-the-findings-of-the-README-review`, cut from
`claude/Build-the-app-without-a-release` (`63acc30`, not merged yet): one merge brings both.

## Context

On 2026-11-11 the owner asked for seven confirmed findings of the review of the README rewrite
to be fixed, then for a code review by a subagent:

1. The pause claims are true only for the periodic reminder.
2. The `--detect 60` example does not match the dialog.
3. The `CLAUDE.md` rule lacks the exceptions in ADR 0077.
4. ADRs 0059, 0024 and 0045 make stale claims about the README without the required
   bookkeeping.
5. The writing rules live only in the plan.
6. ADR 0076's title says more than its decision.
7. The `win-x64` label holds only on an x64 SDK.

For item 7 the owner chose to pin x64.

Measured on 2026-11-11: `dotnet publish ... -r win-x64 --self-contained false` builds. It also
rewrites `src/MindfulnessReminder.App/packages.lock.json` and
`src/MindfulnessReminder.Core/packages.lock.json`, adding an empty
`"net8.0-windows7.0/win-x64": {}` and `"net8.0/win-x64": {}`. The deps.json then has
`runtimeTarget` `.NETCoreApp,Version=v8.0/win-x64`. The lock files were reverted afterwards.

## Changes

1. **Pause claims (`README.md`).**
   - The intro's "it waits instead of interrupting" becomes "it does not interrupt you".
   - In "Using it", the tooltip shows "that reminders are paused", not "why one is waiting".
   - The tooltip paragraph in "Pausing" starts with "While the periodic reminder is on". It also
     says that with the periodic reminder off the tooltip shows only the name and version, and
     that a skipped unlock reminder leaves its reason in the log (R-060, R-112).
   - "What it will and will not catch" says that a periodic reminder waits for as long as the
     microphone is open and an unlock reminder is skipped (R-014, R-033). The sentence "Hovering
     over the tray icon shows why" goes.
2. **The `--detect` example (`README.md`).**
   - The block becomes the dialog's text as `App.xaml.cs` writes it: "Watched for 60 seconds.",
     a blank line, then `DetectionLog.Render`. Lines start at 0:01 and have no padding
     (`0:01 - 0:04   mic off, camera off, Normal   ->  None`). Then a blank line, "A reminder
     would have been held back for 44 of the 60 readings.", a blank line, and
     `Mindfulness Reminder X.Y.Z`.
   - The text says the results come in a message box, both for the single reading and at the
     end of the timeline.
3. **The `CLAUDE.md` rule.** "No version number" becomes "no number of the app's own version
   (examples write X.Y.Z)". It gains "no test, class or API names" and "the licence notice keeps
   its copyright line" (ADR 0077), plus the writing rules of item 5 (ADR 0079).
4. **Stale ADRs (ADR 0024 rules).**
   - ADR 0077 gains the header line `Supersedes: 0024 in part (the README's summary of the
     gate); 0059 in part (the README's examples)`. ADR 0024's and ADR 0059's Status lines gain
     the matching clauses. Header lines may be completed after a merge.
   - ADR 0045, line 37, an alternative's reason, gets a `(Restated 2026-11-11: ...)` note
     beside it: the README no longer says "Don't reintroduce a literal", and the rule is in
     `docs/design/versioning-and-licence.md`.
5. **Writing rules.** New **ADR 0079**: the README is written in plain sentences, with no em-dash
   asides (`—` or ` -- `), no bold lead-in on every bullet, and no "deliberately", "on purpose",
   "honest", "happily" or "the whole point". It extends ADR 0077 and supersedes nothing. The
   `CLAUDE.md` rule (item 3) and `/new-feature` step 8 cite it.
6. **ADR 0076's title**, fixed in place as an unclear phrase (ADR 0024): "The README builds
   with the full release, into artifacts/X.Y.Z/ with a zip beside it, and the application runs
   from there". The file name stays. The ADR 0036 note's phrase "the application is built only
   by `scripts/release.ps1`" becomes "the README builds the application with
   `scripts/release.ps1`".
7. **Pin win-x64.**
   - `process.json`: the publish entry gets `"args": ["-r", "win-x64", "--self-contained",
     "false"]`.
   - `Directory.Build.props`: `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>` with
     `Condition="!$(MSBuildProjectName.EndsWith('Tests'))"`, which declares it on App and Core.
     ADR 0034's literal `.Tests` would also catch `MindfulnessReminder.ProcessTests`, whose
     lock file would then gain a test runtime section.
   - `dotnet restore` writes the section into the two lock files, and they are committed.
   - New **ADR 0080**, which supersedes ADR 0036 in part (a publish with no runtime). ADR 0036's
     Status gains the clause.
   - The texts that say "no runtime" or "any-CPU" are updated: `process.json` `$comment`, the
     release skill step 5, the `CLAUDE.md` Workflow line. `README.md` asks for the x64 .NET 8
     Desktop Runtime.
   - `CHANGELOG.md` gets an Internal line.

## Verification

1. `scripts/check-decisions.ps1`: both sides of every supersession agree.
2. `release.ps1 -Build` in the worktree:
   - `MindfulnessReminder.exe` is PE machine 8664;
   - the deps.json says `win-x64`;
   - `git status` shows no lock file rewritten by the publish;
   - the zip's hash matches its `.sha256`.
3. Grep `README.md` for the old pause sentences, `—` and ` -- `, and check the `--detect` block
   against `DetectionLog.Render` and `App.xaml.cs:312-326`.
4. The gate (process tests included, since `scripts/`-adjacent files differ from main), then
   commit.
5. A code review by one subagent of `main...HEAD`, which covers both unmerged branches. Report
   its findings, then propose the merge.

## What happened (2026-11-11)

- `dotnet restore` with the declared runtime changed only the two lock files the plan named.
  Each gained one empty runtime section, and the test projects' lock files did not change.
- `release.ps1 -Build` in the worktree took 250 s and exited 0. Its verify ran 321 app tests and
  120 process tests and passed in 243 s.
  - The publish left both lock files byte for byte as the restore wrote them (hashes compared
    before and after).
  - `MindfulnessReminder.exe` is PE machine 8664, and the deps.json names `v8.0/win-x64`.
  - The zip's SHA-256 matches its `.sha256`.
- `check-decisions.ps1`: 80 decisions, with both sides of the supersessions of 0024, 0036 and
  0059 in agreement.
- Deviations:
  - The ADR 0036 note quoted the Decision with double quotes nested inside double quotes.
    While its overclaim was fixed in place, the inner title became `'Deployment note'`
    (single quotes).
  - The `CLAUDE.md` release line was reflowed.
