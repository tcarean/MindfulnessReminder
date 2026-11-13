# 0089. The text boxes of Settings get a dark context menu of the theme's own

Date: 2026-11-13
Status: accepted 2026-11-13

## Context

The review of 2026-11-13 found (CR-08) that a right-click in a text box of Settings opens WPF's
Cut, Copy and Paste menu drawn with the stock light templates over the dark window, against
R-075. It named two ways out, a dark menu or no menu, and left the choice to the owner, who
chose the dark menu on 2026-11-13.

Measured that day with a probe that merges `DarkTheme.xaml` into a window's resources, as
`SettingsWindow` does, opens the menu of a `TextBox` and reads it (Windows PowerShell's WPF,
.NET Framework 4):

| The theme | The menu's type | Its background |
| --- | --- | --- |
| as it was | `EditorContextMenu` | `#F5F5F5` |
| with implicit `ContextMenu` and `MenuItem` styles added | `EditorContextMenu` | `#F5F5F5` |
| with a `ContextMenu` of its own set by the `TextBox` style | `ContextMenu` | `#2A302E` |

So the menu WPF builds for a box that has none is not reached by implicit styles in the
window's resources. Why was not isolated: its type is a class of WPF's own, not `ContextMenu`
itself, and whether that or the place the styles sit in is the cause was not separated.

## Decision

`DarkTheme.xaml` holds a `ContextMenu` of its own, `TextBoxContextMenu`, with Cut, Copy and
Paste on `ApplicationCommands`, and the implicit `TextBox` style sets it on every text box of a
window that merges the theme. Its two templates, `DarkContextMenuStyle` and
`DarkMenuItemStyle`, are keyed, use the palette already in the file, and each carries its
disabled trigger (ADR 0065). Each item names the box the menu was opened on as its command's
target.

## Alternatives rejected

- **No context menu on these boxes.** One setter, and the keyboard shortcuts still work; it
  takes away pasting with the mouse, which the owner chose to keep.
- **Implicit `ContextMenu` and `MenuItem` styles, leaving WPF's menu in place.** Measured: they
  do not reach it.
- **Merging the theme into `Application.Resources` so that the styles might reach it.** Not
  tried: ADR 0064 forbids it, since the reminder would be repainted.
- **Items without a command target.** Measured in a window that was not the active one: Cut
  and Copy stayed disabled with text selected in the box; with the target they were enabled.

## Consequences

- The three captions are the theme's, in English (`Cut`, `Copy`, `Paste`, with the access keys
  of WPF's menu), where WPF's own come from its resources.
- The menu is one object per window, shared by its text boxes; opened on one, its commands go
  to that one.
- Anything WPF's menu would add for a box (a spelling suggestion, were spell checking ever
  switched on) is not in this one.
- Guards: none by test (ADR 0042). Measured on the worktree build's `SettingsWindow` itself,
  on .NET 8, opened by a throwaway program with a save function that does nothing: all four
  boxes carry the menu; background `#2A302E`, border `#4E5854`; with nothing selected the
  three items are disabled and grey (`#5E6A66`); with the text selected Cut and Copy are
  enabled (`#E6EAE8`) and the highlighted item is drawn on `#343B39`. Paste was disabled in
  every run, in WPF's menu as well; what the clipboard held was not looked at, so an enabled
  Paste was not seen. The menu was opened by the program, not by a mouse or the keyboard, and
  no command was run. Still to be looked at in the tray copy: a right-click in a message box
  and in a number box, Shift+F10, and a Paste.
