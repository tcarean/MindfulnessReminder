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
/// Which argument <c>--preview</c> takes as its text (R-045), and that it is never one of the
/// application's own switches (R-102, CR-10).
/// </summary>
public class CommandLineSwitchesTests
{
    [Theory]
    [InlineData("Take a slow breath", "--preview", "Take a slow breath")]
    [InlineData("Breathe", "--PREVIEW", "Breathe")]
    [InlineData("Breathe", "--restarted", "--preview", "Breathe")]
    [InlineData("Breathe", "--preview", "--restarted", "Breathe")]
    [InlineData("Breathe", "--preview", "--detect=30", "--Restarted", "Breathe", "out")]
    [InlineData("", "--preview", "")]
    [InlineData(null, "--preview")]
    [InlineData(null, "--preview", "--detect")]
    [InlineData(null, "--preview", "--detect=30")]
    [InlineData(null, "--preview", "--preview")]
    [InlineData(null, "Breathe", "--preview")]
    [InlineData(null, "--restarted", "Breathe")]
    public void R045_The_preview_text_is_the_first_argument_after_the_switch_that_is_not_a_switch(
        string? expected,
        params string[] args)
    {
        Assert.Equal(expected, CommandLineSwitches.PreviewText(args));
    }

    /// <summary>
    /// The command line the review typed: before the fix the reminder said <c>--restarted</c>.
    /// With no text the caller shows the default message.
    /// </summary>
    [Theory]
    [InlineData("--preview", "--restarted")]
    [InlineData("--preview", "--RESTARTED")]
    public void R102_Preview_does_not_take_the_restarted_switch_as_its_text(params string[] args)
    {
        Assert.Null(CommandLineSwitches.PreviewText(args));
    }

    [Fact]
    public void R045_No_arguments_carry_no_preview_text_and_null_arguments_are_refused()
    {
        Assert.Null(CommandLineSwitches.PreviewText([]));
        Assert.Throws<ArgumentNullException>(() => CommandLineSwitches.PreviewText(null!));
    }
}
