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
    DuckStationConfigWriter.cs # DuckStation [Pad1] NeGcon writer
    PpssppConfigWriter.cs     # PPSSPP [ControlMapping] device-code writer
    Rpcs3ConfigWriter.cs      # RPCS3 YAML MMJoystick writer
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

| SharpDX Property | DInput Index | PCSX2 Format | Supermodel Format | DuckStation | PPSSPP Axis ID | RPCS3 Name |
|---|---|---|---|---|---|---|
| X | 0 | Axis0 | XAXIS | Axis0 | 0 (4000/4001) | X |
| Y | 1 | Axis1 | YAXIS | Axis1 | 1 (4002/4003) | Y |
| Z | 2 | Axis2 | ZAXIS | Axis2 | 11 (4022/4023) | Z |
| RotationX | 3 | Axis3 | RXAXIS | Axis3 | 12 (4024/4025) | RX |
| RotationY | 4 | Axis4 | RYAXIS | Axis4 | 13 (4026/4027) | RY |
| RotationZ | 5 | Axis5 | RZAXIS | Axis5 | 14 (4028/4029) | RZ |
| Sliders[0] | 6 | Axis6 | — | Axis6 | — | — |
| Sliders[1] | 7 | Axis7 | — | Axis7 | — | — |

### SharpDX Axis Range
All axes return values in range **0–65535** (midpoint = 32767).

### Button Indexing
- SharpDX/DInput: **0-indexed** (Button0, Button1, ...)
- PCSX2: **0-indexed** (DInput-N/Button0, DInput-N/Button1, ...)
- DuckStation: **0-indexed** (DInput-N/Button0, DInput-N/Button1, ...)
- Supermodel: **1-indexed** (BUTTON1, BUTTON2, ...)
- PPSSPP: **code-based** (188 + buttonIndex, so Button0 → code 188)
- RPCS3: **1-indexed** (Button 1, Button 2, ...)

### Detection Engine
- **Noise filtering**: Measures per-axis jitter during 400ms calibration, sets threshold = max(6000, jitter * 3)
- **Debouncing**: Requires 3 consecutive polls above threshold before registering input
- **Re-baseline**: 3s cooldown between wizard steps for user to return to neutral
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

### Supported
- **PCSX2** — Writes `[Pad1]` in `PCSX2.ini` + `RacingWheel.ini` profile (DualShock2)
- **Supermodel** — Writes `Input*` keys in `Supermodel.ini`
- **DuckStation** — Writes `[Pad1]` in `settings.ini` (NeGcon controller for analog racing)
- **PPSSPP** — Writes `[ControlMapping]` in `controls.ini` (device-code pairs)
- **RPCS3** — Writes full YAML `Default.yml` (MMJoystick handler)

### Planned
- MAME, Dolphin, Model 2

## LaunchBox Integration

**Test build:** `H:\RDriveRedux\Launchbox-Racing`

The app expects a LaunchBox root path to locate emulator config files:
- PCSX2: `{root}\Emulators\PCSX2\RunWizard=0\inis\PCSX2.ini`
- PCSX2 profile: `{root}\Emulators\PCSX2\RunWizard=0\inputprofiles\RacingWheel.ini`
- Supermodel: `{root}\Emulators\Sega Model 3\Config\Supermodel.ini`
- DuckStation: `{root}\Emulators\DuckStation\settings.ini`
- PPSSPP: `{root}\Emulators\ppsspp\memstick\PSP\SYSTEM\controls.ini`
- RPCS3: `{root}\Emulators\rpcs3\config\input_configs\global\Default.yml`
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
- Targets: PCSX2, Supermodel, DuckStation, PPSSPP, RPCS3
- Ensures stable DInput device indices regardless of which controllers are connected

## Emulator-Specific Notes

### DuckStation (PS1)
Uses **NeGcon** controller type — a PS1 racing controller with analog steering, gas (I), and brake (II). This is preferred over AnalogController/DualShock for racing games because it provides true analog gas/brake. Binding format: `DInput-N/+AxisM`, `DInput-N/ButtonM`, `DInput-N/Hat0 Dir`.

### PPSSPP (PSP)
Uses **device-code pairs** in `[ControlMapping]`: `{deviceId}-{keyCode}`. Device ID = `10 + DInputIndex`. Button codes = `188 + buttonIndex`. Axis codes = `4000 + androidAxisId * 2` (positive) / `+1` (negative). Supports comma-separated multi-binding per key. PSP has no analog triggers; gas/brake map to Cross/Square buttons using axis codes.

### RPCS3 (PS3)
Uses **YAML** with **MMJoystick** handler (DirectInput). Device = `"Joystick #{DInputIndex + 1}"` (1-indexed). Buttons are 1-indexed: `"Button N"`. Axes use format `"+Axis X+"` / `"-Axis X-"`. Gas/brake dual-map to both triggers (R2/L2) and face buttons (Cross/Square) for broad game compatibility.

## Working Style

- Read configs before modifying them
- Back up files before writing (IniEditor.BackupFile creates timestamped .bak copies)
- All paths relative to LaunchBox root for portability
- Test with actual hardware before shipping changes
