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

using MindfulnessReminder.Core;

namespace MindfulnessReminder.Tests;

/// <summary>
/// What Save in the Settings window does once the boxes read: the settings are written, then
/// applied, and a registry that refuses the write stops both without stopping the application
/// (CR-01 of the review of 2026-11-13).
/// </summary>
public class SettingsCommitTests
{
    private static readonly ReminderSettings Edited = ReminderSettings.Default with { IntervalMinutes = 45 };

    private readonly FakeRegistryStore _registry = new();
    private readonly FakeDecisionLog _log = new();
    private readonly List<ReminderSettings> _applied = [];

    private bool Commit() =>
        SettingsCommit.TrySaveAndApply(new RegistrySettingsStore(_registry, _log), Edited, _applied.Add, _log);

    [Fact]
    public void R044_An_accepted_save_writes_the_settings_and_then_applies_them()
    {
        Assert.True(Commit());

        Assert.Equal(Edited, new RegistrySettingsStore(_registry).Load());
        Assert.Equal([Edited], _applied);
        Assert.Equal(["Settings saved: " + DecisionLog.Summary(Edited)], _log.Lines);
    }

    [Theory]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(System.Security.SecurityException))]
    public void R046_A_save_the_registry_refuses_applies_nothing_and_says_so_in_the_log(Type refusal)
    {
        _registry.WriteRefusal = (Exception)Activator.CreateInstance(refusal)!;

        Assert.False(Commit());

        Assert.Empty(_applied);
        Assert.Equal(0, _registry.WriteCount);
        Assert.Equal(["Settings not saved: the registry refused (" + refusal.Name + ")"], _log.Lines);
    }

    /// <summary>
    /// Only a refusal is expected. Anything else is an error like any other and goes to the
    /// crash handler, with nothing applied.
    /// </summary>
    [Fact]
    public void R046_An_error_that_is_not_a_refusal_is_not_swallowed()
    {
        _registry.WriteRefusal = new InvalidOperationException("not a refusal");

        Assert.Throws<InvalidOperationException>(() => Commit());

        Assert.Empty(_applied);
    }

    [Fact]
    public void R046_The_refusal_line_names_the_exception_by_its_type()
    {
        Assert.Equal(
            "Settings not saved: the registry refused (UnauthorizedAccessException)",
            DecisionLog.SettingsSaveRefused(new UnauthorizedAccessException("Access is denied.")));
    }
}
