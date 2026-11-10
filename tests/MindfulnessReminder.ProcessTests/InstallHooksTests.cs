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
/// scripts/install-hooks.ps1 points git at scripts/hooks/, once per clone. The project is the root
/// of its repository: a project below the root is refused and nothing is written. These tests
/// build a throwaway repository and copy the real script and the hooks into it.
/// </summary>
[Trait("Category", "Process")]
public sealed class InstallHooksTests : IDisposable
{
    private readonly string _repo = Path.Combine(Path.GetTempPath(), "mindfulness-process-install", Guid.NewGuid().ToString("N"));

    public InstallHooksTests()
    {
        Directory.CreateDirectory(_repo);
        (int exitCode, string output) = ScriptRunner.Run(_repo, gitDir: null, "git", "init", "-q", "-b", "main");
        Assert.True(exitCode == 0, output);
    }

    [Fact]
    public void AtTheRepositoryRoot_PointsGitAtScriptsHooks()
    {
        CopyScripts(_repo);

        (int exitCode, string output) = Install(_repo);
        Assert.True(exitCode == 0, output);
        Assert.Contains("core.hooksPath = scripts/hooks", output);
        Assert.Equal("scripts/hooks", GitOutput("config", "--get", "core.hooksPath"));
    }

    [Fact]
    public void BelowTheRepositoryRoot_IsRefused_AndWritesNothing()
    {
        string project = Path.Combine(_repo, "Proj");
        CopyScripts(project);

        (int exitCode, string output) = Install(project);
        Assert.Equal(1, exitCode);
        Assert.Contains("the root of its repository", output);
        Assert.False(Directory.Exists(Path.Combine(_repo, ".githooks")), ".githooks was written");
        Assert.Equal(string.Empty, GitOutput("config", "--get", "core.hooksPath"));
    }

    /// <summary>Outside a repository git writes an error; the script answers with its own advice, not with git's message.</summary>
    [Fact]
    public void OutsideARepository_SaysToRunGitInitFirst()
    {
        string plain = _repo + "-plain";
        CopyScripts(plain);
        try
        {
            (int exitCode, string output) = Install(plain);
            Assert.Equal(1, exitCode);
            Assert.Contains("Not inside a git repository. Run git init first.", output);
        }
        finally
        {
            ScriptRunner.DeleteTree(plain);
        }
    }

    /// <summary>git answers with the real path; the script, started through a junction, must not take that for another folder.</summary>
    [WindowsFact]
    public void ThroughAJunction_TheRootIsStillTheRoot()
    {
        CopyScripts(_repo);
        string link = _repo + "-link";
        ScriptRunner.MakeJunction(link, _repo);
        try
        {
            (int exitCode, string output) = Install(link);
            Assert.True(exitCode == 0, output);
            Assert.Equal("scripts/hooks", GitOutput("config", "--get", "core.hooksPath"));
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    public void Dispose()
    {
        ScriptRunner.DeleteTree(_repo);
    }

    /// <summary>The real install-hooks.ps1 and hooks, in <paramref name="project"/>/scripts/.</summary>
    private static void CopyScripts(string project)
    {
        string scripts = Path.Combine(Repo.Root, "scripts");
        ScriptRunner.CopyTree(Path.Combine(scripts, "hooks"), Path.Combine(project, "scripts", "hooks"));
        File.Copy(Path.Combine(scripts, "install-hooks.ps1"), Path.Combine(project, "scripts", "install-hooks.ps1"));
    }

    private static (int ExitCode, string Output) Install(string project)
    {
        return ScriptRunner.RunScript(project, Path.Combine(project, "scripts", "install-hooks.ps1"));
    }

    private string GitOutput(params string[] args)
    {
        return ScriptRunner.Run(_repo, gitDir: null, "git", args).Output.Trim();
    }
}
