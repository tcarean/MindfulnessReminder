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
/// What Save in the Settings window does with the settings it read: they are written to the
/// store and then applied, and a store that refuses the write stops both (R-044, R-046).
/// </summary>
public static class SettingsCommit
{
    /// <summary>
    /// Writes <paramref name="settings"/> to <paramref name="store"/> and, once written, hands
    /// them to <paramref name="apply"/>. An exception that is not a refused write is not
    /// caught, and nothing is applied then either.
    /// </summary>
    /// <param name="store">Where the settings are kept between runs; not null.</param>
    /// <param name="settings">The settings to keep and apply; not null.</param>
    /// <param name="apply">
    /// Puts the settings in force, as <see cref="ReminderCoordinator.ApplySettings"/> does;
    /// not null. Called only after the write succeeded.
    /// </param>
    /// <param name="log">Where a refusal is written (R-046); <c>null</c> keeps none.</param>
    /// <returns>
    /// <c>true</c> when the settings were written and applied; <c>false</c> when the store
    /// refused the write (<see cref="RegistryRefusal.IsWriteRefusal"/>), in which case nothing
    /// was applied.
    /// </returns>
    public static bool TrySaveAndApply(
        ISettingsStore store,
        ReminderSettings settings,
        Action<ReminderSettings> apply,
        IDecisionLog? log = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(apply);

        // Saved first: settings that were applied and not stored would be contradicted by the
        // next start, which reads the store.
        try
        {
            store.Save(settings);
        }
        catch (Exception ex) when (RegistryRefusal.IsWriteRefusal(ex))
        {
            // A key this account may not write (a policy, security software) is not a reason
            // to end the application: the caller tells the user, and the settings in force
            // stay as they were.
            (log ?? NullDecisionLog.Instance).Write(DecisionLog.SettingsSaveRefused(ex));
            return false;
        }

        apply(settings);
        return true;
    }
}
