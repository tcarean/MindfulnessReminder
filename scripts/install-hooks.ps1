# SPDX-License-Identifier: GPL-3.0-or-later
# install-hooks.ps1 - tells git to run this project's git hooks at every commit.
#
# WHAT IT DOES
#   git can run small scripts, called hooks, at fixed moments. There are three:
#   scripts/hooks/pre-commit (the gate, at every commit), scripts/hooks/pre-merge-commit (the
#   same gate, at a merge git records without conflicts) and scripts/hooks/commit-msg (the
#   one-line message rule). git looks for hooks in the folder named by its setting
#   core.hooksPath; this script sets it to scripts/hooks. The path is relative, so each worktree
#   runs the hooks of its own checkout. The project must be the root of its repository: below
#   the root the script changes nothing and fails.
#   It changes only that git setting; it runs no check.
#
# WHO CALLS IT
#   Nobody automatically. You run it by hand, once after git init and once in every new clone
#   (git settings are not cloned):
#       powershell -NoProfile -ExecutionPolicy Bypass -File scripts/install-hooks.ps1
#
# RESULT
#   exit 0  hooks installed; it prints what it set
#   exit 1  not inside a git repository, not at its root, or git config failed
$ErrorActionPreference = 'Stop'
# The project folder is the parent of scripts/.
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    # The root of the git repository this folder belongs to.
    $gitRoot = (git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or -not $gitRoot) { throw 'Not inside a git repository. Run git init first.' }
    $gitRoot = [System.IO.Path]::GetFullPath($gitRoot).TrimEnd('\', '/')
    $here = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')
    if ($gitRoot -ne $here) {
        throw ("The project must be the root of its repository: it is {0}, and the repository's root is {1}. Nothing was changed." -f $here, $gitRoot)
    }

    git config core.hooksPath scripts/hooks
    if ($LASTEXITCODE -ne 0) { throw 'git config failed.' }
    Write-Host 'core.hooksPath = scripts/hooks'
}
finally {
    Pop-Location
}
