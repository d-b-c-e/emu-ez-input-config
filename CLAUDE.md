# EmuEz Input Config - Claude Agent Instructions

## Project Context

A Windows desktop app that detects racing wheel inputs via DirectInput and writes correct emulator configurations automatically. Part of the [LaunchBox Racing Redux](https://github.com/d-b-c-e/LaunchboxRacingRedux) project.

## Tech Stack

- **C# / .NET 10** — Windows Forms application
- **SharpDX.DirectInput 4.2.0** — Raw DirectInput access for device enumeration, axis polling, button/hat detection
- **System.Text.Json** — Serialization for input-config.json

## Architecture

```
src/EmuEzInputConfig/
  Detection/
    DirectInputManager.cs    # SharpDX wrapper: device enumeration, acquisition, polling
    InputDetector.cs          # Baseline capture, noise measurement, debounced input detection
    DetectionWizard.cs        # 16 wizard step definitions (steering, gas, brake, etc.)
  ConfigWriters/
    IConfigWriter.cs          # Interface for per-emulator config writers
    Pcsx2ConfigWriter.cs      # PCSX2 [Pad1] section writer
    SupermodelConfigWriter.cs # Supermodel Input* line writer
    DevReorderDeployer.cs     # DevReorder INI + DLL deployment
  Models/
    InputMapping.cs           # InputConfig, InputMapping, DeviceInfo data models
  Util/
    IniEditor.cs              # Section-aware INI file editor
  Form1.cs                    # Main WinForms UI with wizard and axis visualization
```

## Key Technical Details

### DirectInput Axis Mapping
SharpDX axis names map to emulator-specific indices:

| SharpDX Property | DInput Index | PCSX2 Format | Supermodel Format |
|---|---|---|---|
| X | 0 | Axis0 | XAXIS |
| Y | 1 | Axis1 | YAXIS |
| Z | 2 | Axis2 | ZAXIS |
| RotationX | 3 | Axis3 | RXAXIS |
| RotationY | 4 | Axis4 | RYAXIS |
| RotationZ | 5 | Axis5 | RZAXIS |
| Sliders[0] | 6 | Axis6 | — |
| Sliders[1] | 7 | Axis7 | — |

### SharpDX Axis Range
All axes return values in range **0–65535** (midpoint = 32767).

### Button Indexing
- SharpDX/DInput: **0-indexed** (Button0, Button1, ...)
- Supermodel: **1-indexed** (BUTTON1, BUTTON2, ...)
- PCSX2: **0-indexed** (DInput-N/Button0, DInput-N/Button1, ...)

### Detection Engine
- **Noise filtering**: Measures per-axis jitter during 400ms calibration, sets threshold = max(16384, jitter * 3)
- **Debouncing**: Requires 3 consecutive polls above threshold before registering input
- **Re-baseline**: 1.5s cooldown between wizard steps for user to return to neutral
- **onPoll callback**: Real-time axis values fed to UI for visualization

### Moza R12 Known Mappings
- Steering: X axis (index 0)
- Gas: Z axis (index 2)
- Brake: RotationX axis (index 3)
- Handbrake: Sliders[0] (index 6)
- Gear Up: Button 13 (0-indexed)
- Gear Down: Button 12 (0-indexed)
- Start: Button 36 (0-indexed)
- Coin: Button 23 (0-indexed)
- View: Button 3 (0-indexed)

## Target Emulators

### Supported (Phase 1)
- **PCSX2** — Writes `[Pad1]` in `PCSX2.ini` + `RacingWheel.ini` profile
- **Supermodel** — Writes `Input*` keys in `Supermodel.ini`

### Planned
- MAME, DuckStation, Dolphin, Model 2, RPCS3, PPSSPP

## LaunchBox Integration

The app expects a LaunchBox root path to locate emulator config files:
- PCSX2: `{root}\Emulators\PCSX2\RunWizard=0\inis\PCSX2.ini`
- PCSX2 profile: `{root}\Emulators\PCSX2\RunWizard=0\inputprofiles\RacingWheel.ini`
- Supermodel: `{root}\Emulators\Sega Model 3\Config\Supermodel.ini`
- DevReorder source DLL: `{root}\Emulators\PCSX2\dinput8.dll`

## Building

```
dotnet build
dotnet run --project src/EmuEzInputConfig
```

## DevReorder

The app deploys DevReorder (portable DInput device ordering) to emulator directories:
- Generates `devreorder.ini` with `[order]` (shifter, wheel) and `[hidden]` (gamepad)
- Copies `dinput8.dll` from PCSX2 directory to other emulator directories
- Ensures stable DInput device indices regardless of which controllers are connected

## Working Style

- Read configs before modifying them
- Back up files before writing (IniEditor.BackupFile creates timestamped .bak copies)
- All paths relative to LaunchBox root for portability
- Test with actual hardware before shipping changes
