---
name: release
description: Cut a release - pick the version from the CHANGELOG, run scripts/release.ps1 (verify, version bump in Directory.Build.props, CHANGELOG section, publish what process.json lists), then propose the commit and the tag, which the owner runs on main. Use when the user says /release or asks to release, tag or publish a version.
---

# Release

The script does the mechanical part; you do the judgement and the asking. A release is cut on
`main`, in the main checkout, in a session the owner opened in the project folder there; commits
and tags on `main` are the owner's (ADRs 0021 and 0029): nothing here commits, tags or
pushes. The owner chooses the version number (the rule "Version and year are the owner's");
you propose one.

1. **Where we are.** `git branch --show-current`, `git status --porcelain -- .` and
   `git rev-parse HEAD`; note the hash. A release is cut from `main`, with a clean tree;
   `release.ps1` refuses any other branch. If the tree is dirty, stop: that work is a task of
   its own, `/wrap-up` first.
2. **What is being released.** Read `[Unreleased]` in `CHANGELOG.md`, the newest released
   heading (`## [X.Y.Z] - date`) and the current `<Version>` in `Directory.Build.props`. If
   `<Version>` is above the newest released heading, the owner has already bumped it: propose
   that number. Otherwise propose the next version by SemVer: a `Removed` or a
   behaviour-breaking `Changed` entry means major; anything under `Added` means minor; only
   `Fixed` and `Internal` means patch. State the reasoning in one line and ask the owner to
   confirm or override the number. The script refuses a version that is not above the newest
   released heading, is below `<Version>`, or is already tagged.
3. **Figures.** `docs/validation.json` has no case (ADR 0039): skip. If it ever has cases,
   ask whether to run `/validate` first; a release is the moment a stale figure gets noticed.
4. **Dry run.** `scripts/release.ps1 -Version X.Y.Z -WhatIf` and paste its output: tag name,
   Unreleased content and what would change. The tag is `vX.Y.Z` (ADR 0074).
5. **Run it.** `scripts/release.ps1 -Version X.Y.Z`, with a 10-minute timeout: it runs verify
   with every test (`-All`, about three minutes), edits `Directory.Build.props` and
   `CHANGELOG.md`, publishes each entry of `release.publish` to a staging folder and merges
   it into `artifacts/X.Y.Z/<output>/` with a hash check: here the application,
   framework-dependent, into `artifacts/X.Y.Z/` itself (`dotnet publish -c Release` of
   `src/MindfulnessReminder.App`, no runtime; its `PlatformTarget` x64 makes the executable x64,
   ADR 0080). It writes
   `artifacts/X.Y.Z/release.json` (the version, the commit, and this project's version, commit,
   unreleased flag and whether its folder was clean) and, when `process.json` sets `release.zip`,
   as it does here (`MindfulnessReminder-X.Y.Z-win-x64.zip`, ADR 0076), the zip in
   `artifacts/` with its `.sha256` beside it (ADR 0071). Paste the last
   lines. If it fails, report and stop; it changes nothing before verify passes. A collision
   (two entries publishing different bytes at one path) stops it after the version files
   changed: paste the collision lines and stop; the owner decides, usually an `output` subfolder
   for that entry in `process.json`, then `scripts/release.ps1 -Version X.Y.Z -PublishOnly` redoes
   the publish alone. A program running from `artifacts/X.Y.Z/` stops the publish the same way,
   before anything is deleted: paste the line naming it, ask the owner to **Exit** it, then
   redo the publish alone (ADR 0078). The release itself starts and stops nothing (ADR 0076).
6. **Check, propose, then ask.** `git rev-parse HEAD` must still be the hash of step 1, and
   `git status --porcelain -- .` must list only `CHANGELOG.md` and, unless the owner had already
   set the version, `Directory.Build.props` (a publish that rewrote a lock file is ADR 0034).
   If HEAD moved or anything else
   changed, stop and report: another session or hand worked on `main` meanwhile. Otherwise show
   `git diff --stat`, then propose, on separate lines, the commit limited to the two files,
   `git commit -m "Release X.Y.Z" -- Directory.Build.props CHANGELOG.md`, and the tag command
   `git tag -a <tag> -m "Release X.Y.Z"` with the tag name the script printed. The owner runs
   them by hand; the commit hook refuses a commit of yours on `main`, and `git tag` asks the
   owner first. Wait. Never push; if the owner wants the tag pushed, say the command and let
   them decide.
7. **After.** Add nothing else to the CHANGELOG; the fresh `[Unreleased]` is already there.
   Note the artifact path and size in the chat, and the zip and its `.sha256` when there is
   one: uploading them anywhere is the owner's, by hand. Then hand the owner the switch to the
   new version, for them to do: **Exit** the copy in the tray, then start
   `artifacts/X.Y.Z/MindfulnessReminder.exe`, which moves the Windows startup entry to it
   (R-006). The README builds with `scripts/release.ps1 -Build`, not with a release, and names no
   version, so a release changes nothing in it (ADR 0078).
