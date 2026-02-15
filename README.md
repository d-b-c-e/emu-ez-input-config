# EmuEz Input Config

A Windows desktop app that detects racing wheel inputs via DirectInput and writes correct emulator configurations automatically.

## Why?

Racing wheel setups involve multiple devices (wheel base, shifter, pedals) and multiple emulators (PCSX2, Supermodel, MAME, DuckStation, etc.), each with its own config format. Mapping inputs manually is tedious and error-prone.

**EmuEz Input Config** solves this with a guided wizard:
1. Detects all connected DirectInput devices with real names (not "Microsoft PC-joystick driver")
2. Walks you through each input: steering, gas, brake, gear paddles, d-pad, face buttons
3. Shows real-time axis visualization so you can see exactly what's happening
4. Configurable hotkeys tab — customize exit, fast forward, save/load state keys before writing
5. Writes correct configs for every supported emulator in one click
6. Deploys DevReorder (portable DInput device ordering) to keep device indices stable

## Supported Emulators

- **PCSX2** (PlayStation 2) — `[Pad1]` DualShock2 in PCSX2.ini + RacingWheel.ini profile
- **Supermodel** (Sega Model 3) — `Input*` keys in Supermodel.ini
- **DuckStation** (PlayStation 1) — `[Pad1]` NeGcon (analog racing controller) in settings.ini
- **PPSSPP** (PSP) — `[ControlMapping]` device-code pairs in controls.ini
- **RPCS3** (PlayStation 3) — YAML MMJoystick config in Default.yml
- **MAME** (Arcade) — XML ctrlr file + mame.ini settings

Planned: Dolphin, Model 2, RetroArch.

## Requirements

- Windows 10/11
- .NET 10 Runtime (or later)
- A DirectInput-compatible racing wheel

## Building

```
dotnet build
```

## Usage

```
dotnet run --project src/EmuEzInputConfig
```

Or build and run the .exe from `bin/Debug/net10.0-windows/`.

Set the LaunchBox root path, click **Start Detection**, follow the prompts, then click **Write Configs**.

## Architecture

```
src/EmuEzInputConfig/
  Detection/          # DirectInput device enumeration, noise filtering, input detection
  ConfigWriters/      # Per-emulator config writers (PCSX2, Supermodel, DuckStation, PPSSPP, RPCS3, MAME)
  Models/             # Data models (InputConfig, InputMapping, DeviceInfo, HotkeyConfig)
  Util/               # INI file editor, hotkey format translators
  Form1.cs            # Main WinForms UI — tabbed (Input Detection + Hotkeys)
```

## Related

Part of the [LaunchBox Racing Redux](https://github.com/d-b-c-e/LaunchboxRacingRedux) project — a portable, curated racing game cabinet setup.
