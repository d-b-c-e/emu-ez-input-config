namespace EmuEzInputConfig.ConfigWriters;

using EmuEzInputConfig.Models;
using EmuEzInputConfig.Util;

/// <summary>
/// Writes Supermodel (Sega Model 3) input bindings.
/// Target file: Emulators\Sega Model 3\Config\Supermodel.ini
/// </summary>
public class SupermodelConfigWriter : IConfigWriter
{
    public string EmulatorName => "Supermodel";

    // DInput axis name → Supermodel axis name (DInput mode)
    private static readonly Dictionary<string, string> AxisNameMap = new()
    {
        ["X"] = "XAXIS", ["Y"] = "YAXIS", ["Z"] = "ZAXIS",
        ["RotationX"] = "RXAXIS", ["RotationY"] = "RYAXIS", ["RotationZ"] = "RZAXIS",
        ["Slider0"] = "SLIDER1", ["Slider1"] = "SLIDER2",
    };

    private static string? GetAxisName(InputMapping? m)
    {
        if (m?.Type != "axis" || m.Axis == null) return null;
        return AxisNameMap.GetValueOrDefault(m.Axis);
    }

    private static int? GetButtonNumber(InputMapping? m)
    {
        // Supermodel buttons are 1-indexed in DInput mode
        if (m?.Type != "button") return null;
        return (m.ButtonIndex ?? -1) + 1;
    }

    public bool ConfigExists(string launchboxRoot)
    {
        string ini = Path.Combine(launchboxRoot, @"Emulators\Sega Model 3\Config\Supermodel.ini");
        return File.Exists(ini);
    }

    public Dictionary<string, string> GenerateBindings(InputConfig config)
    {
        var m = config.Mappings;
        // DevReorder makes wheel DInput-1, so Supermodel JOY number = DInputIndex + 1
        int joyNum = (config.Wheel?.DInputIndex ?? 1) + 1;
        string joy = $"JOY{joyNum}";

        string? steerAxis = GetAxisName(m.GetValueOrDefault("steering"));
        string? gasAxis = GetAxisName(m.GetValueOrDefault("gas"));
        string? brakeAxis = GetAxisName(m.GetValueOrDefault("brake"));

        int? gearUpBtn = GetButtonNumber(m.GetValueOrDefault("gearUp"));
        int? gearDownBtn = GetButtonNumber(m.GetValueOrDefault("gearDown"));
        int? startBtn = GetButtonNumber(m.GetValueOrDefault("start"));
        int? coinBtn = GetButtonNumber(m.GetValueOrDefault("coin"));

        var bindings = new Dictionary<string, string>();

        if (steerAxis != null)
        {
            bindings["InputSteering"] = $"{joy}_{steerAxis}";
            bindings["InputSteeringLeft"] = $"{joy}_{steerAxis}_NEG";
            bindings["InputSteeringRight"] = $"{joy}_{steerAxis}_POS";
        }
        if (gasAxis != null)
            bindings["InputAccelerator"] = $"{joy}_{gasAxis}_POS";
        if (brakeAxis != null)
            bindings["InputBrake"] = $"{joy}_{brakeAxis}_POS";

        // Handbrake
        var hb = m.GetValueOrDefault("handbrake");
        if (hb?.Type == "axis")
        {
            string? hbAxis = GetAxisName(hb);
            if (hbAxis != null) bindings["InputHandBrake"] = $"{joy}_{hbAxis}_POS";
        }
        else if (hb?.Type == "button")
        {
            int? hbBtn = GetButtonNumber(hb);
            if (hbBtn.HasValue) bindings["InputHandBrake"] = $"{joy}_BUTTON{hbBtn}";
        }

        if (gearUpBtn.HasValue)
            bindings["InputGearShiftUp"] = $"{joy}_BUTTON{gearUpBtn}";
        if (gearDownBtn.HasValue)
            bindings["InputGearShiftDown"] = $"{joy}_BUTTON{gearDownBtn}";
        if (startBtn.HasValue)
            bindings["InputStart1"] = $"{joy}_BUTTON{startBtn},KEY_1";
        if (coinBtn.HasValue)
            bindings["InputCoin1"] = $"{joy}_BUTTON{coinBtn},KEY_5";

        return bindings;
    }

    public void WriteConfig(string launchboxRoot, InputConfig config)
    {
        string iniPath = Path.Combine(launchboxRoot, @"Emulators\Sega Model 3\Config\Supermodel.ini");
        if (!File.Exists(iniPath)) return;

        var bindings = GenerateBindings(config);
        IniEditor.BackupFile(iniPath);
        IniEditor.UpdateValues(iniPath, bindings);
    }
}
