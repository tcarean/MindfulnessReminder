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

public class TrayTooltipTests
{
    [Fact]
    public void R060_Without_a_countdown_the_tooltip_is_just_the_banner()
    {
        Assert.Equal(VersionInfo.Banner, TrayTooltip.Format(VersionInfo.Banner, null));
    }

    [Fact]
    public void R060_With_a_countdown_the_tooltip_says_how_many_minutes_are_left()
    {
        var text = TrayTooltip.Format(VersionInfo.Banner, TimeSpan.FromMinutes(12));

        Assert.Equal(VersionInfo.Banner + " - next in 12 min", text);
    }

    /// <summary>
    /// Rounded up, so the tooltip reads "30 min" the instant a 30-minute countdown restarts.
    /// Rounding down would show 29 for the first 59 seconds of every interval.
    /// </summary>
    [Fact]
    public void R061_Part_minutes_round_up()
    {
        Assert.Contains("30 min", TrayTooltip.Format("x", TimeSpan.FromSeconds(1799)));
        Assert.Contains("29 min", TrayTooltip.Format("x", TimeSpan.FromSeconds(1740)));
    }

    [Fact]
    public void R061_The_last_minute_reads_as_one_rather_than_zero()
    {
        Assert.Contains("1 min", TrayTooltip.Format("x", TimeSpan.FromSeconds(4)));
        Assert.Contains("1 min", TrayTooltip.Format("x", TimeSpan.Zero));
    }

    /// <summary>
    /// The reason this is a testable function at all: NotifyIcon.Text throws above 63
    /// characters, so the longest string the app can produce has to be shown to fit. Every
    /// shape it can take goes through here, not just the countdown -- a reason added one day
    /// that is a few characters too long would take the tray icon down with it.
    /// </summary>
    [Fact]
    public void R062_The_longest_tooltip_the_app_can_produce_fits_the_tray_limit()
    {
        var longestCountdown = TimeSpan.FromMinutes(ReminderSettings.MaxIntervalMinutes);
        var shapes = new List<(TimeSpan? Remaining, ReminderSuppression Suppression)>
        {
            (null, ReminderSuppression.None),
        };

        foreach (var suppression in Enum.GetValues<ReminderSuppression>())
        {
            shapes.Add((longestCountdown, suppression));
        }

        foreach (var (remaining, suppression) in shapes)
        {
            var tooltip = TrayTooltip.Format(VersionInfo.Banner, remaining, suppression);

            Assert.True(
                tooltip.Length <= TrayTooltip.MaxLength,
                $"tooltip is {tooltip.Length} characters: {tooltip}");

            // The length alone proves nothing: Format cuts at the limit, so every shape fits
            // whatever its text. What R-062 promises is that none of them needed the cut
            // (CR-11). Behind a banner of one character the same shape is far below the
            // limit and so comes out whole: what follows the banner there is what has to
            // follow the real banner here, to the last character.
            const string ShortBanner = "x";
            var whole = VersionInfo.Banner
                        + TrayTooltip.Format(ShortBanner, remaining, suppression)[ShortBanner.Length..];

            Assert.True(
                tooltip == whole,
                $"tooltip was cut at {tooltip.Length} characters: {tooltip}; whole, it is {whole.Length}: {whole}");
        }
    }

    /// <summary>
    /// The reason replaces the countdown. What is being counted while a reminder is held back
    /// is the retry gap -- seconds -- and rounding that up to minutes would leave the tooltip
    /// reading "1 min" for the whole of an hour-long call.
    /// </summary>
    [Theory]
    [InlineData(ReminderSuppression.MicrophoneInUse, "mic")]
    [InlineData(ReminderSuppression.FullScreen, "full screen")]
    public void R060_A_held_back_reminder_says_why_instead_of_counting_down(
        ReminderSuppression suppression,
        string expected)
    {
        var text = TrayTooltip.Format("x", TimeSpan.FromSeconds(20), suppression);

        Assert.Contains("paused", text);
        Assert.Contains(expected, text);
        Assert.DoesNotContain("next in", text);
    }

    [Fact]
    public void R060_Nothing_held_back_leaves_the_countdown_exactly_as_it_was()
    {
        Assert.Equal(
            TrayTooltip.Format("x", TimeSpan.FromMinutes(12)),
            TrayTooltip.Format("x", TimeSpan.FromMinutes(12), ReminderSuppression.None));
    }

    [Fact]
    public void R062_An_over_long_banner_is_truncated_rather_than_allowed_to_throw()
    {
        var text = TrayTooltip.Format(new string('x', 200), TimeSpan.FromMinutes(5));

        Assert.Equal(TrayTooltip.MaxLength, text.Length);
    }
}
