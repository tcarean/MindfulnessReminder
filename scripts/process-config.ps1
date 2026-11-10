# SPDX-License-Identifier: GPL-3.0-or-later
# process-config.ps1 - reads process.json, the one file that tells the scripts about the project.
#
# WHAT IT DOES
#   What the scripts need to know about the project (the solution name, where the code and the
#   tests are, what a release publishes) lives in process.json at the project root, never in a
#   script (the rule "Process files"). This file is
#   dot-sourced by the scripts (". (Join-Path $PSScriptRoot 'process-config.ps1')") and gives them
#   Read-ProcessConfig, which reads <project folder>/process.json and returns its values with the paths
#   already normalised (forward slashes, one trailing slash on folders), Find-Project,
#   which finds the project a file or folder belongs to (the Claude Code hooks use it,
#   ADR 0020), Get-ScriptShell, the PowerShell to start another script with, and
#   Test-ProcessPathsChanged, whether a change touches what the process tests test.
#   A missing process.json, or a missing required key, is an error with the key named: a project
#   that has scripts/ must say what it is; nothing is guessed from the folder layout.
#
#   Keys:
#     solution            the .sln file verify.ps1 restores, formats, builds and tests
#     testFilter          optional; a `dotnet test --filter` expression the gate applies
#                         ("" runs everything)
#     codeFolders         folders whose change needs a green verify before a Claude turn
#                         ends (claude-stop-gate.ps1)
#     codeFiles           optional; single files that count as code, the same way
#     testFolders         folders whose change needs a green verify before a turn ends
#     testData.folder     the folder holding manifest.json, fixtures/ and large/
#     testData.largeEnv   the environment variable that may point at the large files instead
#     mutate.testProject  the test project folder Stryker runs from (its stryker-config.json)
#     release.publish     what release.ps1 publishes: a list of { project, output, args };
#                         output is a subfolder of artifacts/<version>/ ("" is that folder
#                         itself), args are extra `dotnet publish` arguments. project is a
#                         project file inside this project's folder; release.ps1 refuses one
#                         outside it (ADR 0071, ADR 0075)
#     release.zip         optional; the file name of the zip of artifacts/<version>/, written
#                         to artifacts/ with a .sha256 beside it; `{version}` is replaced;
#                         absent or empty: no zip
#     release.branch      optional; the branch a release is cut on ("main" when absent;
#                         release.ps1 refuses any other, ADR 0029)
#     audit.alsoRead      optional; files or folders, besides docs/ and the test data README
#                         and manifest, that the docs-auditor agent may read: expected outputs,
#                         notices. Never code.
#     processTests        optional; the tests of the process itself (the scripts), which take
#                         minutes: category is their Category trait, paths the files and
#                         folders they test. verify.ps1 leaves out Category=<category> unless a
#                         path differs from main (Test-ProcessPathsChanged), CI is "true" or it
#                         is run with -All; the Stop hook judges an edit under paths as code.
#                         Absent: no such tests, nothing left out
#
# WHO CALLS IT
#   Every script in scripts/ and scripts/hooks/ that needs a project value. Nobody runs it.
#
# RESULT
#   Read-ProcessConfig returns the object; it throws when process.json or a required key is missing.
#   Find-Project returns a full path, or $null.

# Reads and checks process.json in $ProjectDir. Throws with the file or key named.
function Read-ProcessConfig([string]$ProjectDir) {
    $path = Join-Path $ProjectDir 'process.json'
    if (-not (Test-Path -LiteralPath $path)) {
        throw "process.json not found in $ProjectDir. The scripts need it: it names the solution, the code and test folders and the release."
    }
    $cfg = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json

    # A required key that is absent is a mistake, not a default: say which one.
    foreach ($key in @('solution', 'codeFolders', 'testFolders', 'testData', 'mutate', 'release')) {
        if ($null -eq $cfg.PSObject.Properties[$key]) { throw "process.json: missing key '$key'." }
    }
    if (-not $cfg.testData.folder) { throw "process.json: missing key 'testData.folder'." }
    if (-not $cfg.testData.largeEnv) { throw "process.json: missing key 'testData.largeEnv'." }
    if (-not $cfg.mutate.testProject) { throw "process.json: missing key 'mutate.testProject'." }
    if ($null -eq $cfg.release.PSObject.Properties['publish']) { throw "process.json: missing key 'release.publish'." }

    # Optional keys get their empty value, so a script never tests for their presence.
    if ($null -eq $cfg.PSObject.Properties['testFilter']) { $cfg | Add-Member -NotePropertyName testFilter -NotePropertyValue '' }
    if ($null -eq $cfg.PSObject.Properties['codeFiles']) { $cfg | Add-Member -NotePropertyName codeFiles -NotePropertyValue @() }
    if ($null -eq $cfg.PSObject.Properties['audit']) { $cfg | Add-Member -NotePropertyName audit -NotePropertyValue ([pscustomobject]@{ alsoRead = @() }) }
    if ($null -eq $cfg.audit.PSObject.Properties['alsoRead']) { $cfg.audit | Add-Member -NotePropertyName alsoRead -NotePropertyValue @() }
    $cfg.audit.alsoRead = @($cfg.audit.alsoRead | ForEach-Object { $_.Replace('\', '/') })
    if ($null -eq $cfg.PSObject.Properties['processTests']) { $cfg | Add-Member -NotePropertyName processTests -NotePropertyValue ([pscustomobject]@{ category = ''; paths = @() }) }
    if ($null -eq $cfg.processTests.PSObject.Properties['category']) { $cfg.processTests | Add-Member -NotePropertyName category -NotePropertyValue '' }
    if ($null -eq $cfg.processTests.PSObject.Properties['paths']) { $cfg.processTests | Add-Member -NotePropertyName paths -NotePropertyValue @() }
    $cfg.processTests.paths = @($cfg.processTests.paths | Where-Object { $_ } | ForEach-Object { $_.Replace('\', '/') })

    # Folders: forward slashes and exactly one trailing slash, whatever the file says. @( )
    # keeps a one-element list a list; ConvertFrom-Json would hand a single string back.
    $cfg.codeFolders = @($cfg.codeFolders | ForEach-Object { ConvertTo-ConfigFolder $_ })
    $cfg.testFolders = @($cfg.testFolders | ForEach-Object { ConvertTo-ConfigFolder $_ })
    $cfg.codeFiles = @($cfg.codeFiles | ForEach-Object { $_.Replace('\', '/') })
    $cfg.testData.folder = ConvertTo-ConfigFolder $cfg.testData.folder
    $cfg.mutate.testProject = ConvertTo-ConfigFolder $cfg.mutate.testProject
    $cfg.release.publish = @($cfg.release.publish)
    if (-not $cfg.release.PSObject.Properties['branch'] -or -not $cfg.release.branch) {
        if ($cfg.release.PSObject.Properties['branch']) { $cfg.release.PSObject.Properties.Remove('branch') }
        $cfg.release | Add-Member -NotePropertyName branch -NotePropertyValue 'main'
    }
    if ($null -eq $cfg.release.PSObject.Properties['zip'] -or $null -eq $cfg.release.zip) {
        if ($cfg.release.PSObject.Properties['zip']) { $cfg.release.PSObject.Properties.Remove('zip') }
        $cfg.release | Add-Member -NotePropertyName zip -NotePropertyValue ''
    }
    foreach ($p in $cfg.release.publish) {
        if (-not $p.project) { throw "process.json: every release.publish entry needs 'project'." }
        if ($null -eq $p.PSObject.Properties['output']) { $p | Add-Member -NotePropertyName output -NotePropertyValue '' }
        if ($null -eq $p.PSObject.Properties['args']) { $p | Add-Member -NotePropertyName args -NotePropertyValue @() }
        $p.args = @($p.args)
    }
    return $cfg
}

# "src\" or "src" or "src/" -> "src/".
function ConvertTo-ConfigFolder([string]$Folder) {
    return $Folder.Replace('\', '/').TrimEnd('/') + '/'
}

# The project $Path (a file or a folder, existing or not) belongs to: the nearest folder at or
# above it that holds $Marker (process.json unless told otherwise; the commit gate asks for
# scripts\verify.ps1, the git pre-commit hook's rule), as a full path without a trailing
# separator; $null when there is none. The walk stops at the top of the repository or worktree
# the path is in (a folder holding .git), as pre-commit does, so a path in a worktree made under
# a project's .claude/worktrees/ is never taken for a path of that project.
function Find-Project([string]$Path, [string]$Marker = 'process.json') {
    if (-not $Path) { return $null }
    $dir = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { $dir = [System.IO.Path]::GetDirectoryName($dir) }
    while ($dir) {
        if (Test-Path -LiteralPath (Join-Path $dir $Marker) -PathType Leaf) { return $dir.TrimEnd('\', '/') }
        if (Test-Path -LiteralPath (Join-Path $dir '.git')) { return $null }
        $dir = [System.IO.Path]::GetDirectoryName($dir)
    }
    return $null
}

# Whether the process tests can be affected by the change in $ProjectDir: true when a path of
# processTests.paths differs from where the branch left the main branch (release.branch,
# "main" by default; the remote's, origin/<branch>, when there is no local one), committed on
# the branch, staged, modified or untracked. On the main branch itself the base is HEAD, so only
# what is not committed yet counts (during a merge, the index holds what the merge brings).
# True too when git cannot answer (no git, no repository, no commit, no main branch to compare
# with): the tests run rather than a guess leaving them out. False when the project names no
# process tests.
function Test-ProcessPathsChanged($Config, [string]$ProjectDir) {
    $paths = @($Config.processTests.paths)
    if (-not $Config.processTests.category -or $paths.Count -eq 0) { return $false }
    if (-not (Get-Command git -CommandType Application -ErrorAction SilentlyContinue)) { return $true }
    # A native command's stderr stops a script whose $ErrorActionPreference is Stop under
    # Windows PowerShell 5.1, even sent to $null; here a failure is an answer, not an error.
    $ErrorActionPreference = 'Continue'
    $head = (git -C $ProjectDir rev-parse --verify --quiet HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $head) { return $true }
    $mainBranch = if ($Config.release.branch) { $Config.release.branch } else { 'main' }
    $base = $head
    if ((git -C $ProjectDir branch --show-current 2>$null) -ne $mainBranch) {
        # A local main left behind the remote's would count what main gained since as the
        # branch's change: of the two merge bases, the newer one is where the branch left main.
        $base = $null
        foreach ($ref in @($mainBranch, "origin/$mainBranch")) {
            $mergeBase = (git -C $ProjectDir merge-base HEAD $ref 2>$null)
            if ($LASTEXITCODE -ne 0 -or -not $mergeBase) { continue }
            if (-not $base) { $base = $mergeBase; continue }
            git -C $ProjectDir merge-base --is-ancestor $base $mergeBase 2>$null
            if ($LASTEXITCODE -eq 0) { $base = $mergeBase }
        }
        if (-not $base) { return $true }
    }
    $changed = @(git -C $ProjectDir diff --name-only $base -- @paths 2>$null)
    if ($LASTEXITCODE -ne 0) { return $true }
    $changed += @(git -C $ProjectDir diff --cached --name-only $base -- @paths 2>$null)
    $changed += @(git -C $ProjectDir ls-files --others --exclude-standard -- @paths 2>$null)
    return @($changed | Where-Object { $_ }).Count -gt 0
}

# The PowerShell running this script (powershell.exe 5.1 or pwsh), to start another script
# with: a gate started under pwsh, as CI starts it, stays under pwsh, and "powershell" does not
# exist where only pwsh is installed.
function Get-ScriptShell {
    return (Get-Process -Id $PID).Path
}

# Full path of a project folder from a process.json value ("src/" -> <project>\src\).
function Get-ConfigFolderPath([string]$ProjectDir, [string]$Folder) {
    return (Join-Path $ProjectDir ($Folder.Replace('/', '\'))).TrimEnd('\') + '\'
}
