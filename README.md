# Mindfulness Reminder

A small break reminder for Windows. It sits in the notification area and, every time you
unlock the computer, shows a window that says **Ground yourself** on top of everything else.
While you work, it reminds you again every 30 minutes. If you are on a call or in a
full-screen app, it does not interrupt you. In **Settings...** you can turn either
reminder off, give each one its own message, and change the interval.

## Build and run

You need the .NET 9 SDK, 9.0.318 or a later 9.0.3xx (the version `global.json` asks for), and
the .NET 8 Desktop Runtime for x64, because the app runs on .NET 8.

From the repository folder:

```
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1 -Build
```

The script runs the tests first and builds nothing if one fails. Then it builds the app into
`artifacts\X.Y.Z\`, where `X.Y.Z` is the version it prints at the end, and writes
`MindfulnessReminder-X.Y.Z-win-x64.zip` and its `.sha256` next to that folder. It changes no
file of the repository, so you can run it again after a `git pull`.

Start `artifacts\X.Y.Z\MindfulnessReminder.exe`. On its first run it adds itself to Windows
startup.

Only one copy runs at a time. Close the running one with **Exit** in the tray menu before you
build again or start a new version; the script stops, and deletes nothing, while the app runs
from the folder it is about to replace. If the app runs at Windows startup, starting a copy
makes Windows start that copy from then on, so after a new version, or after moving the
folder, start it once by hand.

## Using it

- Right-click the tray icon for the menu: **Settings...**, **Run at Windows startup**,
  **About** and **Exit**.
- The app adds itself to Windows startup on its first run only. If you untick **Run at
  Windows startup**, it stays off.
- The reminder appears in the middle of the primary monitor, above every other window, with
  one **Close** button. Esc closes it too. Enter does not, so a reminder that pops up while
  you are typing is not dismissed by accident.
- Hover over the tray icon to see when the next reminder is due, for example
  `Mindfulness Reminder X.Y.Z - next in 12 min`, or that reminders are paused:
  `Mindfulness Reminder X.Y.Z - paused while your mic is on`.
- **Settings**, **About** and the tray menu are always dark. The reminder window is light, so
  it stands out over dark windows.
- If the app crashes after running for at least a minute, Windows starts it again, as long as
  Windows Error Reporting is on (it is by default). A dialog shows the error first, and the
  new copy shows a balloon: `Restarted after an error. The details are in the Windows
  Application log.` The error and its stack trace are in Event Viewer, under Windows Logs >
  Application, source `.NET Runtime`, event 1025.
- It keeps a log in `%LOCALAPPDATA%\MindfulnessReminder\log.txt`: one line for each start,
  lock and unlock, each reminder it showed, skipped or held back and why, and each change of
  settings. Open it in any text editor when a reminder did not appear when you expected it.
  The file and the older `log.1.txt` kept beside it never take much more than half a
  megabyte, and the log never leaves your computer.

## Two reminders, two messages

**Remind me when I unlock the computer** and **Remind me while I work** are separate
switches in **Settings...**, both on by default. Under each one is the **Message** that
reminder shows. Both start as *Ground yourself* and can be up to 50 characters long. A message
is greyed out while its reminder is off, and you cannot save an empty message for a reminder
that is on.

The two switches do not affect each other. With the unlock reminder off, unlocking still
restarts the countdown, so the next periodic reminder comes a full interval after you get
back. With the periodic reminder off, you still get the unlock reminder. The pause switches
described below apply to both.

A long message is drawn smaller instead of making the window bigger, so the reminder always
has the same size and position. To see how a message will look before you save it:

```
artifacts\X.Y.Z\MindfulnessReminder.exe --preview "Take a slow breath"
```

This shows the window once and exits. It does not touch the copy in the tray or your
settings.

## Periodic reminders

The periodic reminder is on by default and comes every 30 minutes. Its switch and the
interval are in **Settings...**; the interval is greyed out while the switch is off. You can
set anything from 1 to 480 minutes, and saving the settings starts the countdown again from
the beginning.

The countdown follows the clock, so sleep does not stretch it: a reminder that came due while
the computer was asleep shows up as soon as it wakes. Locking the computer stops the
countdown, and unlocking starts a full interval again. If 10 minutes were left when you
locked it and you come back three hours later, the next reminder is 30 minutes after you
unlock.

## Pausing for calls and full screen

Two switches in **Settings...**, both on by default, hold a reminder back instead of putting
it on top of what you are doing:

- **Pause while the microphone or camera is in use**: a call, a meeting, a recording.
- **Pause during full screen and presentations**: a game, a film, a shared screen, or
  Windows presentation mode.

A periodic reminder that is held back waits. The app looks again every 20 seconds, for as
long as it takes, and shows the reminder at the first look after the call or the full screen
ends. **Check again every N seconds** sets that gap, from 5 to 300; it is shared by both
switches and can be changed while either one is on. An unlock reminder that is held back is
skipped, not postponed.

While the periodic reminder is on, the tray tooltip shows when this is happening: within about
ten seconds of joining a call it reads
`Mindfulness Reminder X.Y.Z - paused while your mic is on` instead of the countdown, and the
countdown comes back just as quickly when the call ends. With the periodic reminder off, the
tooltip shows only the name and version, and an unlock reminder that was skipped leaves its
reason in the log.

### How it detects them

For the microphone and the camera, it reads the record Windows keeps for its own privacy
indicator, the microphone icon in the notification area: which apps have the microphone or the
camera open right now. That works with any app (Zoom, Teams in the app or in a browser, Slack
huddles, Meet, Webex, Discord), and it still works when you are muted, because these apps keep
the microphone open while muted.

For full screen, it asks Windows the question Windows asks itself before it shows a
notification: is a full-screen app, a game or a presentation in front?

### What it will and will not catch

Anything that keeps the microphone open counts, meeting or not: a voice channel left open,
dictation, screen recording. For as long as the microphone is open, a periodic reminder waits
and an unlock reminder is skipped.

It does not notice a meeting you only listen to, without ever opening your microphone.

### Checking it on your machine

This is hard to try without a real call or a real game, so the app can show you what it
detects:

```
artifacts\X.Y.Z\MindfulnessReminder.exe --detect
```

It opens a message box that lists the apps using the microphone or the camera, says what
Windows reports about full screen, and tells you whether a reminder would be held back with
your current settings.

To check full screen, add a number of seconds:

```
artifacts\X.Y.Z\MindfulnessReminder.exe --detect 60
```

A single reading cannot catch full screen, because typing the command means leaving it. With
a number, it takes one reading a second for that long, up to ten minutes. Start it, switch to
the game and play for a while. When the time is up, a message box shows what it saw:

```
Watched for 60 seconds.

0:01 - 0:04   mic off, camera off, Normal   ->  None
0:05 - 0:48   mic off, camera off, FullScreen   ->  FullScreen
0:49 - 1:00   mic off, camera off, Normal   ->  None

A reminder would have been held back for 44 of the 60 readings.

Mindfulness Reminder X.Y.Z
```

The message box appears only at the end, so nothing gets in the way while it measures. Both
`--detect` and `--preview` can run while the app is in the tray.

## Licence

Mindfulness Reminder: a break reminder for Windows.
Copyright (C) 2026 Tudor Carean

This program is free software: you can redistribute it and/or modify it under the terms of
the **GNU General Public License v3** as published by the Free Software Foundation, either
version 3 of the License, or (at your option) any later version.

It is distributed in the hope that it will be useful, but **WITHOUT ANY WARRANTY**; without
even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
General Public License for more details.

The full text is in [LICENSE](LICENSE) and at <https://www.gnu.org/licenses/gpl-3.0.html>.
**About**, in the tray menu, shows the notice in short, and every release has a copy of
`LICENSE` next to the executable.
