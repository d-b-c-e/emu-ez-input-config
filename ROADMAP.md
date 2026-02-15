# Roadmap

## Automated Testing

### Tier 1: Config Writer Unit Tests (Highest Priority)

**Effort:** 1-2 sessions | **CI:** Yes — runs on any `windows-latest` runner

The config writers are pure functions: `InputConfig` in, bindings out. They have zero hardware dependencies and are the most likely source of real bugs (wrong axis index, off-by-one button numbering, wrong format string).

#### Project Setup

```
tests/EmuEzInputConfig.Tests/
  ConfigWriters/
    Pcsx2ConfigWriterTests.cs
    SupermodelConfigWriterTests.cs
    DuckStationConfigWriterTests.cs
    PpssppConfigWriterTests.cs
    Rpcs3ConfigWriterTests.cs
    MameConfigWriterTests.cs
    DevReorderDeployerTests.cs
  Util/
    IniEditorTests.cs
    HotkeyFormatterTests.cs
  TestHelpers/
    InputConfigBuilder.cs       # Fluent builder for test configs (Moza R12 defaults)
```

Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`

#### What to Test

**Per-emulator binding correctness (using Moza R12 known mappings as reference):**

| Test Case | What It Catches |
|---|---|
| Steering axis → correct format per emulator | Wrong axis index, wrong direction sign |
| Gas/brake axes → correct format | FullAxis vs +Axis confusion (DuckStation), wrong axis ID |
| Button indexing (0 vs 1 indexed) | Off-by-one: PCSX2/DuckStation=0-indexed, Supermodel/RPCS3/MAME=1-indexed |
| Device numbering | `DInput-1` vs `JOY2` vs `Joystick #2` vs device `11` |
| Hat/D-pad format | `Hat0Up` vs `HAT1UP` vs `NKCODE_DPAD_UP` |
| Skipped mapping → empty string | No crash on missing optional inputs |
| MAME XML structure validity | Valid XML with correct `<mapdevice>` and `<port>` elements |
| RPCS3 YAML structure | Valid YAML, correct indentation, handler = MMJoystick |
| PPSSPP multi-binding | Comma-separated codes, no duplicates |
| Hotkey format strings | Qt format (`Keyboard/F5`), PPSSPP format (`1-135`), modifier combos |

**INI/XML/YAML round-trip tests:**

| Test Case | What It Catches |
|---|---|
| `IniEditor.UpdateSection` preserves other sections | Overwriting unrelated config |
| `IniEditor.UpdateSection` appends missing keys | Silent key drops |
| `IniEditor.BackupFile` creates timestamped `.bak` | Backup failures |
| MAME `UpdateMameIni` preserves comments and structure | Flat-format line corruption |
| DevReorder INI generation | Wrong device names, missing `[hidden]` section |

**Example test pattern:**

```csharp
[Fact]
public void Pcsx2Writer_MozaR12_CorrectSteeringBinding()
{
    var config = new InputConfig
    {
        Wheel = new DeviceInfo { ProductName = "MOZA Racing R12", DInputIndex = 1 },
        Mappings = new()
        {
            ["steering"] = new() { Type = "axis", Axis = "X", Direction = "+" },
            ["gas"] = new() { Type = "axis", Axis = "Z", Direction = "+" },
            ["brake"] = new() { Type = "axis", Axis = "RotationX", Direction = "+" },
            ["gearUp"] = new() { Type = "button", ButtonIndex = 13 },
            ["gearDown"] = new() { Type = "button", ButtonIndex = 12 },
        },
    };

    var bindings = new Pcsx2ConfigWriter().GenerateBindings(config);

    Assert.Equal("DInput-1/-Axis0", bindings["LLeft"]);
    Assert.Equal("DInput-1/+Axis0", bindings["LRight"]);
    Assert.Equal("DInput-1/+Axis2", bindings["R2"]);       // Gas
    Assert.Equal("DInput-1/+Axis3", bindings["L2"]);       // Brake
    Assert.Equal("DInput-1/Button13", bindings["R1"]);     // Gear up (0-indexed)
    Assert.Equal("DInput-1/Button12", bindings["L1"]);     // Gear down (0-indexed)
}

[Fact]
public void MameWriter_MozaR12_CorrectButtonIndexing()
{
    // MAME is 1-indexed: button 13 → JOYCODE_1_BUTTON14
    var config = BuildMozaR12Config();
    var bindings = new MameConfigWriter().GenerateBindings(config);

    Assert.Equal("JOYCODE_1_BUTTON14", bindings["P1_BUTTON1"]);  // gearUp: 13+1
    Assert.Equal("JOYCODE_1_BUTTON13", bindings["P1_BUTTON2"]);  // gearDown: 12+1
    Assert.Equal("JOYCODE_1_XAXIS", bindings["P1_PADDLE"]);
}
```

#### CI Pipeline

```yaml
# .github/workflows/test.yml
name: Tests
on: [push, pull_request]
jobs:
  unit-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test tests/EmuEzInputConfig.Tests/
```

---

### Tier 2: Virtual Device Integration Tests

**Effort:** 3-5 sessions | **CI:** Self-hosted runner only (requires kernel driver)

Uses **vJoy** to create real DirectInput devices visible to SharpDX, then drives the detection engine end-to-end without physical hardware.

#### vJoy Overview

[vJoy](https://github.com/shauleiz/vJoy) is a virtual joystick driver for Windows that creates kernel-level HID devices. It supports up to 16 virtual devices, each with 8 axes (X, Y, Z, RX, RY, RZ, Slider0, Slider1), 128 buttons, and 4 POV hat switches — matching the exact axis set our app uses.

**Key detail:** vJoy's internal range is 0-32767, but DirectInput scales it to 0-65535 when reporting to consumers. Our `InputDetector` (which expects 0-65535 with midpoint 32767) works correctly with vJoy devices without modification.

| SharpDX Property | vJoy Axis Constant | Compatible |
|---|---|---|
| X | HID_USAGE_X | Yes |
| Y | HID_USAGE_Y | Yes |
| Z | HID_USAGE_Z | Yes |
| RotationX | HID_USAGE_RX | Yes |
| RotationY | HID_USAGE_RY | Yes |
| RotationZ | HID_USAGE_RZ | Yes |
| Sliders[0] | HID_USAGE_SL0 | Yes |
| Sliders[1] | HID_USAGE_SL1 | Yes |

#### C# API

NuGet package: `vJoy.Wrapper`

```csharp
using vJoy.Wrapper;

var joystick = new VirtualJoystick(1);
joystick.Aquire();

// Simulate steering full left (X axis = 0)
joystick.SetJoystickAxis(0, Axis.HID_USAGE_X);

// Simulate gas pedal (Z axis = 32767 = full press)
joystick.SetJoystickAxis(32767, Axis.HID_USAGE_Z);

// Simulate button 13 press (gear up paddle)
joystick.SetJoystickButton(true, 14);  // vJoy is 1-indexed
joystick.SetJoystickButton(false, 14);

joystick.Release();
```

#### Test Flow

```
1. Install vJoy, configure 1 device with all 8 axes + 128 buttons + 4 hats
2. Create VirtualJoystick via vJoy.Wrapper
3. Instantiate DirectInputManager + InputDetector
4. Call CaptureBaselines() — all axes at center (16383 in vJoy = 32767 in DInput)
5. Set vJoy X axis to 0 (full left deflection)
6. Call WaitForInput() — should detect axis X change
7. Assert: returned InputMapping.Axis == "X", Direction == "-"
8. Repeat for gas (Z axis), brake (RX axis), buttons, hats
9. Verify noise filtering: small axis jitter (±200) should NOT trigger detection
10. Verify debouncing: axis change for only 1 poll should NOT register
```

#### What This Covers

- `DirectInputManager.EnumerateDevices()` finds virtual devices
- `InputDetector.CaptureBaselines()` reads correct initial values
- `InputDetector.MeasureNoise()` calculates reasonable thresholds
- `InputDetector.WaitForInput()` detects axis, button, and hat changes
- Noise filtering rejects sub-threshold jitter
- Debouncing requires 3 consecutive polls above threshold
- Re-baseline captures new neutral position after input

#### Alternatives Considered

| Tool | Verdict | Why |
|---|---|---|
| **vJoy** | **Use this** | Kernel-level HID, exact axis match, C# wrapper, widely used |
| ViGEmBus | Not suitable | Only emulates Xbox 360 / DS4 (XInput), not arbitrary DInput joysticks. Project retired Nov 2023. |
| Microsoft VHF | Overkill | Requires writing a kernel-mode driver. vJoy already does this. |

---

### Tier 3: Emulator Launch Validation (Not Recommended)

**Effort:** 1-2 weeks | **CI:** Not practical | **ROI:** Low

This tier would launch actual emulators with written configs and verify they respond to virtual input. Research findings:

| Emulator | Headless Mode | Command |
|---|---|---|
| PCSX2 | Yes | `pcsx2-qt.exe -batch -nogui game.iso` |
| MAME | Partial | `mame.exe game -str 5 -nothrottle` (auto-exits after N seconds) |
| DuckStation | Yes | `duckstation-qt-x64.exe -batch game.bin` |
| RPCS3 | Yes | `rpcs3.exe --no-gui game/` |
| PPSSPP | Limited | No documented headless flag |
| Supermodel | No | No headless mode |

**Why skip:** The failure mode this catches — "config is syntactically valid but emulator misinterprets it" — is rare and varies by emulator version. It requires emulator binaries, ROM/ELF files, GPU drivers, screenshot comparison infrastructure, and significant maintenance. Manual testing with real hardware is faster and more reliable for this specific validation.

**Possible lightweight alternative:** Launch emulators with `-str` (MAME) or `-batch` flags, check exit codes for config parse failures. No input verification, just "does it load without crashing." Low effort, narrow coverage.

---

### Architectural Refactoring for Testability

Currently `Form1.cs` mixes UI and business logic. For better test coverage:

1. **Extract `IInputDetector` interface** from `InputDetector` to allow mocking in tests
2. **Extract `WizardController` class** that orchestrates detection steps and produces an `InputConfig` — testable without UI
3. **Form becomes a thin view** binding to the controller

This would enable testing wizard logic (step progression, skip handling, timeout, baseline recapture) without WinForms.

**WinForms UI testing** is possible with [FlaUI](https://github.com/FlaUI/FlaUI) (use `FlaUI.UIA2` for WinForms), but the ROI is low unless the UI changes frequently. Prefer testing business logic directly.

---

### ROI Summary

| Strategy | Effort | Bug Coverage | CI-Friendly | Priority |
|---|---|---|---|---|
| Config writer unit tests | Low | High — axis mapping, indexing, format | Yes | **P0 — Do first** |
| INI/XML/YAML round-trip tests | Low | Medium — file writing correctness | Yes | **P0 — Do first** |
| Hotkey formatter tests | Low | Medium — key translation correctness | Yes | **P0 — Do first** |
| vJoy detection integration tests | Medium | Medium — detection engine | Self-hosted only | **P1 — Do second** |
| WizardController extraction + tests | Medium | Medium — wizard orchestration | Yes | **P2 — When refactoring** |
| FlaUI UI tests | Medium | Low — UI-specific bugs | Self-hosted only | **P3 — Optional** |
| Emulator launch validation | High | Low — config load issues | No | **Skip** |

**Bottom line:** Tier 1 tests catch the majority of real bugs and run everywhere. Tier 2 with vJoy is a strong follow-up that eliminates the need for physical hardware during development. Manual testing with a real wheel remains essential for the final in-game validation.

---

## Planned Emulators

### Dolphin (GameCube / Wii)

- Config format: INI-based `GCPadNew.ini` in `User/Config/`
- Binding format: `Device = DInput/0/{DeviceName}`, axes as `Axis {N}+` / `Axis {N}-`
- Supports analog triggers, full axis range

### Model 2 Emulator (Sega Model 2)

- Config format: Binary `.input` files (68-116 bytes per game), undocumented
- **Blocker:** No programmatic way to generate these without reverse-engineering the binary format
- May require file-level hex templates or contributions to the emulator project
- **Status:** Deferred until binary format is documented

### RetroArch

- Config format: `autoconfig/dinput/{DeviceName}.cfg`
- Binding format: Axes as `"+0"` / `"-0"` (string-quoted), buttons as plain 0-indexed numbers, hats as `h0up/h0down/h0left/h0right`
- Generates per-device autoconfig profiles
- Also supports `retroarch.cfg` for global input overrides
