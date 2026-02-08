# Hotkii

A Windows 11 tray application that registers global hotkeys for switching virtual desktops, changing audio output devices, and focusing application windows.

Runs in the system tray with no visible window. Right-click the tray icon to exit.

![Hotkii tray icon](pics/hotkii_tray_shot.png)

## Requirements

- Windows 11 22H2 or later. Earlier versions probably will not work, and maybe later versions will not work either when Microsoft breaks something.
- [.NET 10.0 **Desktop** Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (x64) — must be the "Desktop Runtime", not the regular runtime


## Building

Requires .NET 10.0 SDK. The build targets `win-x64`.

```
dotnet publish -c Release
```

The output is in `bin/Release/net10.0-windows/win-x64/publish/`. Copy the entire `publish` directory to the Windows machine and run `Hotkii.exe`.

The project can be built from Linux or Windows.  This probably sounds ridiculous, but I haven't actually tried building this on Windows.


## Configuration

On first run, a `config.json` is created next to the executable with default bindings. Edit this file to change hotkeys. Comments and trailing commas are allowed.

Each hotkey entry has:

```json
{
  "key": "F13",
  "modifiers": [],
  "action": "switch-desktop",
  "args": { "desktop": 1 }
}
```

- `key` — a key name from the [System.Windows.Forms.Keys](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.keys) enum (e.g. `F13`, `A`, `D1` for the `1` key)
- `modifiers` — array of `Ctrl`, `Alt`, `Shift`, `Win`
- `action` — one of the actions listed below
- `args` — action-specific arguments

Some key combinations involving `Win` are reserved by Windows and will fail to register. `Ctrl+Alt` combinations are the safest for custom shortcuts.


## Actions

### switch-desktop

Switch to a virtual desktop by number.

| Arg | Type | Description |
|---|---|---|
| `desktop` | int | Desktop number, starting from 1 |

### audio-set-device

Set the default audio output device.

| Arg | Type | Description |
|---|---|---|
| `device` | string | Partial device name (case-insensitive) |

### audio-cycle-devices

Cycle through a list of audio output devices. Each press switches to the next available device in the list. Handy for switching between Speakers and Headphones with a single hotkey, for example.

| Arg | Type | Description |
|---|---|---|
| `devices` | string[] | List of partial device names |

### focus-app

Bring a running application's window to the foreground, but only if it has a window on the current virtual desktop. Consecutive presses cycle through multiple windows of the same application.

| Arg | Type | Description |
|---|---|---|
| `process` | string | Process name without `.exe` |


## Startup output

Hotkii logs to the console it was launched from. This includes the list of detected audio devices, registered hotkeys, and any errors. Run it from a terminal to see this output.
