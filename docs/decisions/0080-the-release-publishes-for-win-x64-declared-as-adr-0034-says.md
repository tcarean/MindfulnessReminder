# 0080. The release publishes for win-x64, the runtime declared on every project but the tests

Date: 2026-11-11
Status: accepted 2026-11-11
Supersedes: 0036 in part (a publish with no runtime)

## Context

ADR 0076 named the release's zip `MindfulnessReminder-{version}-win-x64.zip`, after one reading
of the published executable (PE machine 8664). ADR 0036 had decided a framework-dependent,
any-CPU publish with no extra arguments and no runtime declared. Without a runtime, the apphost
`MindfulnessReminder.exe` takes the architecture of the SDK that runs the publish
(`DefaultAppHostRuntimeIdentifier` is `NETCoreSdkRuntimeIdentifier`), so the name was true only
on an x64 SDK. The review of 2026-11-11 found this, and the owner chose to pin x64.

Measured that day: `dotnet publish -r win-x64 --self-contained false` of the App builds, gives a
deps.json whose `runtimeTarget` is `.NETCoreApp,Version=v8.0/win-x64`, and rewrites the lock
files of the App and of Core. It adds an empty section to each,
`"net8.0-windows7.0/win-x64": {}` and `"net8.0/win-x64": {}`. That is the change ADR 0034
describes.

## Decision

- The publish entry in `process.json` passes `-r win-x64 --self-contained false`. The
  publish stays framework-dependent: it needs the x64 .NET 8 Desktop Runtime, which the README
  names.
- `Directory.Build.props` declares `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>` on every
  project whose name does not end in `Tests`, as ADR 0034 says. Every restore then writes the
  runtime's section into the lock files of the App and Core, which are committed with it, and
  the release's publish changes none.
- ADR 0034 itself writes `.Tests`, which would miss `MindfulnessReminder.ProcessTests`.

## Alternatives rejected

- **Drop the runtime from the zip's name** (`MindfulnessReminder-{version}.zip`). The owner
  chose to keep the label and make it true. Without it, the zip would not say which runtime its
  executable needs.
- **A `<RuntimeIdentifier>` (singular) in the App's project file.** It would move every build's
  output to `bin/Release/net8.0-windows/win-x64/`, the folder `CLAUDE.md`'s `--preview` command
  does not name, for a choice only the release needs.
- **The runtime on the test projects too.** They are never published, and in AstroPhotoAnalyzer
  the same declaration wrote 844 lines into each test project's lock file (ADR 0034).

## Consequences

- The zip's name holds on any machine. A release cut with an arm64 SDK still produces an x64
  executable, and the runtime pack it needs is restored for win-x64.
- `dotnet build` and the gate are unchanged: `RuntimeIdentifiers` (plural) adds the runtime to
  restore only, and the build output stays in `bin/Release/net8.0-windows/`.
- A change to the publish's runtime is a change to `process.json`, `Directory.Build.props`, the
  two lock files and the zip's name together.
- Guards: none by test. A publish that rewrote a lock file would show in `git status` at
  `/release` step 6 (ADR 0034), and CI restores in locked mode.
