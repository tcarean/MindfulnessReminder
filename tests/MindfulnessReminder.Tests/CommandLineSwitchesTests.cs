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
/// Which argument <c>--preview</c> takes as its text (R-045) and <c>--detect</c> as its seconds
/// (R-081, R-082), and that neither is one of the application's own switches (R-102, CR-10).
/// </summary>
public class CommandLineSwitchesTests
{
    [Theory]
    [InlineData("Take a slow breath", "--preview", "Take a slow breath")]
    [InlineData("Breathe", "--PREVIEW", "Breathe")]
    [InlineData("Breathe", "--restarted", "--preview", "Breathe")]
    [InlineData("Breathe", "--preview", "--restarted", "Breathe")]
    [InlineData("Breathe", "--preview", "--detect=30", "--Restarted", "Breathe", "out")]
    [InlineData("Breathe", "--preview", "--detect", "30", "Breathe")]
    [InlineData("Breathe", "--preview", "--detect", "--restarted", "30", "Breathe")]
    [InlineData("Breathe", "--preview", "--detect=30", "Breathe")]
    [InlineData("", "--preview", "")]
    [InlineData(null, "--preview")]
    [InlineData(null, "--preview", "--detect")]
    [InlineData(null, "--preview", "--detect=30")]
    [InlineData(null, "--preview", "--detect", "30")]
    [InlineData(null, "--preview", "--DETECT", "Breathe")]
    [InlineData(null, "--detect", "--preview", "30")]
    [InlineData(null, "--detect", "--restarted", "--preview", "30")]
    [InlineData("Breathe", "--detect", "--preview", "30", "Breathe")]
    [InlineData("Breathe", "--detect", "30", "--preview", "Breathe")]
    [InlineData("Breathe", "--detect=30", "--preview", "Breathe")]
    [InlineData(null, "--preview", "--preview")]
    [InlineData(null, "Breathe", "--preview")]
    public void R045_The_preview_text_is_the_first_argument_after_the_switch_that_is_not_a_switch(
        string? expected,
        params string[] args)
    {
        Assert.True(CommandLineSwitches.TryGetPreviewText(args, out var text));
        Assert.Equal(expected, text);
    }

    /// <summary>
    /// The switch counts only as an argument of its own: <c>--preview=Breathe</c> is not it,
    /// which is also how <see cref="ApplicationRestart.ShouldRegister"/> reads it.
    /// </summary>
    [Theory]
    [InlineData("--restarted", "Breathe")]
    [InlineData("--detect", "30")]
    [InlineData("--preview=Breathe")]
    [InlineData("--previews", "Breathe")]
    public void R045_Without_the_preview_switch_there_is_no_preview_and_no_text(params string[] args)
    {
        Assert.False(CommandLineSwitches.TryGetPreviewText(args, out var text));
        Assert.Null(text);
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
        Assert.True(CommandLineSwitches.TryGetPreviewText(args, out var text));
        Assert.Null(text);
    }

    [Theory]
    [InlineData(0, "--detect")]
    [InlineData(30, "--detect", "30")]
    [InlineData(30, "--detect=30")]
    [InlineData(30, "--DETECT=30")]
    [InlineData(45, "--Detect", "45", "60")]
    public void R081_Detect_takes_its_seconds_as_the_next_argument_or_after_an_equals_sign(
        int seconds,
        params string[] args)
    {
        Assert.True(CommandLineSwitches.TryGetDetectDuration(args, out var duration));
        Assert.Equal(TimeSpan.FromSeconds(seconds), duration);
    }

    [Theory]
    [InlineData("--preview")]
    [InlineData("--restarted", "30")]
    [InlineData("--detection", "30")]
    public void R081_Without_the_detect_switch_no_duration_is_found(params string[] args)
    {
        Assert.False(CommandLineSwitches.TryGetDetectDuration(args, out var duration));
        Assert.Equal(TimeSpan.Zero, duration);
    }

    [Theory]
    [InlineData(0, "--detect", "abc")]
    [InlineData(0, "--detect", "1.5")]
    [InlineData(0, "--detect=")]
    [InlineData(0, "--detect=0")]
    [InlineData(0, "--detect", "-5")]
    [InlineData(600, "--detect", "600")]
    [InlineData(600, "--detect", "601")]
    [InlineData(600, "--detect=99999")]
    public void R082_A_duration_that_is_not_a_whole_number_above_zero_is_one_reading_and_ten_minutes_is_the_most(
        int seconds,
        params string[] args)
    {
        Assert.True(CommandLineSwitches.TryGetDetectDuration(args, out var duration));
        Assert.Equal(TimeSpan.FromSeconds(seconds), duration);
        Assert.Equal(TimeSpan.FromMinutes(10), CommandLineSwitches.MaxDetectDuration);
    }

    /// <summary>
    /// R-102 has <c>--detect</c> ignore <c>--restarted</c>: the command line reads as it would
    /// without it, so the seconds are found behind it.
    /// </summary>
    [Theory]
    [InlineData(30, "--detect", "--restarted", "30")]
    [InlineData(30, "--detect", "--RESTARTED", "30")]
    [InlineData(30, "--restarted", "--detect", "30")]
    [InlineData(0, "--detect", "--restarted")]
    public void R102_Detect_passes_over_the_restarted_switch_to_find_its_seconds(int seconds, params string[] args)
    {
        Assert.True(CommandLineSwitches.TryGetDetectDuration(args, out var duration));
        Assert.Equal(TimeSpan.FromSeconds(seconds), duration);
    }

    [Fact]
    public void R081_Null_arguments_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => CommandLineSwitches.TryGetDetectDuration(null!, out _));
    }

    [Fact]
    public void R045_No_arguments_carry_no_preview_text_and_null_arguments_are_refused()
    {
        Assert.False(CommandLineSwitches.TryGetPreviewText([], out var text));
        Assert.Null(text);
        Assert.Throws<ArgumentNullException>(() => CommandLineSwitches.TryGetPreviewText(null!, out _));
    }
}
