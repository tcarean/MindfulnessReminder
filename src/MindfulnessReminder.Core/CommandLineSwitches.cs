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
    /// Finds the text <c>--preview</c> was given: the first argument after the switch that is
    /// not one of the application's own switches, nor the value that follows a bare
    /// <c>--detect</c> (R-045). Windows adds <c>--restarted</c> to a copy it starts again, and
    /// a switch is never what the reminder should say (R-102).
    /// </summary>
    /// <param name="args">The command-line arguments, without the program's name; not null.</param>
    /// <returns>
    /// The text as written, not normalized; it can be empty. <c>null</c> when the switch is not
    /// among the arguments or nothing but switches and their values follows it; the comparison
    /// ignores case.
    /// </returns>
    public static string? PreviewText(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var afterSwitch = false;
        var isDetectValue = false;

        foreach (var argument in args)
        {
            if (!afterSwitch)
            {
                afterSwitch = argument.Equals(Preview, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            // "--detect 30": the argument after the bare switch is its number of seconds,
            // whatever it is, as App reads it. "--detect=30" carries its value with it.
            if (isDetectValue)
            {
                isDetectValue = false;
                continue;
            }

            if (!IsSwitch(argument))
            {
                return argument;
            }

            isDetectValue = argument.Equals(Detect, StringComparison.OrdinalIgnoreCase);
        }

        return null;
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
