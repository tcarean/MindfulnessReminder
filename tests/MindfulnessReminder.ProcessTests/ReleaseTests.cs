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

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace MindfulnessReminder.ProcessTests;

/// <summary>
/// scripts/release.ps1 refuses, before it changes anything, a release off main and a version
/// the owner did not leave room for (ADR 0029): the floor is the newest released
/// CHANGELOG heading, and a version the owner already set in &lt;Version&gt; is released as it
/// is. These tests build a throwaway repository whose root is the project, Proj, with the real
/// release.ps1 and process-config.ps1 and a stub verify.ps1 that records that it ran; the owner has
/// bumped &lt;Version&gt; to 2.3.0 and the newest release is 2.2.0, as in FlatCheck on 2026-10-01.
/// </summary>
[Trait("Category", "Process")]
public sealed class ReleaseTests : IDisposable
{
    private const string Changelog =
        "# Changelog\n\n## [Unreleased]\n\n### Added\n\n- Something new.\n\n## [2.2.0] - 2026-09-23\n\n- Older.\n";

    private readonly string _repo = Path.Combine(Path.GetTempPath(), "mindfulness-process-release", Guid.NewGuid().ToString("N"));
    private readonly string _project;
    private readonly string _worktree;

    public ReleaseTests()
    {
        _project = Path.Combine(_repo, "Proj");
        _worktree = _repo + "-wt";
        Directory.CreateDirectory(Path.Combine(_project, "scripts"));
        Git("init", "-q", "-b", "main");
        Git("config", "user.name", "Test");
        Git("config", "user.email", "test@example.com");
        string scripts = Path.Combine(Repo.Root, "scripts");
        foreach (string script in new[] { "release.ps1", "process-config.ps1", "merge-publish.ps1" })
        {
            File.Copy(Path.Combine(scripts, script), Path.Combine(_project, "scripts", script));
        }

        File.WriteAllText(Path.Combine(_project, "scripts", "verify.ps1"),
            "Set-Content -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'ran.txt') -Value 'verify'\nexit 0\n");
        File.WriteAllText(Path.Combine(_project, "process.json"), ScriptRunner.ProcessJson());
        WriteProps("2.3.0");
        File.WriteAllText(Path.Combine(_project, "CHANGELOG.md"), Changelog);
        File.WriteAllText(Path.Combine(_project, ".gitignore"), "ran.txt\nartifacts/\nbin/\nobj/\n");
        Commit("Start");
    }

    [Fact]
    public void OffMain_IsRefused_BeforeAnythingRuns()
    {
        Git("switch", "-q", "-c", "claude/Task");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("A release is cut on main", output);
        Assert.Contains("branch 'claude/Task'", output);
        AssertNothingChanged();
    }

    [Fact]
    public void ADetachedHead_IsRefused()
    {
        Git("switch", "-q", "--detach");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("a detached HEAD", output);
        Assert.False(File.Exists(Path.Combine(_project, "ran.txt")), "verify ran");
    }

    /// <summary>A linked worktree may hold main when the main checkout was switched away; it is still not the main checkout.</summary>
    [Fact]
    public void ALinkedWorktreeOnMain_IsRefused()
    {
        Git("switch", "-q", "-c", "parked");
        Git("worktree", "add", "-q", _worktree, "main");

        (int exitCode, string output) = ScriptRunner.RunScript(
            _worktree, Path.Combine(_worktree, "scripts", "release.ps1"), "-Version", "2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("this is a linked worktree", output);
        Assert.False(File.Exists(Path.Combine(_worktree, "ran.txt")), "verify ran");
    }

    /// <summary>A standalone repository whose main branch has another name says so in process.json.</summary>
    [Fact]
    public void TheReleaseBranch_ComesFromProcessJson()
    {
        Git("branch", "-m", "master");
        (int exitCode, string output) = Release("2.3.0", "-WhatIf");
        Assert.Equal(1, exitCode);
        Assert.Contains("A release is cut on main", output);

        File.WriteAllText(Path.Combine(_project, "process.json"),
            ScriptRunner.ProcessJson().Replace("\"publish\": []", "\"publish\": [], \"branch\": \"master\"", StringComparison.Ordinal));
        Commit("Release from master");
        (exitCode, output) = Release("2.3.0", "-WhatIf");
        Assert.True(exitCode == 0, output);
    }

    [Theory]
    [InlineData("2.03.0")]
    [InlineData("02.3.0")]
    [InlineData("2.3.00")]
    public void AVersionWithALeadingZero_IsRefused(string version)
    {
        (int exitCode, string output) = Release(version, "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("without leading zeros", output);
        AssertNothingChanged();
    }

    /// <summary>A project that never released has no floor: the owner's version goes out as it is.</summary>
    [Fact]
    public void ACHANGELOGWithNoReleasedHeading_HasNoFloor()
    {
        File.WriteAllText(Path.Combine(_project, "CHANGELOG.md"), "# Changelog\n\n## [Unreleased]\n\n- First.\n");
        Commit("No release yet");

        (int exitCode, string output) = Release("2.3.0", "-WhatIf");
        Assert.True(exitCode == 0, output);
        Assert.Contains("newest released none", output);
    }

    [Fact]
    public void AReleasedVersion_IsRefused()
    {
        WriteProps("2.2.0");
        Commit("Back to 2.2.0");

        (int exitCode, string output) = Release("2.2.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("already released", output);
    }

    [Fact]
    public void AVersionNotAboveTheNewestReleasedHeading_IsRefused()
    {
        WriteProps("2.1.0");
        Commit("Back to 2.1.0");

        (int exitCode, string output) = Release("2.1.5", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("not above 2.2.0", output);
    }

    [Fact]
    public void AVersionBelowTheOnesTheOwnerSet_IsRefused()
    {
        WriteProps("2.4.0");
        Commit("Owner bumps to 2.4.0");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("below <Version> 2.4.0", output);
        AssertNothingChanged();
    }

    [Fact]
    public void AnExistingTag_IsRefused()
    {
        Git("tag", "v2.3.0");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("Tag v2.3.0 already exists", output);
    }

    [Fact]
    public void WhatIf_ChangesNothing()
    {
        (int exitCode, string output) = Release("2.3.0", "-WhatIf");
        Assert.True(exitCode == 0, output);
        Assert.Contains("newest released 2.2.0", output);
        Assert.Contains("leave <Version> at 2.3.0", output);
        AssertNothingChanged();
    }

    /// <summary>The owner already set 2.3.0: the release uses it, closes [Unreleased] and leaves the props file alone.</summary>
    [Fact]
    public void TheVersionTheOwnerSet_IsReleasedAsItIs()
    {
        byte[] props = File.ReadAllBytes(Path.Combine(_project, "Directory.Build.props"));

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.True(exitCode == 0, output);
        Assert.True(File.Exists(Path.Combine(_project, "ran.txt")), "verify did not run");
        Assert.Equal(props, File.ReadAllBytes(Path.Combine(_project, "Directory.Build.props")));
        Assert.Contains("unchanged, already 2.3.0", output);
        Assert.Contains("-- Directory.Build.props CHANGELOG.md", output);

        string changelog = File.ReadAllText(Path.Combine(_project, "CHANGELOG.md"));
        string today = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains($"## [Unreleased]\n\n## [2.3.0] - {today}\n\n### Added\n\n- Something new.", changelog);
    }

    [Fact]
    public void AVersionAboveTheOnesTheOwnerSet_IsWrittenToTheProps()
    {
        (int exitCode, string output) = Release("2.4.0", "-NoPublish");
        Assert.True(exitCode == 0, output);
        Assert.Contains("<Version>2.4.0</Version>", File.ReadAllText(Path.Combine(_project, "Directory.Build.props")));
        Assert.Contains("2.3.0 -> 2.4.0", output);
    }

    /// <summary>A release publishes this project only: an entry outside its folder is refused before anything runs.</summary>
    [Fact]
    public void AnEntryOutsideTheProject_IsRefused()
    {
        WriteProcessJson("""{ "project": "../Proj2/p.csproj" }""");
        Commit("An entry outside the project");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("'../Proj2/p.csproj' is outside the project folder", output);
        AssertNothingChanged();
    }

    /// <summary>The tag vX.Y.Z names a version of the whole repository, so a release is cut from a project that is its root.</summary>
    [Fact]
    public void AProjectBelowTheGitRoot_IsRefused()
    {
        string sub = Path.Combine(_project, "Sub");
        ScriptRunner.CopyTree(Path.Combine(_project, "scripts"), Path.Combine(sub, "scripts"));
        foreach (string file in new[] { "process.json", "Directory.Build.props", "CHANGELOG.md" })
        {
            File.Copy(Path.Combine(_project, file), Path.Combine(sub, file));
        }

        Commit("A project below the root");

        (int exitCode, string output) = ScriptRunner.RunScript(sub, Path.Combine(sub, "scripts", "release.ps1"), "-Version", "2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("the root of its repository", output);
        Assert.False(File.Exists(Path.Combine(sub, "ran.txt")), "verify ran");
    }

    /// <summary>The refusal lists the paths as git status prints them: " M" is a change not staged, kept with its space.</summary>
    [Fact]
    public void ADirtyTree_IsRefused_WithThePathsAsGitPrintsThem()
    {
        File.AppendAllText(Path.Combine(_project, "CHANGELOG.md"), "- Not committed.\n");

        (int exitCode, string output) = Release("2.3.0", "-NoPublish");
        Assert.Equal(1, exitCode);
        Assert.Contains("Working tree is not clean", output);
        Assert.Contains(" M CHANGELOG.md", output);
        Assert.False(File.Exists(Path.Combine(_project, "ran.txt")), "verify ran");
    }

    /// <summary>
    /// -PublishOnly runs step 5 alone, from any branch, and leaves the version files as they are:
    /// a small library is published through the staging folder into its output subfolder,
    /// release.json lists this project as its one component, clean, and the zip, whose entry
    /// names use forward slashes whatever the shell, holds every published file, a hidden one
    /// included, and has its hash beside it.
    /// </summary>
    [Fact]
    public void PublishOnly_WritesReleaseJsonAndTheZip_FromAnyBranch()
    {
        string lib = WriteLibrary("src/Lib", items: """<None Include="hidden.txt" CopyToPublishDirectory="PreserveNewest" />""");
        File.WriteAllText(Path.Combine(lib, "hidden.txt"), "hidden\n");
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(Path.Combine(lib, "hidden.txt"), FileAttributes.Hidden);
        }

        WriteProcessJson("""{ "project": "src/Lib/Lib.csproj", "output": "lib" }""", zip: "Proj-{version}-win-x64.zip");
        Commit("Zip the release");
        Git("switch", "-q", "-c", "claude/Task");
        byte[] props = File.ReadAllBytes(Path.Combine(_project, "Directory.Build.props"));
        byte[] changelog = File.ReadAllBytes(Path.Combine(_project, "CHANGELOG.md"));

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly");
        Assert.True(exitCode == 0, output);
        Assert.False(File.Exists(Path.Combine(_project, "ran.txt")), "verify ran");
        Assert.Equal(props, File.ReadAllBytes(Path.Combine(_project, "Directory.Build.props")));
        Assert.Equal(changelog, File.ReadAllBytes(Path.Combine(_project, "CHANGELOG.md")));

        string artifacts = Path.Combine(_project, "artifacts");
        using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(artifacts, "2.3.0", "release.json")));
        JsonElement json = release.RootElement;
        (int _, string head) = ScriptRunner.Run(_project, gitDir: null, "git", "rev-parse", "HEAD");
        Assert.Equal("Proj", json.GetProperty("name").GetString());
        Assert.Equal("2.3.0", json.GetProperty("version").GetString());
        Assert.Equal(head.Trim(), json.GetProperty("commit").GetString());
        JsonElement component = Assert.Single(json.GetProperty("components").EnumerateArray());
        Assert.Equal("Proj", component.GetProperty("name").GetString());
        Assert.Equal("2.3.0", component.GetProperty("version").GetString());
        Assert.Equal(head.Trim(), component.GetProperty("commit").GetString());
        Assert.True(component.GetProperty("unreleased").GetBoolean());
        Assert.True(component.GetProperty("clean").GetBoolean());

        string zip = Path.Combine(artifacts, "Proj-2.3.0-win-x64.zip");
        using (ZipArchive archive = ZipFile.OpenRead(zip))
        {
            string[] entries = archive.Entries.Select(e => e.FullName).ToArray();
            Assert.Contains("release.json", entries);
            Assert.Contains("lib/Lib.dll", entries);
            Assert.Contains("lib/hidden.txt", entries);
            Assert.DoesNotContain(entries, e => e.Contains('\\', StringComparison.Ordinal) || e.StartsWith(".stage", StringComparison.Ordinal));
        }

        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(zip))).ToLowerInvariant();
        Assert.Equal($"{hash}  Proj-2.3.0-win-x64.zip\n", File.ReadAllText(zip + ".sha256"));
    }

    /// <summary>-WhatIf with -PublishOnly previews: no publish, no release.json, no zip, the previous folder left as it was.</summary>
    [Fact]
    public void PublishOnly_WithWhatIf_ChangesNothing()
    {
        WriteProcessJson(string.Empty, zip: "Proj-{version}.zip");
        Commit("Zip the release");
        string old = Path.Combine(_project, "artifacts", "2.3.0", "old.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(old)!);
        File.WriteAllText(old, "previous run\n");

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly", "-WhatIf");
        Assert.True(exitCode == 0, output);
        Assert.Contains("WhatIf: would publish", output);
        Assert.True(File.Exists(old), "the previous run's folder was emptied");
        Assert.False(File.Exists(Path.Combine(_project, "artifacts", "2.3.0", "release.json")), "release.json was written");
        Assert.False(File.Exists(Path.Combine(_project, "artifacts", "Proj-2.3.0.zip")), "the zip was written");
    }

    /// <summary>
    /// Outside a git repository, as a project just copied from the kit is before its first commit,
    /// git's error does not stop the publish: release.json has empty commits and says not clean.
    /// </summary>
    [Fact]
    public void PublishOnly_OutsideAGitRepository_WritesEmptyCommits()
    {
        string plain = _repo + "-plain";
        ScriptRunner.CopyTree(_project, plain);
        ScriptRunner.DeleteTree(Path.Combine(plain, ".git"));

        (int exitCode, string output) = ScriptRunner.RunScript(plain, Path.Combine(plain, "scripts", "release.ps1"), "-Version", "2.3.0", "-PublishOnly");
        try
        {
            Assert.True(exitCode == 0, output);
            Assert.Contains("fatal: not a git repository", output);
            using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(plain, "artifacts", "2.3.0", "release.json")));
            Assert.Equal(string.Empty, release.RootElement.GetProperty("commit").GetString());
            JsonElement component = Assert.Single(release.RootElement.GetProperty("components").EnumerateArray());
            Assert.Equal(string.Empty, component.GetProperty("commit").GetString());
            Assert.False(component.GetProperty("clean").GetBoolean());
        }
        finally
        {
            ScriptRunner.DeleteTree(plain);
        }
    }

    /// <summary>
    /// Without git on PATH, after a publish whose dotnet and merge left an exit code of 0 behind,
    /// release.json still says not clean and the output says why. Windows only: the PATH is
    /// built from dotnet's folder, PowerShell's and System32, where git is not.
    /// </summary>
    [Fact]
    public void PublishOnly_WithoutGit_RecordsNotClean()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string dotnet = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator)
            .First(d => d.Length > 0 && File.Exists(Path.Combine(d, "dotnet.exe")));
        string path = string.Join(Path.PathSeparator, dotnet, Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0"), Environment.SystemDirectory);
        WriteLibrary("src/Lib");
        WriteProcessJson("""{ "project": "src/Lib/Lib.csproj", "output": "lib" }""");
        Commit("One library");

        (int exitCode, string output) = ScriptRunner.RunWith(_project, new Dictionary<string, string> { ["PATH"] = path },
            ScriptRunner.Shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(_project, "scripts", "release.ps1"), "-Version", "2.3.0", "-PublishOnly");
        Assert.True(exitCode == 0, output);
        Assert.Contains("git was not found", output);
        using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(_project, "artifacts", "2.3.0", "release.json")));
        JsonElement component = Assert.Single(release.RootElement.GetProperty("components").EnumerateArray());
        Assert.Equal(string.Empty, component.GetProperty("commit").GetString());
        Assert.False(component.GetProperty("clean").GetBoolean());
    }

    /// <summary>-PublishOnly checks nothing, so release.json says when a component's folder held uncommitted files.</summary>
    [Fact]
    public void PublishOnly_FromADirtyTree_RecordsItNotClean()
    {
        File.WriteAllText(Path.Combine(_project, "scratch.txt"), "not committed\n");

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly");
        Assert.True(exitCode == 0, output);
        using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(_project, "artifacts", "2.3.0", "release.json")));
        JsonElement component = Assert.Single(release.RootElement.GetProperty("components").EnumerateArray());
        Assert.False(component.GetProperty("clean").GetBoolean());
    }

    /// <summary>A full release changes the two version files itself; release.json still says this project is clean.</summary>
    [Fact]
    public void AFullRelease_RecordsItselfClean_DespiteTheVersionFilesItChanged()
    {
        (int exitCode, string output) = Release("2.4.0");
        Assert.True(exitCode == 0, output);
        using JsonDocument release = JsonDocument.Parse(File.ReadAllText(Path.Combine(_project, "artifacts", "2.4.0", "release.json")));
        JsonElement component = Assert.Single(release.RootElement.GetProperty("components").EnumerateArray());
        Assert.Equal("2.4.0", component.GetProperty("version").GetString());
        Assert.False(component.GetProperty("unreleased").GetBoolean());
        Assert.True(component.GetProperty("clean").GetBoolean());
    }

    /// <summary>Two entries that publish different bytes at one path stop the release, which writes no release.json.</summary>
    [Fact]
    public void TwoEntriesThatCollide_StopThePublish()
    {
        WriteLibrary("src/A");
        WriteLibrary("src/B", source: "namespace B;\n\npublic static class Marker\n{\n}\n");
        WriteProcessJson("""{ "project": "src/A/Lib.csproj", "output": "" }, { "project": "src/B/Lib.csproj", "output": "" }""");
        Commit("Two libraries named Lib");

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly");
        Assert.Equal(1, exitCode);
        Assert.Contains("collision  Lib.dll", output);
        Assert.Contains("src/B/Lib.csproj collides with what the entries before it published", output);
        Assert.False(File.Exists(Path.Combine(_project, "artifacts", "2.3.0", "release.json")), "release.json was written");
    }

    /// <summary>release.json is the release's own file: a publish that ships one at the root is a collision, not overwritten.</summary>
    [Fact]
    public void APublishedReleaseJson_IsACollision()
    {
        string lib = WriteLibrary("src/Lib", items: """<None Include="release.json" CopyToPublishDirectory="PreserveNewest" />""");
        File.WriteAllText(Path.Combine(lib, "release.json"), "{ \"app\": \"own\" }\n");
        WriteProcessJson("""{ "project": "src/Lib/Lib.csproj", "output": "" }""");
        Commit("A library that ships release.json");

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly");
        Assert.Equal(1, exitCode);
        Assert.Contains("collision  release.json", output);
        Assert.Equal("{ \"app\": \"own\" }\n", File.ReadAllText(Path.Combine(_project, "artifacts", "2.3.0", "release.json")));
    }

    /// <summary>A publish that fails says so, without the advice meant for a collision.</summary>
    [Fact]
    public void AFailedPublish_IsNotReportedAsACollision()
    {
        WriteProcessJson("""{ "project": "src/Missing/Missing.csproj", "output": "" }""");
        Commit("An entry whose project is missing");

        (int exitCode, string output) = Release("2.3.0", "-PublishOnly");
        Assert.Equal(1, exitCode);
        Assert.Contains("dotnet publish (src/Missing/Missing.csproj) failed", output);
        Assert.DoesNotContain("output subfolder", output);
        Assert.DoesNotContain("collides", output);
    }

    public void Dispose()
    {
        ScriptRunner.DeleteTree(_worktree);
        ScriptRunner.DeleteTree(_repo);
    }

    private (int ExitCode, string Output) Release(string version, params string[] args)
    {
        return ScriptRunner.RunScript(_project, Path.Combine(_project, "scripts", "release.ps1"), ["-Version", version, .. args]);
    }

    private void WriteProps(string version)
    {
        File.WriteAllText(Path.Combine(_project, "Directory.Build.props"),
            $"<Project>\n  <PropertyGroup>\n    <Version>{version}</Version>\n  </PropertyGroup>\n</Project>\n");
    }

    /// <summary>Proj's process.json with the given release.publish entries and, when given, release.zip.</summary>
    private void WriteProcessJson(string entries, string? zip = null)
    {
        string zipKey = zip is null ? string.Empty : $", \"zip\": \"{zip}\"";
        File.WriteAllText(Path.Combine(_project, "process.json"),
            ScriptRunner.ProcessJson().Replace("\"publish\": []", $"\"publish\": [{entries}]{zipKey}", StringComparison.Ordinal));
    }

    /// <summary>
    /// A class library Lib.csproj in <paramref name="folder"/> (relative to Proj), on the SDK's own
    /// framework so it builds offline; <paramref name="items"/> go in an ItemGroup and
    /// <paramref name="source"/>, when given, in Code.cs. Returns the folder.
    /// </summary>
    private string WriteLibrary(string folder, string items = "", string? source = null)
    {
        string dir = Path.GetFullPath(Path.Combine(_project, folder));
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Lib.csproj"),
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>
              </PropertyGroup>
              <ItemGroup>{items}</ItemGroup>
            </Project>
            """);
        if (source is not null)
        {
            File.WriteAllText(Path.Combine(dir, "Code.cs"), source);
        }

        return dir;
    }

    /// <summary>No file changed and verify did not run: the refusal came before step 2.</summary>
    private void AssertNothingChanged()
    {
        (int _, string status) = ScriptRunner.Run(_project, gitDir: null, "git", "status", "--porcelain");
        Assert.Equal(string.Empty, status.Trim());
        Assert.False(File.Exists(Path.Combine(_project, "ran.txt")), "verify ran");
    }

    private void Commit(string message)
    {
        Git("add", "-A");
        Git("-c", "core.hooksPath=no-hooks", "commit", "-q", "-m", message);
    }

    private void Git(params string[] args)
    {
        (int exitCode, string output) = ScriptRunner.Run(_project, gitDir: null, "git", args);
        Assert.True(exitCode == 0, output);
    }
}
