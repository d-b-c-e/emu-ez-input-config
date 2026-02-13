namespace EmuEzInputConfig.ConfigWriters;

using System.Text;
using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;

/// <summary>
/// Writes RPCS3 input config YAML using the MMJoystick handler (DirectInput).
/// Device naming: "Joystick #{DInputIndex + 1}" (1-indexed).
/// Buttons are 1-indexed. Axis format: "+Axis X+" / "-Axis X-".
/// Target file: Emulators\rpcs3\config\input_configs\global\Default.yml
/// </summary>
public class Rpcs3ConfigWriter : IConfigWriter
{
    public string EmulatorName => "RPCS3";

    // DInput axis name → RPCS3 MMJoystick axis name
    private static readonly Dictionary<string, string> AxisNameMap = new()
    {
        ["X"] = "X", ["Y"] = "Y", ["Z"] = "Z",
        ["RotationX"] = "RX", ["RotationY"] = "RY", ["RotationZ"] = "RZ",
    };

    private static string? GetAxisBinding(InputMapping? m, bool positive)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        if (!AxisNameMap.TryGetValue(m.Axis, out string? name)) return null;
        return positive ? $"+Axis {name}+" : $"-Axis {name}-";
    }

    private static string? GetButtonBinding(InputMapping? m)
    {
        // RPCS3 MMJoystick uses 1-indexed buttons
        if (m?.Type != "button" || !m.ButtonIndex.HasValue) return null;
        return $"Button {m.ButtonIndex.Value + 1}";
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string yml = Path.Combine(launchboxRoot, @"Emulators\rpcs3\config\input_configs\global\Default.yml");
        return File.Exists(yml);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;

        string? steerPos = GetAxisBinding(m.GetValueOrDefault("steering"), positive: true);
        string? steerNeg = GetAxisBinding(m.GetValueOrDefault("steering"), positive: false);
        string? gasAxis = GetAxisBinding(m.GetValueOrDefault("gas"), positive: true);
        string? brakeAxis = GetAxisBinding(m.GetValueOrDefault("brake"), positive: true);

        string? gearUpBtn = GetButtonBinding(m.GetValueOrDefault("gearUp"));
        string? gearDownBtn = GetButtonBinding(m.GetValueOrDefault("gearDown"));
        string? startBtn = GetButtonBinding(m.GetValueOrDefault("start"));
        string? coinBtn = GetButtonBinding(m.GetValueOrDefault("coin"));

        string? btnA = GetButtonBinding(m.GetValueOrDefault("btnA"));
        string? btnB = GetButtonBinding(m.GetValueOrDefault("btnB"));
        string? btnX = GetButtonBinding(m.GetValueOrDefault("btnX"));
        string? btnY = GetButtonBinding(m.GetValueOrDefault("btnY"));

        // Face buttons take priority over axis fallback (same pattern as PCSX2)
        var bindings = new Dictionary<string, string>
        {
            ["Left Stick Left"] = steerNeg ?? "",
            ["Left Stick Right"] = steerPos ?? "",
            ["Left Stick Down"] = "",
            ["Left Stick Up"] = "",
            ["Right Stick Left"] = "",
            ["Right Stick Right"] = "",
            ["Right Stick Down"] = "",
            ["Right Stick Up"] = "",
            ["Start"] = startBtn ?? "",
            ["Select"] = coinBtn ?? "",
            ["PS Button"] = "",
            ["Square"] = btnX ?? brakeAxis ?? "",
            ["Cross"] = btnA ?? gasAxis ?? "",
            ["Circle"] = btnB ?? startBtn ?? "",
            ["Triangle"] = btnY ?? gearUpBtn ?? "",
            ["Left"] = "",
            ["Down"] = "",
            ["Right"] = "",
            ["Up"] = "",
            ["R1"] = gearUpBtn ?? "",
            ["R2"] = gasAxis ?? "",
            ["R3"] = "",
            ["L1"] = gearDownBtn ?? "",
            ["L2"] = brakeAxis ?? "",
            ["L3"] = "",
        };

        return bindings;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        string ymlPath = Path.Combine(launchboxRoot,
            @"Emulators\rpcs3\config\input_configs\global\Default.yml");
        if (!File.Exists(ymlPath)) return;

        IniEditor.BackupFile(ymlPath);

        int joystickNum = (config.Wheel?.DInputIndex ?? 1) + 1;
        var bindings = GenerateBindings(config);
        string yaml = BuildYaml(joystickNum, bindings);
        File.WriteAllText(ymlPath, yaml);
    }

    private static string BuildYaml(int joystickNum, Dictionary<string, string> bindings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Player 1 Input:");
        sb.AppendLine("  Handler: MMJoystick");
        sb.AppendLine($"  Device: \"Joystick #{joystickNum}\"");
        sb.AppendLine("  Config:");

        // Button/axis bindings
        foreach (var (key, value) in bindings)
        {
            sb.AppendLine($"    {key}: \"{value}\"");
        }

        // Default config options (matching existing RPCS3 racing defaults)
        sb.AppendLine("  Buddy Device: \"\"");
        sb.AppendLine("  Pressure Intensity Button: \"\"");
        sb.AppendLine("  Pressure Intensity Percent: 50");
        sb.AppendLine("  Pressure Intensity Toggle Mode: false");
        sb.AppendLine("  Pressure Intensity Deadzone: 0");
        sb.AppendLine("  Left Stick Deadzone: 0");
        sb.AppendLine("  Right Stick Deadzone: 0");
        sb.AppendLine("  Left Trigger Threshold: 0");
        sb.AppendLine("  Right Trigger Threshold: 0");
        sb.AppendLine("  Left Pad Squircle Factor: 0");
        sb.AppendLine("  Right Pad Squircle Factor: 0");
        sb.AppendLine("  Color Value R: 0");
        sb.AppendLine("  Color Value G: 0");
        sb.AppendLine("  Color Value B: 0");
        sb.AppendLine("  Enable Large Vibration Motor: true");
        sb.AppendLine("  Enable Small Vibration Motor: false");
        sb.AppendLine("  Switch Vibration Motors: false");
        sb.AppendLine("  Mouse Movement Mode: Relative");
        sb.AppendLine("  Mouse Deadzone X: 60");
        sb.AppendLine("  Mouse Deadzone Y: 60");
        sb.AppendLine("  Mouse Acceleration X: 200");
        sb.AppendLine("  Mouse Acceleration Y: 250");
        sb.AppendLine("  Left Stick Lerp Factor: 100");
        sb.AppendLine("  Right Stick Lerp Factor: 100");

        return sb.ToString();
    }
}
