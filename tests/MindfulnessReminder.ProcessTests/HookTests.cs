// SPDX-License-Identifier: GPL-3.0-or-later
//
// Copyright (C) 2026 Tudor Carean
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Text.Json.Nodes;
using Xunit;

namespace MindfulnessReminder.ProcessTests;

/// <summary>
/// The Claude Code hooks judge the project the work is in, not the folder the session started in
/// (ADR 0020). CLAUDE_PROJECT_DIR stays at that folder when the session works in a worktree; the
/// commit gate follows the commit's own folder, the Stop hook the folder of each edited file.
/// These tests build a throwaway repository holding two projects, A and B, each with a process.json
/// and a stub verify.ps1 that records that it ran and exits with a chosen
/// code. The hooks are the real ones, copied into B, the project the session "started" in. The
/// repository starts on a claude/ branch, the only kind the assistant may commit on (ADR 0021).
/// Windows only: the hooks are Windows PowerShell scripts that call cmd, and the settings that
/// run them name powershell; on the Linux CI these tests are skipped.
/// </summary>
[Trait("Category", "Process")]
public sealed class HookTests : IDisposable
{
    private readonly string _repo = Path.Combine(Path.GetTempPath(), "mindfulness-process-hooks", Guid.NewGuid().ToString("N"));
    private readonly string _a;
    private readonly string _b;
    private readonly string _temp;
    private readonly string _session = Guid.NewGuid().ToString("N");

    public HookTests()
    {
        _a = Path.Combine(_repo, "A");
        _b = Path.Combine(_repo, "B");
        _temp = Path.Combine(_repo, "tmp");
        Directory.CreateDirectory(_temp);
        Git("init", "-q");
        Git("symbolic-ref", "HEAD", "refs/heads/claude/Test-the-hooks");
        MakeProject(_a);
        MakeProject(_b);

        string scripts = Path.Combine(Repo.Root, "scripts");
        File.Copy(Path.Combine(scripts, "process-config.ps1"), Path.Combine(_b, "scripts", "process-config.ps1"));
        foreach (string hook in new[] { "claude-commit-gate.ps1", "claude-stop-gate.ps1" })
        {
            File.Copy(Path.Combine(scripts, "hooks", hook), Path.Combine(_b, "scripts", "hooks", hook));
        }
    }

    [WindowsFact]
    public void CommitGate_RunsTheScriptsOfTheProjectInCwd_NotOfClaudeProjectDir()
    {
        (int exitCode, string output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_a));
        Assert.Empty(Ran(_b));
    }

    [WindowsFact]
    public void CommitGate_ReturnsTheVerdictOfThatProject()
    {
        WriteStub(_a, "verify.ps1", 1);
        (int exitCode, string output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.Equal(2, exitCode);
        Assert.Contains("verify.ps1 failed in " + _a, output);
        Assert.Empty(Ran(_b));
    }

    /// <summary>The folder named in the command wins over cwd: git -C, or a cd before the commit.</summary>
    [WindowsTheory]
    [InlineData("git -C \"{B}\" commit -m \"Fix a thing\"")]
    [InlineData("git -C ../B commit -m \"Fix a thing\"")]
    [InlineData("cd \"{B}\" && git commit -m \"Fix a thing\"")]
    [InlineData("cd {bashB} && git status && git commit -m \"Fix a thing\"")]
    [InlineData("cd ../B; git commit -m \"Fix a thing\"")]
    [InlineData("Set-Location -Path '{B}'; git commit -m \"Fix a thing\"")]
    [InlineData("Push-Location ../B; Push-Location ../A; Pop-Location; git commit -m \"Fix a thing\"")]
    public void CommitGate_FollowsTheFolderTheCommandMovesTo(string command)
    {
        string bashB = "/" + char.ToLowerInvariant(_b[0]) + _b[2..].Replace('\\', '/');
        command = command.Replace("{B}", _b).Replace("{bashB}", bashB);

        (int exitCode, string output) = RunCommitGate(cwd: _a, command);
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_b));
        Assert.Empty(Ran(_a));
    }

    [WindowsTheory]
    [InlineData("git commit --no-verify -m \"Fix a thing\"", "--no-verify")]
    [InlineData("git commit --no-veri -m \"Fix a thing\"", "--no-verify")]
    [InlineData("git -C ../B commit -n -m \"Fix a thing\"", "--no-verify")]
    [InlineData("git commit -anm \"Fix a thing\"", "--no-verify")]
    [InlineData("git add . && git commit -m \"First\" && git commit --amend -n --no-edit", "--no-verify")]
    [InlineData("git -c core.hooksPath=nowhere commit -m \"Fix a thing\"", "core.hooksPath")]
    public void CommitGate_BlocksWhatSkipsTheGitHooks_BeforeAnythingRuns(string command, string reason)
    {
        (int exitCode, string output) = RunCommitGate(cwd: _a, command);
        Assert.Equal(2, exitCode);
        Assert.Contains(reason, output);
        Assert.Empty(Ran(_a));
        Assert.Empty(Ran(_b));
    }

    /// <summary>A commit of the repository's own files, outside every project, has no gate to run.</summary>
    [WindowsFact]
    public void CommitGate_LetsACommitOutsideEveryProjectThrough()
    {
        File.WriteAllText(Path.Combine(_repo, ".gitignore"), "tmp/\n");
        Git("add", ".gitignore");
        (int exitCode, string output) = RunCommitGate(cwd: _repo, "git commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Empty(Ran(_a));
        Assert.Empty(Ran(_b));
    }

    /// <summary>An -n inside the message, or an -m whose value starts with n, is not --no-verify.</summary>
    [WindowsTheory]
    [InlineData("git commit -m \"Say -n in the message\"")]
    [InlineData("git commit -mnote")]
    public void CommitGate_DoesNotTakeAMessageForAnOption(string command)
    {
        (int exitCode, string output) = RunCommitGate(cwd: _a, command);
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_a));
    }

    /// <summary>Text in a heredoc is written, not run: its "cd" moves nothing.</summary>
    [WindowsFact]
    public void CommitGate_IgnoresACdInsideAHeredoc()
    {
        (int exitCode, string output) = RunCommitGate(cwd: _a, "cat > note.sh <<'EOF'\ncd ../B\nEOF\ngit commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_a));
        Assert.Empty(Ran(_b));
    }

    /// <summary>Bash's escaped space is part of the path.</summary>
    [WindowsFact]
    public void CommitGate_ReadsAnEscapedSpaceInAPath()
    {
        string spaced = Path.Combine(_repo, "C D");
        MakeProject(spaced);
        (int exitCode, string output) = RunCommitGate(cwd: _a, "cd ../C\\ D && git commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(spaced));
        Assert.Empty(Ran(_a));
    }

    /// <summary>A folder the hook cannot see, like a shell variable, blocks the commit rather than guessing.</summary>
    [WindowsTheory]
    [InlineData("git -C $wt commit -m \"Fix a thing\"")]
    [InlineData("cd \"$env:WT\"; git commit -m \"Fix a thing\"")]
    public void CommitGate_BlocksAFolderItCannotFind(string command)
    {
        (int exitCode, string output) = RunCommitGate(cwd: _a, command);
        Assert.Equal(2, exitCode);
        Assert.Contains("cannot tell which folder", output);
        Assert.Empty(Ran(_a));
        Assert.Empty(Ran(_b));
    }

    /// <summary>The projects come from the files the commit takes, as in pre-commit, not from the folder alone.</summary>
    [WindowsFact]
    public void CommitGate_RunsEveryProjectTheStagedFilesTouch()
    {
        Edit(_a, "src", "X.cs");
        Edit(_b, "src", "Y.cs");
        Git("add", "A/src/X.cs", "B/src/Y.cs");
        (int exitCode, string output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_a));
        Assert.Equal(["verify.ps1"], Ran(_b));
    }

    /// <summary>From the repository root, where no project is, the staged files still name theirs.</summary>
    [WindowsFact]
    public void CommitGate_FromTheRepositoryRoot_RunsTheProjectOfTheStagedFile()
    {
        Edit(_b, "src", "Y.cs");
        Git("add", "B/src/Y.cs");
        WriteStub(_b, "verify.ps1", 1);
        (int exitCode, string output) = RunCommitGate(cwd: _repo, "git commit -m \"Fix a thing\"");
        Assert.Equal(2, exitCode);
        Assert.Contains("verify.ps1 failed in " + _b, output);
        Assert.Empty(Ran(_a));
    }

    /// <summary>git commit -a takes the changed tracked files too, which are not staged yet.</summary>
    [WindowsFact]
    public void CommitGate_WithAll_CountsTheChangedTrackedFiles()
    {
        Edit(_b, "src", "Y.cs");
        Git("add", "B/src/Y.cs");
        GitCommit("Add Y");
        File.AppendAllText(Path.Combine(_b, "src", "Y.cs"), "// changed\n");
        (int exitCode, string output) = RunCommitGate(cwd: _repo, "git commit -am \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(_b));
    }

    /// <summary>A commit on a branch the assistant did not create is the owner's (ADR 0021), before any gate runs.</summary>
    [WindowsTheory]
    [InlineData("main")]
    [InlineData("master")]
    [InlineData("feature/claude-x")]
    public void CommitGate_OnABranchNotNamedClaude_IsBlocked(string branch)
    {
        Git("symbolic-ref", "HEAD", "refs/heads/" + branch);
        (int exitCode, string output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.Equal(2, exitCode);
        Assert.Contains("the owner's", output);
        Assert.Contains(branch, output);
        Assert.Empty(Ran(_a));
    }

    [WindowsFact]
    public void CommitGate_OnADetachedHead_IsBlocked()
    {
        File.WriteAllText(Path.Combine(_repo, ".gitignore"), "tmp/\n");
        Git("add", ".gitignore");
        GitCommit("First");
        Git("checkout", "-q", "--detach");
        (int exitCode, string output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.Equal(2, exitCode);
        Assert.Contains("detached HEAD", output);
        Assert.Empty(Ran(_a));
    }

    /// <summary>The branch is the one of the commit's folder: a worktree on claude/ commits while the main checkout is on main.</summary>
    [WindowsFact]
    public void CommitGate_JudgesTheBranchOfTheCommitsWorktree()
    {
        File.WriteAllText(Path.Combine(_repo, ".gitignore"), "tmp/\nwt/\n");
        Git("add", ".gitignore");
        GitCommit("First");
        Git("worktree", "add", "-q", "-b", "claude/Other-task", "wt");
        Git("switch", "-q", "-c", "main");
        string worktreeA = Path.Combine(_repo, "wt", "A");
        MakeProject(worktreeA);
        (int exitCode, string output) = RunCommitGate(cwd: worktreeA, "git commit -m \"Fix a thing\"");
        Assert.True(exitCode == 0, output);
        Assert.Equal(["verify.ps1"], Ran(worktreeA));

        (exitCode, output) = RunCommitGate(cwd: _a, "git commit -m \"Fix a thing\"");
        Assert.Equal(2, exitCode);
        Assert.Contains("main", output);
    }

    [WindowsFact]
    public void CommitGate_IgnoresWhatIsNotACommit()
    {
        (int exitCode, _) = RunCommitGate(cwd: _a, "echo \"git commit\"");
        Assert.Equal(0, exitCode);
        Assert.Empty(Ran(_a));
    }

    [WindowsFact]
    public void StopGate_JudgesAnEditByItsOwnProject_WhateverTheSessionFolder()
    {
        Edit(_a, "src", "X.cs");
        (int exitCode, string output) = RunStopGate(sessionDir: _b);
        Assert.Equal(2, exitCode);
        Assert.Contains(_a + ":", output);
        Assert.Contains("no .verify-stamp", output);
        Assert.DoesNotContain(_b + ":", output);
    }

    [WindowsFact]
    public void StopGate_FreshStampInThatProject_Passes()
    {
        Edit(_a, "src", "X.cs");
        Green(_a);
        (int exitCode, string output) = RunStopGate(sessionDir: _b);
        Assert.True(exitCode == 0, output);
    }

    /// <summary>A CHANGELOG line is a judgment, not a gate (the kit's ADR 0023): uncommitted code with a clean CHANGELOG passes.</summary>
    [WindowsFact]
    public void StopGate_UncommittedCodeWithCleanChangelog_Passes()
    {
        File.WriteAllText(Path.Combine(_a, "CHANGELOG.md"), "# Changelog\n");
        Git("add", "A/CHANGELOG.md");
        GitCommit("Changelog");
        Edit(_a, "src", "X.cs");
        Green(_a);
        (int exitCode, string output) = RunStopGate(sessionDir: _b);
        Assert.True(exitCode == 0, output);
    }

    /// <summary>The paths process.json gives the process tests are judged like code: an unverified edit there does not end the turn.</summary>
    [WindowsFact]
    public void StopGate_AnEditUnderTheProcessTestPaths_NeedsAGreenVerify()
    {
        File.WriteAllText(Path.Combine(_a, "process.json"), ScriptRunner.ProcessJson().Replace(
            "\"release\": { \"publish\": [] }",
            "\"release\": { \"publish\": [] }, \"processTests\": { \"category\": \"Process\", \"paths\": [\"scripts/\"] }",
            StringComparison.Ordinal));
        Edit(_a, "scripts", "extra.ps1");
        (int exitCode, string output) = RunStopGate(sessionDir: _b);
        Assert.Equal(2, exitCode);
        Assert.Contains("has not run green with tests", output);

        Green(_a);
        (exitCode, output) = RunStopGate(sessionDir: _b);
        Assert.True(exitCode == 0, output);
    }

    /// <summary>The scripts, the skills and the rules are the project's own: an edit there, verified, ends the turn like any other.</summary>
    [WindowsTheory]
    [InlineData("scripts")]
    [InlineData(".claude/skills/wrap-up")]
    [InlineData(".claude/rules")]
    public void StopGate_AnEditOfTheProcessFiles_IsNotRefusedAsAnotherFolders(string folder)
    {
        Edit(_a, folder.Replace('/', Path.DirectorySeparatorChar), "extra.md");
        Green(_a);
        (int exitCode, string output) = RunStopGate(sessionDir: _b);
        Assert.True(exitCode == 0, output);
    }

    [WindowsFact]
    public void StopGate_JudgesEachProjectSeparately()
    {
        Edit(_a, "src", "X.cs");
        Green(_a);
        Edit(_b, "src", "Y.cs");
        (int exitCode, string output) = RunStopGate(sessionDir: _a);
        Assert.Equal(2, exitCode);
        Assert.Contains(_b + ":", output);
        Assert.DoesNotContain(_a + ":", output);
    }

    public void Dispose()
    {
        ScriptRunner.DeleteTree(_repo);
    }

    /// <summary>A project folder: process.json and a stub verify.ps1, green.</summary>
    private static void MakeProject(string project)
    {
        Directory.CreateDirectory(Path.Combine(project, "scripts", "hooks"));
        Directory.CreateDirectory(Path.Combine(project, "src"));
        File.WriteAllText(Path.Combine(project, "process.json"), ScriptRunner.ProcessJson());
        WriteStub(project, "verify.ps1", 0);
    }

    /// <summary>A stand-in for the project's verify.ps1: appends its name to ran.txt, then exits.</summary>
    private static void WriteStub(string project, string name, int exitCode)
    {
        File.WriteAllText(Path.Combine(project, "scripts", name),
            $"Add-Content -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'ran.txt') -Value '{name}'\nexit {exitCode}\n");
    }

    /// <summary>The stubs that ran in a project, in order.</summary>
    private static string[] Ran(string project)
    {
        string path = Path.Combine(project, "ran.txt");
        return File.Exists(path) ? File.ReadAllLines(path).Where(l => l.Length > 0).ToArray() : [];
    }

    /// <summary>Writes a file and adds it to the session's edit list, as claude-track-edits.ps1 does.</summary>
    private void Edit(string project, string folder, string name)
    {
        string path = Path.Combine(project, folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "// edited\n");
        File.AppendAllText(Path.Combine(_temp, $"claude-edits-{_session}.txt"), path + "\n");
    }

    /// <summary>A project after a green verify: a stamp newer than every edit.</summary>
    private static void Green(string project)
    {
        string stamp = Path.Combine(project, ".verify-stamp");
        File.WriteAllText(stamp, "green\n");
        File.SetLastWriteTimeUtc(stamp, DateTime.UtcNow.AddMinutes(1));
    }

    private void Git(params string[] args)
    {
        Assert.Equal(0, ScriptRunner.Run(_repo, gitDir: null, "git", args).ExitCode);
    }

    /// <summary>A commit in the throwaway repository, with no hooks: it only sets up the history a test needs.</summary>
    private void GitCommit(string message)
    {
        Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "core.hooksPath=no-hooks", "commit", "-q", "-m", message);
    }

    /// <summary>Runs the commit gate from B, the session's folder, with the command run in <paramref name="cwd"/>.</summary>
    private (int ExitCode, string Output) RunCommitGate(string cwd, string command)
    {
        string payload = new JsonObject
        {
            ["session_id"] = _session,
            ["cwd"] = cwd,
            ["hook_event_name"] = "PreToolUse",
            ["tool_name"] = "Bash",
            ["tool_input"] = new JsonObject { ["command"] = command },
        }.ToJsonString();
        return RunHook("claude-commit-gate.ps1", payload, sessionDir: _b);
    }

    /// <summary>Runs the Stop hook of a session started, and still standing, in <paramref name="sessionDir"/>.</summary>
    private (int ExitCode, string Output) RunStopGate(string sessionDir)
    {
        string payload = new JsonObject
        {
            ["session_id"] = _session,
            ["cwd"] = sessionDir,
            ["hook_event_name"] = "Stop",
            ["stop_hook_active"] = false,
        }.ToJsonString();
        return RunHook("claude-stop-gate.ps1", payload, sessionDir);
    }

    private (int ExitCode, string Output) RunHook(string hook, string payload, string sessionDir)
    {
        Dictionary<string, string> environment = new()
        {
            ["CLAUDE_PROJECT_DIR"] = sessionDir,
            ["TEMP"] = _temp,
        };
        return ScriptRunner.RunHook(sessionDir, Path.Combine(_b, "scripts", "hooks", hook), payload, environment);
    }
}

/// <summary>A fact that runs on Windows only, where the Claude Code hooks run.</summary>
internal sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "The Claude Code hooks run on Windows only.";
        }
    }
}

/// <summary>A theory that runs on Windows only, where the Claude Code hooks run.</summary>
internal sealed class WindowsTheoryAttribute : TheoryAttribute
{
    public WindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "The Claude Code hooks run on Windows only.";
        }
    }
}
