namespace EmuEzInputConfig.ConfigWriters;

using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;

/// <summary>
/// Writes PCSX2 [Pad1] and [InputSources] sections.
/// Target files:
///   - Emulators\PCSX2\RunWizard=0\inis\PCSX2.ini
///   - Emulators\PCSX2\RunWizard=0\inputprofiles\RacingWheel.ini
/// </summary>
public class Pcsx2ConfigWriter : IConfigWriter
{
    public string EmulatorName => "PCSX2";

    // DInput axis name → PCSX2 axis index
    private static readonly Dictionary<string, int> AxisIndexMap = new()
    {
        ["X"] = 0, ["Y"] = 1, ["Z"] = 2,
        ["RotationX"] = 3, ["RotationY"] = 4, ["RotationZ"] = 5,
        ["Slider0"] = 6, ["Slider1"] = 7,
    };

    private static int? GetAxisIndex(InputMapping? m)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        return AxisIndexMap.GetValueOrDefault(m.Axis, -1) is var idx && idx >= 0 ? idx : null;
    }

    private static int? GetButtonIndex(InputMapping? m)
    {
        if (m?.Type != "button") return null;
        return m.ButtonIndex;
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string ini = Path.Combine(launchboxRoot, @"Emulators\PCSX2\RunWizard=0\inis\PCSX2.ini");
        return File.Exists(ini);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;
        int dinputIdx = config.Wheel?.DInputIndex ?? 1;
        string dev = $"DInput-{dinputIdx}";

        int? steerIdx = GetAxisIndex(m.GetValueOrDefault("steering"));
        int? gasIdx = GetAxisIndex(m.GetValueOrDefault("gas"));
        int? brakeIdx = GetAxisIndex(m.GetValueOrDefault("brake"));

        int? gearUpBtn = GetButtonIndex(m.GetValueOrDefault("gearUp"));
        int? gearDownBtn = GetButtonIndex(m.GetValueOrDefault("gearDown"));
        int? startBtn = GetButtonIndex(m.GetValueOrDefault("start"));
        int? coinBtn = GetButtonIndex(m.GetValueOrDefault("coin"));

        int? btnA = GetButtonIndex(m.GetValueOrDefault("btnA"));
        int? btnB = GetButtonIndex(m.GetValueOrDefault("btnB"));
        int? btnX = GetButtonIndex(m.GetValueOrDefault("btnX"));
        int? btnY = GetButtonIndex(m.GetValueOrDefault("btnY"));

        string GetDpad(string stepName, string hatDir)
        {
            var mapping = m.GetValueOrDefault(stepName);
            if (mapping?.Type == "hat") return $"{dev}/Hat0{hatDir}";
            if (mapping?.Type == "button" && mapping.ButtonIndex.HasValue)
                return $"{dev}/Button{mapping.ButtonIndex}";
            return "";
        }

        var pad = new Dictionary<string, string>
        {
            ["Type"] = "DualShock2",
            ["InvertL"] = "0",
            ["InvertR"] = "0",
            ["Deadzone"] = "0",
            ["AxisScale"] = "1.33",
            ["LargeMotorScale"] = "1",
            ["SmallMotorScale"] = "1",
            ["ButtonDeadzone"] = "0",
            ["PressureModifier"] = "0.5",

            // D-Pad
            ["Up"] = GetDpad("dpadUp", "Up"),
            ["Right"] = GetDpad("dpadRight", "Right"),
            ["Down"] = GetDpad("dpadDown", "Down"),
            ["Left"] = GetDpad("dpadLeft", "Left"),

            // Face buttons: physical button if detected, fallback to axis/function
            ["Cross"] = btnA.HasValue ? $"{dev}/Button{btnA}" :
                        gasIdx.HasValue ? $"{dev}/+Axis{gasIdx}" : "",
            ["Circle"] = btnB.HasValue ? $"{dev}/Button{btnB}" :
                         startBtn.HasValue ? $"{dev}/Button{startBtn}" : "",
            ["Square"] = btnX.HasValue ? $"{dev}/Button{btnX}" :
                         brakeIdx.HasValue ? $"{dev}/+Axis{brakeIdx}" : "",
            ["Triangle"] = btnY.HasValue ? $"{dev}/Button{btnY}" :
                           gearUpBtn.HasValue ? $"{dev}/Button{gearUpBtn}" : "",

            // Start/Select
            ["Select"] = coinBtn.HasValue ? $"{dev}/Button{coinBtn}" : "",
            ["Start"] = startBtn.HasValue ? $"{dev}/Button{startBtn}" : "",

            // Shoulders — gear paddles
            ["L1"] = gearDownBtn.HasValue ? $"{dev}/Button{gearDownBtn}" : "",
            ["R1"] = gearUpBtn.HasValue ? $"{dev}/Button{gearUpBtn}" : "",

            // Triggers — ALWAYS gas/brake axes (analog for racing)
            ["L2"] = brakeIdx.HasValue ? $"{dev}/+Axis{brakeIdx}" : "",
            ["R2"] = gasIdx.HasValue ? $"{dev}/+Axis{gasIdx}" : "",

            // Stick buttons — not mapped on wheel
            ["L3"] = "",
            ["R3"] = "",
            ["Analog"] = "",

            // Left stick — steering
            ["LUp"] = "",
            ["LRight"] = steerIdx.HasValue ? $"{dev}/+Axis{steerIdx}" : "",
            ["LDown"] = "",
            ["LLeft"] = steerIdx.HasValue ? $"{dev}/-Axis{steerIdx}" : "",

            // Right stick — not used
            ["RUp"] = "",
            ["RRight"] = "",
            ["RDown"] = "",
            ["RLeft"] = "",

            // Force feedback
            ["LargeMotor"] = steerIdx.HasValue ? $"{dev}/FullAxis{steerIdx}" : "",
            ["SmallMotor"] = "",
        };

        return pad;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        var pad = GenerateBindings(config);
        var inputSources = new Dictionary<string, string>
        {
            ["Keyboard"] = "true",
            ["Mouse"] = "true",
            ["SDL"] = "false",
            ["DInput"] = "true",
            ["XInput"] = "false",
            ["SDLControllerEnhancedMode"] = "false",
            ["SDLPS5PlayerLED"] = "false",
        };

        string[] targets =
        [
            Path.Combine(launchboxRoot, @"Emulators\PCSX2\RunWizard=0\inis\PCSX2.ini"),
            Path.Combine(launchboxRoot, @"Emulators\PCSX2\RunWizard=0\inputprofiles\RacingWheel.ini"),
        ];

        foreach (var target in targets)
        {
            if (!File.Exists(target)) continue;
            IniEditor.BackupFile(target);
            IniEditor.UpdateSection(target, "Pad1", pad);
            IniEditor.UpdateSection(target, "InputSources", inputSources);
        }
    }
}
