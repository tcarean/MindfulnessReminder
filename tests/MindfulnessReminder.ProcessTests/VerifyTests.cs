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
/// scripts/verify.ps1 is the gate. This test runs the real one, with the real checks it calls, on
/// a throwaway project: one class library in X.sln, no decisions, no design notes, an empty test
/// data manifest, and the process tests named Category=Process over scripts/. Restore, format,
/// build and test do run, so each run takes about 15 s.
/// </summary>
[Trait("Category", "Process")]
public sealed class VerifyTests : IDisposable
{
    private const string Guid = "{11111111-1111-1111-1111-111111111111}";

    private readonly string _project = Path.Combine(Path.GetTempPath(), "mindfulness-process-verify", System.Guid.NewGuid().ToString("N"));

    public VerifyTests()
    {
        ScriptRunner.CopyTree(Path.Combine(Repo.Root, "scripts"), Path.Combine(_project, "scripts"));
        File.WriteAllText(Path.Combine(_project, "process.json"), ScriptRunner.ProcessJson()
            .Replace("\"solution\": \"X.sln\",", "\"solution\": \"X.sln\", \"testFilter\": \"Category!=Validation\",", StringComparison.Ordinal)
            .Replace(
                "\"release\": { \"publish\": [] }",
                "\"release\": { \"publish\": [] }, \"processTests\": { \"category\": \"Process\", \"paths\": [\"scripts/\"] }",
                StringComparison.Ordinal));
        Directory.CreateDirectory(Path.Combine(_project, "tests", "testdata"));
        File.WriteAllText(Path.Combine(_project, "tests", "testdata", "manifest.json"), "{ \"files\": [] }\n");

        // On the SDK's own framework, so it builds offline.
        string lib = Path.Combine(_project, "src", "X");
        Directory.CreateDirectory(lib);
        File.WriteAllText(Path.Combine(lib, "X.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n"
            + "    <TargetFramework>net$(BundledNETCoreAppTargetFrameworkVersion)</TargetFramework>\n"
            + "  </PropertyGroup>\n</Project>\n");
        File.WriteAllText(Path.Combine(lib, "Marker.cs"), "namespace X;\n\npublic static class Marker\n{\n}\n");
        File.WriteAllText(Path.Combine(_project, "X.sln"), string.Join("\n",
            "Microsoft Visual Studio Solution File, Format Version 12.00",
            $"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"X\", \"src\\X\\X.csproj\", \"{Guid}\"",
            "EndProject",
            "Global",
            "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution",
            "\t\tDebug|Any CPU = Debug|Any CPU",
            "\t\tRelease|Any CPU = Release|Any CPU",
            "\tEndGlobalSection",
            "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution",
            $"\t\t{Guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
            $"\t\t{Guid}.Debug|Any CPU.Build.0 = Debug|Any CPU",
            $"\t\t{Guid}.Release|Any CPU.ActiveCfg = Release|Any CPU",
            $"\t\t{Guid}.Release|Any CPU.Build.0 = Release|Any CPU",
            "\tEndGlobalSection",
            "EndGlobal",
            string.Empty));
    }

    /// <summary>Every step runs, in order, and none compares the project with a kit.</summary>
    [Fact]
    public void AGreenRun_RunsEveryStep_ComparesWithNoKit_AndWritesTheStamp()
    {
        (int exitCode, string output) = ScriptRunner.RunScript(_project, Path.Combine(_project, "scripts", "verify.ps1"));
        Assert.True(exitCode == 0, output);
        Assert.DoesNotContain("kit files", output);
        int at = 0;
        foreach (string step in new[] { "== test data manifest", "== decisions", "== design notes", "== restore", "== format", "== build", "== test", "== OK in " })
        {
            int next = output.IndexOf(step, at, StringComparison.Ordinal);
            Assert.True(next >= 0, $"'{step}' missing or out of order:\n{output}");
            at = next;
        }

        Assert.True(File.Exists(Path.Combine(_project, ".verify-stamp")), "verify wrote no stamp");

        // %TEMP% is in no repository: git cannot tell what changed, so the process tests run, and
        // the filter is the project's own alone.
        Assert.Contains("process tests: run", output);
        Assert.Contains("test filter: (Category!=Validation)\n", output.ReplaceLineEndings("\n"));
    }

    /// <summary>
    /// On main with nothing changed under scripts/, the process tests are left out, joined to the
    /// project's own filter, and the output says so; -All runs them, and so does CI. The suite
    /// itself may run in CI: ScriptRunner removes CI, and the run in CI passes it on purpose.
    /// </summary>
    [Fact]
    public void ACleanCheckout_LeavesTheProcessTestsOut_UnlessAllOrCI()
    {
        Git("init", "-q", "-b", "main");
        Git("add", "-A");
        Git("-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "core.hooksPath=no-hooks", "commit", "-q", "-m", "First");

        (int exitCode, string output) = ScriptRunner.RunScript(_project, Path.Combine(_project, "scripts", "verify.ps1"));
        Assert.True(exitCode == 0, output);
        Assert.Contains("process tests: left out", output);
        Assert.Contains("test filter: (Category!=Validation)&Category!=Process", output);

        (exitCode, output) = ScriptRunner.RunScript(_project, Path.Combine(_project, "scripts", "verify.ps1"), "-All");
        Assert.True(exitCode == 0, output);
        Assert.Contains("process tests: run (-All)", output);
        Assert.Contains("test filter: (Category!=Validation)\n", output.ReplaceLineEndings("\n"));

        (exitCode, output) = ScriptRunner.RunWith(_project, new Dictionary<string, string> { ["CI"] = "true" },
            ScriptRunner.Shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(_project, "scripts", "verify.ps1"));
        Assert.True(exitCode == 0, output);
        Assert.Contains("process tests: run (CI)", output);
    }

    public void Dispose()
    {
        ScriptRunner.DeleteTree(_project);
    }

    private void Git(params string[] args)
    {
        (int exitCode, string output) = ScriptRunner.Run(_project, gitDir: null, "git", args);
        Assert.True(exitCode == 0, output);
    }
}
