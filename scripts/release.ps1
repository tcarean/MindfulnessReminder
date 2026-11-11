# SPDX-License-Identifier: GPL-3.0-or-later
# release.ps1 - the mechanical part of a release: checks, version number, CHANGELOG, build.
#
# WHAT IT DOES
#   1. Checks, and stops without changing anything if one fails (ADR 0029):
#        - the checkout is the main checkout (not a linked worktree) and is on main, or on
#          the branch process.json names as release.branch: a release is cut there, where the
#          owner commits and tags; a task branch, a detached HEAD or a worktree is refused;
#        - this project's working tree is clean;
#        - -Version is MAJOR.MINOR.PATCH without leading zeros (2.3.0, not 2.03.0, which
#          would compare as 2.3.0 but be written as 2.03.0), is not already a released
#          heading of CHANGELOG.md ("## [X.Y.Z] - date"), and is above the highest of them:
#          that is the floor, since a release always closes [Unreleased] under such a
#          heading (with none, no floor);
#        - -Version is not below <Version> in Directory.Build.props. It may equal it: the
#          owner may bump <Version> per feature, and the release then uses it;
#        - CHANGELOG.md has something under [Unreleased];
#        - this project is the root of its repository, and every release.publish entry is a
#          project file inside it: the tag vX.Y.Z names a version of the whole repository;
#        - the git tag for this version does not exist yet;
#        - unless -NoPublish, no program runs from artifacts/<version>/ (see step 5), so a tray
#          copy started there after a -Build of this version stops the release before the gate
#          and before any file changes (ADR 0078).
#   2. Runs scripts/verify.ps1 -All, the process tests included. If it is red, it stops;
#      still nothing changed.
#   3. Writes the new version into <Version> in Directory.Build.props, unless it is already
#      there.
#   4. In CHANGELOG.md, turns "## [Unreleased]" into "## [X.Y.Z] - <today>" and puts a new,
#      empty "## [Unreleased]" above it.
#   5. Publishes what process.json lists under release.publish into artifacts/<version>/, which
#      it empties first, unless -NoPublish:
#        - first, when a program runs from an executable under artifacts/<version>/, it stops
#          and names it, deleting nothing: the application runs from its release folder (ADR
#          0076), and emptying the folder would delete files until a locked one stopped it. A
#          program is seen by its process's path or, where that cannot be read (an elevated
#          copy, a 64-bit one from 32-bit PowerShell), by an executable or DLL under the folder
#          that refuses to open for writing (ADR 0078);
#        - each entry with its own extra `dotnet publish` arguments into a staging folder,
#          artifacts/<version>/.stage/<n>/, then merged into artifacts/<version>/<output>/ by
#          scripts/merge-publish.ps1, which copies nothing when a file already there has other
#          bytes (a collision: two publishes that want different files at one path). A
#          collision stops the release after steps 3 and 4; the owner decides, usually an
#          output subfolder for that entry, and re-runs with -PublishOnly. .stage/ is removed
#          after the last merge;
#        - writes artifacts/<version>/release.json: this project's name (its folder), the
#          version, the date, the commit, and its components, which are this project alone,
#          with its <Version>, its last commit, whether its CHANGELOG has [Unreleased] content
#          and whether its folder was clean (the two version files a full release has just
#          changed do not count). An entry that published a release.json at the root
#          collides with it. Outside git the commits are empty and clean is false;
#        - when process.json sets release.zip, zips artifacts/<version>/ into artifacts/<zip name>
#          ("{version}" replaced) and writes the zip's SHA-256 beside it in <zip name>.sha256.
#   It commits nothing and tags nothing. At the end it prints the commit and the tag to
#   propose: the commit limited to Directory.Build.props and CHANGELOG.md, and the tag vX.Y.Z.
#
#   With -Build it releases nothing. It reads the version from <Version> in
#   Directory.Build.props, stops when a program runs from artifacts/<version>/, runs
#   scripts/verify.ps1 (the gate a commit passes, not -All), and when that is green, does step 5
#   for that version. None of the other checks of step 1 apply and no file of the repository
#   changes, so it runs on any branch, tag, clone or worktree, and again after a pull. It is how
#   the README builds the application (ADR 0078). It needs git, as verify does.
#
# WHO CALLS IT
#   - Claude, in the /release skill, in a session the owner opened in the project folder of
#     the main checkout: first with -WhatIf, then for real; the skill then proposes the commit
#     and the tag, which the owner runs (ADRs 0010, 0021 and 0029)
#   - anyone who builds the application from a clone, as the README says, and the owner for
#     their own copy between releases:
#       powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1 -Build
#   - you, by hand:
#       powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1 -Version 0.2.0
#
# OPTIONS
#   -Version X.Y.Z   the new version; required, except with -Build
#   -Build           build the version in Directory.Build.props: verify, then step 5, with no
#                    check of step 1 and no change to any file of the repository; it excludes
#                    -Version, -PublishOnly and -NoPublish. With -WhatIf it says what it would
#                    do, and stops
#   -WhatIf          run the step 1 checks and show what would change; touch nothing
#   -NoPublish       skip step 5
#   -PublishOnly     run step 5 alone for -Version, with none of the checks and no change to
#                    Directory.Build.props or CHANGELOG.md: the retry after a collision, or a
#                    measurement of what a release would publish, from any branch. With
#                    -WhatIf it says what it would publish, and stops
#
# RESULT
#   exit 0  done, or the -WhatIf preview finished; it prints the files changed and the tag
#   exit 1  a check failed or verify was red (nothing changed: a program running from
#           artifacts/<version>/ is one of the checks), or publish failed, two publishes
#           collided or a program started from that folder since. In a full release the last
#           three come after steps 3 and 4: CHANGELOG.md, and Directory.Build.props unless
#           <Version> was already the release's, are changed at that point, and the message
#           says which. With -Build nothing changes.
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Version,
    [switch]$Build,
    [switch]$NoPublish,
    [switch]$PublishOnly
)

$ErrorActionPreference = 'Stop'
# The project folder is the parent of scripts/.
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'process-config.ps1')
$config = Read-ProcessConfig $root

# <Version> in Directory.Build.props, and the [Unreleased] section of CHANGELOG.md: from its
# heading to the next "## [" heading or the end of the file.
$VersionPattern = '<Version>([^<]+)</Version>'
$UnreleasedPattern = '(?ms)^## \[Unreleased\]\s*$(.*?)(?=^## \[|\z)'
# MAJOR.MINOR.PATCH without leading zeros: the text goes into a heading, a tag and a folder
# name, the number into comparisons.
$VersionFormat = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$'

# git's standard output in $Dir as one string, or $null when git is not on PATH or fails; then
# $script:GitError holds why, in git's own words. Only trailing white space is trimmed: the
# first column of `git status --porcelain` carries meaning (" M" not staged, "M " staged).
# git's error is collected, not raised: under Windows PowerShell 5.1 a native command's
# stderr, even sent to $null, stops a script whose $ErrorActionPreference is Stop (measured
# 2026-10-24), and a missing git is a CommandNotFoundException.
function Invoke-GitText([string]$Dir, [string[]]$Arguments) {
    if (-not (Get-Command git -CommandType Application -ErrorAction SilentlyContinue)) {
        $script:GitError = 'git was not found on PATH'
        return $null
    }
    $ErrorActionPreference = 'Continue'
    $output = @(& git -C $Dir @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        $script:GitError = (@($output | ForEach-Object { "$_" }) -join ' ').Trim()
        return $null
    }
    return (@($output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }) -join "`n").TrimEnd()
}

# A process.json path (forward slashes) as a path under the project on this system: pwsh on Linux
# takes a backslash for part of a file name.
function ConvertTo-LocalPath([string]$Relative) {
    return Join-Path $root ($Relative -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar)
}

# This project's folder as a full path, without a trailing separator.
function Get-ProjectRoot {
    return [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')
}

# The project's name, the one release.json gives it: its folder as git resolves it, so that a
# project reached through a junction keeps its own name; in a linked worktree, the folder of the
# main checkout, which holds git's common directory, not the worktree's (ADR 0078); the folder
# this script sees outside git.
function Get-ProjectName {
    $top = Invoke-GitText $root @('rev-parse', '--show-toplevel')
    if (-not $top) { return Split-Path (Get-ProjectRoot) -Leaf }
    # The test the main-checkout check of a release makes: a linked worktree has a git folder of
    # its own, beside the common one.
    $gitDir = Invoke-GitText $root @('rev-parse', '--absolute-git-dir')
    $common = Invoke-GitText $root @('rev-parse', '--path-format=absolute', '--git-common-dir')
    if ($gitDir -and $common -and [System.IO.Path]::GetFullPath($gitDir).TrimEnd('\', '/') -ne [System.IO.Path]::GetFullPath($common).TrimEnd('\', '/')) {
        return Split-Path (Split-Path ([System.IO.Path]::GetFullPath($common).TrimEnd('\', '/')) -Parent) -Leaf
    }
    return Split-Path $top -Leaf
}

# Throws, with the entry and the folder named, when a release.publish entry is not a project
# file inside this project's folder: a release publishes this project only.
function Assert-PublishEntries {
    $here = Get-ProjectRoot
    foreach ($p in $config.release.publish) {
        $projectFile = [System.IO.Path]::GetFullPath((ConvertTo-LocalPath $p.project))
        if (-not $projectFile.StartsWith($here + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw ("release.publish entry '{0}' is outside the project folder {1}: a release publishes this project only." -f $p.project, $here)
        }
    }
}

# What release.json says about one component: its name (Get-ProjectName), its <Version> ('' when absent),
# its last commit ('' outside git), whether its CHANGELOG.md has content under [Unreleased],
# and whether its folder is clean, with the uncommitted paths when it is not. $Exclude names
# paths, relative to the component, that do not count against clean: the version files a
# full release has just changed itself.
function Get-ComponentInfo([string]$ComponentRoot, [string[]]$Exclude = @()) {
    $version = ''
    $propsFile = Join-Path $ComponentRoot 'Directory.Build.props'
    if (Test-Path -LiteralPath $propsFile) {
        $vm = [regex]::Match([System.IO.File]::ReadAllText($propsFile), $VersionPattern)
        if ($vm.Success) { $version = $vm.Groups[1].Value }
    }
    $pending = $false
    $changelogFile = Join-Path $ComponentRoot 'CHANGELOG.md'
    if (Test-Path -LiteralPath $changelogFile) {
        $um = [regex]::Match([System.IO.File]::ReadAllText($changelogFile), $UnreleasedPattern)
        $pending = $um.Success -and $um.Groups[1].Value.Trim().Length -gt 0
    }
    $commit = Invoke-GitText $ComponentRoot @('log', '-1', '--format=%H', '--', '.')
    # Where-Object first: $null piped on would make one bare ':(exclude)', which excludes all.
    $status = Invoke-GitText $ComponentRoot (@('status', '--porcelain', '--', '.') + @($Exclude | Where-Object { $_ } | ForEach-Object { ":(exclude)$_" }))
    $dirty = if ($null -eq $status) { @("(git status failed: $script:GitError)") } elseif ($status) { @($status -split "`n") } else { @() }
    return [pscustomobject]@{
        Name = Get-ProjectName
        Version = $version
        Commit = if ($commit) { $commit } else { '' }
        Unreleased = $pending
        Clean = $dirty.Count -eq 0
        Dirty = $dirty
    }
}

# A JSON string literal: quotes, backslashes and control characters escaped.
function ConvertTo-JsonText([string]$Text) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $Text.ToCharArray()) {
        switch ($ch) {
            '"' { [void]$sb.Append('\"') }
            '\' { [void]$sb.Append('\\') }
            default {
                if ([int]$ch -lt 32) { [void]$sb.AppendFormat('\u{0:x4}', [int]$ch) } else { [void]$sb.Append($ch) }
            }
        }
    }
    return '"' + $sb.ToString() + '"'
}

# Whether $Path is open in a way that refuses writing, as the executable and the DLLs of a
# running program are, whoever runs it. A file that is gone, or that this account may not
# write (read-only), is not in use.
function Test-FileLocked([string]$Path) {
    try {
        $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $stream.Dispose()
        return $false
    }
    catch [System.IO.FileNotFoundException] { return $false }
    catch [System.IO.DirectoryNotFoundException] { return $false }
    catch [System.IO.IOException] { return $true }
    catch { return $false }
}

# Throws when a program runs from under $Folder, naming the processes whose path it can read and
# the executables and DLLs that refuse writing; $Rerun says what to do once it is closed.
# Emptying the folder would delete files until a locked one stopped it, and the application runs
# from its release folder (ADR 0076, ADR 0078). The locked files find what a path cannot: an
# elevated copy, and a 64-bit one when this is 32-bit Windows PowerShell, whose Path is empty.
function Assert-FolderNotInUse([string]$Folder, [string]$Rerun) {
    if (-not (Test-Path -LiteralPath $Folder)) { return }
    $prefix = [System.IO.Path]::GetFullPath($Folder).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $inUse = @(Get-Process | Where-Object { $_.Path -and $_.Path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase) })
    $locked = @(Get-ChildItem -LiteralPath $Folder -Recurse -File -Force | Where-Object { $_.Extension -in '.exe', '.dll' -and (Test-FileLocked $_.FullName) })
    if ($inUse.Count -gt 0 -or $locked.Count -gt 0) {
        $what = if ($inUse.Count -gt 0) { ($inUse | ForEach-Object { '{0} (process {1})' -f $_.ProcessName, $_.Id }) -join ', ' } else { 'A program' }
        $files = if ($locked.Count -gt 0) { ' ({0} in use)' -f (($locked | ForEach-Object { $_.Name }) -join ', ') } else { '' }
        throw ("{0} is running from {1}{2}; close it (Exit in its tray menu, for the application), then {3}" -f $what, $Folder, $files, $Rerun)
    }
}

# Step 5: stage and merge every publish, write release.json, zip. Throws on a failed publish,
# a collision or a program running from the folder, with what to do next. -AfterVersionEdits:
# a full release, which has just changed Directory.Build.props and CHANGELOG.md itself; they do
# not make this project unclean. $Changed names, for the messages, the version files it changed
# ("CHANGELOG.md is" when <Version> was already the release's). -Build: the build of the version
# in Directory.Build.props, which changes nothing in the repository.
function Publish-Release([switch]$AfterVersionEdits, [switch]$Build, [string]$Changed = 'Directory.Build.props and CHANGELOG.md are') {
    # Checked first: an entry outside the project stops before anything is built.
    Assert-PublishEntries
    $out = Join-Path (ConvertTo-LocalPath 'artifacts') $Version
    $versionFiles = if ($AfterVersionEdits) { "$Changed already changed." } elseif ($Build) { 'No file of the repository was changed (-Build).' } else { 'The version files were not touched (-PublishOnly).' }
    $rerun = if ($Build) { "run -Build again. $versionFiles" } else { "re-run with -PublishOnly -Version $Version. $versionFiles" }
    Assert-FolderNotInUse $out $rerun
    # A previous run's files would be compared with this run's and reported as collisions,
    # and would end up in the zip: the folder is rebuilt from nothing.
    if (Test-Path -LiteralPath $out) { Remove-Item -LiteralPath $out -Recurse -Force }
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    $stage = Join-Path $out '.stage'
    $index = 0
    foreach ($p in $config.release.publish) {
        $index++
        $staged = Join-Path $stage $index
        $target = if ($p.output) { Join-Path $out ($p.output -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar) } else { $out }
        $extra = @($p.args)
        dotnet publish (ConvertTo-LocalPath $p.project) -c Release -o $staged --nologo -v q @extra
        if ($LASTEXITCODE -ne 0) { throw ("dotnet publish ({0}) failed; fix the cause shown above and {1}" -f $p.project, $rerun) }
        & (Get-ScriptShell) -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'merge-publish.ps1') -From $staged -Into $target
        # merge-publish.ps1: exit 1 is a collision, any other failure is an error of its own.
        if ($LASTEXITCODE -eq 1) { throw ("{0} collides with what the entries before it published. Resolve it (usually an output subfolder for that entry in process.json) and {1}" -f $p.project, $rerun) }
        if ($LASTEXITCODE -ne 0) { throw ("merge-publish.ps1 failed for {0} (exit {1}); see its message above, then {2}" -f $p.project, $LASTEXITCODE, $rerun) }
    }
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }

    # release.json is the release's own file: an entry that published one at the root collides
    # with it, as two entries collide, and is never overwritten.
    $manifest = Join-Path $out 'release.json'
    if (Test-Path -LiteralPath $manifest) {
        Write-Host ("collision  release.json  (published by an entry; the release writes its own into {0})" -f $out) -ForegroundColor Red
        throw ("An entry of release.publish published release.json into {0}, where the release writes its own. Give that entry an output subfolder in process.json and {1}" -f $out, $rerun)
    }

    # release.json: what this folder holds, for the people who receive it and for a program
    # that shows the versions. UTF-8 without a byte order mark, LF line ends. Its one component
    # is this project; when the folder held uncommitted files it says so ("clean": false):
    # -PublishOnly checks nothing.
    $exclude = if ($AfterVersionEdits) { @('Directory.Build.props', 'CHANGELOG.md') } else { @() }
    $components = @(Get-ComponentInfo (Get-ProjectRoot) $exclude)
    $head = Invoke-GitText $root @('rev-parse', 'HEAD')
    if (-not $head) { $head = '' }
    $lines = @(
        '{',
        ('  "name": {0},' -f (ConvertTo-JsonText (Get-ProjectName))),
        ('  "version": {0},' -f (ConvertTo-JsonText $Version)),
        ('  "date": {0},' -f (ConvertTo-JsonText (Get-Date).ToString('yyyy-MM-dd'))),
        ('  "commit": {0},' -f (ConvertTo-JsonText $head)),
        '  "components": ['
    )
    for ($i = 0; $i -lt $components.Count; $i++) {
        $c = $components[$i]
        $comma = if ($i -lt $components.Count - 1) { ',' } else { '' }
        $lines += ('    {{ "name": {0}, "version": {1}, "commit": {2}, "unreleased": {3}, "clean": {4} }}{5}' -f
            (ConvertTo-JsonText $c.Name), (ConvertTo-JsonText $c.Version), (ConvertTo-JsonText $c.Commit),
            $c.Unreleased.ToString().ToLowerInvariant(), $c.Clean.ToString().ToLowerInvariant(), $comma)
    }
    $lines += @('  ]', '}')
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($manifest, (($lines -join "`n") + "`n"), $utf8NoBom)
    foreach ($c in $components) {
        $flag = if ($c.Unreleased) { ', unreleased changes' } else { '' }
        if (-not $c.Clean) { $flag += ', NOT CLEAN: ' + @($c.Dirty)[0] }
        Write-Host ("component {0} {1} ({2}{3})" -f $c.Name, $c.Version, $(if ($c.Commit) { $c.Commit.Substring(0, [Math]::Min(7, $c.Commit.Length)) } else { 'no commit' }), $flag)
    }

    # -Force: a hidden file is published like any other, as merge-publish.ps1 copied it.
    $files = @(Get-ChildItem -LiteralPath $out -Recurse -File -Force)
    $size = ($files | Measure-Object Length -Sum).Sum
    Write-Host ("published {0} ({1} files, {2:n1} MB)" -f $out, $files.Count, ($size / 1MB))

    # The zip sits in artifacts/, beside the folder, so it never contains itself. Entry by
    # entry, with forward slashes: under Windows PowerShell 5.1 both Compress-Archive and
    # ZipFile.CreateFromDirectory write backslashes into the entry names (measured
    # 2026-10-24), which other unzip tools read as part of the file name.
    if ($config.release.zip) {
        $zipName = $config.release.zip.Replace('{version}', $Version)
        $zip = Join-Path (ConvertTo-LocalPath 'artifacts') $zipName
        if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
        Add-Type -AssemblyName System.IO.Compression
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $outFull = [System.IO.Path]::GetFullPath($out).TrimEnd('\', '/')
        $stream = [System.IO.File]::Open($zip, [System.IO.FileMode]::CreateNew)
        $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($f in Get-ChildItem -LiteralPath $outFull -Recurse -File -Force) {
                $entry = $f.FullName.Substring($outFull.Length + 1).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
                [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $f.FullName, $entry, [System.IO.Compression.CompressionLevel]::Optimal)
            }
        }
        finally {
            $archive.Dispose()
            $stream.Dispose()
        }
        $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
        [System.IO.File]::WriteAllText($zip + '.sha256', ("{0}  {1}`n" -f $hash, $zipName), $utf8NoBom)
        Write-Host ("zipped    {0} ({1:n1} MB), SHA-256 in {2}.sha256" -f $zip, ((Get-Item -LiteralPath $zip).Length / 1MB), $zipName)
    }
}

Push-Location $root
try {
    # -Build: the gate, then step 5 for the version in Directory.Build.props; no check of step 1
    # and no change to a file of the repository (ADR 0078).
    if ($Build) {
        if ($Version -or $PublishOnly -or $NoPublish) { throw '-Build takes the version from Directory.Build.props: it excludes -Version, -PublishOnly and -NoPublish.' }
        $vm = [regex]::Match([System.IO.File]::ReadAllText((Join-Path $root 'Directory.Build.props')), $VersionPattern)
        if (-not $vm.Success) { throw 'No <Version> element in Directory.Build.props.' }
        $Version = $vm.Groups[1].Value
        if ($Version -notmatch $VersionFormat) { throw "<Version> in Directory.Build.props must be MAJOR.MINOR.PATCH without leading zeros, got '$Version'." }
        Write-Host ("== build {0}, the <Version> in Directory.Build.props: verify, then publish; no file of the repository changes" -f $Version) -ForegroundColor Cyan
        # Before the gate: the owner's tray copy may run from this very folder (ADR 0078).
        Assert-FolderNotInUse (Join-Path (ConvertTo-LocalPath 'artifacts') $Version) 'run -Build again; nothing was changed.'
        # -WhatIf previews here, as for -PublishOnly: nothing of verify or of step 5 starts.
        if ($WhatIfPreference) {
            $targets = ($config.release.publish | ForEach-Object { $_.project }) -join ', '
            $zipPlan = if ($config.release.zip) { ', zip ' + $config.release.zip.Replace('{version}', $Version) } else { '' }
            Assert-PublishEntries
            Write-Host ("WhatIf: would run verify, then publish {0} into artifacts/{1}/ after emptying it, write release.json{2}." -f $(if ($targets) { $targets } else { 'nothing' }), $Version, $zipPlan)
            exit 0
        }
        # The gate a commit passes, not -All: the tests of the scripts run only when scripts/
        # differs from main, as at a commit.
        & (Get-ScriptShell) -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify.ps1')
        if ($LASTEXITCODE -ne 0) { throw 'verify.ps1 failed; nothing was published, and no file of the repository changed.' }
        Publish-Release -Build
        Write-Host ("== built {0} into artifacts/{0}/. No file of the repository changed; nothing is committed or tagged." -f $Version) -ForegroundColor Green
        exit 0
    }
    if (-not $Version) { throw 'Give -Version X.Y.Z to cut a release, or -Build to build the version in Directory.Build.props without one.' }

    # No leading zeros: the text goes into the heading and the tag, the number into comparisons.
    if ($Version -notmatch $VersionFormat) { throw "Version must be MAJOR.MINOR.PATCH without leading zeros, got '$Version'." }
    # [version] compares numerically: 0.10.0 is greater than 0.9.0.
    $new = [version]$Version
    if ($PublishOnly -and $NoPublish) { throw '-PublishOnly and -NoPublish exclude each other.' }

    # -PublishOnly: step 5 alone, no checks, no version files (ADR 0071).
    if ($PublishOnly) {
        Write-Host ("== publish only {0}: no checks, Directory.Build.props and CHANGELOG.md untouched" -f $Version) -ForegroundColor Cyan
        # -WhatIf previews here: step 5 runs dotnet publish and a child script, which a
        # -WhatIf would not reach, so nothing of it starts.
        if ($WhatIfPreference) {
            $targets = ($config.release.publish | ForEach-Object { $_.project }) -join ', '
            $zipPlan = if ($config.release.zip) { ', zip ' + $config.release.zip.Replace('{version}', $Version) } else { '' }
            Assert-PublishEntries
            Write-Host ("WhatIf: would publish {0} into artifacts/{1}/ after emptying it, write release.json{2}." -f $(if ($targets) { $targets } else { 'nothing' }), $Version, $zipPlan)
            exit 0
        }
        Publish-Release
        Write-Host '== published. Nothing is committed or tagged by this script.' -ForegroundColor Green
        exit 0
    }

    # --- preconditions -----------------------------------------------------------------
    # A release is cut on main (or process.json's release.branch), in the main checkout
    # (ADR 0029). An empty name is a detached HEAD. The main checkout is the worktree whose git
    # folder is the common one, the test the git pre-commit hook makes (ADR 0026);
    # absolute forms, since git may report either side relative to a subfolder.
    $releaseBranch = $config.release.branch
    $gitDir = [System.IO.Path]::GetFullPath((git rev-parse --absolute-git-dir)).TrimEnd('\', '/')
    $commonDir = [System.IO.Path]::GetFullPath((git rev-parse --path-format=absolute --git-common-dir)).TrimEnd('\', '/')
    if ($gitDir -ne $commonDir) {
        throw "A release is cut on $releaseBranch, in the main checkout; this is a linked worktree ($root)."
    }
    # The tag vX.Y.Z names a version of the whole repository, so the project is its root. git
    # says where this folder is inside the repository, nothing at its root: comparing two paths
    # instead would take the same folder, reached through a junction, for another.
    $prefix = (git rev-parse --show-prefix)
    if ($LASTEXITCODE -ne 0) { throw 'git rev-parse --show-prefix failed; see its message above.' }
    if ($prefix) {
        throw ("A release is cut from a project that is the root of its repository; this project is the folder {0} of the repository at {1}." -f $prefix.TrimEnd('/'), (git rev-parse --show-toplevel))
    }
    $branch = (git branch --show-current 2>$null)
    if ($branch -ne $releaseBranch) {
        $where = if ($branch) { "branch '$branch'" } else { 'a detached HEAD' }
        throw "A release is cut on $releaseBranch, in the main checkout; this checkout is on $where."
    }

    # 'git status --porcelain -- .' lists the uncommitted changes in this folder, one per line.
    $dirty = @(git status --porcelain -- . 2>$null | Where-Object { $_ })
    if ($dirty.Count -gt 0) {
        throw ("Working tree is not clean; commit or stash first:`n" + ($dirty -join "`n"))
    }

    # The current version is the text between <Version> and </Version>.
    $propsPath = Join-Path $root 'Directory.Build.props'
    $props = [System.IO.File]::ReadAllText($propsPath)
    $m = [regex]::Match($props, $VersionPattern)
    if (-not $m.Success) { throw 'No <Version> element in Directory.Build.props.' }
    $current = [version]$m.Groups[1].Value
    if ($new -lt $current) { throw "Version $new is below <Version> $current in Directory.Build.props, which the owner already set; release $current or above." }

    # The released versions: every "## [X.Y.Z] - date" heading. The highest is the floor.
    $changelogPath = Join-Path $root 'CHANGELOG.md'
    $changelog = [System.IO.File]::ReadAllText($changelogPath)
    $released = @([regex]::Matches($changelog, '(?m)^## \[(\d+\.\d+\.\d+)\]') | ForEach-Object { [version]$_.Groups[1].Value })
    if ($released -contains $new) { throw "Version $new is already released: CHANGELOG.md has a [$new] heading." }
    $floor = $released | Sort-Object -Descending | Select-Object -First 1
    if ($floor -and $new -le $floor) { throw "Version $new is not above $floor, the newest released heading in CHANGELOG.md." }

    # The [Unreleased] section ($UnreleasedPattern).
    $unreleased = [regex]::Match($changelog, $UnreleasedPattern)
    if (-not $unreleased.Success) { throw 'CHANGELOG.md has no [Unreleased] section.' }
    if ($unreleased.Groups[1].Value.Trim().Length -eq 0) { throw '[Unreleased] is empty; nothing to release.' }

    # A release publishes this project only.
    Assert-PublishEntries

    # A program running from the release's folder, as the tray copy does after a -Build of the
    # version this release keeps, stops it here, before the gate and the edits (ADR 0078); step 5
    # looks again.
    if (-not $NoPublish) { Assert-FolderNotInUse (Join-Path (ConvertTo-LocalPath 'artifacts') $Version) 'run the release again; nothing was changed.' }

    # --- tag name ---------------------------------------------------------------------
    $tag = "v$Version"
    if (git tag --list $tag) { throw "Tag $tag already exists." }

    # -WhatIf stops here: the checks passed, nothing was changed.
    $floorText = if ($floor) { $floor } else { 'none' }
    Write-Host ("== release {0} (<Version> {1}, newest released {2}), tag {3}" -f $new, $current, $floorText, $tag) -ForegroundColor Cyan
    if ($WhatIfPreference) {
        $targets = ($config.release.publish | ForEach-Object { $_.project }) -join ', '
        $propsPlan = if ($new -eq $current) { "leave <Version> at $current" } else { "set <Version> to $new" }
        $zipPlan = if ($config.release.zip) { ', zip ' + $config.release.zip.Replace('{version}', $Version) } else { '' }
        Write-Host ("WhatIf: would run verify, {0}, close [Unreleased] in CHANGELOG, publish {1}, write release.json{2}." -f $propsPlan, $targets, $zipPlan)
        Write-Host ("Unreleased content ({0} lines):" -f ($unreleased.Groups[1].Value.Trim() -split "`n").Count)
        Write-Host $unreleased.Groups[1].Value.Trim()
        exit 0
    }

    # --- gate ---------------------------------------------------------------------------
    & (Get-ScriptShell) -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify.ps1') -All
    if ($LASTEXITCODE -ne 0) { throw 'verify.ps1 failed; release aborted, nothing changed.' }

    # --- files --------------------------------------------------------------------------
    # From here on, files change. Only the number inside <Version>...</Version> is replaced;
    # the rest of Directory.Build.props stays as it is. When the owner already set it, the file
    # is left alone.
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $propsChange = if ($new -eq $current) { 'unchanged, already {0}' -f $Version } else { '{0} -> {1}' -f $current, $Version }
    if ($new -ne $current) {
        $props = $props.Substring(0, $m.Groups[1].Index) + $Version + $props.Substring($m.Groups[1].Index + $m.Groups[1].Length)
        [System.IO.File]::WriteAllText($propsPath, $props, $utf8)
    }

    # CHANGELOG: a new, empty [Unreleased] on top; the old one becomes "[X.Y.Z] - date".
    # The file keeps its own line endings (CRLF or LF).
    $date = (Get-Date).ToString('yyyy-MM-dd')
    $nl = if ($changelog.Contains("`r`n")) { "`r`n" } else { "`n" }
    $header = "## [Unreleased]$nl$nl## [$Version] - $date"
    $changelog = $changelog.Substring(0, $unreleased.Index) + $header + $changelog.Substring($unreleased.Index + '## [Unreleased]'.Length)
    [System.IO.File]::WriteAllText($changelogPath, $changelog, $utf8)
    Write-Host ("changed  Directory.Build.props ({0}), CHANGELOG.md ([{1}] - {2})" -f $propsChange, $Version, $date)

    # --- artifacts ----------------------------------------------------------------------
    # artifacts/ is git-ignored.
    $changed = if ($new -eq $current) { 'CHANGELOG.md is' } else { 'Directory.Build.props and CHANGELOG.md are' }
    if (-not $NoPublish) { Publish-Release -AfterVersionEdits -Changed $changed }

    Write-Host ("== ready. Propose: git commit -m ""Release {0}"" -- Directory.Build.props CHANGELOG.md, then git tag -a {1} -m ""Release {0}"". Nothing is committed or tagged by this script." -f $Version, $tag) -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}
