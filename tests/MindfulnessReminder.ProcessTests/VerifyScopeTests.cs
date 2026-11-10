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

using Xunit;

namespace MindfulnessReminder.ProcessTests;

/// <summary>
/// The process tests take minutes, so scripts/verify.ps1 runs them only when the change touches
/// what they test: Test-ProcessPathsChanged in process-config.ps1 says whether a path of process.json's
/// processTests.paths differs from where the branch left main, committed, staged, modified or
/// untracked. When git cannot tell, it says yes. These tests build a throwaway repository with
/// a project at its root and ask the real function through a one-line probe script.
/// </summary>
[Trait("Category", "Process")]
public sealed class VerifyScopeTests : IDisposable
{
    private readonly string _repo = Path.Combine(Path.GetTempPath(), "mindfulness-process-scope", Guid.NewGuid().ToString("N"));

    public VerifyScopeTests()
    {
        Directory.CreateDirectory(Path.Combine(_repo, "scripts"));
        Directory.CreateDirectory(Path.Combine(_repo, "src"));
        File.Copy(Path.Combine(Repo.Root, "scripts", "process-config.ps1"), Path.Combine(_repo, "scripts", "process-config.ps1"));
        File.WriteAllText(Path.Combine(_repo, "scripts", "probe.ps1"),
            ". (Join-Path $PSScriptRoot 'process-config.ps1')\n"
            + "$root = Split-Path -Parent $PSScriptRoot\n"
            + "Write-Output (Test-ProcessPathsChanged (Read-ProcessConfig $root) $root)\n");
        File.WriteAllText(Path.Combine(_repo, "process.json"), ScriptRunner.ProcessJson().Replace(
            "\"release\": { \"publish\": [] }",
            "\"release\": { \"publish\": [] }, \"processTests\": { \"category\": \"Process\", \"paths\": [\"scripts/\"] }",
            StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(_repo, "src", "X.cs"), "// first\n");
        Git("init", "-q", "-b", "main");
        Git("config", "user.name", "Test");
        Git("config", "user.email", "test@example.com");
        Commit("First");
    }

    [Fact]
    public void OnACleanBranch_NothingChanged()
    {
        Git("switch", "-q", "-c", "claude/Task");
        Assert.Equal("False", Probe());
    }

    [Theory]
    [InlineData("staged")]
    [InlineData("modified")]
    [InlineData("untracked")]
    [InlineData("committed")]
    public void AChangeUnderScripts_OnABranchOffMain_CountsInEveryState(string state)
    {
        Git("switch", "-q", "-c", "claude/Task");
        string script = Path.Combine(_repo, "scripts", "probe.ps1");
        string extra = Path.Combine(_repo, "scripts", "extra.ps1");
        switch (state)
        {
            case "staged":
                File.WriteAllText(extra, "# new\n");
                Git("add", "scripts/extra.ps1");
                break;
            case "modified":
                File.AppendAllText(script, "# changed\n");
                break;
            case "untracked":
                File.WriteAllText(extra, "# new\n");
                break;
            default:
                File.WriteAllText(extra, "# new\n");
                Commit("Add a script");
                break;
        }

        Assert.Equal("True", Probe());
    }

    [Fact]
    public void AChangeOutsideThePaths_DoesNotCount()
    {
        Git("switch", "-q", "-c", "claude/Task");
        File.AppendAllText(Path.Combine(_repo, "src", "X.cs"), "// task\n");
        Commit("Change the code");
        File.AppendAllText(Path.Combine(_repo, "src", "X.cs"), "// more\n");
        Git("add", "src/X.cs");
        Assert.Equal("False", Probe());
    }

    /// <summary>On main the base is HEAD: what main already holds does not count, what is not committed yet does.</summary>
    [Fact]
    public void OnMain_OnlyWhatIsNotCommittedCounts()
    {
        File.WriteAllText(Path.Combine(_repo, "scripts", "extra.ps1"), "# new\n");
        Commit("Add a script on main");
        Assert.Equal("False", Probe());

        File.AppendAllText(Path.Combine(_repo, "scripts", "extra.ps1"), "# changed\n");
        Assert.Equal("True", Probe());
    }

    /// <summary>A clone made with -b holds no local main: the branch is compared with the remote's.</summary>
    [Fact]
    public void WithOnlyTheRemotesMain_TheBranchIsComparedWithIt()
    {
        Git("update-ref", "refs/remotes/origin/main", "main");
        Git("switch", "-q", "-c", "claude/Task");
        Git("branch", "-q", "-D", "main");
        File.AppendAllText(Path.Combine(_repo, "src", "X.cs"), "// task\n");
        Commit("Change the code");
        Assert.Equal("False", Probe());

        File.WriteAllText(Path.Combine(_repo, "scripts", "extra.ps1"), "# new\n");
        Commit("Add a script");
        Assert.Equal("True", Probe());
    }

    /// <summary>With no main to compare with, git cannot tell what the branch changed: the tests run.</summary>
    [Fact]
    public void WithNoMainAtAll_EverythingCounts()
    {
        Git("switch", "-q", "-c", "claude/Task");
        Git("branch", "-q", "-D", "main");
        Assert.Equal("True", Probe());
    }

    /// <summary>The main branch is the one process.json names as release.branch, here master.</summary>
    [Fact]
    public void TheMainBranch_ComesFromProcessJson()
    {
        Git("branch", "-m", "master");
        File.WriteAllText(Path.Combine(_repo, "process.json"), ScriptRunner.ProcessJson().Replace(
            "\"release\": { \"publish\": [] }",
            "\"release\": { \"publish\": [], \"branch\": \"master\" }, \"processTests\": { \"category\": \"Process\", \"paths\": [\"scripts/\"] }",
            StringComparison.Ordinal));
        Commit("Release from master");
        Assert.Equal("False", Probe());

        Git("switch", "-q", "-c", "claude/Task");
        File.WriteAllText(Path.Combine(_repo, "scripts", "extra.ps1"), "# new\n");
        Commit("Add a script");
        Assert.Equal("True", Probe());
    }

    /// <summary>Where git cannot answer, the tests run: a guess must not leave them out.</summary>
    [Fact]
    public void OutsideAGitRepository_EverythingCounts()
    {
        ScriptRunner.DeleteTree(Path.Combine(_repo, ".git"));
        Assert.Equal("True", Probe());
    }

    /// <summary>A project without processTests has none to leave out.</summary>
    [Fact]
    public void WithoutProcessTests_NothingCounts()
    {
        File.WriteAllText(Path.Combine(_repo, "process.json"), ScriptRunner.ProcessJson());
        File.WriteAllText(Path.Combine(_repo, "scripts", "extra.ps1"), "# new\n");
        Assert.Equal("False", Probe());
    }

    public void Dispose()
    {
        ScriptRunner.DeleteTree(_repo);
    }

    private string Probe()
    {
        (int exitCode, string output) = ScriptRunner.RunScript(_repo, Path.Combine(_repo, "scripts", "probe.ps1"));
        Assert.True(exitCode == 0, output);
        return output.Trim();
    }

    private void Commit(string message)
    {
        Git("add", "-A");
        Git("-c", "core.hooksPath=no-hooks", "commit", "-q", "-m", message);
    }

    private void Git(params string[] args)
    {
        (int exitCode, string output) = ScriptRunner.Run(_repo, gitDir: null, "git", args);
        Assert.True(exitCode == 0, output);
    }
}
