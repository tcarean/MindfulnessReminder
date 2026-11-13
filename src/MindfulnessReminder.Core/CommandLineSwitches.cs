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

using System.Globalization;

namespace MindfulnessReminder.Core;

/// <summary>
/// The switches the executable understands, in one place: App parses them, and
/// <see cref="ApplicationRestart"/> decides from them, so both read the same strings.
/// </summary>
public static class CommandLineSwitches
{
    /// <summary>Shows the reminder window once and quits (R-045).</summary>
    public const string Preview = "--preview";

    /// <summary>Reports what the activity monitor sees and quits (R-080).</summary>
    public const string Detect = "--detect";

    /// <summary>What Windows passes to a copy it started again after a crash (R-100, R-102).</summary>
    public const string Restarted = "--restarted";

    /// <summary>
    /// The longest <c>--detect</c> watches for: ten minutes. Long enough for any test, short
    /// enough that a typo ends (R-081, R-082).
    /// </summary>
    public static readonly TimeSpan MaxDetectDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Whether the arguments carry a switch, written as the switch alone or as the switch, an
    /// equals sign and a value (<c>--detect=30</c>); the comparison ignores case.
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name; not null.</param>
    /// <param name="name">The switch to look for, with its leading dashes.</param>
    /// <returns><c>true</c> when any argument is the switch or starts with it and <c>=</c>.</returns>
    public static bool Has(IReadOnlyList<string> args, string name)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);

        foreach (var argument in args)
        {
            if (argument.Equals(name, StringComparison.OrdinalIgnoreCase)
                || argument.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether the arguments carry a switch written exactly as itself, with no value attached;
    /// the comparison ignores case.
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name; not null.</param>
    /// <param name="name">The switch to look for, with its leading dashes.</param>
    /// <returns><c>true</c> when any argument equals the switch.</returns>
    public static bool HasExactly(IReadOnlyList<string> args, string name)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(name);

        foreach (var argument in args)
        {
            if (argument.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds whether <c>--preview</c> was given, and the text given with it: the first
    /// argument after the switch that is neither one of the application's own switches nor
    /// the number of seconds of a bare <c>--detect</c>, wherever that switch stands (R-045). Windows adds
    /// <c>--restarted</c> to a copy it starts again, and a switch is never what the reminder
    /// should say (R-102).
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name; not null.</param>
    /// <param name="text">
    /// The text as written, not normalized; it can be empty. <c>null</c> when the switch was
    /// not given, or was given with nothing but switches and their values after it.
    /// </param>
    /// <returns>
    /// <c>true</c> when <c>--preview</c> is among the arguments as an argument of its own; the
    /// comparison ignores case.
    /// </returns>
    public static bool TryGetPreviewText(IReadOnlyList<string> args, out string? text)
    {
        ArgumentNullException.ThrowIfNull(args);

        text = null;
        var found = false;
        var detectSecondsPending = false;

        // Every argument is looked at, those before "--preview" too: a bare "--detect" there
        // still has its seconds to come, perhaps after "--preview" ("--detect --preview 30"),
        // and TryGetDetectDuration would read them so.
        foreach (var argument in args)
        {
            if (IsSwitch(argument))
            {
                found |= argument.Equals(Preview, StringComparison.OrdinalIgnoreCase);

                // "--detect 30": the bare switch is followed by its seconds, perhaps behind
                // other switches. "--detect=30" has them with it.
                detectSecondsPending |= argument.Equals(Detect, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (detectSecondsPending)
            {
                detectSecondsPending = false;
                continue;
            }

            if (found)
            {
                text = argument;
                break;
            }
        }

        return found;
    }

    /// <summary>
    /// Finds how long <c>--detect</c> should watch for: <c>--detect 30</c> and
    /// <c>--detect=30</c> both mean thirty seconds (R-081). After the bare switch the seconds
    /// are the next argument that is not one of the application's own switches. A value that
    /// is not a whole number above zero is the single reading, not a complaint: a diagnostic
    /// that refuses to run because its argument was mistyped is no help to anybody (R-082).
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name; not null.</param>
    /// <param name="duration">
    /// How long to watch for, at most <see cref="MaxDetectDuration"/>; zero for the single
    /// reading, and when the switch was not given.
    /// </param>
    /// <returns><c>true</c> when the switch is among the arguments; the comparison ignores case.</returns>
    public static bool TryGetDetectDuration(IReadOnlyList<string> args, out TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(args);

        duration = TimeSpan.Zero;

        for (var i = 0; i < args.Count; i++)
        {
            var argument = args[i];

            if (argument.StartsWith(Detect + "=", StringComparison.OrdinalIgnoreCase))
            {
                duration = Seconds(argument[(Detect.Length + 1)..]);
                return true;
            }

            if (!argument.Equals(Detect, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // The bare switch: its seconds are the next argument that is not a switch of the
            // application's own, so that "--detect --restarted 30" is thirty seconds (R-102).
            for (var next = i + 1; next < args.Count; next++)
            {
                if (!IsSwitch(args[next]))
                {
                    duration = Seconds(args[next]);
                    break;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>Reads the value of <c>--detect</c> as a duration.</summary>
    /// <param name="text">The value as written; not null.</param>
    /// <returns>
    /// That many seconds, at most <see cref="MaxDetectDuration"/>; zero when the text is not a
    /// whole number above zero in the invariant culture.
    /// </returns>
    private static TimeSpan Seconds(string text)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || value <= 0)
        {
            return TimeSpan.Zero;
        }

        var wanted = TimeSpan.FromSeconds(value);
        return wanted > MaxDetectDuration ? MaxDetectDuration : wanted;
    }

    /// <summary>
    /// Whether an argument is one of the application's own switches, in any form the
    /// application reads it in; the comparison ignores case.
    /// </summary>
    /// <param name="argument">One command-line argument; not null.</param>
    /// <returns>
    /// <c>true</c> for <c>--preview</c>, <c>--restarted</c>, <c>--detect</c> and
    /// <c>--detect=</c> with a value.
    /// </returns>
    private static bool IsSwitch(string argument)
        => argument.Equals(Preview, StringComparison.OrdinalIgnoreCase)
           || argument.Equals(Restarted, StringComparison.OrdinalIgnoreCase)
           || argument.Equals(Detect, StringComparison.OrdinalIgnoreCase)
           || argument.StartsWith(Detect + "=", StringComparison.OrdinalIgnoreCase);
}
