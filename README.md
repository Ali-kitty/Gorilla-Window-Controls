# Gorilla Window Controls

A BepInEx plugin for **Gorilla Tag** that gives you full control over the game window. Change resolution, toggle fullscreen, move the game to a different monitor, and apply custom window sizes — all from an in-game GUI or automatically on launch.

Perfect for streamers, multi-monitor setups, and anyone who wants Gorilla Tag to behave exactly how they want it to.

## What it's used for

- **Streaming / recording** — resize the game to fit your overlay layout or capture a specific region (e.g. the OBS preset crops the game to 1920x1111 for widescreen recordings with room for a camera bar)
- **Multi-monitor setups** — move Gorilla Tag to your secondary monitor while you browse, stream, or work on the main screen
- **Performance tuning** — run the game at a lower resolution for smoother gameplay, or force a specific resolution that your monitor handles best
- **Borderless fullscreen** — get true fullscreen without titlebars or borders for a cleaner look
- **Quick switching** — swap between resolutions and displays on the fly without restarting the game

## Features

- **Custom resolution** — set any width and height you want
- **Fullscreen toggle** — switch between borderless fullscreen and windowed mode
- **Multi-monitor support** — pick which display the game opens on
- **Presets** — quick buttons for 1920x1080, 3440x1440, and OBS (1920x1111)
- **Auto-apply on launch** — your settings load as soon as the game starts
- **Persistent config** — settings save to a BepInEx config file between sessions
- **F8 GUI** — press F8 to open and close the control panel in-game

## How it works

The mod hooks into the Windows API (`user32.dll`) to directly resize and reposition the game window. When you hit Apply, it finds the Gorilla Tag window, adjusts its style (border, titlebar, popup mode), moves it to your chosen display, and sets the resolution via Unity's `Screen.SetResolution`. All settings are stored in BepInEx's config system so they persist across launches.

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for Gorilla Tag (win-x64)
2. Drop `GorillaWindowController.dll` into `BepInEx/plugins/`
3. Launch the game — press **F8** to open the window controller GUI

## Usage

- Press **F8** to open/close the control panel
- Enter width/height or click a preset
- Toggle fullscreen on/off
- Select a display (if you have multiple monitors)
- Click **Apply** to apply changes

Settings are saved to `BepInEx/config/com.ali.gorillawindowcontroller.cfg`.

## Adding custom resolution presets

The presets are hardcoded as buttons in `Plugin.cs` around line 144. To add your own:

1. Open `Plugin.cs`
2. Find the preset buttons section (starts with `GUILayout.Label("Presets")`)
3. Copy an existing button block and change the values:

```csharp
if (GUILayout.Button("2560x1440"))
{
    widthText = "2560"; heightText = "1440";
    ApplySettings();
}
```

You can add as many as you want. Rebuild the DLL and replace it in `BepInEx/plugins/`.

---

*This product is not affiliated with Another Axiom Inc. or its videogames Gorilla Tag and Orion Drift and is not endorsed or otherwise sponsored by Another Axiom. Portions of the materials contained herein are property of Another Axiom. ©2021 Another Axiom Inc.*
