# 0080. The application's executable is x64 whatever SDK builds it: PlatformTarget x64, with no runtime declared

Date: 2026-11-11
Status: accepted 2026-11-11
Supersedes: 0036 in part (the any-CPU publish)

## Context

ADR 0076 named the release's zip `MindfulnessReminder-{version}-win-x64.zip`, after one reading
of the published executable (PE machine 8664). ADR 0036 had decided a framework-dependent,
any-CPU publish with no extra arguments and no runtime declared. Without a runtime, the apphost
`MindfulnessReminder.exe` takes the architecture of the SDK that runs the publish: the SDK sets
`DefaultAppHostRuntimeIdentifier` to `NETCoreSdkRuntimeIdentifier`. So the name was true only on
an x64 SDK. The review of 2026-11-11 found this, and the owner chose to pin x64.

The first fix, measured that day, passed `-r win-x64 --self-contained false` to the publish and
declared `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>` on App and Core, as ADR 0034 says.
It worked, and the lock files of both gained an empty runtime section. But the review of that
branch found, and I confirmed in the App's `project.assets.json`, that every restore then
downloads three runtime packs at 8.0.34 that a framework-dependent publish never uses:

- `Microsoft.AspNetCore.App.Runtime.win-x64` (11.6 MB);
- `Microsoft.NETCore.App.Runtime.win-x64` (34.5 MB);
- `Microsoft.WindowsDesktop.App.Runtime.win-x64` (35.1 MB).

That is 81.2 MB, for every CI run (its `setup-dotnet` keeps no cache) and every fresh clone.
`Microsoft.NET.RuntimeIdentifierInference.targets:196`, in SDK 9.0.321, sets
`DefaultAppHostRuntimeIdentifier` to `win-x64` when `PlatformTarget` is `x64`.

## Decision

`MindfulnessReminder.App.csproj` sets `<PlatformTarget>x64</PlatformTarget>`. The executable,
and the App's own assembly, are x64 whatever SDK builds them. The publish keeps no extra
arguments and no runtime is declared, so restore downloads no runtime pack, the lock files do
not change, and ADR 0034 does not apply. Core stays any-CPU. The publish stays
framework-dependent, so it needs the x64 .NET 8 Desktop Runtime, which the README names.

## Alternatives rejected

- **`-r win-x64 --self-contained false` with `<RuntimeIdentifiers>` (the first fix).** It
  downloads the 81.2 MB of runtime packs above on every restore without a cache. It also
  changes the two lock files, and it needed ADR 0034's test-project condition widened from
  `.Tests` to `Tests` to leave out `MindfulnessReminder.ProcessTests`.
- **Dropping the runtime from the zip's name** (`MindfulnessReminder-{version}.zip`). The owner
  chose to keep the label and make it true. Without it, the zip would not say which runtime its
  executable needs.
- **A `<RuntimeIdentifier>` (singular) in the App's project file.** It would move every build's
  output to `bin/Release/net8.0-windows/win-x64/`, a folder `CLAUDE.md`'s `--preview` command
  does not name.

## Consequences

- The zip's name holds on any machine. A release cut with an arm64 SDK downloads only the small
  x64 apphost pack, and produces an x64 executable.
- `dotnet build` and the gate are unchanged: `PlatformTarget` does not change the output folder,
  `bin/Release/net8.0-windows/`. The tests reference Core, which stays any-CPU.
- The App's own assembly is x64 too. It is only ever loaded by its x64 executable.
- Guards: none by test. Measured on 2026-11-11 after a `-Build`:
  - `MindfulnessReminder.exe` and `MindfulnessReminder.dll` are PE machine 8664;
  - `MindfulnessReminder.Core.dll` is 014C, any-CPU;
  - the App's `project.assets.json` lists no runtime pack;
  - both lock files have main's bytes.

  The measurement is recorded in
  `docs/plans/2026-11-11-fix-the-findings-of-the-branch-review.md`.
